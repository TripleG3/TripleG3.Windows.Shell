using Microsoft.Extensions.DependencyInjection;
using TripleG3.Windows.Shell;

using var provider = new ServiceCollection()
    .AddTripleG3WindowsShell()
    .BuildServiceProvider();

var windows = provider.GetRequiredService<IWindowHandleService>();
var screenCapture = provider.GetRequiredService<IScreenCaptureService>();

Console.WriteLine("TripleG3.Windows.Shell consumer smoke sample");
Console.WriteLine($"Desktop window handle available: {windows.GetDesktopWindow() != nint.Zero}");
Console.WriteLine($"Foreground window handle: 0x{windows.GetForegroundWindow():X}");
Console.WriteLine($"Screen capture service: {screenCapture.GetType().Name}");