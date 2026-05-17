# TripleG3.Windows.Shell

`TripleG3.Windows.Shell` is a Windows-only .NET 10 library for working with core Win32 shell, windowing, screen capture, graphics, process, networking, device, HID, USB, security, cryptography, SSPI, and smart card APIs from managed code.

The library exposes two layers:

- **Static native DLL wrappers** for `user32.dll`, `gdi32.dll`, `kernel32.dll`, `shell32.dll`, common Windows networking DLLs, Windows device/USB DLLs, and Windows security/cryptography/smart-card DLLs.
- **App-facing services and interfaces** for common operations that should be easy to inject, consume, and test.

The static wrappers are the low-level escape hatch. The services are the preferred API for normal application code.

## Platform support

This project targets Windows only:

```xml
<TargetFramework>net10.0-windows</TargetFramework>
```

Do not add runtime operating-system guards for normal library code. The target framework already communicates and enforces the Windows-only contract.

## Current public API

| API | Type | Purpose | Recommended consumer |
|---|---|---|---|
| `User32` | Static class | Dynamic access to `user32.dll` exports | Advanced/native interop callers and internal services |
| `Gdi32` | Static class | Dynamic access to `gdi32.dll` exports | Advanced/native interop callers and internal services |
| `Kernel32` | Static class | Dynamic access to `kernel32.dll` exports | Advanced/native interop callers and internal services |
| `Shell32` | Static class | Dynamic access to `shell32.dll` exports | Advanced/native interop callers and internal services |
| `Ws2_32` | Static class | Dynamic access to Winsock TCP/UDP exports in `Ws2_32.dll` | Advanced/native interop callers |
| `WinInet` | Static class | Dynamic access to high-level HTTP/FTP exports in `WinInet.dll` | Advanced/native interop callers |
| `WinHttp` | Static class | Dynamic access to service-friendly HTTP exports in `WinHttp.dll` | Advanced/native interop callers |
| `Dnsapi` | Static class | Dynamic access to DNS exports in `Dnsapi.dll` | Advanced/native interop callers |
| `Iphlpapi` | Static class | Dynamic access to network adapter, routing, and IP helper exports in `Iphlpapi.dll` | Advanced/native interop callers |
| `Wlanapi` | Static class | Dynamic access to Wi-Fi management exports in `Wlanapi.dll` | Advanced/native interop callers |
| `Advapi32` | Static class | Dynamic access to registry, service control, security descriptor, LSA, and event logging exports in `Advapi32.dll` | Advanced/native interop callers |
| `Crypt32` | Static class | Dynamic access to certificate store and encoding/decoding exports in `Crypt32.dll` | Advanced/native interop callers |
| `BCrypt` | Static class | Dynamic access to CNG primitive algorithm exports in `BCrypt.dll` | Advanced/native interop callers |
| `NCrypt` | Static class | Dynamic access to CNG key storage provider and hardware-backed key exports in `NCrypt.dll` | Advanced/native interop callers |
| `Secur32` | Static class | Dynamic access to SSPI authentication exports in `Secur32.dll` | Advanced/native interop callers |
| `Winscard` | Static class | Dynamic access to smart card resource manager exports in `Winscard.dll` | Advanced/native interop callers |
| `SCardDlg` | Static class | Dynamic access to smart card common dialog exports in `SCardDlg.dll` | Advanced/native interop callers |
| `SetupApi` | Static class | Dynamic access to device installation and hardware enumeration exports in `SetupAPI.dll` | Advanced/native interop callers |
| `CfgMgr32` | Static class | Dynamic access to configuration manager device tree exports in `CfgMgr32.dll` | Advanced/native interop callers |
| `Hid` | Static class | Dynamic access to HID/gamepad/sensor exports in `Hid.dll` | Advanced/native interop callers |
| `WinUsb` | Static class | Dynamic access to USB device communication exports in `WinUsb.dll` | Advanced/native interop callers |
| `Psapi` | Static class | Dynamic access to process and module inspection exports in `Psapi.dll` | Advanced/native interop callers |
| `Pdh` | Static class | Dynamic access to performance data helper exports in `Pdh.dll` | Advanced/native interop callers |
| `Ntdll` | Static class | Dynamic access to low-level NT exports in `Ntdll.dll` | Advanced/native interop callers |
| `DbgHelp` | Static class | Dynamic access to symbol and stack walking exports in `DbgHelp.dll` | Advanced/native interop callers |
| `IWindowHandleService` | Interface | App-facing window handle operations | Application code |
| `User32WindowHandleService` | Concrete service | `IWindowHandleService` implementation backed by `User32` | Direct construction or DI registration |
| `IScreenCaptureService` | Interface | App-facing screenshot capture for monitors, coordinate bounds, and windows | Application code |
| `User32Gdi32ScreenCaptureService` | Concrete service | `IScreenCaptureService` implementation backed by `User32` and `Gdi32` | Direct construction or DI registration |
| `ScreenCapture` | Class | Disposable screenshot result containing a `Bitmap`, source bounds, and selected monitor metadata | Application code |
| `ScreenCaptureBounds` | Record struct | Virtual-screen capture rectangle using `X1`, `Y1`, `X2`, and `Y2` edges | Application code |
| `ScreenCaptureMonitor` | Record | Monitor snapshot metadata including index, native handle, bounds, work area, and primary-display flag | Application code |
| `WindowsShellServiceCollectionExtensions` | Static class | Dependency injection registration | Application startup/composition root |

