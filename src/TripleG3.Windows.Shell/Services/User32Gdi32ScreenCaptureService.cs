using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell;

/// <summary>
/// Implements <see cref="IScreenCaptureService" /> by composing focused delegates from <see cref="User32" /> and <see cref="Gdi32" />.
/// </summary>
public sealed class User32Gdi32ScreenCaptureService : IScreenCaptureService
{
    private const int DeviceNameLength = 32;
    private const int SystemMetricVirtualScreenX = 76;
    private const int SystemMetricVirtualScreenY = 77;
    private const int SystemMetricVirtualScreenWidth = 78;
    private const int SystemMetricVirtualScreenHeight = 79;
    private const uint MonitorInfoPrimary = 0x00000001;
    private const uint SourceCopyRasterOperation = 0x00CC0020;
    private const uint CaptureBltRasterOperation = 0x40000000;
    private const uint PrintWindowRenderFullContent = 0x00000002;

    private static readonly nint InvalidGdiObject = -1;

    private readonly EnumDisplayMonitorsDelegate _enumDisplayMonitors;
    private readonly GetMonitorInfoDelegate _getMonitorInfo;
    private readonly GetSystemMetricsDelegate _getSystemMetrics;
    private readonly GetWindowRectDelegate _getWindowRect;
    private readonly GetDCDelegate _getDC;
    private readonly IsWindowDelegate _isWindow;
    private readonly PrintWindowDelegate _printWindow;
    private readonly ReleaseDCDelegate _releaseDC;
    private readonly BitBltDelegate _bitBlt;
    private readonly CreateCompatibleBitmapDelegate _createCompatibleBitmap;
    private readonly CreateCompatibleDCDelegate _createCompatibleDC;
    private readonly DeleteDCDelegate _deleteDC;
    private readonly DeleteObjectDelegate _deleteObject;
    private readonly SelectObjectDelegate _selectObject;

    /// <summary>Creates a new service backed by native <c>user32.dll</c> and <c>gdi32.dll</c> exports.</summary>
    public User32Gdi32ScreenCaptureService()
        : this(
            User32.GetFunction<EnumDisplayMonitorsDelegate>("EnumDisplayMonitors"),
            User32.GetFunction<GetMonitorInfoDelegate>("GetMonitorInfoW"),
            User32.GetFunction<GetSystemMetricsDelegate>("GetSystemMetrics"),
            User32.GetFunction<GetWindowRectDelegate>("GetWindowRect"),
            User32.GetFunction<GetDCDelegate>("GetDC"),
            User32.GetFunction<IsWindowDelegate>("IsWindow"),
            User32.GetFunction<PrintWindowDelegate>("PrintWindow"),
            User32.GetFunction<ReleaseDCDelegate>("ReleaseDC"),
            Gdi32.GetFunction<BitBltDelegate>("BitBlt"),
            Gdi32.GetFunction<CreateCompatibleBitmapDelegate>("CreateCompatibleBitmap"),
            Gdi32.GetFunction<CreateCompatibleDCDelegate>("CreateCompatibleDC"),
            Gdi32.GetFunction<DeleteDCDelegate>("DeleteDC"),
            Gdi32.GetFunction<DeleteObjectDelegate>("DeleteObject"),
            Gdi32.GetFunction<SelectObjectDelegate>("SelectObject"))
    {
    }

    private User32Gdi32ScreenCaptureService(
        EnumDisplayMonitorsDelegate enumDisplayMonitors,
        GetMonitorInfoDelegate getMonitorInfo,
        GetSystemMetricsDelegate getSystemMetrics,
        GetWindowRectDelegate getWindowRect,
        GetDCDelegate getDC,
        IsWindowDelegate isWindow,
        PrintWindowDelegate printWindow,
        ReleaseDCDelegate releaseDC,
        BitBltDelegate bitBlt,
        CreateCompatibleBitmapDelegate createCompatibleBitmap,
        CreateCompatibleDCDelegate createCompatibleDC,
        DeleteDCDelegate deleteDC,
        DeleteObjectDelegate deleteObject,
        SelectObjectDelegate selectObject)
    {
        _enumDisplayMonitors = enumDisplayMonitors;
        _getMonitorInfo = getMonitorInfo;
        _getSystemMetrics = getSystemMetrics;
        _getWindowRect = getWindowRect;
        _getDC = getDC;
        _isWindow = isWindow;
        _printWindow = printWindow;
        _releaseDC = releaseDC;
        _bitBlt = bitBlt;
        _createCompatibleBitmap = createCompatibleBitmap;
        _createCompatibleDC = createCompatibleDC;
        _deleteDC = deleteDC;
        _deleteObject = deleteObject;
        _selectObject = selectObject;
    }

