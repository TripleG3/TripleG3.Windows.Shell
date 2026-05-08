using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace TripleG3.Windows.Shell;

/// <summary>
/// Provides a version-tolerant wrapper over the native Windows <c>user32.dll</c> module.
/// </summary>
/// <remarks>
/// The set of exported <c>user32.dll</c> functions can differ between Windows versions, and several exports are
/// undocumented. This wrapper therefore discovers the exports available on the current machine at runtime and lets
/// callers bind the exact delegate signature they need with <see cref="GetFunction{TDelegate}(string)" /> or
/// <c>TryGetFunction&lt;TDelegate&gt;</c>.
/// </remarks>
public static class User32
{
	/// <summary>The canonical Windows module name for User32.</summary>
	public const string LibraryName = "user32.dll";

	private const int InitialPathBufferLength = 260;
	private const int MaximumPathBufferLength = 32768;
	private const uint PortableExecutableSignature = 0x00004550;
	private const ushort PortableExecutable32Magic = 0x10b;
	private const ushort PortableExecutable64Magic = 0x20b;
	private const int ExportDataDirectoryOffset32 = 96;
	private const int ExportDataDirectoryOffset64 = 112;
	private const int ImageSectionHeaderSize = 40;

	private static readonly Lazy<nint> s_moduleHandle = new(LoadUser32, LazyThreadSafetyMode.ExecutionAndPublication);
	private static readonly Lazy<string> s_modulePath = new(GetUser32ModulePath, LazyThreadSafetyMode.ExecutionAndPublication);
	private static readonly Lazy<IReadOnlyList<Export>> s_exports = new(EnumerateExports, LazyThreadSafetyMode.ExecutionAndPublication);
	private static readonly Lazy<IReadOnlyList<string>> s_exportNames = new(GetExportNames, LazyThreadSafetyMode.ExecutionAndPublication);
	private static readonly ConcurrentDictionary<string, nint> s_namedExportCache = new(StringComparer.Ordinal);
	private static readonly ConcurrentDictionary<int, nint> s_ordinalExportCache = new();

	/// <summary>Gets the loaded native module handle for <c>user32.dll</c>.</summary>
	/// <exception cref="DllNotFoundException">Thrown when Windows cannot load <c>user32.dll</c>.</exception>
	public static nint ModuleHandle => s_moduleHandle.Value;

	/// <summary>Gets the full path to the loaded <c>user32.dll</c> module.</summary>
	public static string ModulePath => s_modulePath.Value;

	/// <summary>Gets metadata for every exported <c>user32.dll</c> function available on the current machine.</summary>
	public static IReadOnlyList<Export> Exports => s_exports.Value;

	/// <summary>Gets every named <c>user32.dll</c> export available on the current machine.</summary>
	public static IReadOnlyList<string> ExportNames => s_exportNames.Value;

	/// <summary>Attempts to resolve a named <c>user32.dll</c> export to its native function pointer.</summary>
	/// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
	/// <param name="address">When this method returns, contains the native function pointer if found.</param>
	/// <returns><see langword="true" /> when the export exists; otherwise, <see langword="false" />.</returns>
	public static bool TryGetExport(string name, out nint address)
	{
		ThrowIfInvalidExportName(name);

		if (s_namedExportCache.TryGetValue(name, out address))
		{
			return true;
		}

		address = NativeMethods.GetProcAddress(ModuleHandle, name);
		if (address == 0)
		{
			return false;
		}

		s_namedExportCache.TryAdd(name, address);
		return true;
	}

	/// <summary>Resolves a named <c>user32.dll</c> export to its native function pointer.</summary>
	/// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
	/// <returns>The native function pointer for the requested export.</returns>
	/// <exception cref="EntryPointNotFoundException">Thrown when the export does not exist in the loaded <c>user32.dll</c>.</exception>
	public static nint GetExport(string name)
	{
		if (TryGetExport(name, out var address))
		{
			return address;
		}

		throw CreateEntryPointNotFoundException(name, Marshal.GetLastWin32Error());
	}

	/// <summary>Attempts to resolve a <c>user32.dll</c> export by ordinal to its native function pointer.</summary>
	/// <param name="ordinal">The 16-bit export ordinal.</param>
	/// <param name="address">When this method returns, contains the native function pointer if found.</param>
	/// <returns><see langword="true" /> when the ordinal exists; otherwise, <see langword="false" />.</returns>
	public static bool TryGetExport(int ordinal, out nint address)
	{
		ThrowIfInvalidOrdinal(ordinal);

		if (s_ordinalExportCache.TryGetValue(ordinal, out address))
		{
			return true;
		}

		address = NativeMethods.GetProcAddress(ModuleHandle, (nint)ordinal);
		if (address == 0)
		{
			return false;
		}

		s_ordinalExportCache.TryAdd(ordinal, address);
		return true;
	}

	/// <summary>Resolves a <c>user32.dll</c> export by ordinal to its native function pointer.</summary>
	/// <param name="ordinal">The 16-bit export ordinal.</param>
	/// <returns>The native function pointer for the requested export.</returns>
	/// <exception cref="EntryPointNotFoundException">Thrown when the ordinal does not exist in the loaded <c>user32.dll</c>.</exception>
	public static nint GetExport(int ordinal)
	{
		if (TryGetExport(ordinal, out var address))
		{
			return address;
		}

		throw CreateEntryPointNotFoundException($"#{ordinal}", Marshal.GetLastWin32Error());
	}

    /// <summary>Resolves a named <c>user32.dll</c> export and converts it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
    /// <returns>A delegate bound to the native function pointer.</returns>
    /// <remarks>
    /// Delegate types should normally be decorated with <see cref="UnmanagedFunctionPointerAttribute" /> using
    /// <see cref="CallingConvention.Winapi" /> and the correct charset and <c>SetLastError</c> value for the target API.
    /// </remarks>
    public static TDelegate GetFunction<TDelegate>(string name)
        where TDelegate : Delegate
    {
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(GetExport(name));
    }

