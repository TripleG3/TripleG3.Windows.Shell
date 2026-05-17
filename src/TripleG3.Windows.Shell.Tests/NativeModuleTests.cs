using System.Runtime.InteropServices;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class NativeModuleTests
{
    private const string KnownExportName = "GetCurrentProcess";

    [TestMethod]
    public void ModuleHandle_ReturnsLoadedModuleHandle()
    {
        var module = CreateKernel32Module();

        Assert.AreNotEqual(nint.Zero, module.ModuleHandle);
    }

    [TestMethod]
    public void ModulePath_PointsToLoadedModule()
    {
        var module = CreateKernel32Module();

        Assert.IsTrue(File.Exists(module.ModulePath), $"Expected {module.ModulePath} to exist.");
        Assert.AreEqual(Kernel32.LibraryName, Path.GetFileName(module.ModulePath), ignoreCase: true);
    }

    [TestMethod]
    public void Exports_ContainsKnownNamedExport()
    {
        var module = CreateKernel32Module();

        Assert.IsNotEmpty(module.ExportNames);
        CollectionAssert.Contains(module.ExportNames.ToList(), KnownExportName);
    }

    [TestMethod]
    public void ExportNames_AreSortedAndMatchNamedExports()
    {
        var module = CreateKernel32Module();
        var expectedNames = module.Exports
            .Where(export => export.Name is not null)
            .Select(export => export.Name!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(expectedNames, module.ExportNames.ToArray());
    }

    [TestMethod]
    public void GetExport_KnownName_ReturnsSamePointerAsTryGetExport()
    {
        var module = CreateKernel32Module();
        var address = module.GetExport(KnownExportName);
        var found = module.TryGetExport(KnownExportName, out var tryAddress);

        Assert.IsTrue(found);
        Assert.AreNotEqual(nint.Zero, address);
        Assert.AreEqual(address, tryAddress);
    }

    [TestMethod]
    public void GetExport_KnownOrdinal_ReturnsSamePointerAsNamedExport()
    {
        var module = CreateKernel32Module();
        var export = module.Exports.Single(export => export.Name == KnownExportName);
        var namedAddress = module.GetExport(KnownExportName);
        var ordinalAddress = module.GetExport(export.Ordinal);

        Assert.AreEqual(namedAddress, ordinalAddress);
    }

    [TestMethod]
    public void TryGetExport_MissingName_ReturnsFalseAndZeroAddress()
    {
        var module = CreateKernel32Module();
        var found = module.TryGetExport("TripleG3MissingNativeModuleExport", out var address);

        Assert.IsFalse(found);
        Assert.AreEqual(nint.Zero, address);
    }

    [TestMethod]
    public void GetExport_MissingName_ThrowsEntryPointNotFoundException()
    {
        var module = CreateKernel32Module();

        AssertThrows<EntryPointNotFoundException>(() => module.GetExport("TripleG3MissingNativeModuleExport"));
    }

    [TestMethod]
    public void TryGetExport_InvalidName_ThrowsArgumentException()
    {
        var module = CreateKernel32Module();

        AssertThrows<ArgumentException>(() => module.TryGetExport(" ", out _));
    }

    [TestMethod]
    public void TryGetExport_InvalidOrdinal_ThrowsArgumentOutOfRangeException()
    {
        var module = CreateKernel32Module();

        AssertThrows<ArgumentOutOfRangeException>(() => module.TryGetExport(0, out _));
    }

    [TestMethod]
    public void GetFunction_KnownName_BindsCallableDelegate()
    {
        var module = CreateKernel32Module();
        var getCurrentProcess = module.GetFunction<GetCurrentProcessDelegate>(KnownExportName);

        Assert.AreNotEqual(nint.Zero, getCurrentProcess());
    }

    [TestMethod]
    public void TryGetFunction_MissingName_ReturnsFalseAndNullDelegate()
    {
        var module = CreateKernel32Module();
        var found = module.TryGetFunction<GetCurrentProcessDelegate>("TripleG3MissingNativeModuleExport", out var function);

        Assert.IsFalse(found);
        Assert.IsNull(function);
    }

    [TestMethod]
    public void NativeExport_MetadataReportsStableValues()
    {
        var namedExport = new NativeExport(KnownExportName, 42, 123, null);
        var forwardedExport = new NativeExport("Forwarded", 43, 456, "example.Target");

        Assert.AreEqual(KnownExportName, namedExport.Name);
        Assert.AreEqual(42, namedExport.Ordinal);
        Assert.AreEqual(123U, namedExport.RelativeVirtualAddress);
        Assert.IsNull(namedExport.ForwardedTo);
        Assert.AreEqual("example.Target", forwardedExport.ForwardedTo);
    }

    private static NativeModule CreateKernel32Module()
    {
        return new NativeModule(Kernel32.LibraryName, typeof(Kernel32));
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