    /// <inheritdoc />
    public IReadOnlyList<ScreenCaptureMonitor> GetMonitors()
    {
        var monitors = new List<PendingMonitor>();
        Exception? callbackException = null;

        MonitorEnumProc callback = EnumerateMonitor;

        bool EnumerateMonitor(nint monitorHandle, nint monitorDC, ref NativeRect monitorRect, nint data)
        {
            try
            {
                var monitorInfo = new MonitorInfoEx
                {
                    Size = (uint)Marshal.SizeOf<MonitorInfoEx>(),
                };

                if (!_getMonitorInfo(monitorHandle, ref monitorInfo))
                {
                    throw CreateLastWin32Exception("GetMonitorInfoW");
                }

                monitors.Add(new PendingMonitor(
                    monitorHandle,
                    monitorInfo.DeviceName ?? string.Empty,
                    CreateBounds(monitorInfo.Monitor, nameof(monitorInfo.Monitor)),
                    CreateBounds(monitorInfo.Work, nameof(monitorInfo.Work)),
                    (monitorInfo.Flags & MonitorInfoPrimary) == MonitorInfoPrimary));

                return true;
            }
            catch (Exception exception)
            {
                callbackException = exception;
                return false;
            }
        }

        if (!_enumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero))
        {
            if (callbackException is not null)
            {
                throw callbackException;
            }

            throw CreateLastWin32Exception("EnumDisplayMonitors");
        }

        if (callbackException is not null)
        {
            throw callbackException;
        }