    /// <summary>Attempts to resolve a named <c>user32.dll</c> export and convert it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
    /// <param name="function">When this method returns, contains the bound delegate if the export exists.</param>
    /// <returns><see langword="true" /> when the export exists; otherwise, <see langword="false" />.</returns>
    public static bool TryGetFunction<TDelegate>(string name, [NotNullWhen(true)] out TDelegate? function)
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

    /// <summary>Resolves a <c>user32.dll</c> export by ordinal and converts it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="ordinal">The 16-bit export ordinal.</param>
    /// <returns>A delegate bound to the native function pointer.</returns>
    public static TDelegate GetFunction<TDelegate>(int ordinal)
        where TDelegate : Delegate
    {
        return Marshal.GetDelegateForFunctionPointer<TDelegate>(GetExport(ordinal));
    }

    /// <summary>Attempts to resolve a <c>user32.dll</c> export by ordinal and convert it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="ordinal">The 16-bit export ordinal.</param>
    /// <param name="function">When this method returns, contains the bound delegate if the ordinal exists.</param>
    /// <returns><see langword="true" /> when the ordinal exists; otherwise, <see langword="false" />.</returns>
    public static bool TryGetFunction<TDelegate>(int ordinal, [NotNullWhen(true)] out TDelegate? function)
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

	private static nint LoadUser32()
	{
		if (NativeLibrary.TryLoad(LibraryName, typeof(User32).Assembly, DllImportSearchPath.System32, out var handle))
		{
			return handle;
		}

		throw new DllNotFoundException($"Unable to load {LibraryName} from the Windows system directory.");
	}

	private static string GetUser32ModulePath()
	{
		for (var bufferLength = InitialPathBufferLength; bufferLength <= MaximumPathBufferLength; bufferLength *= 2)
		{
			var buffer = new char[bufferLength];
			var length = NativeMethods.GetModuleFileName(ModuleHandle, buffer, buffer.Length);
			if (length == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), $"Unable to determine the loaded path for {LibraryName}.");
			}

			if (length < buffer.Length)
			{
				return new string(buffer, 0, length);
			}
		}

		throw new PathTooLongException($"The loaded path for {LibraryName} exceeded {MaximumPathBufferLength} characters.");
	}

    private static ReadOnlyCollection<string> GetExportNames()
    {
        return new ReadOnlyCollection<string>([.. Exports
            .Where(export => export.Name is not null)
            .Select(export => export.Name!)
            .Order(StringComparer.Ordinal)]);
    }

    private static IReadOnlyList<Export> EnumerateExports()
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
			return Array.Empty<Export>();
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

		var exports = new List<Export>((int)numberOfFunctions);
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

			exports.Add(new Export(name, ordinal, functionRva, forwardedTo));
		}

		return new ReadOnlyCollection<Export>([.. exports
			.OrderBy(export => export.Ordinal)
			.ThenBy(export => export.Name, StringComparer.Ordinal)]);
	}

	private static List<SectionHeader> ReadSectionHeaders(BinaryReader reader, long sectionHeaderOffset, int sectionCount)
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

	private static uint AddRvaOffset(uint rva, ulong offset)
	{
		var result = rva + offset;
		if (result > uint.MaxValue)
		{
			throw new InvalidDataException($"{ModulePath} contains an invalid relative virtual address.");
		}

		return (uint)result;
	}

	private static long RvaToFileOffset(uint rva, IReadOnlyList<SectionHeader> sections)
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

	private static uint ReadUInt32At(BinaryReader reader, long fileOffset)
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

	private static ushort ReadUInt16At(BinaryReader reader, long fileOffset)
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

	private static string ReadNullTerminatedAscii(BinaryReader reader, long fileOffset)
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

    private static void EnsureCanRead(Stream stream, int byteCount)
    {
        EnsureCanReadAt(stream, stream.Position, byteCount);
    }

    private static void EnsureCanReadAt(Stream stream, long fileOffset, int byteCount)
	{
		if (fileOffset < 0 || byteCount < 0 || fileOffset > stream.Length - byteCount)
		{
			throw new InvalidDataException($"{ModulePath} ended before the expected export metadata could be read.");
		}
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

	private static EntryPointNotFoundException CreateEntryPointNotFoundException(string export, int errorCode)
	{
		var reason = errorCode == 0
			? "No additional Win32 error information was provided."
			: $"Win32 error {errorCode}: {new Win32Exception(errorCode).Message}";

		return new EntryPointNotFoundException($"{LibraryName} does not export '{export}'. {reason}");
	}

	/// <summary>Describes one native export from the loaded <c>user32.dll</c> module.</summary>
	/// <param name="Name">The export name, or <see langword="null" /> when the function is exported by ordinal only.</param>
	/// <param name="Ordinal">The export ordinal.</param>
	/// <param name="RelativeVirtualAddress">The export relative virtual address from the portable executable export table.</param>
	/// <param name="ForwardedTo">The forwarded export target, or <see langword="null" /> when the export is implemented in <c>user32.dll</c>.</param>
	public sealed record Export(string? Name, int Ordinal, uint RelativeVirtualAddress, string? ForwardedTo)
	{
		/// <summary>Gets a stable display value for the export name or ordinal.</summary>
		public string NameOrOrdinal => Name ?? $"#{Ordinal}";

		/// <summary>Gets a value indicating whether this export has a name.</summary>
		public bool IsNamed => Name is not null;

		/// <summary>Gets a value indicating whether this export forwards to another native module.</summary>
		public bool IsForwarded => ForwardedTo is not null;
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
