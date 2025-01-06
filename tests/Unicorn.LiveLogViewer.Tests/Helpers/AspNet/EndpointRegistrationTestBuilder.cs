using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Unicorn.LiveLogViewer.Tests.Helpers.AspNet;

/// <summary>
/// Implements a fake <see cref="IEndpointRouteBuilder"/> that can be used to inspect the registered endpoints.
/// </summary>
public class EndpointRegistrationTestBuilder : IEndpointRouteBuilder
{
    private readonly IApplicationBuilder _applicationBuilder;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICollection<EndpointDataSource> _dataSources;

    /// <summary>
    /// Implements a fake <see cref="IEndpointRouteBuilder"/> that can be used to inspect the registered endpoints.
    /// </summary>
    public EndpointRegistrationTestBuilder(IServiceCollection? services = null)
    {
        // Registering endpoint requires some basic services to be registered
        services ??= new ServiceCollection();
        services.TryAddSingleton(Substitute.For<ILoggerFactory>());

        // Builds the service provider
        _serviceProvider = services.BuildServiceProvider();

        // Build a fake application builder
        _applicationBuilder = Substitute.For<IApplicationBuilder>();
        _applicationBuilder.ApplicationServices.Returns(_serviceProvider);

        // Builds the data sources list
        _dataSources = new List<EndpointDataSource>();
    }

    /// <inheritdoc/>
    IApplicationBuilder IEndpointRouteBuilder.CreateApplicationBuilder() => _applicationBuilder;

    /// <inheritdoc/>
    IServiceProvider IEndpointRouteBuilder.ServiceProvider => _serviceProvider;

    /// <inheritdoc/>
    ICollection<EndpointDataSource> IEndpointRouteBuilder.DataSources => _dataSources;

    /// <summary>
    /// Computes the list of all route endpoints that have been registered in an easy to compare representation.
    /// </summary>
    /// <returns>A collection of <see cref="RouteDetails"/> that represents all the registered route endpoints.</returns>
    public IReadOnlyCollection<RouteDetails> GetRouteDetails()
    {
        var endpoints = RouteEndpoints
            .Select(e => new RouteDetails(e.GetRoute(), e.GetMethods(), e.GetDelegate()))
            .ToList();

        return endpoints;
    }

    /// <summary>
    /// Returns the list of all <see cref="RouteEndpoint"/> that have been registered on the endpoint.
    /// </summary>
    public IEnumerable<RouteEndpoint> RouteEndpoints => _dataSources.SelectMany(ds => ds.Endpoints).OfType<RouteEndpoint>();
}