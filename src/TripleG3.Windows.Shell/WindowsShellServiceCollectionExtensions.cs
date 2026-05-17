using Microsoft.Extensions.DependencyInjection;

namespace TripleG3.Windows.Shell;

/// <summary>
/// Dependency injection registration helpers for TripleG3 Windows Shell services.
/// </summary>
public static class WindowsShellServiceCollectionExtensions
{
    /// <summary>Adds the app-facing TripleG3 Windows Shell services.</summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <returns>The same service collection so additional calls can be chained.</returns>
    public static IServiceCollection AddTripleG3WindowsShell(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IWindowHandleService, User32WindowHandleService>();
        services.AddSingleton<IScreenCaptureService, User32Gdi32ScreenCaptureService>();

        return services;
    }
}