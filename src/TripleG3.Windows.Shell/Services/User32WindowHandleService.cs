using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell;

/// <summary>
/// Implements <see cref="IWindowHandleService" /> by binding focused delegates from <see cref="User32" />.
/// </summary>
public sealed class User32WindowHandleService : IWindowHandleService
{
    private readonly GetDesktopWindowDelegate _getDesktopWindow;
    private readonly GetForegroundWindowDelegate _getForegroundWindow;
    private readonly IsWindowDelegate _isWindow;

    /// <summary>Creates a new service backed by native <c>user32.dll</c> exports.</summary>
    public User32WindowHandleService()
        : this(
            User32.GetFunction<GetDesktopWindowDelegate>("GetDesktopWindow"),
            User32.GetFunction<GetForegroundWindowDelegate>("GetForegroundWindow"),
            User32.GetFunction<IsWindowDelegate>("IsWindow"))
    {
    }

    private User32WindowHandleService(
        GetDesktopWindowDelegate getDesktopWindow,
        GetForegroundWindowDelegate getForegroundWindow,
        IsWindowDelegate isWindow)
    {
        _getDesktopWindow = getDesktopWindow;
        _getForegroundWindow = getForegroundWindow;
        _isWindow = isWindow;
    }

    /// <inheritdoc />
    public nint GetDesktopWindow()
    {
        return _getDesktopWindow();
    }

    /// <inheritdoc />
    public nint GetForegroundWindow()
    {
        return _getForegroundWindow();
    }

    /// <inheritdoc />
    public bool IsWindow(nint windowHandle)
    {
        return _isWindow(windowHandle);
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint GetDesktopWindowDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint GetForegroundWindowDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool IsWindowDelegate(nint windowHandle);
}