## Design rules for users and AI agents

Use these rules when adding features or consuming the library:

1. Keep native DLL wrappers such as `User32`, `Gdi32`, `Kernel32`, `Shell32`, `Ws2_32`, `WinInet`, `WinHttp`, `Dnsapi`, `Iphlpapi`, `Wlanapi`, `Advapi32`, `Crypt32`, `BCrypt`, `NCrypt`, `Secur32`, `Winscard`, `SCardDlg`, `SetupApi`, `CfgMgr32`, `Hid`, `WinUsb`, `Psapi`, `Pdh`, `Ntdll`, and `DbgHelp` static.
2. Do not create broad interfaces like `IUser32`, `IGdi32`, `IKernel32`, `IShell32`, or `IWinHttp`.
3. Add small capability-based interfaces for app-facing behavior.
4. Prefer dependency injection for application code.
5. Use the static wrappers directly only when you need low-level export discovery or a function that does not yet have a service abstraction.
6. Bind native functions with delegates that exactly match the Win32 signature.
7. Include `A`/`W` suffixes for exports that have ANSI and Unicode variants, such as `GetWindowTextW`.
8. Treat native handles carefully. Only release or destroy handles that the API contract says you own.

Good service names describe capabilities:

- `IWindowHandleService`
- `IWindowEnumerationService`
- `IWindowPlacementService`
- `IClipboardService`
- `IScreenCaptureService`
- `IConsoleService`

Avoid service names that mirror DLL names:

- `IUser32`
- `IGdi32`
- `IKernel32`
- `IShell32`

## Static wrapper model

`User32`, `Gdi32`, `Kernel32`, `Shell32`, `Ws2_32`, `WinInet`, `WinHttp`, `Dnsapi`, `Iphlpapi`, `Wlanapi`, `Advapi32`, `Crypt32`, `BCrypt`, `NCrypt`, `Secur32`, `Winscard`, `SCardDlg`, `SetupApi`, `CfgMgr32`, `Hid`, `WinUsb`, `Psapi`, `Pdh`, `Ntdll`, and `DbgHelp` all follow the same pattern.

Each wrapper exposes:

| Member | Description |
|---|---|
| `LibraryName` | Canonical DLL name. |
| `ModuleHandle` | Loaded native module handle. |
| `ModulePath` | Full path to the loaded system DLL. |
| `Exports` | Metadata for every export available on the current machine. |
| `ExportNames` | Sorted names for named exports. |
| `TryGetExport(string, out nint)` | Resolve a named export to a native function pointer. |
| `GetExport(string)` | Resolve a named export or throw. |
| `TryGetExport(int, out nint)` | Resolve an ordinal export to a native function pointer. |
| `GetExport(int)` | Resolve an ordinal export or throw. |
| `TryGetFunction<TDelegate>(...)` | Resolve an export and convert it to a managed delegate. |
| `GetFunction<TDelegate>(...)` | Resolve an export to a managed delegate or throw. |

The wrappers load DLLs from the Windows system directory and parse the portable executable export table so callers can discover the exports available on the current OS build. The networking, device, security, cryptography, SSPI, and smart-card wrappers are intentionally low-level; prefer `System.Net`, `System.Net.Http`, `System.Security.Cryptography`, `System.Security.Cryptography.X509Certificates`, built-in .NET abstractions, and vendor SDKs unless you specifically need a Windows-native export.

## Quick start with dependency injection

Register app-facing services at startup:

