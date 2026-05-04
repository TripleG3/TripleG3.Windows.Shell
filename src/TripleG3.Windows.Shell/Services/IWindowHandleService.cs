namespace TripleG3.Windows.Shell;

/// <summary>
/// Provides app-facing access to common Win32 window handle queries.
/// </summary>
public interface IWindowHandleService
{
    /// <summary>Gets the desktop window handle.</summary>
    nint GetDesktopWindow();

    /// <summary>Gets the foreground window handle, or <see cref="nint.Zero" /> when no foreground window exists.</summary>
    nint GetForegroundWindow();

    /// <summary>Determines whether a native window handle identifies an existing window.</summary>
    /// <param name="windowHandle">The native window handle to validate.</param>
    /// <returns><see langword="true" /> when <paramref name="windowHandle" /> identifies an existing window; otherwise, <see langword="false" />.</returns>
    bool IsWindow(nint windowHandle);
}