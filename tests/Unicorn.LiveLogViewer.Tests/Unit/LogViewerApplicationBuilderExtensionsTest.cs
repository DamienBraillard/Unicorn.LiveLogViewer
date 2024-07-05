using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit;

public class LogViewerApplicationBuilderExtensionsTest
{
    private readonly ServiceCollection _serviceCollection;
    private readonly Lazy<IServiceProvider> _serviceProvider;

    public LogViewerApplicationBuilderExtensionsTest()
    {
        _serviceCollection = [];
        _serviceProvider = new Lazy<IServiceProvider>(() => _serviceCollection.BuildServiceProvider());
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
}