```csharp
using Microsoft.Extensions.DependencyInjection;
using TripleG3.Windows.Shell;

var services = new ServiceCollection()
    .AddTripleG3WindowsShell()
    .BuildServiceProvider();

var windows = services.GetRequiredService<IWindowHandleService>();
var screenCapture = services.GetRequiredService<IScreenCaptureService>();

nint desktopWindow = windows.GetDesktopWindow();
nint foregroundWindow = windows.GetForegroundWindow();
bool isForegroundWindow = foregroundWindow != nint.Zero && windows.IsWindow(foregroundWindow);

using ScreenCapture desktopCapture = screenCapture.CaptureAllMonitors();
desktopCapture.SavePng("desktop.png");
```

Prefer this model when writing application logic because it is easy to replace, mock, and test.

### Capture screens, monitors, bounds, and windows

Use `IScreenCaptureService` for app-facing screenshot operations. Monitor bounds use Windows virtual-screen coordinates, so monitors positioned to the left or above the primary display can have negative `X1` or `Y1` values. `X2` and `Y2` are exclusive edges, which makes the captured size `X2 - X1` by `Y2 - Y1`.

```csharp
using Microsoft.Extensions.DependencyInjection;
using TripleG3.Windows.Shell;

using var provider = new ServiceCollection()
    .AddTripleG3WindowsShell()
    .BuildServiceProvider();

var capture = provider.GetRequiredService<IScreenCaptureService>();

IReadOnlyList<ScreenCaptureMonitor> monitors = capture.GetMonitors();

// Capture every monitor as one virtual-desktop image.
using ScreenCapture allMonitors = capture.CaptureAllMonitors();
allMonitors.SavePng("all-monitors.png");

// Capture one monitor by the stable index returned in this monitor snapshot.
using ScreenCapture primaryMonitor = capture.CaptureMonitor(0);
primaryMonitor.SavePng("primary-monitor.png");

// Capture multiple selected monitors as the union of their virtual-screen bounds.
if (monitors.Count > 1)
{
    using ScreenCapture selectedMonitors = capture.CaptureMonitors([0, 1]);
    selectedMonitors.SavePng("selected-monitors.png");
}

// Capture a coordinate region using X1, Y1, X2, Y2.
using ScreenCapture region = capture.CaptureBounds(100, 100, 900, 700);
region.SavePng("region.png");

// Capture a specific window.
var windows = provider.GetRequiredService<IWindowHandleService>();
nint foregroundWindow = windows.GetForegroundWindow();
if (foregroundWindow != nint.Zero)
{
    using ScreenCapture window = capture.CaptureWindow(foregroundWindow);
    window.SavePng("foreground-window.png");
}
```

`CaptureMonitors(...)` preserves the selected monitors' virtual-desktop layout by capturing the union rectangle around them. If selected monitors are separated by a gap, the output image includes the gap exactly as Windows represents it in the virtual desktop.

`CaptureWindow(...)` first attempts `PrintWindow` with full-content rendering and falls back to capturing the window's current screen bounds if the window does not render through `PrintWindow`. Windows may still block or omit protected surfaces, secure desktop content, minimized windows, DRM/video overlays, or content from apps that do not render through normal GDI/window-capture paths.

## Quick start with static wrappers

Use the static wrappers when you need raw Win32 access.

### List available exports

```csharp
using TripleG3.Windows.Shell;

foreach (var exportName in User32.ExportNames.Take(20))
{
    Console.WriteLine(exportName);
}
```

The same model works for `Gdi32.ExportNames`, `Kernel32.ExportNames`, `Shell32.ExportNames`, networking wrappers such as `Ws2_32.ExportNames`, `WinHttp.ExportNames`, or `Iphlpapi.ExportNames`, security and cryptography wrappers such as `Advapi32.ExportNames`, `Crypt32.ExportNames`, `BCrypt.ExportNames`, `NCrypt.ExportNames`, or `Secur32.ExportNames`, smart-card wrappers such as `Winscard.ExportNames` or `SCardDlg.ExportNames`, device wrappers such as `SetupApi.ExportNames`, `CfgMgr32.ExportNames`, `Hid.ExportNames`, or `WinUsb.ExportNames`, and system diagnostics wrappers such as `Psapi.ExportNames`, `Pdh.ExportNames`, `Ntdll.ExportNames`, or `DbgHelp.ExportNames`.

### Resolve a native function pointer

