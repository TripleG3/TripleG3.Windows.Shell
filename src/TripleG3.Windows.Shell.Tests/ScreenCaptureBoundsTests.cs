using System.Drawing;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class ScreenCaptureBoundsTests
{
    [TestMethod]
    public void FromSize_PositiveSize_ReturnsExclusiveEdges()
    {
        var bounds = ScreenCaptureBounds.FromSize(-1920, 100, 800, 600);

        Assert.AreEqual(-1920, bounds.X1);
        Assert.AreEqual(100, bounds.Y1);
        Assert.AreEqual(-1120, bounds.X2);
        Assert.AreEqual(700, bounds.Y2);
        Assert.AreEqual(800, bounds.Width);
        Assert.AreEqual(600, bounds.Height);
    }

    [TestMethod]
    public void FromSize_NonPositiveWidth_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ScreenCaptureBounds.FromSize(0, 0, 0, 10));
    }

    [TestMethod]
    public void FromSize_NonPositiveHeight_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ScreenCaptureBounds.FromSize(0, 0, 10, 0));
    }

    [TestMethod]
    public void ToRectangle_ValidBounds_ReturnsDrawingRectangle()
    {
        var bounds = new ScreenCaptureBounds(1, 2, 6, 9);

        var rectangle = bounds.ToRectangle();

        Assert.AreEqual(new Rectangle(1, 2, 5, 7), rectangle);
    }

    [TestMethod]
    public void ToRectangle_InvalidBounds_ThrowsArgumentOutOfRangeException()
    {
        var bounds = new ScreenCaptureBounds(10, 0, 1, 10);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => bounds.ToRectangle());
    }
}
