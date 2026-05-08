using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class NetworkNativeWrapperTests
{
    [TestMethod]
    public void Ws2_32_ExposesWinsockExports()
    {
        AssertKnownWrapperState(Ws2_32.LibraryName, () => Ws2_32.ModuleHandle, () => Ws2_32.ModulePath, () => Ws2_32.ExportNames,
            "WSAStartup", "WSACleanup", "socket");
        Assert.IsNotNull(Ws2_32.GetFunction<WSAGetLastErrorDelegate>("WSAGetLastError"));
    }

    [TestMethod]
    public void WinInet_ExposesHighLevelInternetExports()
    {
        AssertKnownWrapperState(WinInet.LibraryName, () => WinInet.ModuleHandle, () => WinInet.ModulePath, () => WinInet.ExportNames,
            "InternetOpenW", "InternetCloseHandle", "InternetReadFile");
        Assert.IsNotNull(WinInet.GetFunction<InternetGetConnectedStateDelegate>("InternetGetConnectedState"));
    }

    [TestMethod]
    public void WinHttp_ExposesHttpExports()
    {
        AssertKnownWrapperState(WinHttp.LibraryName, () => WinHttp.ModuleHandle, () => WinHttp.ModulePath, () => WinHttp.ExportNames,
            "WinHttpOpen", "WinHttpCloseHandle", "WinHttpSendRequest");
        Assert.IsNotNull(WinHttp.GetFunction<WinHttpCloseHandleDelegate>("WinHttpCloseHandle"));
    }

    [TestMethod]
    public void Dnsapi_ExposesDnsExports()
    {
        AssertKnownWrapperState(Dnsapi.LibraryName, () => Dnsapi.ModuleHandle, () => Dnsapi.ModulePath, () => Dnsapi.ExportNames,
            "DnsQuery_W", "DnsFree", "DnsRecordListFree");
        Assert.IsNotNull(Dnsapi.GetFunction<DnsFreeDelegate>("DnsFree"));
    }

    [TestMethod]
    public void Iphlpapi_ExposesIpHelperExports()
    {
        AssertKnownWrapperState(Iphlpapi.LibraryName, () => Iphlpapi.ModuleHandle, () => Iphlpapi.ModulePath, () => Iphlpapi.ExportNames,
            "GetAdaptersAddresses", "GetIfTable", "GetIpForwardTable");
        Assert.IsNotNull(Iphlpapi.GetFunction<GetNumberOfInterfacesDelegate>("GetNumberOfInterfaces"));
    }

    [TestMethod]
    public void Wlanapi_ExposesWlanExports()
    {
        AssertKnownWrapperState(Wlanapi.LibraryName, () => Wlanapi.ModuleHandle, () => Wlanapi.ModulePath, () => Wlanapi.ExportNames,
            "WlanOpenHandle", "WlanCloseHandle", "WlanEnumInterfaces");
        Assert.IsNotNull(Wlanapi.GetFunction<WlanCloseHandleDelegate>("WlanCloseHandle"));
    }

    [TestMethod]
    public void NewExportTypes_ReportStableDisplayValues()
    {
        AssertExportMetadata(new Ws2_32.Export(null, 42, 123, null), "#42", isNamed: false, isForwarded: false);
        AssertExportMetadata(new WinInet.Export("Forwarded", 43, 456, "example.Target"), "Forwarded", isNamed: true, isForwarded: true);
        AssertExportMetadata(new WinHttp.Export("Named", 44, 789, null), "Named", isNamed: true, isForwarded: false);
        AssertExportMetadata(new Dnsapi.Export(null, 45, 123, "example.Target"), "#45", isNamed: false, isForwarded: true);
        AssertExportMetadata(new Iphlpapi.Export("IpHelper", 46, 456, null), "IpHelper", isNamed: true, isForwarded: false);
        AssertExportMetadata(new Wlanapi.Export(null, 47, 789, null), "#47", isNamed: false, isForwarded: false);
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

    private static void AssertExportMetadata(dynamic export, string expectedNameOrOrdinal, bool isNamed, bool isForwarded)
    {
        Assert.AreEqual(expectedNameOrOrdinal, export.NameOrOrdinal);
        Assert.AreEqual(isNamed, export.IsNamed);
        Assert.AreEqual(isForwarded, export.IsForwarded);
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
