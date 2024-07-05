using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Unicorn.LiveLogViewer.Tests.Helpers;

public class TestServerBuilder : IWebHostBuilder, IDisposable
{
    private bool _isDisposed;
    private TestServer? _server;
    private readonly List<Action<WebApplicationBuilder>> _webApplicationBuilderSetupDelegates = [];
    private readonly List<Action<WebApplication>> _webApplicationSetupDelegates = [];

    /// <summary>
    /// Registers a callback that configures the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <param name="setup">The action that can receives the <see cref="WebApplicationBuilder"/> to configure.</param>
    /// <returns>The current <see cref="TestServerBuilder"/> for method chaining.</returns>
    public TestServerBuilder SetupWebApplicationBuilder(Action<WebApplicationBuilder> setup)
    {
        ThrowIfServerStartedOrDisposed();
        _webApplicationBuilderSetupDelegates.Add(setup);
        return this;
    }

    /// <summary>
    /// Registers a callback that configures the <see cref="WebApplication"/>.
    /// </summary>
    /// <param name="setup">The action that can receives the <see cref="WebApplication"/> to configure.</param>
    /// <returns>The current <see cref="TestServerBuilder"/> for method chaining.</returns>
    public TestServerBuilder SetupWebApplication(Action<WebApplication> setup)
    {
        ThrowIfServerStartedOrDisposed();
        _webApplicationSetupDelegates.Add(setup);
        return this;
    }

    /// <summary>
    /// Builds and starts the web application.
    /// </summary>
    /// <returns>The <see cref="TestServer"/> that runs the application.</returns>
    public TestServer StartServer()
    {
        ThrowIfServerStartedOrDisposed();
        _server = new TestServer(this);
        return _server;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _server?.Dispose();
        _server = null;
        _webApplicationBuilderSetupDelegates.Clear();
        _webApplicationSetupDelegates.Clear();
        _isDisposed = true;
    }

    private void ThrowIfServerStartedOrDisposed([CallerMemberName] string? caller = null)
    {
        if (_server != null)
            throw new InvalidOperationException($"{caller} cannot be used once the TestServer has been started");
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestServerBuilder));
    }

    IWebHost IWebHostBuilder.Build()
    {
        var builder = WebApplication.CreateBuilder();
        foreach (var setupDelegate in _webApplicationBuilderSetupDelegates)
        {
            setupDelegate(builder);
        }

        var app = builder.Build();
        foreach (var setupDelegate in _webApplicationSetupDelegates)
        {
            setupDelegate(app);
        }

        return new TestWebHost(app);
    }

    IWebHostBuilder IWebHostBuilder.ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate) => throw new NotSupportedException();

    IWebHostBuilder IWebHostBuilder.ConfigureServices(Action<IServiceCollection> configureServices)
    {
        _webApplicationBuilderSetupDelegates.Add(builder => configureServices(builder.Services));
        return this;
    }

    IWebHostBuilder IWebHostBuilder.ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices) => throw new NotSupportedException();

    string IWebHostBuilder.GetSetting(string key) => throw new NotSupportedException();

    IWebHostBuilder IWebHostBuilder.UseSetting(string key, string? value) => throw new NotSupportedException();

    private class TestWebHost(WebApplication application) : IWebHost
    {
        public void Dispose() => application.DisposeAsync().AsTask().GetAwaiter().GetResult();
        public void Start() => StartAsync().GetAwaiter().GetResult();
        public Task StartAsync(CancellationToken cancellationToken = default) => application.StartAsync(cancellationToken);
        public Task StopAsync(CancellationToken cancellationToken = default) => application.StopAsync(cancellationToken);
        public IFeatureCollection ServerFeatures => Services.GetRequiredService<IServer>().Features;
        public IServiceProvider Services => application.Services;
    }
}