```csharp
using TripleG3.Windows.Shell;

if (User32.TryGetExport("GetDesktopWindow", out var address))
{
    Console.WriteLine($"GetDesktopWindow: 0x{address:X}");
}
```

### Bind and call a `user32.dll` function

```csharp
using System.Runtime.InteropServices;
using TripleG3.Windows.Shell;

var getDesktopWindow = User32.GetFunction<GetDesktopWindowDelegate>("GetDesktopWindow");
nint desktopWindow = getDesktopWindow();

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
delegate nint GetDesktopWindowDelegate();
```

### Bind and call a `gdi32.dll` function

```csharp
using System.Runtime.InteropServices;
using TripleG3.Windows.Shell;

const int BlackBrush = 4;

var getStockObject = Gdi32.GetFunction<GetStockObjectDelegate>("GetStockObject");
nint blackBrush = getStockObject(BlackBrush);

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
delegate nint GetStockObjectDelegate(int objectIndex);
```

`GetStockObject` returns a shared stock object handle. Do not delete stock object handles.

### Bind and call a `kernel32.dll` function

```csharp
using System.Runtime.InteropServices;
using TripleG3.Windows.Shell;

var getCurrentProcess = Kernel32.GetFunction<GetCurrentProcessDelegate>("GetCurrentProcess");
nint currentProcessPseudoHandle = getCurrentProcess();

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
delegate nint GetCurrentProcessDelegate();
```

`GetCurrentProcess` returns a pseudo-handle. Do not close it.

### Bind and call a `shell32.dll` function

```csharp
using System.Runtime.InteropServices;
using TripleG3.Windows.Shell;

var isUserAnAdmin = Shell32.GetFunction<IsUserAnAdminDelegate>("IsUserAnAdmin");
bool runningAsAdmin = isUserAnAdmin();

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
[return: MarshalAs(UnmanagedType.Bool)]
delegate bool IsUserAnAdminDelegate();
```

### Bind and call a networking DLL function

```csharp
using System.Runtime.InteropServices;
using TripleG3.Windows.Shell;

var wsaGetLastError = Ws2_32.GetFunction<WSAGetLastErrorDelegate>("WSAGetLastError");
int lastWinsockError = wsaGetLastError();

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
delegate int WSAGetLastErrorDelegate();
```

Use the same model for:

- `WinInet` (`InternetOpenW`, `InternetReadFile`, `InternetCloseHandle`) when interactive client internet APIs are required.
- `WinHttp` (`WinHttpOpen`, `WinHttpSendRequest`, `WinHttpCloseHandle`) for service-friendly HTTP APIs.
- `Dnsapi` (`DnsQuery_W`, `DnsFree`, `DnsRecordListFree`) for DNS APIs.
- `Iphlpapi` (`GetAdaptersAddresses`, `GetIfTable`, `GetIpForwardTable`) for adapter and routing APIs.
- `Wlanapi` (`WlanOpenHandle`, `WlanEnumInterfaces`, `WlanCloseHandle`) for Wi-Fi APIs.
- `Advapi32` (`RegOpenKeyExW`, `OpenSCManagerW`, `RegisterEventSourceW`) for registry, service control, security descriptor, LSA, and event logging APIs.
- `Crypt32` (`CertOpenStore`, `CertCloseStore`, `CryptStringToBinaryW`) for certificates, certificate stores, and encoding/decoding APIs.
- `BCrypt` (`BCryptOpenAlgorithmProvider`, `BCryptGenRandom`, `BCryptCloseAlgorithmProvider`) for CNG primitive algorithms such as hashing, AES, RSA, and random bytes.
- `NCrypt` (`NCryptOpenStorageProvider`, `NCryptOpenKey`, `NCryptFreeObject`) for CNG key storage providers and hardware-backed keys.
- `Secur32` (`AcquireCredentialsHandleW`, `InitializeSecurityContextW`, `FreeCredentialsHandle`) for SSPI authentication APIs such as Kerberos, NTLM, Negotiate, and Schannel.
- `Winscard` (`SCardEstablishContext`, `SCardListReadersW`, `SCardReleaseContext`) for smart card resource manager APIs.
- `SCardDlg` (`SCardUIDlgSelectCardW`, `GetOpenCardNameW`) for smart card selection dialog APIs.
- `SetupApi` (`SetupDiGetClassDevsW`, `SetupDiEnumDeviceInfo`, `SetupDiDestroyDeviceInfoList`) for device installation and hardware enumeration APIs.
- `CfgMgr32` (`CM_Get_Child`, `CM_Get_Sibling`, `CM_Get_Device_IDW`) for configuration manager device tree APIs.
- `Hid` (`HidD_GetHidGuid`, `HidD_GetAttributes`, `HidP_GetCaps`) for HID devices such as gamepads and sensors.
- `WinUsb` (`WinUsb_Initialize`, `WinUsb_ReadPipe`, `WinUsb_Free`) for USB device communication APIs.
- `Psapi` (`EnumProcesses`, `GetProcessMemoryInfo`, `GetModuleFileNameExW`) for process and module inspection APIs.
- `Pdh` (`PdhOpenQueryW`, `PdhCollectQueryData`, `PdhCloseQuery`) for performance counter query APIs.
- `Ntdll` (`NtQueryInformationProcess`, `RtlNtStatusToDosError`, `RtlGetVersion`) for low-level NT runtime APIs.
- `DbgHelp` (`SymInitialize`, `StackWalk64`, `SymCleanup`) for symbol and stack walking APIs.

