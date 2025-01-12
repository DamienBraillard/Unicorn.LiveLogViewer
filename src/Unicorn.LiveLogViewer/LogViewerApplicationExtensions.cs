using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Unicorn.LiveLogViewer.Sources;

namespace Unicorn.LiveLogViewer;

/// <summary>
/// Defines the extension methods that allow to register the Live Log Viewer to an Asp.Net application.
/// </summary>
public static class LogViewerApplicationExtensions
{
    /// <summary>
    /// Registers the required dependencies for the Live Log Viewer.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="options">The <see cref="LogViewerOptions"/> options.</param>
    /// <returns>The specified <see cref="IServiceCollection"/> for further configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="options"/> is <c>null</c>.</exception>
    public static IServiceCollection AddLiveLogViewer(this IServiceCollection services, LogViewerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return RegisterServices(services, _ => options);
    }

    /// <summary>
    /// Registers the required dependencies for the Live Log Viewer.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="optionsBuilder">The optional <see cref="Action{IServiceProvider, LogViewerOptions}"/> callback that allows to configure the <see cref="LogViewerOptions"/>.</param>
    /// <returns>The specified <see cref="IServiceCollection"/> for further configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddLiveLogViewer(this IServiceCollection services, Action<IServiceProvider, LogViewerOptions>? optionsBuilder = null)
    {
        return RegisterServices(services, sp =>
        {
            var options = new LogViewerOptions();
            optionsBuilder?.Invoke(sp, options);
            return options;
        });
    }

    /// <summary>
    /// Registers the required dependencies for the Live Log Viewer.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="optionsFactory">The factory that will create the <see cref="LogViewerOptions"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    /// <returns>The specified <see cref="IServiceCollection"/> for further configuration.</returns>
    private static IServiceCollection RegisterServices(IServiceCollection services, Func<IServiceProvider, LogViewerOptions> optionsFactory)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(optionsFactory);
        services.AddSingleton<ILogProvider>(NullLogProvider.Default);

        return services;
    }

    /// <summary>
    /// Adds the Live Log Viewer endpoints to the application pipeline.
    /// </summary>
    /// <param name="endpointBuilder">The <see cref="IEndpointRouteBuilder"/> to configure.</param>
    /// <param name="basePath">The optional base path for all the endpoints. The viewer page will be visible at this address.</param>
    /// <returns>The specified <see cref="IEndpointRouteBuilder"/> for further configuration of the mapped endpoints.</returns>
    /// <remarks>Unless specified otherwise using the <paramref name="basePath"/> parameter, the log viewer page will be accessible at the <c>/logViewer</c> path.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="endpointBuilder"/> or <paramref name="basePath"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="basePath"/> is not empty and does not start with a <c>'/'</c>.</exception>
    public static IEndpointConventionBuilder MapLiveLogViewer(this IEndpointRouteBuilder endpointBuilder, string basePath = "/logViewer")
    {
        ArgumentNullException.ThrowIfNull(endpointBuilder);
        ArgumentNullException.ThrowIfNull(basePath);
        if (basePath.Length > 0 && basePath[0] != '/')
            throw new ArgumentException($"The base path ({basePath}) must start with a slash.", nameof(basePath));

        // Maps the endpoints
        var group = endpointBuilder.MapGroup(basePath.TrimEnd('/')).ExcludeFromDescription();
        group.MapGet("/", () => { }); // TODO Map real endpoints here
        return group;
    }
}