namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class User32WindowHandleServiceTests
{
    [TestMethod]
    public void GetDesktopWindow_ReturnsExistingWindowHandle()
    {
        var service = new User32WindowHandleService();

        var desktopWindow = service.GetDesktopWindow();

        Assert.AreNotEqual(nint.Zero, desktopWindow);
        Assert.IsTrue(service.IsWindow(desktopWindow));
    }

    [TestMethod]
    public void IsWindow_ZeroHandle_ReturnsFalse()
    {
        var service = new User32WindowHandleService();

        Assert.IsFalse(service.IsWindow(nint.Zero));
    }

    [TestMethod]
    public void GetForegroundWindow_WhenWindowExists_ReturnsExistingWindowHandle()
    {
        var service = new User32WindowHandleService();

        var foregroundWindow = service.GetForegroundWindow();

        if (foregroundWindow != nint.Zero)
        {
            Assert.IsTrue(service.IsWindow(foregroundWindow));
        }
    }
}