Always follow the Windows SDK contract for initialization and cleanup. For example, Winsock APIs that require a session should be used after `WSAStartup` and paired with `WSACleanup`, handles returned by WinInet, WinHTTP, WLAN, Advapi32, Crypt32, BCrypt, NCrypt, Secur32, Winscard, SetupAPI, and WinUSB APIs must be closed with the matching native close/free function, and HID preparsed data must be released according to the HID API contract.

### Probe security, crypto, SSPI, or smart-card exports safely

For sensitive or hardware-backed APIs, separate export discovery from invocation. This lets humans and AI agents decide whether an OS build supports a function without opening registries, services, credentials, providers, smart cards, or dialogs.

```csharp
using TripleG3.Windows.Shell;

if (BCrypt.TryGetExport("BCryptGenRandom", out var genRandomAddress))
{
    Console.WriteLine($"BCryptGenRandom is available at 0x{genRandomAddress:X}.");
}

if (Secur32.TryGetExport("AcquireCredentialsHandleW", out _))
{
    Console.WriteLine("SSPI credential acquisition is available on this OS build.");
}
```

When you do bind one of these exports, define the delegate from the Windows SDK signature and keep ownership rules next to the call site:

```csharp
using System.Runtime.InteropServices;
using TripleG3.Windows.Shell;

var nCryptFreeObject = NCrypt.GetFunction<NCryptFreeObjectDelegate>("NCryptFreeObject");

// Only call this with a valid NCRYPT_HANDLE that your code owns.
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
delegate int NCryptFreeObjectDelegate(nint objectHandle);
```

For AI agents and automation, use this workflow for sensitive native APIs:

1. Prefer a managed .NET API first, such as `RegistryKey`, `ServiceController`, `X509Store`, `RandomNumberGenerator`, `RSA`, `NegotiateStream`, or `SslStream`.
2. Use the static wrapper only when a specific native export is required.
3. Probe with `ExportNames` or `TryGetExport` before binding OS-version-specific functions.
4. Do not call APIs that mutate registry keys, services, security descriptors, LSA state, credentials, key stores, certificates, smart cards, or UI dialogs unless the user explicitly requested that operation.
5. Keep tests for these APIs metadata-only or ignored/manual unless the side effects are fully mocked or isolated.

### Bind and call a device or USB DLL function

Device wrappers use the same delegate-binding API. This example reads the system HID class GUID without opening a device handle:

```csharp
using System.Runtime.InteropServices;
using TripleG3.Windows.Shell;

var hidDGetHidGuid = Hid.GetFunction<HidDGetHidGuidDelegate>("HidD_GetHidGuid");
hidDGetHidGuid(out var hidClassGuid);

Console.WriteLine(hidClassGuid);

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
delegate void HidDGetHidGuidDelegate(out Guid hidGuid);
```

For AI agents and automation, use this workflow:

1. Pick the wrapper that owns the DLL export (`SetupApi`, `CfgMgr32`, `Hid`, or `WinUsb`).
2. Check `ExportNames` or `TryGetExport` for OS-version-specific functions before binding.
3. Define a private delegate that exactly matches the Windows SDK signature.
4. Bind with `GetFunction<TDelegate>` only after confirming handle ownership, buffer lifetime, character set, and cleanup rules.
5. Prefer adding a small app-facing service if application code needs a safe reusable operation instead of raw native access.

## Delegate binding checklist

When using `GetFunction<TDelegate>` or `TryGetFunction<TDelegate>`, the delegate must match the native signature exactly.

