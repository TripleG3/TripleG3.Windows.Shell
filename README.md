# TripleG3.Windows.Shell

`TripleG3.Windows.Shell` is a Windows-only .NET 10 library for working with core Win32 shell, windowing, graphics, and process APIs from managed code.

The library exposes two layers:

- **Static native DLL wrappers** for `user32.dll`, `gdi32.dll`, `kernel32.dll`, and common Windows networking DLLs.
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
| `Ws2_32` | Static class | Dynamic access to Winsock TCP/UDP exports in `Ws2_32.dll` | Advanced/native interop callers |
| `WinInet` | Static class | Dynamic access to high-level HTTP/FTP exports in `WinInet.dll` | Advanced/native interop callers |
| `WinHttp` | Static class | Dynamic access to service-friendly HTTP exports in `WinHttp.dll` | Advanced/native interop callers |
| `Dnsapi` | Static class | Dynamic access to DNS exports in `Dnsapi.dll` | Advanced/native interop callers |
| `Iphlpapi` | Static class | Dynamic access to network adapter, routing, and IP helper exports in `Iphlpapi.dll` | Advanced/native interop callers |
| `Wlanapi` | Static class | Dynamic access to Wi-Fi management exports in `Wlanapi.dll` | Advanced/native interop callers |
| `IWindowHandleService` | Interface | App-facing window handle operations | Application code |
| `User32WindowHandleService` | Concrete service | `IWindowHandleService` implementation backed by `User32` | Direct construction or DI registration |
| `WindowsShellServiceCollectionExtensions` | Static class | Dependency injection registration | Application startup/composition root |

## Design rules for users and AI agents

Use these rules when adding features or consuming the library:

1. Keep native DLL wrappers such as `User32`, `Gdi32`, `Kernel32`, `Ws2_32`, `WinInet`, `WinHttp`, `Dnsapi`, `Iphlpapi`, and `Wlanapi` static.
2. Do not create broad interfaces like `IUser32`, `IGdi32`, `IKernel32`, or `IWinHttp`.
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

## Static wrapper model

`User32`, `Gdi32`, `Kernel32`, `Ws2_32`, `WinInet`, `WinHttp`, `Dnsapi`, `Iphlpapi`, and `Wlanapi` all follow the same pattern.

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

The wrappers load DLLs from the Windows system directory and parse the portable executable export table so callers can discover the exports available on the current OS build. The networking wrappers are intentionally low-level; prefer `System.Net`, `System.Net.Http`, and other managed APIs unless you specifically need a Windows-native export.

## Quick start with dependency injection

Register app-facing services at startup:

```csharp
using Microsoft.Extensions.DependencyInjection;
using TripleG3.Windows.Shell;

var services = new ServiceCollection()
    .AddTripleG3WindowsShell()
    .BuildServiceProvider();

var windows = services.GetRequiredService<IWindowHandleService>();

nint desktopWindow = windows.GetDesktopWindow();
nint foregroundWindow = windows.GetForegroundWindow();
bool isForegroundWindow = foregroundWindow != nint.Zero && windows.IsWindow(foregroundWindow);
```

Prefer this model when writing application logic because it is easy to replace, mock, and test.

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

The same model works for `Gdi32.ExportNames`, `Kernel32.ExportNames`, and the networking wrappers such as `Ws2_32.ExportNames`, `WinHttp.ExportNames`, or `Iphlpapi.ExportNames`.

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

Always follow the Windows SDK contract for initialization and cleanup. For example, Winsock APIs that require a session should be used after `WSAStartup` and paired with `WSACleanup`, and handles returned by WinInet, WinHTTP, and WLAN APIs must be closed with the matching native close function.

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

The current service is `IWindowHandleService`:

```csharp
using TripleG3.Windows.Shell;

public sealed class WindowReporter(IWindowHandleService windows)
{
    public bool HasForegroundWindow()
    {
        var handle = windows.GetForegroundWindow();
        return handle != nint.Zero && windows.IsWindow(handle);
    }
}
```

Future services should compose raw exports into safe operations. For example:

- A window enumeration service can compose `EnumWindows`, `GetWindowTextW`, `GetClassNameW`, and `IsWindowVisible`.
- A screen capture service can compose `User32` device-context calls with `Gdi32` bitmap calls.
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

- Export discovery for `user32.dll`, `gdi32.dll`, `kernel32.dll`, `Ws2_32.dll`, `WinInet.dll`, `WinHttp.dll`, `Dnsapi.dll`, `Iphlpapi.dll`, and `Wlanapi.dll`.
- Named and ordinal export resolution.
- Safe delegate binding for known stable APIs.
- Dependency injection registration for app-facing services.

## Repository layout

```text
src/
  TripleG3.Windows.Shell/
    User32.cs
    Gdi32.cs
    Kernel32.cs
    Ws2_32.cs
    WinInet.cs
    WinHttp.cs
    Dnsapi.cs
    Iphlpapi.cs
    Wlanapi.cs
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
4. Implement app-facing behavior by binding delegates from `User32`, `Gdi32`, or `Kernel32`.
5. Register app-facing services in `AddTripleG3WindowsShell`.
6. Add tests for export resolution, service behavior, and DI registration.

When in doubt, keep the native boundary static and make the consuming workflow injectable.
