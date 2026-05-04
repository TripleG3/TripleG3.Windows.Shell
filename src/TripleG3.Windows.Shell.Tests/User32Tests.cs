using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class User32Tests
{
    private const string KnownExportName = "GetDesktopWindow";

    [TestMethod]
    public void ModuleHandle_ReturnsLoadedUser32Handle()
    {
        Assert.AreNotEqual(nint.Zero, User32.ModuleHandle);
    }

    [TestMethod]
    public void ModulePath_PointsToLoadedUser32Dll()
    {
        Assert.IsTrue(File.Exists(User32.ModulePath), $"Expected {User32.ModulePath} to exist.");
        Assert.AreEqual(User32.LibraryName, Path.GetFileName(User32.ModulePath), ignoreCase: true);
    }

    [TestMethod]
    public void Exports_ContainsKnownNamedUser32Functions()
    {
        var exportNames = User32.ExportNames;

        Assert.IsNotEmpty(exportNames);
        CollectionAssert.Contains(exportNames.ToList(), KnownExportName);
        CollectionAssert.Contains(exportNames.ToList(), "MessageBoxW");
        CollectionAssert.Contains(exportNames.ToList(), "GetForegroundWindow");
    }

    [TestMethod]
    public void ExportNames_AreSortedAndMatchNamedExports()
    {
        var expectedNames = User32.Exports
            .Where(export => export.Name is not null)
            .Select(export => export.Name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(expectedNames, User32.ExportNames.ToArray());
    }

    [TestMethod]
    public void GetExport_KnownName_ReturnsSamePointerAsTryGetExport()
    {
        var address = User32.GetExport(KnownExportName);
        var found = User32.TryGetExport(KnownExportName, out var tryAddress);

        Assert.IsTrue(found);
        Assert.AreNotEqual(nint.Zero, address);
        Assert.AreEqual(address, tryAddress);
    }

    [TestMethod]
    public void GetExport_KnownOrdinal_ReturnsSamePointerAsNamedExport()
    {
        var export = User32.Exports.Single(export => export.Name == KnownExportName);
        var namedAddress = User32.GetExport(KnownExportName);
        var ordinalAddress = User32.GetExport(export.Ordinal);

        Assert.AreEqual(namedAddress, ordinalAddress);
    }

    [TestMethod]
    public void TryGetExport_MissingName_ReturnsFalseAndZeroAddress()
    {
        var found = User32.TryGetExport("TripleG3MissingUser32Export", out var address);

        Assert.IsFalse(found);
        Assert.AreEqual(nint.Zero, address);
    }

    [TestMethod]
    public void GetExport_MissingName_ThrowsEntryPointNotFoundException()
    {
        AssertThrows<EntryPointNotFoundException>(() => User32.GetExport("TripleG3MissingUser32Export"));
    }

    [TestMethod]
    public void TryGetExport_InvalidName_ThrowsArgumentException()
    {
        AssertThrows<ArgumentException>(() => User32.TryGetExport(" ", out _));
    }

    [TestMethod]
    public void TryGetExport_InvalidOrdinal_ThrowsArgumentOutOfRangeException()
    {
        AssertThrows<ArgumentOutOfRangeException>(() => User32.TryGetExport(0, out _));
    }

    [TestMethod]
    public void GetFunction_KnownName_BindsCallableDelegate()
    {
        var getDesktopWindow = User32.GetFunction<GetDesktopWindowDelegate>(KnownExportName);

        Assert.AreNotEqual(nint.Zero, getDesktopWindow());
    }

    [TestMethod]
    public void TryGetFunction_KnownOrdinal_BindsCallableDelegate()
    {
        var export = User32.Exports.Single(export => export.Name == KnownExportName);
        var found = User32.TryGetFunction<GetDesktopWindowDelegate>(export.Ordinal, out var getDesktopWindow);

        Assert.IsTrue(found);
        Assert.IsNotNull(getDesktopWindow);
        Assert.AreNotEqual(nint.Zero, getDesktopWindow());
    }

    [TestMethod]
    public void TryGetFunction_MissingName_ReturnsFalseAndNullDelegate()
    {
        var found = User32.TryGetFunction<GetDesktopWindowDelegate>("TripleG3MissingUser32Export", out var function);

        Assert.IsFalse(found);
        Assert.IsNull(function);
    }

    [TestMethod]
    public void Export_MetadataReportsStableDisplayValues()
    {
        var namedExport = User32.Exports.Single(export => export.Name == KnownExportName);
        var ordinalOnlyExport = new User32.Export(null, 42, 123, null);
        var forwardedExport = new User32.Export("Forwarded", 43, 456, "example.Target");

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
    private delegate nint GetDesktopWindowDelegate();
}