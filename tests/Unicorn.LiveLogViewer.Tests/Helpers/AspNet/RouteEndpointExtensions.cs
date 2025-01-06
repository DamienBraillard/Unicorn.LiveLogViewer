using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace Unicorn.LiveLogViewer.Tests.Helpers.AspNet;

/// <summary>
/// Provides test extension methods on the <see cref="RouteEndpoint"/> type.
/// </summary>
public static class RouteEndpointExtensions
{
    /// <summary>
    /// Returns the route under which the endpoint has been registered.
    /// </summary>
    /// <param name="endpoint">The <see cref="RouteEndpoint"/>.</param>
    /// <returns>The route pattern as defined when the endpoint has been registered.</returns>
    public static string? GetRoute(this RouteEndpoint endpoint) => endpoint.RoutePattern.RawText;

    /// <summary>
    /// Returns the HTTP methods for which the endpoint has been registered.
    /// </summary>
    /// <param name="endpoint">The <see cref="RouteEndpoint"/>.</param>
    /// <returns>The list of HTTP methods for which the endpoint has been registered.</returns>
    public static string[] GetMethods(this RouteEndpoint endpoint) => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.ToArray() ?? [];

    /// <summary>
    /// Returns the method that will handle the endpoint.
    /// </summary>
    /// <param name="endpoint">The <see cref="RouteEndpoint"/>.</param>
    /// <returns>The <see cref="MethodInfo"/> identifying the method that will handle the endpoint.</returns>
    public static MethodInfo? GetDelegate(this RouteEndpoint endpoint) => endpoint.Metadata.GetMetadata<MethodInfo>();

    /// <summary>
    /// Determines whether a metadata of a specific type is defined on the endpoint.
    /// </summary>
    /// <param name="endpoint">The <see cref="RouteEndpoint"/>.</param>
    /// <typeparam name="T">The type of the metadata to search.</typeparam>
    /// <returns><c>true</c> if a metadta item of type <typeparamref name="T"/> can be found; othewrise, <c>false</c>.</returns>
    public static bool HasMetadata<T>(this RouteEndpoint endpoint) where T : class => endpoint.Metadata.GetMetadata<T>() != null;
}