using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace TripleG3.Windows.Shell;

internal sealed class NativeModule
{
    private const int InitialPathBufferLength = 260;
    private const int MaximumPathBufferLength = 32768;
    private const uint PortableExecutableSignature = 0x00004550;
    private const ushort PortableExecutable32Magic = 0x10b;
    private const ushort PortableExecutable64Magic = 0x20b;
    private const int ExportDataDirectoryOffset32 = 96;
    private const int ExportDataDirectoryOffset64 = 112;
    private const int ImageSectionHeaderSize = 40;

    private readonly string _libraryName;
    private readonly Type _ownerType;
    private readonly Lazy<nint> _moduleHandle;
    private readonly Lazy<string> _modulePath;
    private readonly Lazy<IReadOnlyList<NativeExport>> _exports;
    private readonly Lazy<IReadOnlyList<string>> _exportNames;
    private readonly ConcurrentDictionary<string, nint> _namedExportCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<int, nint> _ordinalExportCache = new();

    public NativeModule(string libraryName, Type ownerType)
    {
        _libraryName = libraryName;
        _ownerType = ownerType;
        _moduleHandle = new Lazy<nint>(LoadModule, LazyThreadSafetyMode.ExecutionAndPublication);
        _modulePath = new Lazy<string>(GetModulePathCore, LazyThreadSafetyMode.ExecutionAndPublication);
        _exports = new Lazy<IReadOnlyList<NativeExport>>(EnumerateExports, LazyThreadSafetyMode.ExecutionAndPublication);
        _exportNames = new Lazy<IReadOnlyList<string>>(GetExportNamesCore, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public nint ModuleHandle => _moduleHandle.Value;

    public string ModulePath => _modulePath.Value;

    public IReadOnlyList<NativeExport> Exports => _exports.Value;

    public IReadOnlyList<string> ExportNames => _exportNames.Value;

    public bool TryGetExport(string name, out nint address)
    {
        ThrowIfInvalidExportName(name);

        if (_namedExportCache.TryGetValue(name, out address))
        {
            return true;
        }

        address = NativeMethods.GetProcAddress(ModuleHandle, name);
        if (address == 0)
        {
            return false;
        }

        _namedExportCache.TryAdd(name, address);
        return true;
    }

    public nint GetExport(string name)
    {
        if (TryGetExport(name, out var address))
        {
            return address;
        }

        throw CreateEntryPointNotFoundException(name, Marshal.GetLastWin32Error());
    }

    public bool TryGetExport(int ordinal, out nint address)
    {
        ThrowIfInvalidOrdinal(ordinal);

        if (_ordinalExportCache.TryGetValue(ordinal, out address))
        {
            return true;
        }

        address = NativeMethods.GetProcAddress(ModuleHandle, (nint)ordinal);
        if (address == 0)
        {
            return false;
        }

        _ordinalExportCache.TryAdd(ordinal, address);
        return true;
    }

    public nint GetExport(int ordinal)
    {
        if (TryGetExport(ordinal, out var address))
        {
            return address;
        }

        throw CreateEntryPointNotFoundException($"#{ordinal}", Marshal.GetLastWin32Error());
    }

    public TDelegate GetFunction<TDelegate>(string name)
        where TDelegate : Delegate
    {
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(GetExport(name));
    }

    public bool TryGetFunction<TDelegate>(string name, [NotNullWhen(true)] out TDelegate? function)
        where TDelegate : Delegate
    {
        if (TryGetExport(name, out var address))
        {
            function = Marshal.GetDelegateForFunctionPointer<TDelegate>(address);
            return true;
        }

        function = null;
        return false;
    }

    public TDelegate GetFunction<TDelegate>(int ordinal)
        where TDelegate : Delegate
    {
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(GetExport(ordinal));
    }

    public bool TryGetFunction<TDelegate>(int ordinal, [NotNullWhen(true)] out TDelegate? function)
        where TDelegate : Delegate
    {
        if (TryGetExport(ordinal, out var address))
        {
            function = Marshal.GetDelegateForFunctionPointer<TDelegate>(address);
            return true;
        }

        function = null;
        return false;
    }

    private nint LoadModule()
    {
        if (NativeLibrary.TryLoad(_libraryName, _ownerType.Assembly, DllImportSearchPath.System32, out var handle))
        {
            return handle;
        }

        throw new DllNotFoundException($"Unable to load {_libraryName} from the Windows system directory.");
    }

    private string GetModulePathCore()
    {
        for (var bufferLength = InitialPathBufferLength; bufferLength <= MaximumPathBufferLength; bufferLength *= 2)
        {
            var buffer = new char[bufferLength];
            var length = NativeMethods.GetModuleFileName(ModuleHandle, buffer, buffer.Length);
            if (length == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Unable to determine the loaded path for {_libraryName}.");
            }

            if (length < buffer.Length)
            {
                return new string(buffer, 0, length);
            }
        }

        throw new PathTooLongException($"The loaded path for {_libraryName} exceeded {MaximumPathBufferLength} characters.");
    }

    private ReadOnlyCollection<string> GetExportNamesCore()
    {
        return new ReadOnlyCollection<string>([.. Exports
            .Where(export => export.Name is not null)
            .Select(export => export.Name!)
            .Order(StringComparer.Ordinal)]);
    }

    private IReadOnlyList<NativeExport> EnumerateExports()
    {
        using var stream = File.OpenRead(ModulePath);
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: false);

        if (stream.Length < 64)
        {
            throw new InvalidDataException($"{ModulePath} is too small to be a valid portable executable file.");
        }

        stream.Position = 0x3C;
        var peHeaderOffset = reader.ReadInt32();
        if (peHeaderOffset <= 0 || peHeaderOffset > stream.Length - sizeof(uint))
        {
            throw new InvalidDataException($"{ModulePath} contains an invalid portable executable header offset.");
        }

        stream.Position = peHeaderOffset;
        var signature = reader.ReadUInt32();
        if (signature != PortableExecutableSignature)
        {
            throw new InvalidDataException($"{ModulePath} is not a valid portable executable file.");
        }

        stream.Position += sizeof(ushort);
        var sectionCount = reader.ReadUInt16();
        stream.Position += 12;
        var optionalHeaderSize = reader.ReadUInt16();
        stream.Position += sizeof(ushort);

        var optionalHeaderOffset = stream.Position;
        var optionalHeaderMagic = reader.ReadUInt16();
        var exportDirectoryOffset = optionalHeaderMagic switch
        {
            PortableExecutable32Magic => ExportDataDirectoryOffset32,
            PortableExecutable64Magic => ExportDataDirectoryOffset64,
            _ => throw new InvalidDataException($"{ModulePath} contains an unsupported portable executable optional header.")
        };

        if (optionalHeaderSize < exportDirectoryOffset + (2 * sizeof(uint)))
        {
            throw new InvalidDataException($"{ModulePath} contains an incomplete portable executable optional header.");
        }

        stream.Position = optionalHeaderOffset + exportDirectoryOffset;
        var exportTableRva = reader.ReadUInt32();
        var exportTableSize = reader.ReadUInt32();
        if (exportTableRva == 0 || exportTableSize == 0)
        {
            return Array.Empty<NativeExport>();
        }

        var sectionHeaderOffset = optionalHeaderOffset + optionalHeaderSize;
        var sections = ReadSectionHeaders(reader, sectionHeaderOffset, sectionCount);
        var exportTableOffset = RvaToFileOffset(exportTableRva, sections);

        stream.Position = exportTableOffset + 16;
        var ordinalBase = reader.ReadUInt32();
        var numberOfFunctions = reader.ReadUInt32();
        var numberOfNames = reader.ReadUInt32();
        var addressOfFunctions = reader.ReadUInt32();
        var addressOfNames = reader.ReadUInt32();
        var addressOfNameOrdinals = reader.ReadUInt32();

        if (numberOfFunctions > int.MaxValue || numberOfNames > int.MaxValue)
        {
            throw new InvalidDataException($"{ModulePath} contains more exports than this wrapper can represent.");
        }

        var namesByOrdinalIndex = new Dictionary<uint, string>((int)numberOfNames);
        for (uint nameIndex = 0; nameIndex < numberOfNames; nameIndex++)
        {
            var nameRva = ReadUInt32At(reader, RvaToFileOffset(AddRvaOffset(addressOfNames, nameIndex * sizeof(uint)), sections));
            var ordinalIndex = ReadUInt16At(reader, RvaToFileOffset(AddRvaOffset(addressOfNameOrdinals, nameIndex * sizeof(ushort)), sections));
            namesByOrdinalIndex[ordinalIndex] = ReadNullTerminatedAscii(reader, RvaToFileOffset(nameRva, sections));
        }

        var exports = new List<NativeExport>((int)numberOfFunctions);
        for (uint functionIndex = 0; functionIndex < numberOfFunctions; functionIndex++)
        {
            var functionRva = ReadUInt32At(reader, RvaToFileOffset(AddRvaOffset(addressOfFunctions, functionIndex * sizeof(uint)), sections));
            if (functionRva == 0)
            {
                continue;
            }

            namesByOrdinalIndex.TryGetValue(functionIndex, out var name);
            var ordinal = checked((int)(ordinalBase + functionIndex));
            var forwardedTo = IsRvaInside(functionRva, exportTableRva, exportTableSize)
                ? ReadNullTerminatedAscii(reader, RvaToFileOffset(functionRva, sections))
                : null;

            exports.Add(new NativeExport(name, ordinal, functionRva, forwardedTo));
        }

        return new ReadOnlyCollection<NativeExport>([.. exports
            .OrderBy(export => export.Ordinal)
            .ThenBy(export => export.Name, StringComparer.Ordinal)]);
    }

    private List<SectionHeader> ReadSectionHeaders(BinaryReader reader, long sectionHeaderOffset, int sectionCount)
    {
        var stream = reader.BaseStream;
        var sections = new List<SectionHeader>(sectionCount);
        stream.Position = sectionHeaderOffset;

        for (var sectionIndex = 0; sectionIndex < sectionCount; sectionIndex++)
        {
            EnsureCanRead(stream, ImageSectionHeaderSize);
            stream.Position += 8;
            var virtualSize = reader.ReadUInt32();
            var virtualAddress = reader.ReadUInt32();
            var sizeOfRawData = reader.ReadUInt32();
            var pointerToRawData = reader.ReadUInt32();
            stream.Position += 16;
            sections.Add(new SectionHeader(virtualAddress, virtualSize, sizeOfRawData, pointerToRawData));
        }

        return sections;
    }

    private uint AddRvaOffset(uint rva, ulong offset)
    {
        var result = rva + offset;
        if (result > uint.MaxValue)
        {
            throw new InvalidDataException($"{ModulePath} contains an invalid relative virtual address.");
        }

        return (uint)result;
    }

    private long RvaToFileOffset(uint rva, IReadOnlyList<SectionHeader> sections)
    {
        foreach (var section in sections)
        {
            var sectionSize = Math.Max(section.VirtualSize, section.SizeOfRawData);
            var sectionStart = section.VirtualAddress;
            var sectionEnd = (ulong)sectionStart + sectionSize;
            if (rva >= sectionStart && rva < sectionEnd)
            {
                return checked((long)(section.PointerToRawData + (rva - sectionStart)));
            }
        }

        throw new InvalidDataException($"{ModulePath} contains an export RVA that does not map to a file section.");
    }

    private void EnsureCanRead(Stream stream, int byteCount)
    {
        EnsureCanReadAt(stream, stream.Position, byteCount);
    }

    private void EnsureCanReadAt(Stream stream, long fileOffset, int byteCount)
    {
        if (fileOffset < 0 || byteCount < 0 || fileOffset > stream.Length - byteCount)
        {
            throw new InvalidDataException($"{ModulePath} ended before the expected export metadata could be read.");
        }
    }

    private uint ReadUInt32At(BinaryReader reader, long fileOffset)
    {
        EnsureCanReadAt(reader.BaseStream, fileOffset, sizeof(uint));
        var previousPosition = reader.BaseStream.Position;
        try
        {
            reader.BaseStream.Position = fileOffset;
            return reader.ReadUInt32();
        }
        finally
        {
            reader.BaseStream.Position = previousPosition;
        }
    }

    private ushort ReadUInt16At(BinaryReader reader, long fileOffset)
    {
        EnsureCanReadAt(reader.BaseStream, fileOffset, sizeof(ushort));
        var previousPosition = reader.BaseStream.Position;
        try
        {
            reader.BaseStream.Position = fileOffset;
            return reader.ReadUInt16();
        }
        finally
        {
            reader.BaseStream.Position = previousPosition;
        }
    }

    private string ReadNullTerminatedAscii(BinaryReader reader, long fileOffset)
    {
        EnsureCanReadAt(reader.BaseStream, fileOffset, 1);
        var previousPosition = reader.BaseStream.Position;
        try
        {
            reader.BaseStream.Position = fileOffset;
            var bytes = new List<byte>();
            while (true)
            {
                var value = reader.BaseStream.ReadByte();
                if (value < 0)
                {
                    throw new InvalidDataException($"{ModulePath} contains an unterminated export string.");
                }

                if (value == 0)
                {
                    return Encoding.ASCII.GetString([.. bytes]);
                }

                bytes.Add((byte)value);
            }
        }
        finally
        {
            reader.BaseStream.Position = previousPosition;
        }
    }

    private static bool IsRvaInside(uint rva, uint startRva, uint size)
    {
        return rva >= startRva && rva < (ulong)startRva + size;
    }

    private static void ThrowIfInvalidExportName([NotNull] string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An export name is required.", nameof(name));
        }
    }

    private static void ThrowIfInvalidOrdinal(int ordinal)
    {
        if (ordinal <= 0 || ordinal > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "Export ordinals must be between 1 and 65535.");
        }
    }

    private EntryPointNotFoundException CreateEntryPointNotFoundException(string export, int errorCode)
    {
        var reason = errorCode == 0
            ? "No additional Win32 error information was provided."
            : $"Win32 error {errorCode}: {new Win32Exception(errorCode).Message}";

        return new EntryPointNotFoundException($"{_libraryName} does not export '{export}'. {reason}");
    }

    private readonly record struct SectionHeader(uint VirtualAddress, uint VirtualSize, uint SizeOfRawData, uint PointerToRawData);

    private static partial class NativeMethods
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern nint GetProcAddress(nint hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true, SetLastError = true)]
        internal static extern nint GetProcAddress(nint hModule, nint procName);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("kernel32.dll", EntryPoint = "GetModuleFileNameW", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int GetModuleFileName(nint hModule, [Out] char[] fileName, int size);
    }
}

internal sealed record NativeExport(string? Name, int Ordinal, uint RelativeVirtualAddress, string? ForwardedTo);
