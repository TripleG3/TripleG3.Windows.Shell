using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class SystemNativeWrapperTests
{
    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Psapi_ExposesProcessAndModuleInspectionExports()
    {
        AssertKnownWrapperState(Psapi.LibraryName, () => Psapi.ModuleHandle, () => Psapi.ModulePath, () => Psapi.ExportNames,
            "EnumProcesses", "GetProcessMemoryInfo", "GetModuleFileNameExW");
        Assert.IsNotNull(Psapi.GetFunction<EnumProcessesDelegate>("EnumProcesses"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Pdh_ExposesPerformanceDataHelperExports()
    {
        AssertKnownWrapperState(Pdh.LibraryName, () => Pdh.ModuleHandle, () => Pdh.ModulePath, () => Pdh.ExportNames,
            "PdhOpenQueryW", "PdhCollectQueryData", "PdhCloseQuery");
        Assert.IsNotNull(Pdh.GetFunction<PdhCloseQueryDelegate>("PdhCloseQuery"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Ntdll_ExposesNtRuntimeExports()
    {
        AssertKnownWrapperState(Ntdll.LibraryName, () => Ntdll.ModuleHandle, () => Ntdll.ModulePath, () => Ntdll.ExportNames,
            "NtQueryInformationProcess", "RtlNtStatusToDosError", "RtlGetVersion");
        Assert.IsNotNull(Ntdll.GetFunction<RtlNtStatusToDosErrorDelegate>("RtlNtStatusToDosError"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void DbgHelp_ExposesDebugHelperExports()
    {
        AssertKnownWrapperState(DbgHelp.LibraryName, () => DbgHelp.ModuleHandle, () => DbgHelp.ModulePath, () => DbgHelp.ExportNames,
            "SymInitialize", "SymCleanup", "StackWalk64");
        Assert.IsNotNull(DbgHelp.GetFunction<SymCleanupDelegate>("SymCleanup"));
    }

    [TestMethod]
    public void SystemNativeExportTypes_ReportStableDisplayValues()
    {
        var psapiExport = new Psapi.Export(null, 72, 123, null);
        var pdhExport = new Pdh.Export("Pdh", 73, 456, "example.Target");
        var ntdllExport = new Ntdll.Export("Nt", 74, 789, null);
        var dbgHelpExport = new DbgHelp.Export(null, 75, 123, "example.Target");

        Assert.AreEqual("#72", psapiExport.NameOrOrdinal);
        Assert.IsFalse(psapiExport.IsNamed);
        Assert.IsFalse(psapiExport.IsForwarded);
        Assert.AreEqual("Pdh", pdhExport.NameOrOrdinal);
        Assert.IsTrue(pdhExport.IsNamed);
        Assert.IsTrue(pdhExport.IsForwarded);
        Assert.AreEqual("Nt", ntdllExport.NameOrOrdinal);
        Assert.IsTrue(ntdllExport.IsNamed);
        Assert.IsFalse(ntdllExport.IsForwarded);
        Assert.AreEqual("#75", dbgHelpExport.NameOrOrdinal);
        Assert.IsFalse(dbgHelpExport.IsNamed);
        Assert.IsTrue(dbgHelpExport.IsForwarded);
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
    private delegate bool EnumProcessesDelegate(nint processIds, uint cb, out uint bytesReturned);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int PdhCloseQueryDelegate(nint query);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate uint RtlNtStatusToDosErrorDelegate(int status);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool SymCleanupDelegate(nint process);
}
