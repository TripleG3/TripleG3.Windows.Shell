using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class Gdi32Tests
{
    private const int BlackBrush = 4;
    private const string KnownExportName = "GetStockObject";

    [TestMethod]
    public void ModuleHandle_ReturnsLoadedGdi32Handle()
    {
        Assert.AreNotEqual(nint.Zero, Gdi32.ModuleHandle);
    }

    [TestMethod]
    public void ModulePath_PointsToLoadedGdi32Dll()
    {
        Assert.IsTrue(File.Exists(Gdi32.ModulePath), $"Expected {Gdi32.ModulePath} to exist.");
        Assert.AreEqual(Gdi32.LibraryName, Path.GetFileName(Gdi32.ModulePath), ignoreCase: true);
    }

    [TestMethod]
    public void Exports_ContainsKnownNamedGdi32Functions()
    {
        var exportNames = Gdi32.ExportNames;

        Assert.IsNotEmpty(exportNames);
        CollectionAssert.Contains(exportNames.ToList(), KnownExportName);
        CollectionAssert.Contains(exportNames.ToList(), "CreateCompatibleDC");
        CollectionAssert.Contains(exportNames.ToList(), "DeleteDC");
    }

    [TestMethod]
    public void ExportNames_AreSortedAndMatchNamedExports()
    {
        var expectedNames = Gdi32.Exports
            .Where(export => export.Name is not null)
            .Select(export => export.Name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(expectedNames, Gdi32.ExportNames.ToArray());
    }

    [TestMethod]
    public void GetExport_KnownName_ReturnsSamePointerAsTryGetExport()
    {
        var address = Gdi32.GetExport(KnownExportName);
        var found = Gdi32.TryGetExport(KnownExportName, out var tryAddress);

        Assert.IsTrue(found);
        Assert.AreNotEqual(nint.Zero, address);
        Assert.AreEqual(address, tryAddress);
    }

    [TestMethod]
    public void GetExport_KnownOrdinal_ReturnsSamePointerAsNamedExport()
    {
        var export = Gdi32.Exports.Single(export => export.Name == KnownExportName);
        var namedAddress = Gdi32.GetExport(KnownExportName);
        var ordinalAddress = Gdi32.GetExport(export.Ordinal);

        Assert.AreEqual(namedAddress, ordinalAddress);
    }

    [TestMethod]
    public void TryGetExport_MissingName_ReturnsFalseAndZeroAddress()
    {
        var found = Gdi32.TryGetExport("TripleG3MissingGdi32Export", out var address);

        Assert.IsFalse(found);
        Assert.AreEqual(nint.Zero, address);
    }

    [TestMethod]
    public void GetExport_MissingName_ThrowsEntryPointNotFoundException()
    {
        AssertThrows<EntryPointNotFoundException>(() => Gdi32.GetExport("TripleG3MissingGdi32Export"));
    }

    [TestMethod]
    public void TryGetExport_InvalidName_ThrowsArgumentException()
    {
        AssertThrows<ArgumentException>(() => Gdi32.TryGetExport(" ", out _));
    }

    [TestMethod]
    public void TryGetExport_InvalidOrdinal_ThrowsArgumentOutOfRangeException()
    {
        AssertThrows<ArgumentOutOfRangeException>(() => Gdi32.TryGetExport(0, out _));
    }

    [TestMethod]
    public void GetFunction_KnownName_BindsCallableDelegate()
    {
        var getStockObject = Gdi32.GetFunction<GetStockObjectDelegate>(KnownExportName);

        Assert.AreNotEqual(nint.Zero, getStockObject(BlackBrush));
    }

    [TestMethod]
    public void TryGetFunction_KnownOrdinal_BindsCallableDelegate()
    {
        var export = Gdi32.Exports.Single(export => export.Name == KnownExportName);
        var found = Gdi32.TryGetFunction<GetStockObjectDelegate>(export.Ordinal, out var getStockObject);

        Assert.IsTrue(found);
        Assert.IsNotNull(getStockObject);
        Assert.AreNotEqual(nint.Zero, getStockObject(BlackBrush));
    }

    [TestMethod]
    public void TryGetFunction_MissingName_ReturnsFalseAndNullDelegate()
    {
        var found = Gdi32.TryGetFunction<GetStockObjectDelegate>("TripleG3MissingGdi32Export", out var function);

        Assert.IsFalse(found);
        Assert.IsNull(function);
    }

    [TestMethod]
    public void Export_MetadataReportsStableDisplayValues()
    {
        var namedExport = Gdi32.Exports.Single(export => export.Name == KnownExportName);
        var ordinalOnlyExport = new Gdi32.Export(null, 42, 123, null);
        var forwardedExport = new Gdi32.Export("Forwarded", 43, 456, "example.Target");

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
    private delegate nint GetStockObjectDelegate(int objectIndex);
}
