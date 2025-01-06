using System;
using System.Reflection;

namespace Unicorn.LiveLogViewer.Tests.Helpers.AspNet;

/// <summary>
/// Represents a registered route endpoint.
/// </summary>
/// <param name="RoutePattern">The route patten used to register the route endpoint.</param>
/// <param name="Methods">The HTTP methods used to register the route endpoint.</param>
/// <param name="Method">The CLR method that handles the requests.</param>
public record RouteDetails(string? RoutePattern, string[]? Methods, MethodInfo? Method)
{
    /// <summary>
    /// Initialize a new instance of the <see cref="RouteDetails"/> class.
    /// </summary>
    /// <param name="RoutePattern">The route patten used to register the route endpoint.</param>
    /// <param name="Methods">The HTTP methods used to register the route endpoint.</param>
    /// <param name="Method">The CLR method that handles the requests.</param>
    public RouteDetails(string? RoutePattern, string[]? Methods, Delegate? Method)
        : this(RoutePattern, Methods, Method?.Method)
    {
    }
}