        return monitors
            .OrderByDescending(monitor => monitor.IsPrimary)
            .ThenBy(monitor => monitor.Bounds.X1)
            .ThenBy(monitor => monitor.Bounds.Y1)
            .Select((monitor, index) => new ScreenCaptureMonitor(
                index,
                monitor.MonitorHandle,
                monitor.DeviceName,
                monitor.Bounds,
                monitor.WorkArea,
                monitor.IsPrimary))
            .ToArray();
    }

    /// <inheritdoc />
    public ScreenCapture CaptureAllMonitors()
    {
        var monitors = GetMonitors();

        if (monitors.Count > 0)
        {
            return CaptureMonitors(monitors);
        }

        return CaptureBoundsCore(GetVirtualScreenBounds(), []);
    }

    /// <inheritdoc />
    public ScreenCapture CaptureMonitor(int monitorIndex)
    {
        return CaptureMonitors([monitorIndex]);
    }

    /// <inheritdoc />
    public ScreenCapture CaptureMonitor(ScreenCaptureMonitor monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        return CaptureMonitors([monitor]);
    }

    /// <inheritdoc />
    public ScreenCapture CaptureMonitors(IEnumerable<int> monitorIndices)
    {
        ArgumentNullException.ThrowIfNull(monitorIndices);

        var indices = monitorIndices.Distinct().ToArray();
        if (indices.Length == 0)
        {
            throw new ArgumentException("At least one monitor index must be provided.", nameof(monitorIndices));
        }

        var monitors = GetMonitors();
        var selectedMonitors = new List<ScreenCaptureMonitor>(indices.Length);

        foreach (var index in indices)
        {
            if (index < 0 || index >= monitors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(monitorIndices), index, "Monitor index is outside the current monitor snapshot.");
            }

            selectedMonitors.Add(monitors[index]);
        }

        return CaptureMonitors(selectedMonitors);
    }

    /// <inheritdoc />
    public ScreenCapture CaptureMonitors(IEnumerable<ScreenCaptureMonitor> monitors)
    {
        ArgumentNullException.ThrowIfNull(monitors);

        var selectedMonitors = new List<ScreenCaptureMonitor>();
        foreach (var monitor in monitors)
        {
            if (monitor is null)
            {
                throw new ArgumentException("Monitor collection must not contain null entries.", nameof(monitors));
            }

            selectedMonitors.Add(monitor);
        }

        if (selectedMonitors.Count == 0)
        {
            throw new ArgumentException("At least one monitor must be provided.", nameof(monitors));
        }

        var bounds = CreateUnionBounds(selectedMonitors);

        return CaptureBoundsCore(bounds, selectedMonitors);
    }

    /// <inheritdoc />
    public ScreenCapture CaptureBounds(ScreenCaptureBounds bounds)
    {
        return CaptureBoundsCore(bounds, []);
    }

    /// <inheritdoc />
    public ScreenCapture CaptureBounds(int x1, int y1, int x2, int y2)
    {
        return CaptureBounds(new ScreenCaptureBounds(x1, y1, x2, y2));
    }

    /// <inheritdoc />
    public ScreenCapture CaptureWindow(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            throw new ArgumentException("Window handle must not be zero.", nameof(windowHandle));
        }

        if (!_isWindow(windowHandle))
        {
            throw new ArgumentException("Window handle does not identify an existing window.", nameof(windowHandle));
        }

        if (!_getWindowRect(windowHandle, out var windowRect))
        {
            throw CreateLastWin32Exception("GetWindowRect");
        }

        var bounds = CreateBounds(windowRect, nameof(windowHandle));

        if (TryCaptureWindowWithPrintWindow(windowHandle, bounds, out var bitmap))
        {
            return CreateCaptureResult(bitmap, bounds, []);
        }

        return CaptureBoundsCore(bounds, []);
    }

    private static ScreenCaptureBounds CreateUnionBounds(IReadOnlyList<ScreenCaptureMonitor> monitors)
    {
        var x1 = monitors.Min(monitor => monitor.Bounds.X1);
        var y1 = monitors.Min(monitor => monitor.Bounds.Y1);
        var x2 = monitors.Max(monitor => monitor.Bounds.X2);
        var y2 = monitors.Max(monitor => monitor.Bounds.Y2);

        return new ScreenCaptureBounds(x1, y1, x2, y2);
    }

    private static ScreenCaptureBounds CreateBounds(NativeRect rect, string parameterName)
    {
        var bounds = new ScreenCaptureBounds(rect.Left, rect.Top, rect.Right, rect.Bottom);
        ScreenCaptureBounds.ThrowIfInvalid(bounds, parameterName);

        return bounds;
    }

    private static Win32Exception CreateLastWin32Exception(string operation)
    {
        var lastError = Marshal.GetLastWin32Error();

        return lastError == 0
            ? new Win32Exception($"The native call '{operation}' failed.")
            : new Win32Exception(lastError, $"The native call '{operation}' failed.");
    }

    private ScreenCaptureBounds GetVirtualScreenBounds()
    {
        var x = _getSystemMetrics(SystemMetricVirtualScreenX);
        var y = _getSystemMetrics(SystemMetricVirtualScreenY);
        var width = _getSystemMetrics(SystemMetricVirtualScreenWidth);
        var height = _getSystemMetrics(SystemMetricVirtualScreenHeight);

        return ScreenCaptureBounds.FromSize(x, y, width, height);
    }

    private ScreenCapture CaptureBoundsCore(ScreenCaptureBounds bounds, IReadOnlyList<ScreenCaptureMonitor> monitors)
    {
        ScreenCaptureBounds.ThrowIfInvalid(bounds, nameof(bounds));

        var bitmap = CaptureScreenBoundsToBitmap(bounds);

        return CreateCaptureResult(bitmap, bounds, monitors);
    }

    private static ScreenCapture CreateCaptureResult(Bitmap bitmap, ScreenCaptureBounds bounds, IReadOnlyList<ScreenCaptureMonitor> monitors)
    {
        try
        {
            return new ScreenCapture(bitmap, bounds, monitors);
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    private Bitmap CaptureScreenBoundsToBitmap(ScreenCaptureBounds bounds)
    {
        var screenDC = _getDC(nint.Zero);
        if (screenDC == nint.Zero)
        {
            throw CreateLastWin32Exception("GetDC");
        }

        try
        {
            return CaptureToBitmap(screenDC, bounds, bounds.X1, bounds.Y1, useBitBlt: true);
        }
        finally
        {
            _releaseDC(nint.Zero, screenDC);
        }
    }

    private bool TryCaptureWindowWithPrintWindow(nint windowHandle, ScreenCaptureBounds bounds, [NotNullWhen(true)] out Bitmap? bitmap)
    {
        var screenDC = _getDC(nint.Zero);
        if (screenDC == nint.Zero)
        {
            throw CreateLastWin32Exception("GetDC");
        }

        try
        {
            return TryCaptureToBitmap(screenDC, bounds, _bitBlt, windowHandle, out bitmap);
        }
        finally
        {
            _releaseDC(nint.Zero, screenDC);
        }
    }

    private Bitmap CaptureToBitmap(nint sourceDC, ScreenCaptureBounds bounds, int sourceX, int sourceY, bool useBitBlt)
    {
        if (TryCaptureToBitmap(sourceDC, bounds, _bitBlt, nint.Zero, out var bitmap, sourceX, sourceY, useBitBlt))
        {
            return bitmap;
        }

        throw CreateLastWin32Exception("BitBlt");
    }

    private bool TryCaptureToBitmap(
        nint sourceDC,
        ScreenCaptureBounds bounds,
        BitBltDelegate bitBlt,
        nint printWindow,
        [NotNullWhen(true)] out Bitmap? bitmap,
        int sourceX = 0,
        int sourceY = 0,
        bool useBitBlt = false)
    {
        bitmap = null;

        nint memoryDC = nint.Zero;
        nint bitmapHandle = nint.Zero;
        nint previousObject = nint.Zero;

        try
        {
            memoryDC = _createCompatibleDC(sourceDC);
            if (memoryDC == nint.Zero)
            {
                throw CreateLastWin32Exception("CreateCompatibleDC");
            }

            bitmapHandle = _createCompatibleBitmap(sourceDC, bounds.Width, bounds.Height);
            if (bitmapHandle == nint.Zero)
            {
                throw CreateLastWin32Exception("CreateCompatibleBitmap");
            }

            previousObject = _selectObject(memoryDC, bitmapHandle);
            if (previousObject == nint.Zero || previousObject == InvalidGdiObject)
            {
                throw CreateLastWin32Exception("SelectObject");
            }

            var captured = useBitBlt
                ? bitBlt(memoryDC, 0, 0, bounds.Width, bounds.Height, sourceDC, sourceX, sourceY, SourceCopyRasterOperation | CaptureBltRasterOperation)
                : _printWindow(printWindow, memoryDC, PrintWindowRenderFullContent);

            if (!captured)
            {
                return false;
            }

            bitmap = Bitmap.FromHbitmap(bitmapHandle);
            return true;
        }
        finally
        {
            if (previousObject != nint.Zero && previousObject != InvalidGdiObject && memoryDC != nint.Zero)
            {
                _selectObject(memoryDC, previousObject);
            }

            if (bitmapHandle != nint.Zero)
            {
                _deleteObject(bitmapHandle);
            }

            if (memoryDC != nint.Zero)
            {
                _deleteDC(memoryDC);
            }
        }
    }

    private readonly record struct PendingMonitor(
        nint MonitorHandle,
        string DeviceName,
        ScreenCaptureBounds Bounds,
        ScreenCaptureBounds WorkArea,
        bool IsPrimary);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public uint Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = DeviceNameLength)]
        public string? DeviceName;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool MonitorEnumProc(nint monitorHandle, nint monitorDC, ref NativeRect monitorRect, nint data);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool EnumDisplayMonitorsDelegate(nint deviceContext, nint clipRect, MonitorEnumProc callback, nint data);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool GetMonitorInfoDelegate(nint monitorHandle, ref MonitorInfoEx monitorInfo);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int GetSystemMetricsDelegate(int index);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool GetWindowRectDelegate(nint windowHandle, out NativeRect rect);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    private delegate nint GetDCDelegate(nint windowHandle);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool IsWindowDelegate(nint windowHandle);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool PrintWindowDelegate(nint windowHandle, nint targetDeviceContext, uint flags);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int ReleaseDCDelegate(nint windowHandle, nint deviceContext);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool BitBltDelegate(nint destinationDeviceContext, int destinationX, int destinationY, int width, int height, nint sourceDeviceContext, int sourceX, int sourceY, uint rasterOperation);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    private delegate nint CreateCompatibleBitmapDelegate(nint deviceContext, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    private delegate nint CreateCompatibleDCDelegate(nint deviceContext);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool DeleteDCDelegate(nint deviceContext);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool DeleteObjectDelegate(nint gdiObject);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
    private delegate nint SelectObjectDelegate(nint deviceContext, nint gdiObject);
}
