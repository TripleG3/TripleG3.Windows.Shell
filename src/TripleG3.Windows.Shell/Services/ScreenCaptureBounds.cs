using System.Drawing;

namespace TripleG3.Windows.Shell;

/// <summary>
/// Describes a rectangular screen capture region in virtual-screen coordinates.
/// </summary>
/// <param name="X1">The inclusive left edge of the region.</param>
/// <param name="Y1">The inclusive top edge of the region.</param>
/// <param name="X2">The exclusive right edge of the region.</param>
/// <param name="Y2">The exclusive bottom edge of the region.</param>
public readonly record struct ScreenCaptureBounds(int X1, int Y1, int X2, int Y2)
{
    /// <summary>Gets the width of the region in pixels.</summary>
    public int Width => checked(X2 - X1);

    /// <summary>Gets the height of the region in pixels.</summary>
    public int Height => checked(Y2 - Y1);

    /// <summary>Creates bounds from a top-left point and size.</summary>
    /// <param name="x">The left edge of the region.</param>
    /// <param name="y">The top edge of the region.</param>
    /// <param name="width">The width of the region in pixels.</param>
    /// <param name="height">The height of the region in pixels.</param>
    /// <returns>The equivalent capture bounds.</returns>
    public static ScreenCaptureBounds FromSize(int x, int y, int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Capture width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Capture height must be greater than zero.");
        }

        return new ScreenCaptureBounds(x, y, checked(x + width), checked(y + height));
    }

    /// <summary>Converts the bounds to a <see cref="Rectangle" />.</summary>
    /// <returns>The equivalent drawing rectangle.</returns>
    public Rectangle ToRectangle()
    {
        ThrowIfInvalid(this, nameof(ScreenCaptureBounds));

        return new Rectangle(X1, Y1, Width, Height);
    }

    internal static void ThrowIfInvalid(ScreenCaptureBounds bounds, string parameterName)
    {
        var width = (long)bounds.X2 - bounds.X1;
        var height = (long)bounds.Y2 - bounds.Y1;

        if (width <= 0 || width > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(parameterName, bounds, "Capture bounds must describe a region with a positive width.");
        }

        if (height <= 0 || height > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(parameterName, bounds, "Capture bounds must describe a region with a positive height.");
        }
    }
}