Check these items before calling a native function:

- Calling convention: usually `CallingConvention.Winapi`.
- Character set: use the exact `A` or `W` export when applicable.
- Boolean marshalling: Win32 `BOOL` should usually use `[return: MarshalAs(UnmanagedType.Bool)]`.
- Handle ownership: know whether the returned handle must be released.
- Last error behavior: if a native function sets last error, design the delegate and caller accordingly.
- Pointer-sized values: use `nint`/`nuint` for handles and pointer-sized values.

Example for a Win32 `BOOL` return:

```csharp
using System.Runtime.InteropServices;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
[return: MarshalAs(UnmanagedType.Bool)]
delegate bool IsWindowDelegate(nint windowHandle);
```

## App-facing services

The app-facing layer should stay focused and capability-based.

The current services are `IWindowHandleService` and `IScreenCaptureService`:

```csharp
using TripleG3.Windows.Shell;

public sealed class WindowReporter(IWindowHandleService windows, IScreenCaptureService capture)
{
    public bool HasForegroundWindow()
    {
        var handle = windows.GetForegroundWindow();
        return handle != nint.Zero && windows.IsWindow(handle);
    }

    public void SaveDesktopScreenshot(string filePath)
    {
        using var screenshot = capture.CaptureAllMonitors();
        screenshot.SavePng(filePath);
    }
}
```

Future services should compose raw exports into safe operations. For example:

- A window enumeration service can compose `EnumWindows`, `GetWindowTextW`, `GetClassNameW`, and `IsWindowVisible`.
- A clipboard service can hide the required `OpenClipboard`/`CloseClipboard` lifetime rules.

## Error handling

The static wrappers use two styles:

- `TryGet...` methods return `false` when an export cannot be resolved.
- `Get...` methods throw when an export cannot be resolved.

Use `TryGet...` when probing for OS-version-specific exports. Use `Get...` when the function is required for your code path.

## Testing

Run tests from the repository root:

```powershell
dotnet test
```

Tests are Windows-only and validate:

- Export discovery for `user32.dll`, `gdi32.dll`, `kernel32.dll`, `shell32.dll`, `Ws2_32.dll`, `WinInet.dll`, `WinHttp.dll`, `Dnsapi.dll`, `Iphlpapi.dll`, `Wlanapi.dll`, `Advapi32.dll`, `Crypt32.dll`, `BCrypt.dll`, `NCrypt.dll`, `Secur32.dll`, `Winscard.dll`, `SCardDlg.dll`, `SetupAPI.dll`, `CfgMgr32.dll`, `Hid.dll`, `WinUsb.dll`, `Psapi.dll`, `Pdh.dll`, `Ntdll.dll`, and `DbgHelp.dll`.
- Named and ordinal export resolution.
- Safe delegate binding for known stable APIs.
- Screen capture model validation, PNG saving, and dependency injection registration for app-facing services.

Native export validation tests that could touch Windows services, security packages, crypto providers, smart card readers, dialogs, or other machine state are marked with `Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)`. Do not enable those tests in automated runs unless the environment is intentionally prepared for native validation.

## Repository layout

```text
src/
  TripleG3.Windows.Shell/
    User32.cs
    Gdi32.cs
    Kernel32.cs
    Shell32.cs
    Ws2_32.cs
    WinInet.cs
    WinHttp.cs
    Dnsapi.cs
    Iphlpapi.cs
    Wlanapi.cs
    Advapi32.cs
    Crypt32.cs
    BCrypt.cs
    NCrypt.cs
    Secur32.cs
    Winscard.cs
    SCardDlg.cs
    SetupApi.cs
    CfgMgr32.cs
    Hid.cs
    WinUsb.cs
    Psapi.cs
    Pdh.cs
    Ntdll.cs
    DbgHelp.cs
    NativeModule.cs
    Services/
      IWindowHandleService.cs
      User32WindowHandleService.cs
    WindowsShellServiceCollectionExtensions.cs
  TripleG3.Windows.Shell.Tests/
```

## Contribution guidance

When adding a new capability:

1. Decide whether it is raw native access or app-facing behavior.
2. Raw native access belongs in the static wrapper layer.
3. App-facing behavior belongs behind a small interface.
4. Implement app-facing behavior by binding delegates from the relevant static wrappers.
5. Register app-facing services in `AddTripleG3WindowsShell`.
6. Add tests for export resolution, service behavior, and DI registration.

When in doubt, keep the native boundary static and make the consuming workflow injectable.
