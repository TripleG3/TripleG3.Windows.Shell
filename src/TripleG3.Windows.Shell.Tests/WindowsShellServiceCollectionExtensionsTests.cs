using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Windows.Shell.Tests;

[TestClass]
public sealed class WindowsShellServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddTripleG3WindowsShell_RegistersWindowHandleServiceAsSingleton()
    {
        var services = new ServiceCollection();

        var returnedServices = services.AddTripleG3WindowsShell();

        Assert.AreSame(services, returnedServices);

        var descriptor = services.Single(service => service.ServiceType == typeof(IWindowHandleService));
        Assert.AreEqual(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.AreEqual(typeof(User32WindowHandleService), descriptor.ImplementationType);
    }

    [TestMethod]
    public void AddTripleG3WindowsShell_ResolvedWindowHandleServiceUsesUser32Implementation()
    {
        var services = new ServiceCollection();
        services.AddTripleG3WindowsShell();

        using var provider = services.BuildServiceProvider();

        var firstService = provider.GetRequiredService<IWindowHandleService>();
        var secondService = provider.GetRequiredService<IWindowHandleService>();

        Assert.AreSame(firstService, secondService);
        Assert.AreEqual(typeof(User32WindowHandleService), firstService.GetType());
        Assert.AreNotEqual(nint.Zero, firstService.GetDesktopWindow());
    }

    [TestMethod]
    public void AddTripleG3WindowsShell_NullServices_ThrowsArgumentNullException()
    {
        AssertThrows<ArgumentNullException>(() => WindowsShellServiceCollectionExtensions.AddTripleG3WindowsShell(null!));
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
}