namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class ScreenCaptureMonitorTests
{
    [TestMethod]
    public void Constructor_ValidValues_SetsProperties()
    {
        var bounds = ScreenCaptureBounds.FromSize(-100, 0, 100, 50);
        var workArea = ScreenCaptureBounds.FromSize(-100, 10, 100, 40);

        var monitor = new ScreenCaptureMonitor(1, 123, "\\\\.\\DISPLAY2", bounds, workArea, isPrimary: false);

        Assert.AreEqual(1, monitor.Index);
        Assert.AreEqual((nint)123, monitor.MonitorHandle);
        Assert.AreEqual("\\\\.\\DISPLAY2", monitor.DeviceName);
        Assert.AreEqual(bounds, monitor.Bounds);
        Assert.AreEqual(workArea, monitor.WorkArea);
        Assert.IsFalse(monitor.IsPrimary);
    }

    [TestMethod]
    public void Constructor_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        var bounds = ScreenCaptureBounds.FromSize(0, 0, 1, 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ScreenCaptureMonitor(-1, 1, string.Empty, bounds, bounds, isPrimary: true));
    }

    [TestMethod]
    public void Constructor_ZeroHandle_ThrowsArgumentException()
    {
        var bounds = ScreenCaptureBounds.FromSize(0, 0, 1, 1);

        Assert.ThrowsExactly<ArgumentException>(() => new ScreenCaptureMonitor(0, nint.Zero, string.Empty, bounds, bounds, isPrimary: true));
    }

    [TestMethod]
    public void Constructor_InvalidBounds_ThrowsArgumentOutOfRangeException()
    {
        var bounds = ScreenCaptureBounds.FromSize(0, 0, 1, 1);
        var invalidBounds = new ScreenCaptureBounds(1, 0, 0, 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ScreenCaptureMonitor(0, 1, string.Empty, invalidBounds, bounds, isPrimary: true));
    }
}
