using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;

namespace TripleG3.Windows.Shell;

/// <summary>
/// Represents a captured screenshot image and its virtual-screen source bounds.
/// </summary>
public sealed class ScreenCapture : IDisposable
{
    private bool _disposed;

    /// <summary>Creates a captured screenshot result.</summary>
    /// <param name="bitmap">The captured bitmap. Ownership is transferred to the capture result.</param>
    /// <param name="bounds">The virtual-screen source bounds captured into <paramref name="bitmap" />.</param>
    /// <param name="monitors">The monitors selected for monitor-based captures, or an empty collection for bounds and window captures.</param>
    public ScreenCapture(Bitmap bitmap, ScreenCaptureBounds bounds, IReadOnlyList<ScreenCaptureMonitor> monitors)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(monitors);
        ScreenCaptureBounds.ThrowIfInvalid(bounds, nameof(bounds));

        if (bitmap.Width != bounds.Width || bitmap.Height != bounds.Height)
        {
            throw new ArgumentException("Bitmap dimensions must match the capture bounds dimensions.", nameof(bitmap));
        }

        Bitmap = bitmap;
        Bounds = bounds;
        Monitors = new ReadOnlyCollection<ScreenCaptureMonitor>(monitors.ToArray());
    }

    /// <summary>Gets the captured bitmap.</summary>
    public Bitmap Bitmap { get; }

    /// <summary>Gets the virtual-screen source bounds captured into <see cref="Bitmap" />.</summary>
    public ScreenCaptureBounds Bounds { get; }

    /// <summary>Gets the monitors selected for monitor-based captures, or an empty collection for bounds and window captures.</summary>
    public IReadOnlyList<ScreenCaptureMonitor> Monitors { get; }

    /// <summary>Gets the captured image width in pixels.</summary>
    public int Width => Bounds.Width;

    /// <summary>Gets the captured image height in pixels.</summary>
    public int Height => Bounds.Height;

    /// <summary>Saves the captured bitmap as a PNG file.</summary>
    /// <param name="filePath">The path of the PNG file to write.</param>
    public void SavePng(string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        Bitmap.Save(filePath, ImageFormat.Png);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Bitmap.Dispose();
        _disposed = true;
    }
}
