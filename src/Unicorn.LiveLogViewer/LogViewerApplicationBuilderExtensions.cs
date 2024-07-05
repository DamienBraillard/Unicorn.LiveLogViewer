using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Unicorn.LiveLogViewer.Sources;
using Unicorn.LiveLogViewer.StaticContent;

namespace Unicorn.LiveLogViewer;

/// <summary>
/// Defines the extension methods that allow to add the Live Log Viewer to a <see cref="IApplicationBuilder"/>
/// </summary>
public static class LogViewerApplicationBuilderExtensions
{
    /// <summary>
    /// Exposes the default base path if none is specified.
    /// </summary>
    internal const string DefaultBasePath = "/logViewer";

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
        ArgumentNullException.ThrowIfNull(services);

        // Required dependencies
        services.AddSingleton<ILogProvider>(NullLogProvider.Default);

        // Middlewares
        services.AddSingleton<ILogViewerMiddleware, LogViewerMiddleware>();

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
    /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <param name="basePath">The optional base path for all the endpoints. The viewer page will be visible at this address.</param>
    /// <returns>The specified <see cref="IApplicationBuilder"/> for further configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="basePath"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="basePath"/> does not start with a <c>'/'</c>.</exception>
    public static IApplicationBuilder UseLiveLogViewer(this IApplicationBuilder applicationBuilder, string basePath = DefaultBasePath)
    {
        ArgumentNullException.ThrowIfNull(applicationBuilder);
        ArgumentNullException.ThrowIfNull(basePath);

        // Ensure that the base path does not end with /
        basePath = basePath.TrimEnd('/');

        // Configure a child pipeline under the base path
        return applicationBuilder.Map(basePath, app => app.ConfigurePipeline());
    }

    /// <summary>
    /// Inserts the Live Log Viewer middleware to the application pipeline.
    /// </summary>
    /// <param name="endpointBuilder">The <see cref="IEndpointRouteBuilder"/> to configure.</param>
    /// <param name="basePath">The optional base path for all the endpoints. The viewer page will be visible at this address.</param>
    /// <returns>The specified <see cref="IEndpointRouteBuilder"/> for further configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="basePath"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="basePath"/> does not start with a <c>'/'</c>.</exception>
    public static IEndpointConventionBuilder MapLiveLogViewer(this IEndpointRouteBuilder endpointBuilder, string basePath = DefaultBasePath)
    {
        ArgumentNullException.ThrowIfNull(endpointBuilder);
        ArgumentNullException.ThrowIfNull(basePath);

        // Ensure that the base path does not end with /
        basePath = basePath.TrimEnd('/');

        // Builds the endpoint request delegate
        var requestDelegate = endpointBuilder.CreateApplicationBuilder()
            .UsePathBase(new PathString(basePath))
            .Use(async (context, next) =>
            {
                // We only need the endpoint to create a sub-pipeline here.
                // In this case, the presence of an endpoint (context.GetEndpoint() != null) prevents the DefaultFilesMiddleware and StaticFilesMiddleware to run
                // Hence, we insert this quick middleware to remove the endpoint from the request so that static files can be served.
                context.SetEndpoint(null);
                await next(context);
            })
            .ConfigurePipeline()
            .Build();

        return endpointBuilder.Map($"{basePath}/{{*rest}}", requestDelegate);
    }

    /// <summary>
    /// Configures the pipeline for the LogViewer
    /// </summary>
    /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> pipeline to configure.</param>
    private static IApplicationBuilder ConfigurePipeline(this IApplicationBuilder applicationBuilder)
    {
        var options = applicationBuilder.ApplicationServices.GetRequiredService<LogViewerOptions>();
        applicationBuilder.UseMiddleware<ILogViewerMiddleware>();
        applicationBuilder.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = options.StaticContentProvider,
            DefaultFileNames = [StaticFileNames.Page],
            RedirectToAppendTrailingSlash = true,
        });
        applicationBuilder.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = options.StaticContentProvider
        });
        return applicationBuilder;
    }
}