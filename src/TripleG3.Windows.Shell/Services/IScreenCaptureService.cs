namespace TripleG3.Windows.Shell;

/// <summary>
/// Provides app-facing screenshot capture operations for monitors, bounds, and windows.
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>Gets the monitors currently available in the Windows virtual desktop.</summary>
    /// <returns>A snapshot of monitors with primary monitor first and stable zero-based indices for that snapshot.</returns>
    IReadOnlyList<ScreenCaptureMonitor> GetMonitors();

    /// <summary>Captures the full virtual desktop across all monitors.</summary>
    /// <returns>The captured image and source bounds.</returns>
    ScreenCapture CaptureAllMonitors();

    /// <summary>Captures a single monitor selected by the index returned from <see cref="GetMonitors" />.</summary>
    /// <param name="monitorIndex">The zero-based monitor index.</param>
    /// <returns>The captured image and source bounds.</returns>
    ScreenCapture CaptureMonitor(int monitorIndex);

    /// <summary>Captures a single monitor.</summary>
    /// <param name="monitor">The monitor to capture.</param>
    /// <returns>The captured image and source bounds.</returns>
    ScreenCapture CaptureMonitor(ScreenCaptureMonitor monitor);

    /// <summary>Captures multiple monitors as one image using the union of their virtual-screen bounds.</summary>
    /// <param name="monitorIndices">The zero-based monitor indices returned from <see cref="GetMonitors" />.</param>
    /// <returns>The captured image and source bounds.</returns>
    ScreenCapture CaptureMonitors(IEnumerable<int> monitorIndices);

    /// <summary>Captures multiple monitors as one image using the union of their virtual-screen bounds.</summary>
    /// <param name="monitors">The monitors to capture.</param>
    /// <returns>The captured image and source bounds.</returns>
    ScreenCapture CaptureMonitors(IEnumerable<ScreenCaptureMonitor> monitors);

    /// <summary>Captures a specific virtual-screen coordinate region.</summary>
    /// <param name="bounds">The bounds to capture.</param>
    /// <returns>The captured image and source bounds.</returns>
    ScreenCapture CaptureBounds(ScreenCaptureBounds bounds);

    /// <summary>Captures a specific virtual-screen coordinate region.</summary>
    /// <param name="x1">The inclusive left edge of the region.</param>
    /// <param name="y1">The inclusive top edge of the region.</param>
    /// <param name="x2">The exclusive right edge of the region.</param>
    /// <param name="y2">The exclusive bottom edge of the region.</param>
    /// <returns>The captured image and source bounds.</returns>
    ScreenCapture CaptureBounds(int x1, int y1, int x2, int y2);

    /// <summary>Captures a specific native window.</summary>
    /// <param name="windowHandle">The native window handle to capture.</param>
    /// <returns>The captured image and source bounds.</returns>
    ScreenCapture CaptureWindow(nint windowHandle);
}
