using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell;

/// <summary>
/// Provides a version-tolerant wrapper over the native Windows <c>Secur32.dll</c> module for SSPI authentication APIs such as Kerberos, NTLM, Negotiate, and Schannel.
/// </summary>
/// <remarks>
/// The set of exported <c>Secur32.dll</c> functions can differ between Windows versions and installed security packages.
/// This wrapper discovers the exports available on the current machine at runtime and lets callers bind the exact
/// delegate signature they need with <see cref="GetFunction{TDelegate}(string)" /> or
/// <c>TryGetFunction&lt;TDelegate&gt;</c>. Prefer <see cref="System.Net.Security.NegotiateStream" />,
/// <see cref="System.Net.Security.SslStream" />, and higher-level authentication libraries for ordinary application code;
/// use this type when code needs direct SSPI credential, context, or package-management exports.
/// </remarks>
public static class Secur32
{
    /// <summary>The canonical Windows module name for Secur32.</summary>
    public const string LibraryName = "Secur32.dll";

    private static readonly NativeModule s_module = new(LibraryName, typeof(Secur32));
    private static readonly Lazy<IReadOnlyList<Export>> s_exports = new(GetExports, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Gets the loaded native module handle for <c>Secur32.dll</c>.</summary>
    /// <exception cref="DllNotFoundException">Thrown when Windows cannot load <c>Secur32.dll</c>.</exception>
    public static nint ModuleHandle => s_module.ModuleHandle;

    /// <summary>Gets the full path to the loaded <c>Secur32.dll</c> module.</summary>
    public static string ModulePath => s_module.ModulePath;

    /// <summary>Gets metadata for every exported <c>Secur32.dll</c> function available on the current machine.</summary>
    public static IReadOnlyList<Export> Exports => s_exports.Value;

    /// <summary>Gets every named <c>Secur32.dll</c> export available on the current machine.</summary>
    public static IReadOnlyList<string> ExportNames => s_module.ExportNames;

    /// <summary>Attempts to resolve a named <c>Secur32.dll</c> export to its native function pointer.</summary>
    /// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
    /// <param name="address">When this method returns, contains the native function pointer if found.</param>
    /// <returns><see langword="true" /> when the export exists; otherwise, <see langword="false" />.</returns>
    public static bool TryGetExport(string name, out nint address)
    {
        return s_module.TryGetExport(name, out address);
    }

    /// <summary>Resolves a named <c>Secur32.dll</c> export to its native function pointer.</summary>
    /// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
    /// <returns>The native function pointer for the requested export.</returns>
    /// <exception cref="EntryPointNotFoundException">Thrown when the export does not exist in the loaded <c>Secur32.dll</c>.</exception>
    public static nint GetExport(string name)
    {
        return s_module.GetExport(name);
    }

    /// <summary>Attempts to resolve a <c>Secur32.dll</c> export by ordinal to its native function pointer.</summary>
    /// <param name="ordinal">The 16-bit export ordinal.</param>
    /// <param name="address">When this method returns, contains the native function pointer if found.</param>
    /// <returns><see langword="true" /> when the ordinal exists; otherwise, <see langword="false" />.</returns>
    public static bool TryGetExport(int ordinal, out nint address)
    {
        return s_module.TryGetExport(ordinal, out address);
    }

    /// <summary>Resolves a <c>Secur32.dll</c> export by ordinal to its native function pointer.</summary>
    /// <param name="ordinal">The 16-bit export ordinal.</param>
    /// <returns>The native function pointer for the requested export.</returns>
    /// <exception cref="EntryPointNotFoundException">Thrown when the ordinal does not exist in the loaded <c>Secur32.dll</c>.</exception>
    public static nint GetExport(int ordinal)
    {
        return s_module.GetExport(ordinal);
    }

    /// <summary>Resolves a named <c>Secur32.dll</c> export and converts it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
    /// <returns>A delegate bound to the native function pointer.</returns>
    /// <remarks>
    /// Delegate types should normally be decorated with <see cref="UnmanagedFunctionPointerAttribute" /> using
    /// <see cref="CallingConvention.Winapi" /> and the exact buffer, handle, timestamp, and security status types from the Windows SDK.
    /// For example, bind <c>AcquireCredentialsHandleW</c>, <c>InitializeSecurityContextW</c>, or <c>FreeCredentialsHandle</c> only to a matching delegate.
    /// </remarks>
    public static TDelegate GetFunction<TDelegate>(string name)
        where TDelegate : Delegate
    {
        return s_module.GetFunction<TDelegate>(name);
    }

    /// <summary>Attempts to resolve a named <c>Secur32.dll</c> export and convert it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="name">The exact exported function name, including any ANSI/Unicode suffix such as <c>A</c> or <c>W</c>.</param>
    /// <param name="function">When this method returns, contains the bound delegate if the export exists.</param>
    /// <returns><see langword="true" /> when the export exists; otherwise, <see langword="false" />.</returns>
    public static bool TryGetFunction<TDelegate>(string name, [NotNullWhen(true)] out TDelegate? function)
        where TDelegate : Delegate
    {
        return s_module.TryGetFunction(name, out function);
    }

    /// <summary>Resolves a <c>Secur32.dll</c> export by ordinal and converts it to a managed delegate.</summary>
    /// <typeparam name="TDelegate">The managed delegate type that exactly matches the native function signature.</typeparam>
    /// <param name="ordinal">The 16-bit export ordinal.</param>
    /// <returns>A delegate bound to the native function pointer.</returns>
    public static TDelegate GetFunction<TDelegate>(int ordinal)
        where TDelegate : Delegate
    {
        return s_module.GetFunction<TDelegate>(ordinal);
    }

    /// <summary>Attempts to resolve a <c>Secur32.dll</c> export by ordinal and convert it to a managed delegate.</summary>
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

    /// <summary>Describes one native export from the loaded <c>Secur32.dll</c> module.</summary>
    /// <param name="Name">The export name, or <see langword="null" /> when the function is exported by ordinal only.</param>
    /// <param name="Ordinal">The export ordinal.</param>
    /// <param name="RelativeVirtualAddress">The export relative virtual address from the portable executable export table.</param>
    /// <param name="ForwardedTo">The forwarded export target, or <see langword="null" /> when the export is implemented in <c>Secur32.dll</c>.</param>
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