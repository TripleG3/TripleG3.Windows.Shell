using System.Drawing;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class ScreenCaptureTests
{
    [TestMethod]
    public void Constructor_ValidValues_SetsMetadata()
    {
        var bounds = ScreenCaptureBounds.FromSize(0, 0, 2, 3);
        var monitor = new ScreenCaptureMonitor(0, 1, "Display", bounds, bounds, isPrimary: true);
        var bitmap = new Bitmap(2, 3);

        using var capture = new ScreenCapture(bitmap, bounds, [monitor]);

        Assert.AreSame(bitmap, capture.Bitmap);
        Assert.AreEqual(bounds, capture.Bounds);
        Assert.AreEqual(2, capture.Width);
        Assert.AreEqual(3, capture.Height);
        Assert.HasCount(1, capture.Monitors);
        Assert.AreEqual(monitor, capture.Monitors[0]);
    }

    [TestMethod]
    public void Constructor_BitmapDimensionsDoNotMatchBounds_ThrowsArgumentException()
    {
        using var bitmap = new Bitmap(2, 2);

        Assert.ThrowsExactly<ArgumentException>(() => new ScreenCapture(bitmap, ScreenCaptureBounds.FromSize(0, 0, 1, 1), []));
    }

    [TestMethod]
    public void SavePng_WritesPngFile()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");

        try
        {
            using var capture = new ScreenCapture(new Bitmap(1, 1), ScreenCaptureBounds.FromSize(0, 0, 1, 1), []);

            capture.SavePng(filePath);

            Assert.IsTrue(File.Exists(filePath));
            Assert.IsGreaterThan(0L, new FileInfo(filePath).Length);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [TestMethod]
    public void SavePng_DisposedCapture_ThrowsObjectDisposedException()
    {
        var capture = new ScreenCapture(new Bitmap(1, 1), ScreenCaptureBounds.FromSize(0, 0, 1, 1), []);
        capture.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => capture.SavePng("capture.png"));
    }
}
