using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Unicorn.LiveLogViewer.RequestProcessing;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests;

public class LiveLogViewerApplicationBuilderExtensionsTest
{
    private readonly ServiceCollection _serviceCollection;
    private readonly Lazy<ServiceProvider> _serviceProvider;
    private readonly IApplicationBuilder _applicationBuilder;
    private readonly IEndpointRouteBuilder _endpointRouteBuilder;

    public LiveLogViewerApplicationBuilderExtensionsTest()
    {
        _serviceCollection = [];
        _serviceProvider = new Lazy<ServiceProvider>(() => _serviceCollection.BuildServiceProvider());

        _applicationBuilder = Substitute.For<IApplicationBuilder>();
        _applicationBuilder.ApplicationServices.Returns(_ => _serviceProvider.Value);

        var dataSources = new List<EndpointDataSource>();

        _endpointRouteBuilder = Substitute.For<IEndpointRouteBuilder>();
        _endpointRouteBuilder.DataSources.Returns(_ => dataSources);
        _endpointRouteBuilder.ServiceProvider.Returns(_ => _serviceProvider.Value);
    }

    [Theory]
    [CombinatorialData]
    public void AddLiveLogViewer_NoState_RegistersRequestDispatcher(bool hasOptionsSetup)
    {
        // Arrange

        // Act
        _serviceCollection.AddLiveLogViewer(hasOptionsSetup ? (_, _) => { } : null);

        // Assert
        var service = _serviceProvider.Value.GetService<ILogViewerRequestDispatcher>();
        service.Should().NotBeNull();
    }

    [Theory]
    [CombinatorialData]
    public void AddLiveLogViewer_NoState_RegistersAllTheHandlers(bool hasOptionsSetup)
    {
        // Arrange

        // Act
        _serviceCollection.AddLiveLogViewer(hasOptionsSetup ? (_, _) => { } : null);

        // Assert
        var actualHandlers = _serviceProvider.Value.GetServices<ILogViewerRequestHandler>().Select(t => t.GetType()).ToList();
        var expectedHandlers = typeof(ILogViewerRequestHandler).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && t.IsAssignableTo(typeof(ILogViewerRequestHandler))).ToList();

