namespace TripleG3.Windows.Shell;

/// <summary>
/// Describes a physical monitor available in the Windows virtual desktop.
/// </summary>
public sealed record ScreenCaptureMonitor
{
    /// <summary>Creates a monitor description.</summary>
    /// <param name="index">The zero-based monitor index for the monitor snapshot returned by <see cref="IScreenCaptureService.GetMonitors" />.</param>
    /// <param name="monitorHandle">The native monitor handle.</param>
    /// <param name="deviceName">The native display device name, such as <c>\\.\DISPLAY1</c>.</param>
    /// <param name="bounds">The monitor bounds in virtual-screen coordinates.</param>
    /// <param name="workArea">The monitor work area in virtual-screen coordinates.</param>
    /// <param name="isPrimary">A value indicating whether this monitor is the primary display.</param>
    public ScreenCaptureMonitor(
        int index,
        nint monitorHandle,
        string deviceName,
        ScreenCaptureBounds bounds,
        ScreenCaptureBounds workArea,
        bool isPrimary)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Monitor index must be greater than or equal to zero.");
        }

        if (monitorHandle == nint.Zero)
        {
            throw new ArgumentException("Monitor handle must not be zero.", nameof(monitorHandle));
        }

        ArgumentNullException.ThrowIfNull(deviceName);
        ScreenCaptureBounds.ThrowIfInvalid(bounds, nameof(bounds));
        ScreenCaptureBounds.ThrowIfInvalid(workArea, nameof(workArea));

        Index = index;
        MonitorHandle = monitorHandle;
        DeviceName = deviceName;
        Bounds = bounds;
        WorkArea = workArea;
        IsPrimary = isPrimary;
    }

    /// <summary>Gets the zero-based monitor index for the monitor snapshot returned by <see cref="IScreenCaptureService.GetMonitors" />.</summary>
    public int Index { get; }

    /// <summary>Gets the native monitor handle.</summary>
    public nint MonitorHandle { get; }

    /// <summary>Gets the native display device name, such as <c>\\.\DISPLAY1</c>.</summary>
    public string DeviceName { get; }

    /// <summary>Gets the monitor bounds in virtual-screen coordinates.</summary>
    public ScreenCaptureBounds Bounds { get; }

    /// <summary>Gets the monitor work area in virtual-screen coordinates.</summary>
    public ScreenCaptureBounds WorkArea { get; }

    /// <summary>Gets a value indicating whether this monitor is the primary display.</summary>
    public bool IsPrimary { get; }
}
