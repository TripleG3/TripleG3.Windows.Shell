using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class NetworkNativeWrapperTests
{
    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Ws2_32_ExposesWinsockExports()
    {
        AssertKnownWrapperState(Ws2_32.LibraryName, () => Ws2_32.ModuleHandle, () => Ws2_32.ModulePath, () => Ws2_32.ExportNames,
            "WSAStartup", "WSACleanup", "socket");
        Assert.IsNotNull(Ws2_32.GetFunction<WSAGetLastErrorDelegate>("WSAGetLastError"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void WinInet_ExposesHighLevelInternetExports()
    {
        AssertKnownWrapperState(WinInet.LibraryName, () => WinInet.ModuleHandle, () => WinInet.ModulePath, () => WinInet.ExportNames,
            "InternetOpenW", "InternetCloseHandle", "InternetReadFile");
        Assert.IsNotNull(WinInet.GetFunction<InternetGetConnectedStateDelegate>("InternetGetConnectedState"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void WinHttp_ExposesHttpExports()
    {
        AssertKnownWrapperState(WinHttp.LibraryName, () => WinHttp.ModuleHandle, () => WinHttp.ModulePath, () => WinHttp.ExportNames,
            "WinHttpOpen", "WinHttpCloseHandle", "WinHttpSendRequest");
        Assert.IsNotNull(WinHttp.GetFunction<WinHttpCloseHandleDelegate>("WinHttpCloseHandle"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Dnsapi_ExposesDnsExports()
    {
        AssertKnownWrapperState(Dnsapi.LibraryName, () => Dnsapi.ModuleHandle, () => Dnsapi.ModulePath, () => Dnsapi.ExportNames,
            "DnsQuery_W", "DnsFree", "DnsRecordListFree");
        Assert.IsNotNull(Dnsapi.GetFunction<DnsFreeDelegate>("DnsFree"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Iphlpapi_ExposesIpHelperExports()
    {
        AssertKnownWrapperState(Iphlpapi.LibraryName, () => Iphlpapi.ModuleHandle, () => Iphlpapi.ModulePath, () => Iphlpapi.ExportNames,
            "GetAdaptersAddresses", "GetIfTable", "GetIpForwardTable");
        Assert.IsNotNull(Iphlpapi.GetFunction<GetNumberOfInterfacesDelegate>("GetNumberOfInterfaces"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Wlanapi_ExposesWlanExports()
    {
        AssertKnownWrapperState(Wlanapi.LibraryName, () => Wlanapi.ModuleHandle, () => Wlanapi.ModulePath, () => Wlanapi.ExportNames,
            "WlanOpenHandle", "WlanCloseHandle", "WlanEnumInterfaces");
        Assert.IsNotNull(Wlanapi.GetFunction<WlanCloseHandleDelegate>("WlanCloseHandle"));
    }

    [TestMethod]
    public void NewExportTypes_ReportStableDisplayValues()
    {
        var winsockExport = new Ws2_32.Export(null, 42, 123, null);
        var winInetExport = new WinInet.Export("Forwarded", 43, 456, "example.Target");
        var winHttpExport = new WinHttp.Export("Named", 44, 789, null);
        var dnsExport = new Dnsapi.Export(null, 45, 123, "example.Target");
        var ipHelperExport = new Iphlpapi.Export("IpHelper", 46, 456, null);
        var wlanExport = new Wlanapi.Export(null, 47, 789, null);

        Assert.AreEqual("#42", winsockExport.NameOrOrdinal);
        Assert.IsFalse(winsockExport.IsNamed);
        Assert.IsFalse(winsockExport.IsForwarded);
        Assert.AreEqual("Forwarded", winInetExport.NameOrOrdinal);
        Assert.IsTrue(winInetExport.IsNamed);
        Assert.IsTrue(winInetExport.IsForwarded);
        Assert.AreEqual("Named", winHttpExport.NameOrOrdinal);
        Assert.IsTrue(winHttpExport.IsNamed);
        Assert.IsFalse(winHttpExport.IsForwarded);
        Assert.AreEqual("#45", dnsExport.NameOrOrdinal);
        Assert.IsFalse(dnsExport.IsNamed);
        Assert.IsTrue(dnsExport.IsForwarded);
        Assert.AreEqual("IpHelper", ipHelperExport.NameOrOrdinal);
        Assert.IsTrue(ipHelperExport.IsNamed);
        Assert.IsFalse(ipHelperExport.IsForwarded);
        Assert.AreEqual("#47", wlanExport.NameOrOrdinal);
        Assert.IsFalse(wlanExport.IsNamed);
        Assert.IsFalse(wlanExport.IsForwarded);
    }

    private static void AssertKnownWrapperState(
        string libraryName,
        Func<nint> getModuleHandle,
        Func<string> getModulePath,
        Func<IReadOnlyList<string>> getExportNames,
        params string[] expectedExports)
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive($"{libraryName} export resolution requires Windows.");
        }

        var moduleHandle = getModuleHandle();
        var modulePath = getModulePath();
        var exportNames = getExportNames();

        Assert.AreNotEqual(nint.Zero, moduleHandle);
        Assert.IsTrue(File.Exists(modulePath), $"Expected {modulePath} to exist.");
        Assert.AreEqual(libraryName, Path.GetFileName(modulePath), ignoreCase: true);
        Assert.IsNotEmpty(exportNames);

        foreach (var expectedExport in expectedExports)
        {
            CollectionAssert.Contains(exportNames.ToList(), expectedExport);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int WSAGetLastErrorDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool InternetGetConnectedStateDelegate(out int flags, int reserved);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool WinHttpCloseHandleDelegate(nint handle);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void DnsFreeDelegate(nint data, int freeType);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate uint GetNumberOfInterfacesDelegate(out uint numberOfInterfaces);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate uint WlanCloseHandleDelegate(nint clientHandle, nint reserved);
}
