using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit;

public class LogViewerApplicationBuilderExtensionsTest
{
    private readonly ServiceCollection _serviceCollection;
    private readonly Lazy<IServiceProvider> _serviceProvider;
    private readonly IApplicationBuilder _applicationBuilder;
    private readonly IEndpointRouteBuilder _endpointRouteBuilder;

    public LogViewerApplicationBuilderExtensionsTest()
    {
        _serviceCollection = [];
        _serviceProvider = new Lazy<IServiceProvider>(() => _serviceCollection.BuildServiceProvider());

        _applicationBuilder = new FakeApplicationBuilder(_serviceProvider);

        var dataSources = new List<EndpointDataSource>();

        _endpointRouteBuilder = Substitute.For<IEndpointRouteBuilder>();
        _endpointRouteBuilder.DataSources.Returns(_ => dataSources);
        _endpointRouteBuilder.ServiceProvider.Returns(_ => _serviceProvider.Value);
    }

    [Theory]
    [CombinatorialData]
    public void AddLiveLogViewer_NoState_RegistersTheMiddleware(bool hasOptionsSetup)
    {
        // Arrange

        // Act
        _serviceCollection.AddLiveLogViewer(hasOptionsSetup ? (_, _) => { } : null);

        // Assert
        var service = _serviceProvider.Value.GetService<ILogViewerMiddleware>();
        service.Should().NotBeNull();
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

    // [Theory]
    // [CombinatorialData]
    // public async Task UseLiveLogViewer_Always_AddsTheMiddlewareToThePipeline(bool hasBasePathExplicitlySpecified)
    // {
    //     // Arrange
    //     var middleware = Substitute.For<ILogViewerMiddleware>();
    //     _serviceCollection.AddSingleton<LogViewerOptions>();
    //     _serviceCollection.AddSingleton(middleware);
    //
    //     var basePath = hasBasePathExplicitlySpecified ? "/log/viewer" : LogViewerApplicationBuilderExtensions.DefaultBasePath;
    //     var httpContext = new DefaultHttpContext { Request = { Path = $"{basePath}/xx" } };
    //
    //     // Act
    //     if (hasBasePathExplicitlySpecified)
    //         _applicationBuilder.UseLiveLogViewer(basePath: "/log/viewer");
    //     else
    //         _applicationBuilder.UseLiveLogViewer();
    //
    //     // Assert
    //     await _applicationBuilder.Build()(httpContext);
    //
    //     await middleware.Received(1).InvokeAsync(httpContext, Arg.Any<RequestDelegate>());
    // }
    //
    // [Fact]
    // public void UseLiveLogViewer_NoState_ReturnsApplicationBuilder()
    // {
    //     // Arrange
    //     _serviceCollection.AddLiveLogViewer();
    //
    //     // Act
    //     var result = _applicationBuilder.UseLiveLogViewer();
    //
    //     // Assert
    //     result.Should().BeSameAs(_applicationBuilder);
    // }
    //
    // [Theory]
    // [InlineData("")]
    // [InlineData("a/b")]
    // public void UseLiveLogViewer_SpecifiedBasePathDoesNotStartWithASlash_Throws(string basePath)
    // {
    //     // Arrange
    //
    //     // Act
    //     var action = () => _applicationBuilder.UseLiveLogViewer(basePath: basePath);
    //
    //     // Assert
    //     action.Should().ThrowExactly<ArgumentException>().WithParameterName("basePath").WithMessage("*start*/*");
    // }
    //
    // [Fact]
    // public void UseLiveLogViewer_SpecifiedBasePathIsNull_Throws()
    // {
    //     // Arrange
    //
    //     // Act
    //     var action = () => _applicationBuilder.UseLiveLogViewer(basePath: null!);
    //
    //     // Assert
    //     action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("basePath");
    // }

    // [Fact]
    // public void MapLiveLogViewer_BasePathNotSpecified_UpdatesTheOptions()
    // {
    //     // Arrange
    //     _serviceCollection.AddLiveLogViewer();
    //
    //     // Act
    //     _endpointRouteBuilder.MapLiveLogViewer();
    //
    //     // Assert
    //     var options = _endpointRouteBuilder.ServiceProvider.GetRequiredService<LogViewerOptions>();
    //     options.BasePath.Should().Be("/logViewer");
    // }
    //
    // [Theory]
    // [InlineData("/log/viewer", "/log/viewer")]
    // [InlineData("/log/viewer/", "/log/viewer")]
    // public void MapLiveLogViewer_BasePathSpecified_UpdatesTheOptions(string basePath, string expected)
    // {
    //     // Arrange
    //     _serviceCollection.AddSingleton<LogViewerOptions>();
    //     _serviceCollection.AddSingleton(Substitute.For<ILogViewerRequestDispatcher>());
    //
    //     // Act
    //     _endpointRouteBuilder.MapLiveLogViewer(basePath: basePath);
    //
    //     // Assert
    //     var options = _endpointRouteBuilder.ServiceProvider.GetRequiredService<LogViewerOptions>();
    //     options.BasePath.Should().Be(expected);
    // }
    //
    // [Theory]
    // [CombinatorialData]
    // public void MapLiveLogViewer_Always_AddsTheDispatcherToThePipeline(bool hasBasePathExplicitlySpecified)
    // {
    //     // Arrange
    //     var options = new LogViewerOptions();
    //     _serviceCollection.AddSingleton(options);
    //
    //     var dispatcher = Substitute.For<ILogViewerRequestDispatcher>();
    //     dispatcher.BasePath.Returns(_ => options.BasePath);
    //     _serviceCollection.AddSingleton(dispatcher);
    //
    //     var httpContext = Substitute.For<HttpContext>();
    //
    //     // Act
    //     if (hasBasePathExplicitlySpecified)
    //         _endpointRouteBuilder.MapLiveLogViewer(basePath: "/log/viewer");
    //     else
    //         _endpointRouteBuilder.MapLiveLogViewer();
    //
    //     // Assert
    //     var expectedBasePath = hasBasePathExplicitlySpecified ? "/log/viewer" : "/logViewer";
    //     var endpoint = _endpointRouteBuilder.DataSources.Should().ContainSingle()
    //         .Which.Endpoints.Should().ContainSingle()
    //         .Which.Should().BeOfType<RouteEndpoint>().Subject;
    //
    //     endpoint.RequestDelegate.Should().NotBeNull();
    //     endpoint.Metadata.OfType<HttpMethodMetadata>().Should().ContainSingle()
    //         .Which.HttpMethods.Should().BeEquivalentTo([HttpMethods.Get]);
    //     endpoint.RoutePattern.RawText.Should().MatchRegex(expectedBasePath + @"/\{\*{1,2}\w+\}");
    //
    //     endpoint.RequestDelegate!(httpContext);
    //     dispatcher.Received(1).DispatchAsync(httpContext);
    // }
    //
    // // [Fact]
    // // public void MapLiveLogViewer_NoState_ReturnsCorrectEndpointConventionBuilder()
    // // {
    // //     // Arrange
    // //     var httpContext = Substitute.For<HttpContext>();
    // //     var dispatcher = Substitute.For<ILogViewerRequestDispatcher>();
    // //     _serviceCollection.AddSingleton<LogViewerOptions>();
    // //     _serviceCollection.AddSingleton(dispatcher);
    // //     _serviceCollection.AddSingleton(Substitute.For<ILogViewerRequestHandler>());
    // //
    // //     // Act
    // //     var result = _endpointRouteBuilder.MapLiveLogViewer(basePath: "/log/viewer");
    // //
    // //     // Assert
    // //     var buildMethod = result.GetType().GetMethod("Build") ?? throw new MissingMethodException($"Method Build() not found on type {result.GetType().FullName}");
    // //     var endpoint = (Endpoint?)buildMethod.Invoke(result, []);
    // //
    // //     RouteHandlerBuilder
    // //
    // //     endpoint?.RequestDelegate.Should().NotBeNull();
    // //     endpoint!.RequestDelegate!.Invoke(httpContext);
    // //
    // //     dispatcher.Received(1).DispatchAsync(httpContext);
    // // }
    //
    // [Theory]
    // [InlineData("")]
    // [InlineData("a/b")]
    // public void MapLiveLogViewer_SpecifiedBasePathDoesNotStartWithASlash_Throws(string basePath)
    // {
    //     // Arrange
    //
    //     // Act
    //     var action = () => _endpointRouteBuilder.MapLiveLogViewer(basePath: basePath);
    //
    //     // Assert
    //     action.Should().ThrowExactly<ArgumentException>().WithParameterName("basePath").WithMessage("*start*/*");
    // }
    //
    // [Fact]
    // public void MapLiveLogViewer_SpecifiedBasePathIsNull_Throws()
    // {
    //     // Arrange
    //
    //     // Act
    //     var action = () => _endpointRouteBuilder.MapLiveLogViewer(basePath: null!);
    //
    //     // Assert
    //     action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("basePath");
    // }

    #region Helpers

    private class FakeApplicationBuilder : IApplicationBuilder
    {
        private readonly Lazy<IServiceProvider> _serviceProvider;

        private readonly Stack<Func<RequestDelegate, RequestDelegate>> _middlewareStack;

        public FakeApplicationBuilder(Lazy<IServiceProvider> serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _middlewareStack = new Stack<Func<RequestDelegate, RequestDelegate>>();
        }

        /// <inheritdoc/>
        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            var x = middleware(_ => Task.CompletedTask);

            _middlewareStack.Push(middleware);
            return this;
        }

        public IServiceProvider ApplicationServices
        {
            get => _serviceProvider.Value;
            set => throw new NotSupportedException();
        }

        public IApplicationBuilder New() => new FakeApplicationBuilder(_serviceProvider);

        public RequestDelegate Build()
        {
            RequestDelegate pipeline = _ => Task.CompletedTask;
            while (_middlewareStack.TryPop(out var middleware))
            {
                pipeline = middleware(pipeline);
            }

            return pipeline;
        }

        public IFeatureCollection ServerFeatures => throw new NotSupportedException();
        public IDictionary<string, object?> Properties => throw new NotSupportedException();
    }

    #endregion
}