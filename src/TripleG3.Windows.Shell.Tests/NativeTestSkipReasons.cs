namespace TripleG3.Windows.Shell.Tests;

internal static class NativeTestSkipReasons
{
    public const string RequiresManualNativeValidation = "Requires live Windows native APIs and can trigger real OS behavior; re-enable only when changing the tested wrapper or service.";
}