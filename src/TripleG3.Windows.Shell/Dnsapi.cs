using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell;

/// <summary>
/// Provides a version-tolerant wrapper over the native Windows <c>Dnsapi.dll</c> module for DNS query APIs.
/// </summary>
/// <remarks>
/// The set of exported <c>Dnsapi.dll</c> functions can differ between Windows versions and installed Windows features.
/// This wrapper discovers the exports available on the current machine at runtime and lets callers bind the exact
/// delegate signature they need with <see cref="GetFunction{TDelegate}(string)" /> or
/// <see cref="TryGetFunction{TDelegate}(string, out TDelegate?)" />. Prefer higher-level .NET networking APIs for
/// ordinary application code; use this type when code needs direct access to Windows DNS lookup exports.
/// </remarks>
public static class Dnsapi
{
    /// <summary>The canonical Windows module name for Dnsapi.</summary>
    public const string LibraryName = "Dnsapi.dll";

    private static readonly NativeModule s_module = new(LibraryName, typeof(Dnsapi));
    private static readonly Lazy<IReadOnlyList<Export>> s_exports = new(GetExports, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Gets the loaded native module handle for <c>Dnsapi.dll</c>.</summary>
    /// <exception cref="DllNotFoundException">Thrown when Windows cannot load <c>Dnsapi.dll</c>.</exception>
    public static nint ModuleHandle => s_module.ModuleHandle;

    /// <summary>Gets the full path to the loaded <c>Dnsapi.dll</c> module.</summary>
    public static string ModulePath => s_module.ModulePath;

    /// <summary>Gets metadata for every exported <c>Dnsapi.dll</c> function available on the current machine.</summary>
    public static IReadOnlyList<Export> Exports => s_exports.Value;

    /// <summary>Gets every named <c>Dnsapi.dll</c> export available on the current machine.</summary>
    public static IReadOnlyList<string> ExportNames => s_module.ExportNames;

    /// <summary>Attempts to resolve a named <c>Dnsapi.dll</c> export to its native function pointer.</summary>
    /// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
    /// <param name="address">When this method returns, contains the native function pointer if found.</param>
    /// <returns><see langword="true" /> when the export exists; otherwise, <see langword="false" />.</returns>
    public static bool TryGetExport(string name, out nint address)
    {
        return s_module.TryGetExport(name, out address);
    }

    /// <summary>Resolves a named <c>Dnsapi.dll</c> export to its native function pointer.</summary>
    /// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
    /// <returns>The native function pointer for the requested export.</returns>
    /// <exception cref="EntryPointNotFoundException">Thrown when the export does not exist in the loaded <c>Dnsapi.dll</c>.</exception>
    public static nint GetExport(string name)
    {
        return s_module.GetExport(name);
    }

    /// <summary>Attempts to resolve a <c>Dnsapi.dll</c> export by ordinal to its native function pointer.</summary>
    /// <param name="ordinal">The 16-bit export ordinal.</param>
    /// <param name="address">When this method returns, contains the native function pointer if found.</param>
    /// <returns><see langword="true" /> when the ordinal exists; otherwise, <see langword="false" />.</returns>
    public static bool TryGetExport(int ordinal, out nint address)
    {
        return s_module.TryGetExport(ordinal, out address);
    }

    /// <summary>Resolves a <c>Dnsapi.dll</c> export by ordinal to its native function pointer.</summary>
    /// <param name="ordinal">The 16-bit export ordinal.</param>
    /// <returns>The native function pointer for the requested export.</returns>
    /// <exception cref="EntryPointNotFoundException">Thrown when the ordinal does not exist in the loaded <c>Dnsapi.dll</c>.</exception>
    public static nint GetExport(int ordinal)
    {
        return s_module.GetExport(ordinal);
    }

    /// <summary>Resolves a named <c>Dnsapi.dll</c> export and converts it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
    /// <returns>A delegate bound to the native function pointer.</returns>
    /// <remarks>
    /// Delegate types should normally be decorated with <see cref="UnmanagedFunctionPointerAttribute" /> using
    /// <see cref="CallingConvention.Winapi" /> and the correct charset and <c>SetLastError</c> value for the target API.
    /// For example, bind <c>DnsQuery_W</c> only to a delegate that matches the Windows SDK signature exactly.
    /// </remarks>
    public static TDelegate GetFunction<TDelegate>(string name)
        where TDelegate : Delegate
    {
        return s_module.GetFunction<TDelegate>(name);
    }

    /// <summary>Attempts to resolve a named <c>Dnsapi.dll</c> export and convert it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
    /// <param name="function">When this method returns, contains the bound delegate if the export exists.</param>
    /// <returns><see langword="true" /> when the export exists; otherwise, <see langword="false" />.</returns>
    public static bool TryGetFunction<TDelegate>(string name, [NotNullWhen(true)] out TDelegate? function)
        where TDelegate : Delegate
    {
        return s_module.TryGetFunction(name, out function);
    }

    /// <summary>Resolves a <c>Dnsapi.dll</c> export by ordinal and converts it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="ordinal">The 16-bit export ordinal.</param>
    /// <returns>A delegate bound to the native function pointer.</returns>
    public static TDelegate GetFunction<TDelegate>(int ordinal)
        where TDelegate : Delegate
    {
        return s_module.GetFunction<TDelegate>(ordinal);
    }

    /// <summary>Attempts to resolve a <c>Dnsapi.dll</c> export by ordinal and convert it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="ordinal">The 16-bit export ordinal.</param>
    /// <param name="function">When this method returns, contains the bound delegate if the ordinal exists.</param>
    /// <returns><see langword="true" /> when the ordinal exists; otherwise, <see langword="false" />.</returns>
    public static bool TryGetFunction<TDelegate>(int ordinal, [NotNullWhen(true)] out TDelegate? function)
        where TDelegate : Delegate
    {
        return s_module.TryGetFunction(ordinal, out function);
    }

    private static ReadOnlyCollection<Export> GetExports()
    {
        return new ReadOnlyCollection<Export>([.. s_module.Exports.Select(export =>
            new Export(export.Name, export.Ordinal, export.RelativeVirtualAddress, export.ForwardedTo))]);
    }

    /// <summary>Describes one native export from the loaded <c>Dnsapi.dll</c> module.</summary>
    /// <param name="Name">The export name, or <see langword="null" /> when the function is exported by ordinal only.</param>
    /// <param name="Ordinal">The export ordinal.</param>
    /// <param name="RelativeVirtualAddress">The export relative virtual address from the portable executable export table.</param>
    /// <param name="ForwardedTo">The forwarded export target, or <see langword="null" /> when the export is implemented in <c>Dnsapi.dll</c>.</param>
    public sealed record Export(string? Name, int Ordinal, uint RelativeVirtualAddress, string? ForwardedTo)
    {
        /// <summary>Gets a stable display value for the export name or ordinal.</summary>
        public string NameOrOrdinal => Name ?? $"#{Ordinal}";

        /// <summary>Gets a value indicating whether this export has a name.</summary>
        public bool IsNamed => Name is not null;

        /// <summary>Gets a value indicating whether this export forwards to another native module.</summary>
        public bool IsForwarded => ForwardedTo is not null;
    }
}
