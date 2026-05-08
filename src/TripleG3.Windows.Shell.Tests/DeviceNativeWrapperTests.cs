using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class DeviceNativeWrapperTests
{
    [TestMethod]
    public void SetupApi_ExposesDeviceInstallationExports()
    {
        AssertKnownWrapperState(SetupApi.LibraryName, () => SetupApi.ModuleHandle, () => SetupApi.ModulePath, () => SetupApi.ExportNames,
            "SetupDiGetClassDevsW", "SetupDiEnumDeviceInfo", "SetupDiDestroyDeviceInfoList");
        Assert.IsNotNull(SetupApi.GetFunction<SetupDiDestroyDeviceInfoListDelegate>("SetupDiDestroyDeviceInfoList"));
    }

    [TestMethod]
    public void CfgMgr32_ExposesConfigurationManagerExports()
    {
        AssertKnownWrapperState(CfgMgr32.LibraryName, () => CfgMgr32.ModuleHandle, () => CfgMgr32.ModulePath, () => CfgMgr32.ExportNames,
            "CM_Get_Child", "CM_Get_Sibling", "CM_Get_Device_IDW");
        Assert.IsNotNull(CfgMgr32.GetFunction<CMGetChildDelegate>("CM_Get_Child"));
    }

    [TestMethod]
    public void Hid_ExposesHumanInterfaceDeviceExports()
    {
        AssertKnownWrapperState(Hid.LibraryName, () => Hid.ModuleHandle, () => Hid.ModulePath, () => Hid.ExportNames,
            "HidD_GetHidGuid", "HidD_GetAttributes", "HidP_GetCaps");
        Assert.IsNotNull(Hid.GetFunction<HidDGetHidGuidDelegate>("HidD_GetHidGuid"));
    }

    [TestMethod]
    public void WinUsb_ExposesUsbCommunicationExports()
    {
        AssertKnownWrapperState(WinUsb.LibraryName, () => WinUsb.ModuleHandle, () => WinUsb.ModulePath, () => WinUsb.ExportNames,
            "WinUsb_Initialize", "WinUsb_Free", "WinUsb_ReadPipe");
        Assert.IsNotNull(WinUsb.GetFunction<WinUsbFreeDelegate>("WinUsb_Free"));
    }

    [TestMethod]
    public void DeviceExportTypes_ReportStableDisplayValues()
    {
        var setupApiExport = new SetupApi.Export(null, 52, 123, null);
        var cfgMgrExport = new CfgMgr32.Export("CfgMgr", 53, 456, "example.Target");
        var hidExport = new Hid.Export("Hid", 54, 789, null);
        var winUsbExport = new WinUsb.Export(null, 55, 123, "example.Target");

        Assert.AreEqual("#52", setupApiExport.NameOrOrdinal);
        Assert.IsFalse(setupApiExport.IsNamed);
        Assert.IsFalse(setupApiExport.IsForwarded);
        Assert.AreEqual("CfgMgr", cfgMgrExport.NameOrOrdinal);
        Assert.IsTrue(cfgMgrExport.IsNamed);
        Assert.IsTrue(cfgMgrExport.IsForwarded);
        Assert.AreEqual("Hid", hidExport.NameOrOrdinal);
        Assert.IsTrue(hidExport.IsNamed);
        Assert.IsFalse(hidExport.IsForwarded);
        Assert.AreEqual("#55", winUsbExport.NameOrOrdinal);
        Assert.IsFalse(winUsbExport.IsNamed);
        Assert.IsTrue(winUsbExport.IsForwarded);
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
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool SetupDiDestroyDeviceInfoListDelegate(nint deviceInfoSet);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate uint CMGetChildDelegate(out uint childDeviceInstance, uint deviceInstance, uint flags);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void HidDGetHidGuidDelegate(out Guid hidGuid);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool WinUsbFreeDelegate(nint interfaceHandle);
}
