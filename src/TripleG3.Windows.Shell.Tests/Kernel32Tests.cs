using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class Kernel32Tests
{
    private const string KnownExportName = "GetCurrentProcess";

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void ModuleHandle_ReturnsLoadedKernel32Handle()
    {
        Assert.AreNotEqual(nint.Zero, Kernel32.ModuleHandle);
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void ModulePath_PointsToLoadedKernel32Dll()
    {
        Assert.IsTrue(File.Exists(Kernel32.ModulePath), $"Expected {Kernel32.ModulePath} to exist.");
        Assert.AreEqual(Kernel32.LibraryName, Path.GetFileName(Kernel32.ModulePath), ignoreCase: true);
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Exports_ContainsKnownNamedKernel32Functions()
    {
        var exportNames = Kernel32.ExportNames;

        Assert.IsNotEmpty(exportNames);
        CollectionAssert.Contains(exportNames.ToList(), KnownExportName);
        CollectionAssert.Contains(exportNames.ToList(), "GetCurrentProcessId");
        CollectionAssert.Contains(exportNames.ToList(), "GetTickCount");
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void ExportNames_AreSortedAndMatchNamedExports()
    {
        var expectedNames = Kernel32.Exports
            .Where(export => export.Name is not null)
            .Select(export => export.Name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(expectedNames, Kernel32.ExportNames.ToArray());
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void GetExport_KnownName_ReturnsSamePointerAsTryGetExport()
    {
        var address = Kernel32.GetExport(KnownExportName);
        var found = Kernel32.TryGetExport(KnownExportName, out var tryAddress);

        Assert.IsTrue(found);
        Assert.AreNotEqual(nint.Zero, address);
        Assert.AreEqual(address, tryAddress);
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void GetExport_KnownOrdinal_ReturnsSamePointerAsNamedExport()
    {
        var export = Kernel32.Exports.Single(export => export.Name == KnownExportName);
        var namedAddress = Kernel32.GetExport(KnownExportName);
        var ordinalAddress = Kernel32.GetExport(export.Ordinal);

        Assert.AreEqual(namedAddress, ordinalAddress);
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void TryGetExport_MissingName_ReturnsFalseAndZeroAddress()
    {
        var found = Kernel32.TryGetExport("TripleG3MissingKernel32Export", out var address);

        Assert.IsFalse(found);
        Assert.AreEqual(nint.Zero, address);
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void GetExport_MissingName_ThrowsEntryPointNotFoundException()
    {
        AssertThrows<EntryPointNotFoundException>(() => Kernel32.GetExport("TripleG3MissingKernel32Export"));
    }

    [TestMethod]
    public void TryGetExport_InvalidName_ThrowsArgumentException()
    {
        AssertThrows<ArgumentException>(() => Kernel32.TryGetExport(" ", out _));
    }

    [TestMethod]
    public void TryGetExport_InvalidOrdinal_ThrowsArgumentOutOfRangeException()
    {
        AssertThrows<ArgumentOutOfRangeException>(() => Kernel32.TryGetExport(0, out _));
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void GetFunction_KnownName_BindsCallableDelegate()
    {
        var getCurrentProcess = Kernel32.GetFunction<GetCurrentProcessDelegate>(KnownExportName);

        Assert.AreNotEqual(nint.Zero, getCurrentProcess());
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void TryGetFunction_KnownOrdinal_BindsCallableDelegate()
    {
        var export = Kernel32.Exports.Single(export => export.Name == KnownExportName);
        var found = Kernel32.TryGetFunction<GetCurrentProcessDelegate>(export.Ordinal, out var getCurrentProcess);

        Assert.IsTrue(found);
        Assert.IsNotNull(getCurrentProcess);
        Assert.AreNotEqual(nint.Zero, getCurrentProcess());
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void TryGetFunction_MissingName_ReturnsFalseAndNullDelegate()
    {
        var found = Kernel32.TryGetFunction<GetCurrentProcessDelegate>("TripleG3MissingKernel32Export", out var function);

        Assert.IsFalse(found);
        Assert.IsNull(function);
    }

    [TestMethod]
    [Ignore(NativeTestSkipReasons.RequiresManualNativeValidation)]
    public void Export_MetadataReportsStableDisplayValues()
    {
        var namedExport = Kernel32.Exports.Single(export => export.Name == KnownExportName);
        var ordinalOnlyExport = new Kernel32.Export(null, 42, 123, null);
        var forwardedExport = new Kernel32.Export("Forwarded", 43, 456, "example.Target");

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
    private delegate nint GetCurrentProcessDelegate();
}
