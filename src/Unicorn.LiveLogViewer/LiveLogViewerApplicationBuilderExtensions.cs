using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Unicorn.LiveLogViewer.RequestProcessing;
using Unicorn.LiveLogViewer.Sources;

namespace Unicorn.LiveLogViewer;

/// <summary>
/// Defines the extension methods that allow to add the Live Log Viewer to a <see cref="IApplicationBuilder"/>
/// </summary>
public static class LiveLogViewerApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Live Log Viewer required dependencies and allows to
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="optionsBuilder">The <see cref="Action{LiveLogViewerOptions}"/> that allows to configure the options.</param>
    /// <returns>The specified <see cref="IServiceCollection"/> for further configuration.</returns>
    public static IServiceCollection AddLiveLogViewer(
        this IServiceCollection services,
        Action<IServiceProvider, LogViewerOptions>? optionsBuilder = null)
    {
        // Required dependencies
        services.AddSingleton<ILogProvider>(NullLogProvider.Default);

        // Pipeline
        services.AddSingleton<ILogViewerRequestHandler, StaticContentRequestHandler>();
        services.AddSingleton<ILogViewerRequestHandler, LogEntriesRequestHandler>();
        services.AddSingleton<ILogViewerRequestDispatcher, LogViewerRequestDispatcher>();

        // Options
        services.AddSingleton(sp =>
        {
            var options = new LogViewerOptions();
            optionsBuilder?.Invoke(sp, options);
            return options;
        });

        return services;
    }

    /// <summary>
    /// Inserts the Live Log Viewer middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <param name="basePath">The optional base path for all the endpoints. The viewer page will be visible at this address.</param>
    /// <returns>The specified <see cref="IApplicationBuilder"/> for further configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="basePath"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="basePath"/> does not start with a <c>'/'</c>.</exception>
    public static IApplicationBuilder UseLiveLogViewer(this IApplicationBuilder app, string basePath = "/logViewer")
    {
        var dispatcher = SetupDispatcher(app.ApplicationServices, basePath);

        app.Use(next =>
        {
            return async context =>
            {
                await dispatcher.DispatchAsync(context);
                await next(context);
            };
        });

        return app;
    }

    /// <summary>
    /// Inserts the Live Log Viewer middleware to the application pipeline.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to configure.</param>
    /// <param name="basePath">The optional base path for all the endpoints. The viewer page will be visible at this address.</param>
    /// <returns>The specified <see cref="IEndpointRouteBuilder"/> for further configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="basePath"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="basePath"/> does not start with a <c>'/'</c>.</exception>
    public static IEndpointConventionBuilder MapLiveLogViewer(this IEndpointRouteBuilder endpoints, string basePath = "/logViewer")
    {
        var dispatcher = SetupDispatcher(endpoints.ServiceProvider, basePath);

        return endpoints.MapMethods(
            $"{dispatcher.BasePath}/{{*rest}}",
            [HttpMethods.Get],
            dispatcher.DispatchAsync);
    }

    /// <summary>
    /// Creates and sets-up the <see cref="ILogViewerRequestDispatcher"/> for inserting it in the pipeline.
    /// </summary>
    /// <param name="provider">The <see cref="IServiceProvider"/> used to get the <see cref="LogViewerOptions"/> singleton.</param>
    /// <param name="basePath">The base path to set.</param>
    /// <returns>The <see cref="ILogViewerRequestDispatcher"/> that can be added to the request pipeline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="basePath"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="basePath"/> does not start with a <c>'/'</c>.</exception>
    private static ILogViewerRequestDispatcher SetupDispatcher(IServiceProvider provider, string basePath)
    {
        ArgumentNullException.ThrowIfNull(basePath);
        if (!basePath.StartsWith('/')) throw new ArgumentException("The base path must start with a '/'.", nameof(basePath));

        // Update the options (ensure that the base path has no trailing slash)
        var options = provider.GetRequiredService<LogViewerOptions>();
        options.BasePath = basePath.EndsWith('/') ? basePath[..^1] : basePath;

        // Create and return the dispatcher
        return provider.GetRequiredService<ILogViewerRequestDispatcher>();
    }
}