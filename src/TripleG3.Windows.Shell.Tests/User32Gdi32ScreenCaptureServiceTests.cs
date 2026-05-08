namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class User32Gdi32ScreenCaptureServiceTests
{
    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void GetMonitors_ReturnsIndexedMonitorSnapshot()
    {
        var service = new User32Gdi32ScreenCaptureService();

        var monitors = service.GetMonitors();

        Assert.IsNotEmpty(monitors);
        CollectionAssert.AreEqual(Enumerable.Range(0, monitors.Count).ToArray(), monitors.Select(monitor => monitor.Index).ToArray());
        Assert.IsTrue(monitors.All(monitor => monitor.Bounds.Width > 0 && monitor.Bounds.Height > 0));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void CaptureAllMonitors_ReturnsBitmapMatchingBounds()
    {
        var service = new User32Gdi32ScreenCaptureService();

        using var capture = service.CaptureAllMonitors();

        Assert.IsGreaterThan(0, capture.Width);
        Assert.IsGreaterThan(0, capture.Height);
        Assert.AreEqual(capture.Bounds.Width, capture.Bitmap.Width);
        Assert.AreEqual(capture.Bounds.Height, capture.Bitmap.Height);
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void CaptureMonitor_FirstMonitor_ReturnsBitmapMatchingMonitorBounds()
    {
        var service = new User32Gdi32ScreenCaptureService();
        var firstMonitor = service.GetMonitors()[0];

        using var capture = service.CaptureMonitor(firstMonitor.Index);

        Assert.AreEqual(firstMonitor.Bounds, capture.Bounds);
        Assert.AreEqual(firstMonitor.Bounds.Width, capture.Bitmap.Width);
        Assert.AreEqual(firstMonitor.Bounds.Height, capture.Bitmap.Height);
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void CaptureBounds_OnePixelOnFirstMonitor_ReturnsOnePixelBitmap()
    {
        var service = new User32Gdi32ScreenCaptureService();
        var firstMonitor = service.GetMonitors()[0];

        using var capture = service.CaptureBounds(firstMonitor.Bounds.X1, firstMonitor.Bounds.Y1, firstMonitor.Bounds.X1 + 1, firstMonitor.Bounds.Y1 + 1);

        Assert.AreEqual(1, capture.Width);
        Assert.AreEqual(1, capture.Height);
        Assert.AreEqual(1, capture.Bitmap.Width);
        Assert.AreEqual(1, capture.Bitmap.Height);
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void CaptureWindow_ForegroundWindow_WhenAvailable_ReturnsBitmapMatchingBounds()
    {
        var windows = new User32WindowHandleService();
        var windowHandle = windows.GetForegroundWindow();
        if (windowHandle == nint.Zero)
        {
            Assert.Inconclusive("No foreground window is available to capture.");
        }

        var service = new User32Gdi32ScreenCaptureService();

        using var capture = service.CaptureWindow(windowHandle);

        Assert.IsGreaterThan(0, capture.Width);
        Assert.IsGreaterThan(0, capture.Height);
        Assert.AreEqual(capture.Bounds.Width, capture.Bitmap.Width);
        Assert.AreEqual(capture.Bounds.Height, capture.Bitmap.Height);
    }
}
