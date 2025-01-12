using System;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Unicorn.LiveLogViewer.Sources;
using Unicorn.LiveLogViewer.Tests.Helpers.AspNet;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit;

public class LogViewerApplicationExtensionsTest
{
    [Theory]
    [MemberData(nameof(AddOverloadTestCase.TheoryData), MemberType = typeof(AddOverloadTestCase))]
    public void AddLiveLogViewer_AllOverloads_RegistersTheNullLogProviderAsSingleton(AddOverloadTestCase testCase)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        testCase.CallCorrectOverload(services);

        // Assert
        GetServiceAndVerifySingleton<ILogProvider>(services)
            .Should().NotBeNull().And.BeOfType<NullLogProvider>();
    }

    [Fact]
    public void AddLiveLogViewer_WithoutOptions_RegistersDefaultOptionsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLiveLogViewer();

        // Assert
        GetServiceAndVerifySingleton<LogViewerOptions>(services)
            .Should().BeEquivalentTo(new LogViewerOptions());
    }

    [Fact]
    public void AddLiveLogViewer_WithOptions_RegistersTheSpecifiedOptionsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new LogViewerOptions();

        // Act
        services.AddLiveLogViewer(options);

        // Assert
        GetServiceAndVerifySingleton<LogViewerOptions>(services)
            .Should().BeSameAs(options);
    }

    [Fact]
    public void AddLiveLogViewer_WithOptionsBuilder_RegistersTheConfiguredOptionsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        LogViewerOptions? configuredOptions = null;

        // Act
        services.AddLiveLogViewer((_, x) => configuredOptions = x);

        // Assert
        GetServiceAndVerifySingleton<LogViewerOptions>(services)
            .Should().BeSameAs(configuredOptions);
    }

    [Fact]
    public void AddLiveLogViewer_WithOptionsBuilder_InvokesSetupDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        var optionsBuilder = Substitute.For<Action<IServiceProvider, LogViewerOptions>>();

        services.AddSingleton("dummy-service");

        // Act
        services.AddLiveLogViewer(optionsBuilder);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<LogViewerOptions>();

        optionsBuilder.Received(1).Invoke(
            Arg.Is<IServiceProvider>(sp => sp.GetService<string>() == "dummy-service"),
            options);
    }

    [Theory]
    [MemberData(nameof(AddOverloadTestCase.TheoryData), MemberType = typeof(AddOverloadTestCase))]
    public void AddLiveLogViewer_AllOverloads_ReturnsTheServiceCollection(AddOverloadTestCase testCase)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = testCase.CallCorrectOverload(services);

        // Assert
        result.Should().BeSameAs(services);
    }

    [Theory]
    [MemberData(nameof(AddOverloadTestCase.TheoryData), MemberType = typeof(AddOverloadTestCase))]
    public void AddLiveLogViewer_ServicesIsNull_Throws(AddOverloadTestCase testCase)
    {
        // Arrange

        // Act
        var action = () => testCase.CallCorrectOverload(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddLiveLogViewer_OptionsIsNull_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        LogViewerOptions options = null!;

        // Act
        var action = () => services.AddLiveLogViewer(options);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void MapLiveLogViewer_CustomConventionAdded_AppliesTheConventionToTheMappedEndpoints()
    {
        // Arrange
        var endpointBuilder = new EndpointRegistrationTestBuilder();

        // Act
        endpointBuilder.MapLiveLogViewer("/my-base").Add(b => b.Metadata.Add(new TestMetadata()));

        // Assert
        endpointBuilder.RouteEndpoints.Should().AllSatisfy(e => e.HasMetadata<TestMetadata>().Should().BeTrue());
    }

    [Fact]
    public void MapLiveLogViewer_EndpointsRegistered_ExcludeEndpointsFromTheApiDescriptions()
    {
        // Arrange
        var endpointBuilder = new EndpointRegistrationTestBuilder();

        // Act
        endpointBuilder.MapLiveLogViewer("/my-base");

        // Assert
        endpointBuilder.RouteEndpoints.Should().AllSatisfy(e => e.HasMetadata<IExcludeFromDescriptionMetadata>().Should().BeTrue());
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    public void MapLiveLogViewer_NoBasePath_MapsAllEndpoints(string emptyBasePath)
    {
        // Arrange
        var endpointBuilder = new EndpointRegistrationTestBuilder();

        // Act
        endpointBuilder.MapLiveLogViewer(emptyBasePath);

        // Assert
        var routes = endpointBuilder.GetRouteDetails();
        routes.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("/my-base-path", "/my-base-path")]
    [InlineData("/my-base-path/", "/my-base-path")]
    [InlineData("/my-base-path//", "/my-base-path")]
    [InlineData("/my/base/path", "/my/base/path")]
    [InlineData("/my/base/path/", "/my/base/path")]
    [InlineData("/my/base/path//", "/my/base/path")]
    public void MapLiveLogViewer_BasePathSpecified_MapsTheEndpointsWithTheCorrectBasePath(string basePath, string expected)
    {
        // Arrange
        var endpointBuilder = new EndpointRegistrationTestBuilder();

        // Act
        endpointBuilder.MapLiveLogViewer(basePath);

        // Assert
        endpointBuilder.RouteEndpoints.Should().AllSatisfy(e => e.GetRoute().Should().StartWith(expected));
    }

    [Fact]
    public void MapLiveLogViewer_EndpointBuilderIsNull_Throws()
    {
        // Arrange
        IEndpointRouteBuilder endpointBuilder = null!;

        // Act
        var action = () => endpointBuilder.MapLiveLogViewer(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("endpointBuilder");
    }

    [Fact]
    public void MapLiveLogViewer_BasePathIsNull_Throws()
    {
        // Arrange
        var endpointBuilder = Substitute.For<IEndpointRouteBuilder>();

        // Act
        var action = () => endpointBuilder.MapLiveLogViewer(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("basePath");
    }

    [Fact]
    public void MapLiveLogViewer_BasePathDoesNotStartWithASlash_Throws()
    {
        // Arrange
        var endpointBuilder = Substitute.For<IEndpointRouteBuilder>();

        // Act
        var action = () => endpointBuilder.MapLiveLogViewer("my-base-path");

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithParameterName("basePath").WithMessage("*path (my-base-path)*start*slash*");
    }

    #region Theory Data

    public class AddOverloadTestCase(string testCase)
    {
        public static TheoryData<AddOverloadTestCase> TheoryData =>
        [
            new("With options"),
            new("With options builder")
        ];

        public IServiceCollection CallCorrectOverload(IServiceCollection callOn)
        {
            return testCase switch
            {
                "With options" => callOn.AddLiveLogViewer(),
                "With options builder" => callOn.AddLiveLogViewer((_, _) => { }),
                _ => throw new NotSupportedException($"Test case '{testCase}' is not supported.")
            };
        }

        public override string ToString() => testCase;
    }

    #endregion

    #region Helpers

    private static TService GetServiceAndVerifySingleton<TService>(IServiceCollection serviceCollection)
    {
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var service = serviceProvider.GetService<TService>();
        service.Should().NotBeNull(because: "The service should be registered.");

        var secondService = serviceProvider.GetService<TService>();
        secondService.Should().BeSameAs(service, because: "The service should be registered as a singleton.");

        return service!;
    }

    #endregion
}