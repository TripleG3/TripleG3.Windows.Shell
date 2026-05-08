using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class SecurityNativeWrapperTests
{
    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Advapi32_ExposesRegistryServiceSecurityAndEventLogExports()
    {
        AssertKnownWrapperState(Advapi32.LibraryName, () => Advapi32.ModuleHandle, () => Advapi32.ModulePath, () => Advapi32.ExportNames,
            "RegOpenKeyExW", "OpenSCManagerW", "RegisterEventSourceW");
        Assert.IsNotNull(Advapi32.GetFunction<RegCloseKeyDelegate>("RegCloseKey"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Crypt32_ExposesCertificateStoreAndEncodingExports()
    {
        AssertKnownWrapperState(Crypt32.LibraryName, () => Crypt32.ModuleHandle, () => Crypt32.ModulePath, () => Crypt32.ExportNames,
            "CertOpenStore", "CertCloseStore", "CryptStringToBinaryW");
        Assert.IsNotNull(Crypt32.GetFunction<CertCloseStoreDelegate>("CertCloseStore"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void BCrypt_ExposesCngPrimitiveExports()
    {
        AssertKnownWrapperState(BCrypt.LibraryName, () => BCrypt.ModuleHandle, () => BCrypt.ModulePath, () => BCrypt.ExportNames,
            "BCryptOpenAlgorithmProvider", "BCryptCloseAlgorithmProvider", "BCryptGenRandom");
        Assert.IsNotNull(BCrypt.GetFunction<BCryptCloseAlgorithmProviderDelegate>("BCryptCloseAlgorithmProvider"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void NCrypt_ExposesCngKeyStorageExports()
    {
        AssertKnownWrapperState(NCrypt.LibraryName, () => NCrypt.ModuleHandle, () => NCrypt.ModulePath, () => NCrypt.ExportNames,
            "NCryptOpenStorageProvider", "NCryptOpenKey", "NCryptFreeObject");
        Assert.IsNotNull(NCrypt.GetFunction<NCryptFreeObjectDelegate>("NCryptFreeObject"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Secur32_ExposesSspiAuthenticationExports()
    {
        AssertKnownWrapperState(Secur32.LibraryName, () => Secur32.ModuleHandle, () => Secur32.ModulePath, () => Secur32.ExportNames,
            "AcquireCredentialsHandleW", "InitializeSecurityContextW", "FreeCredentialsHandle");
        Assert.IsNotNull(Secur32.GetFunction<FreeCredentialsHandleDelegate>("FreeCredentialsHandle"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Winscard_ExposesSmartCardResourceManagerExports()
    {
        AssertKnownWrapperState(Winscard.LibraryName, () => Winscard.ModuleHandle, () => Winscard.ModulePath, () => Winscard.ExportNames,
            "SCardEstablishContext", "SCardListReadersW", "SCardReleaseContext");
        Assert.IsNotNull(Winscard.GetFunction<SCardReleaseContextDelegate>("SCardReleaseContext"));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void SCardDlg_ExposesSmartCardDialogExports()
    {
        AssertKnownWrapperState(SCardDlg.LibraryName, () => SCardDlg.ModuleHandle, () => SCardDlg.ModulePath, () => SCardDlg.ExportNames,
            "SCardUIDlgSelectCardW", "GetOpenCardNameW");
        Assert.IsNotNull(SCardDlg.GetFunction<SCardUIDlgSelectCardDelegate>("SCardUIDlgSelectCardW"));
    }

    [TestMethod]
    public void SecurityAndCryptoExportTypes_ReportStableDisplayValues()
    {
        var advapi32Export = new Advapi32.Export(null, 62, 123, null);
        var crypt32Export = new Crypt32.Export("Crypt", 63, 456, "example.Target");
        var bcryptExport = new BCrypt.Export("BCrypt", 64, 789, null);
        var ncryptExport = new NCrypt.Export(null, 65, 123, "example.Target");
        var secur32Export = new Secur32.Export("Sspi", 66, 456, null);
        var winscardExport = new Winscard.Export(null, 67, 789, null);
        var sCardDlgExport = new SCardDlg.Export("SmartCardDialog", 68, 123, "example.Target");

        Assert.AreEqual("#62", advapi32Export.NameOrOrdinal);
        Assert.IsFalse(advapi32Export.IsNamed);
        Assert.IsFalse(advapi32Export.IsForwarded);
        Assert.AreEqual("Crypt", crypt32Export.NameOrOrdinal);
        Assert.IsTrue(crypt32Export.IsNamed);
        Assert.IsTrue(crypt32Export.IsForwarded);
        Assert.AreEqual("BCrypt", bcryptExport.NameOrOrdinal);
        Assert.IsTrue(bcryptExport.IsNamed);
        Assert.IsFalse(bcryptExport.IsForwarded);
        Assert.AreEqual("#65", ncryptExport.NameOrOrdinal);
        Assert.IsFalse(ncryptExport.IsNamed);
        Assert.IsTrue(ncryptExport.IsForwarded);
        Assert.AreEqual("Sspi", secur32Export.NameOrOrdinal);
        Assert.IsTrue(secur32Export.IsNamed);
        Assert.IsFalse(secur32Export.IsForwarded);
        Assert.AreEqual("#67", winscardExport.NameOrOrdinal);
        Assert.IsFalse(winscardExport.IsNamed);
        Assert.IsFalse(winscardExport.IsForwarded);
        Assert.AreEqual("SmartCardDialog", sCardDlgExport.NameOrOrdinal);
        Assert.IsTrue(sCardDlgExport.IsNamed);
        Assert.IsTrue(sCardDlgExport.IsForwarded);
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
    private delegate int RegCloseKeyDelegate(nint keyHandle);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool CertCloseStoreDelegate(nint certificateStoreHandle, uint flags);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int BCryptCloseAlgorithmProviderDelegate(nint algorithmHandle, uint flags);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int NCryptFreeObjectDelegate(nint objectHandle);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int FreeCredentialsHandleDelegate(nint credentialHandle);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int SCardReleaseContextDelegate(nint context);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    private delegate int SCardUIDlgSelectCardDelegate(nint openCardNameEx);
}