        actualHandlers.Should().BeEquivalentTo(expectedHandlers);
    }

    [Fact]
    public void AddLiveLogViewer_OptionsBuilderIsNull_RegistersOptionsAsSingleton()
    {
        // Arrange

        // Act
        _serviceCollection.AddLiveLogViewer();

        // Assert
        var service = _serviceProvider.Value.GetService<LogViewerOptions>();
        service.Should().BeEquivalentTo(new LogViewerOptions());

        _serviceCollection.Should().ContainSingle(d => d.ServiceType == typeof(LogViewerOptions))
            .Which.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddLiveLogViewer_OptionsBuilderIsDefined_InvokesSetupDelegate()
    {
        // Arrange
        IServiceProvider? receivedServiceProvider = null;
        LogViewerOptions? receivedOptions = null;

        var optionsBuilder = Substitute.For<Action<IServiceProvider, LogViewerOptions>>();
        optionsBuilder.Invoke(
            Arg.Do<IServiceProvider>(x => receivedServiceProvider = x),
            Arg.Do<LogViewerOptions>(x => receivedOptions = x));

        _serviceCollection.AddSingleton("my-service");

        // Act
        _serviceCollection.AddLiveLogViewer(optionsBuilder);

        // Assert
        var provider = _serviceProvider.Value;
        _ = provider.GetService<LogViewerOptions>();

        optionsBuilder.ReceivedWithAnyArgs(1).Invoke(default!, default!);
        receivedServiceProvider.Should().BeAssignableTo<IServiceProvider>()
            .Which.GetService<string>().Should().BeSameAs("my-service");
        receivedOptions.Should().BeEquivalentTo(new LogViewerOptions());
    }

    [Fact]
    public void AddLiveLogViewer_OptionsBuilderIsDefined_RegistersConfiguredOptions()
    {
        // Arrange
        LogViewerOptions? configuredOptions = null;

        // Act
        _serviceCollection.AddLiveLogViewer((_, o) => configuredOptions = o);

        // Assert
        var options = _serviceProvider.Value.GetService<LogViewerOptions>();
        options.Should().NotBeNull().And.BeSameAs(configuredOptions);
    }

    [Fact]
    public void AddLiveLogViewer_NoState_ReturnsServiceCollection()
    {
        // Arrange

        // Act
        var result = _serviceCollection.AddLiveLogViewer();

        // Assert
        result.Should().BeSameAs(_serviceCollection);
    }

    [Fact]
    public void UseLiveLogViewer_BasePathNotSpecified_UpdatesTheOptions()
    {
        // Arrange
        _serviceCollection.AddLiveLogViewer();

        // Act
        _applicationBuilder.UseLiveLogViewer();

        // Assert
        var options = _applicationBuilder.ApplicationServices.GetRequiredService<LogViewerOptions>();
        options.BasePath.Should().Be("/logViewer");
    }

    [Theory]
    [InlineData("/log/viewer", "/log/viewer")]
    [InlineData("/log/viewer/", "/log/viewer")]
    public void UseLiveLogViewer_BasePathSpecified_UpdatesTheOptions(string basePath, string expected)
    {
        // Arrange
        _serviceCollection.AddSingleton<LogViewerOptions>();
        _serviceCollection.AddSingleton(Substitute.For<ILogViewerRequestDispatcher>());

        // Act
        _applicationBuilder.UseLiveLogViewer(basePath: basePath);

        // Assert
        var options = _applicationBuilder.ApplicationServices.GetRequiredService<LogViewerOptions>();
        options.BasePath.Should().Be(expected);
    }

    [Theory]
    [CombinatorialData]
    public void UseLiveLogViewer_Always_AddsTheDispatcherToThePipeline(bool hasBasePathExplicitlySpecified)
    {
        // Arrange
        var dispatcher = Substitute.For<ILogViewerRequestDispatcher>();
        _serviceCollection.AddSingleton<LogViewerOptions>();
        _serviceCollection.AddSingleton(dispatcher);

        Func<RequestDelegate, RequestDelegate>? useFunc = null;
        _applicationBuilder.Use(Arg.Do<Func<RequestDelegate, RequestDelegate>>(x => useFunc = x));

        var next = Substitute.For<RequestDelegate>();
        var httpContext = Substitute.For<HttpContext>();

        // Act
        if (hasBasePathExplicitlySpecified)
            _applicationBuilder.UseLiveLogViewer(basePath: "/log/viewer");
        else
            _applicationBuilder.UseLiveLogViewer();

        // Assert
        _applicationBuilder.ReceivedWithAnyArgs(1).Use(default!);

        useFunc!(next)(httpContext);
        dispatcher.Received(1).DispatchAsync(httpContext);
        next.Received(1).Invoke(httpContext);
    }

    [Fact]
    public void UseLiveLogViewer_NoState_ReturnsApplicationBuilder()
    {
        // Arrange
        _serviceCollection.AddSingleton<LogViewerOptions>();
        _serviceCollection.AddSingleton<ILogViewerRequestDispatcher, LogViewerRequestDispatcher>();
        _serviceCollection.AddSingleton(Substitute.For<ILogViewerRequestHandler>());

        // Act
        var result = _applicationBuilder.UseLiveLogViewer();

        // Assert
        result.Should().BeSameAs(_applicationBuilder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a/b")]
    public void UseLiveLogViewer_SpecifiedBasePathDoesNotStartWithASlash_Throws(string basePath)
    {
        // Arrange

        // Act
        var action = () => _applicationBuilder.UseLiveLogViewer(basePath: basePath);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithParameterName("basePath").WithMessage("*start*/*");
    }

    [Fact]
    public void UseLiveLogViewer_SpecifiedBasePathIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _applicationBuilder.UseLiveLogViewer(basePath: null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("basePath");
    }

    [Fact]
    public void MapLiveLogViewer_BasePathNotSpecified_UpdatesTheOptions()
    {
        // Arrange
        _serviceCollection.AddLiveLogViewer();

        // Act
        _endpointRouteBuilder.MapLiveLogViewer();

        // Assert
        var options = _endpointRouteBuilder.ServiceProvider.GetRequiredService<LogViewerOptions>();
        options.BasePath.Should().Be("/logViewer");
    }

    [Theory]
    [InlineData("/log/viewer", "/log/viewer")]
    [InlineData("/log/viewer/", "/log/viewer")]
    public void MapLiveLogViewer_BasePathSpecified_UpdatesTheOptions(string basePath, string expected)
    {
        // Arrange
        _serviceCollection.AddSingleton<LogViewerOptions>();
        _serviceCollection.AddSingleton(Substitute.For<ILogViewerRequestDispatcher>());

        // Act
        _endpointRouteBuilder.MapLiveLogViewer(basePath: basePath);

        // Assert
        var options = _endpointRouteBuilder.ServiceProvider.GetRequiredService<LogViewerOptions>();
        options.BasePath.Should().Be(expected);
    }

    [Theory]
    [CombinatorialData]
    public void MapLiveLogViewer_Always_AddsTheDispatcherToThePipeline(bool hasBasePathExplicitlySpecified)
    {
        // Arrange
        var options = new LogViewerOptions();
        _serviceCollection.AddSingleton(options);

        var dispatcher = Substitute.For<ILogViewerRequestDispatcher>();
        dispatcher.BasePath.Returns(_ => options.BasePath);
        _serviceCollection.AddSingleton(dispatcher);

        var httpContext = Substitute.For<HttpContext>();

        // Act
        if (hasBasePathExplicitlySpecified)
            _endpointRouteBuilder.MapLiveLogViewer(basePath: "/log/viewer");
        else
            _endpointRouteBuilder.MapLiveLogViewer();

        // Assert
        var expectedBasePath = hasBasePathExplicitlySpecified ? "/log/viewer" : "/logViewer";
        var endpoint = _endpointRouteBuilder.DataSources.Should().ContainSingle()
            .Which.Endpoints.Should().ContainSingle()
            .Which.Should().BeOfType<RouteEndpoint>().Subject;

        endpoint.RequestDelegate.Should().NotBeNull();
        endpoint.Metadata.OfType<HttpMethodMetadata>().Should().ContainSingle()
            .Which.HttpMethods.Should().BeEquivalentTo([HttpMethods.Get]);
        endpoint.RoutePattern.RawText.Should().MatchRegex(expectedBasePath + @"/\{\*{1,2}\w+\}");

        endpoint.RequestDelegate!(httpContext);
        dispatcher.Received(1).DispatchAsync(httpContext);
    }

    // [Fact]
    // public void MapLiveLogViewer_NoState_ReturnsCorrectEndpointConventionBuilder()
    // {
    //     // Arrange
    //     var httpContext = Substitute.For<HttpContext>();
    //     var dispatcher = Substitute.For<ILogViewerRequestDispatcher>();
    //     _serviceCollection.AddSingleton<LogViewerOptions>();
    //     _serviceCollection.AddSingleton(dispatcher);
    //     _serviceCollection.AddSingleton(Substitute.For<ILogViewerRequestHandler>());
    //
    //     // Act
    //     var result = _endpointRouteBuilder.MapLiveLogViewer(basePath: "/log/viewer");
    //
    //     // Assert
    //     var buildMethod = result.GetType().GetMethod("Build") ?? throw new MissingMethodException($"Method Build() not found on type {result.GetType().FullName}");
    //     var endpoint = (Endpoint?)buildMethod.Invoke(result, []);
    //
    //     RouteHandlerBuilder
    //
    //     endpoint?.RequestDelegate.Should().NotBeNull();
    //     endpoint!.RequestDelegate!.Invoke(httpContext);
    //
    //     dispatcher.Received(1).DispatchAsync(httpContext);
    // }

    [Theory]
    [InlineData("")]
    [InlineData("a/b")]
    public void MapLiveLogViewer_SpecifiedBasePathDoesNotStartWithASlash_Throws(string basePath)
    {
        // Arrange

        // Act
        var action = () => _endpointRouteBuilder.MapLiveLogViewer(basePath: basePath);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithParameterName("basePath").WithMessage("*start*/*");
    }

    [Fact]
    public void MapLiveLogViewer_SpecifiedBasePathIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _endpointRouteBuilder.MapLiveLogViewer(basePath: null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("basePath");
    }
}