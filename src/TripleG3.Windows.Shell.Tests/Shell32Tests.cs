using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class Shell32Tests
{
    private const string KnownExportName = "IsUserAnAdmin";

    [TestMethod]
    public void ModuleHandle_ReturnsLoadedShell32Handle()
    {
        Assert.AreNotEqual(nint.Zero, Shell32.ModuleHandle);
    }

    [TestMethod]
    public void ModulePath_PointsToLoadedShell32Dll()
    {
        Assert.IsTrue(File.Exists(Shell32.ModulePath), $"Expected {Shell32.ModulePath} to exist.");
        Assert.AreEqual(Shell32.LibraryName, Path.GetFileName(Shell32.ModulePath), ignoreCase: true);
    }

    [TestMethod]
    public void Exports_ContainsKnownNamedShell32Functions()
    {
        var exportNames = Shell32.ExportNames;

        Assert.IsNotEmpty(exportNames);
        CollectionAssert.Contains(exportNames.ToList(), KnownExportName);
        CollectionAssert.Contains(exportNames.ToList(), "ShellExecuteW");
        CollectionAssert.Contains(exportNames.ToList(), "SHGetFolderPathW");
    }

    [TestMethod]
    public void ExportNames_AreSortedAndMatchNamedExports()
    {
        var expectedNames = Shell32.Exports
            .Where(export => export.Name is not null)
            .Select(export => export.Name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(expectedNames, Shell32.ExportNames.ToArray());
    }

    [TestMethod]
    public void GetExport_KnownName_ReturnsSamePointerAsTryGetExport()
    {
        var address = Shell32.GetExport(KnownExportName);
        var found = Shell32.TryGetExport(KnownExportName, out var tryAddress);

        Assert.IsTrue(found);
        Assert.AreNotEqual(nint.Zero, address);
        Assert.AreEqual(address, tryAddress);
    }

    [TestMethod]
    public void GetExport_KnownOrdinal_ReturnsSamePointerAsNamedExport()
    {
        var export = Shell32.Exports.Single(export => export.Name == KnownExportName);
        var namedAddress = Shell32.GetExport(KnownExportName);
        var ordinalAddress = Shell32.GetExport(export.Ordinal);

        Assert.AreEqual(namedAddress, ordinalAddress);
    }

    [TestMethod]
    public void TryGetExport_MissingName_ReturnsFalseAndZeroAddress()
    {
        var found = Shell32.TryGetExport("TripleG3MissingShell32Export", out var address);

        Assert.IsFalse(found);
        Assert.AreEqual(nint.Zero, address);
    }

    [TestMethod]
    public void GetExport_MissingName_ThrowsEntryPointNotFoundException()
    {
        AssertThrows<EntryPointNotFoundException>(() => Shell32.GetExport("TripleG3MissingShell32Export"));
    }

    [TestMethod]
    public void TryGetExport_InvalidName_ThrowsArgumentException()
    {
        AssertThrows<ArgumentException>(() => Shell32.TryGetExport(" ", out _));
    }

    [TestMethod]
    public void TryGetExport_InvalidOrdinal_ThrowsArgumentOutOfRangeException()
    {
        AssertThrows<ArgumentOutOfRangeException>(() => Shell32.TryGetExport(0, out _));
    }

    [TestMethod]
    public void GetFunction_KnownName_BindsCallableDelegate()
    {
        var isUserAnAdmin = Shell32.GetFunction<IsUserAnAdminDelegate>(KnownExportName);

        _ = isUserAnAdmin();
    }

    [TestMethod]
    public void TryGetFunction_KnownOrdinal_BindsCallableDelegate()
    {
        var export = Shell32.Exports.Single(export => export.Name == KnownExportName);
        var found = Shell32.TryGetFunction<IsUserAnAdminDelegate>(export.Ordinal, out var isUserAnAdmin);

        Assert.IsTrue(found);
        Assert.IsNotNull(isUserAnAdmin);
        _ = isUserAnAdmin();
    }

    [TestMethod]
    public void TryGetFunction_MissingName_ReturnsFalseAndNullDelegate()
    {
        var found = Shell32.TryGetFunction<IsUserAnAdminDelegate>("TripleG3MissingShell32Export", out var function);

        Assert.IsFalse(found);
        Assert.IsNull(function);
    }

    [TestMethod]
    public void Export_MetadataReportsStableDisplayValues()
    {
        var namedExport = Shell32.Exports.Single(export => export.Name == KnownExportName);
        var ordinalOnlyExport = new Shell32.Export(null, 42, 123, null);
        var forwardedExport = new Shell32.Export("Forwarded", 43, 456, "example.Target");

        Assert.IsTrue(namedExport.IsNamed);
        Assert.AreEqual(KnownExportName, namedExport.NameOrOrdinal);
        Assert.IsFalse(ordinalOnlyExport.IsNamed);
        Assert.AreEqual("#42", ordinalOnlyExport.NameOrOrdinal);
        Assert.IsTrue(forwardedExport.IsForwarded);
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception exception)
        {
            Assert.Fail($"Expected {typeof(TException).Name}, but caught {exception.GetType().Name}: {exception.Message}");
        }

        Assert.Fail($"Expected {typeof(TException).Name}, but no exception was thrown.");
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool IsUserAnAdminDelegate();
}
