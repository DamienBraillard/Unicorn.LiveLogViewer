using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;
using Unicorn.LiveLogViewer.Models;
using Unicorn.LiveLogViewer.Sources;
using Unicorn.LiveLogViewer.Tests.Helpers;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit;

public class LogViewerMiddlewareTest
{
    private readonly ILogProvider _sourceProvider;
    private readonly RequestDelegate _next;
    private readonly LogViewerMiddlewareDouble _target;

    public LogViewerMiddlewareTest()
    {
        _sourceProvider = Substitute.For<ILogProvider>();
        _next = Substitute.For<RequestDelegate>();
        _target = Substitute.ForPartsOf<LogViewerMiddlewareDouble>(_sourceProvider);
    }

    [Fact]
    public void Constructor_ProviderIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => new LogViewerMiddleware(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("sourceProvider");
    }

    [Theory]
    [CombinatorialData]
    public async Task InvokeAsync_ValidGetLogEventsPath_InvokesGetLogEventsAsync(bool requestPathHasTrailingSlash)
    {
        // Arrange
        var context = CreateContext(path: requestPathHasTrailingSlash ? "/sources/my-source-id/" : "/sources/my-source-id");

        _target.WhenForAnyArgs(o => o.PublicGetLogEventsAsync(default!, default!, default)).DoNotCallBase();

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        await _target.Received(1).PublicGetLogEventsAsync("my-source-id", context.Response, context.RequestAborted);
    }

    [Theory]
    [CombinatorialData]
    public async Task InvokeAsync_ValidGetLogSourcesPath_InvokesGetLogSourcesAsync(bool requestPathHasTrailingSlash)
    {
        // Arrange
        var context = CreateContext(path: requestPathHasTrailingSlash ? "/sources/" : "/sources");

        _target.WhenForAnyArgs(o => o.PublicGetLogSourcesAsync(default!, default)).DoNotCallBase();

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        await _target.Received(1).PublicGetLogSourcesAsync(context.Response, context.RequestAborted);
    }

    [Theory]
    [InlineData("/sources")]
    [InlineData("/sources/my-source-id")]
    public async Task InvokeAsync_ValidPath_DoesNotInvokeNext(string path)
    {
        // Arrange
        var context = CreateContext(path: path);

        _target.WhenForAnyArgs(o => o.PublicGetLogEventsAsync(default!, default!, default)).DoNotCallBase();
        _target.WhenForAnyArgs(o => o.PublicGetLogSourcesAsync(default!, default)).DoNotCallBase();

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        await _next.DidNotReceiveWithAnyArgs().Invoke(default!);
    }

    [Fact]
    public async Task InvokeAsync_InvalidPath_InvokesNext()
    {
        // Arrange
        var context = CreateContext(path: "/bad-or-invalid-path");

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task GetLogEventsAsync_Always_OpensTheSource()
    {
        // Arrange
        var context = CreateContext();

        // Act
        await _target.PublicGetLogEventsAsync("my-source-id", context.Response, context.RequestAborted);

        // Assert
        await _sourceProvider.Received(1).OpenAsync("my-source-id", context.RequestAborted);
    }

    [Fact]
    public async Task GetLogEventsAsync_SourceExists_DisposesTheSource()
    {
        // Arrange
        var context = CreateContext();

        var source = Substitute.For<ILogSource>();
        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs(source);

        // Act
        await _target.PublicGetLogEventsAsync("my-source-id", context.Response, context.RequestAborted);

        // Assert
        await source.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task GetLogEventsAsync_SourceExists_ReadsTheLogEvents()
    {
        // Arrange
        var context = CreateContext();

        var source = Substitute.For<ILogSource>();
        source.ReadAsync(default!, default).ReturnsForAnyArgs(0);

        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs(source);

        // Act
        await _target.PublicGetLogEventsAsync("my-source-id", context.Response, context.RequestAborted);

        // Assert
        await source.Received(1).ReadAsync(Arg.Is<ArraySegment<LogEvent>>(x => x.Count > 50), context.RequestAborted);
    }

    [Fact]
    public async Task GetLogEventsAsync_SourceExistsAndHasEvents_SetsTheHttpHeaders()
    {
        // Arrange
        var context = CreateContext();

        var source = Substitute.For<ILogSource>();
        source.ReadAsync(default!, default).ReturnsForAnyArgs(
            ci =>
            {
                var buffer = ci.Arg<ArraySegment<LogEvent>>();
                buffer[0] = new LogEvent();
                return 1;
            },
            _ => 0);

        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs(source);

        // Act
        await _target.PublicGetLogEventsAsync("my-source-id", context.Response, context.RequestAborted);

        // Assert
        context.Response.Headers.ContentType.Should().BeEquivalentTo("application/json; charset=utf-8");
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetLogEventsAsync_SourceExistsAndHasNoEvents_SetsTheHttpHeaders()
    {
        // Arrange
        var context = CreateContext();

        var source = Substitute.For<ILogSource>();
        source.ReadAsync(default!, default).ReturnsForAnyArgs(0);

        // Act
        await _target.PublicGetLogEventsAsync("my-source-id", context.Response, context.RequestAborted);

        // Assert
        context.Response.Headers.ContentType.Should().BeEmpty();
        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task GetLogEventsAsync_SourceExistsAndYields3Events_WritesTheLogEvents()
    {
        // Arrange
        var bodyStream = new MemoryStream();
        var context = CreateContext(bodyStream: bodyStream);

        LogEvent[] events =
        [
            new LogEvent { Message = "entry-1" },
            new LogEvent { Message = "entry-2" },
            new LogEvent { Message = "entry-3" },
        ];

        var source = Substitute.For<ILogSource>();
        source.ReadAsync(default!, default).ReturnsForAnyArgs(
            ci =>
            {
                var buffer = ci.Arg<ArraySegment<LogEvent>>();
                buffer[0] = events[0];
                buffer[1] = events[1];
                return 2;
            },
            ci =>
            {
                var buffer = ci.Arg<ArraySegment<LogEvent>>();
                buffer[0] = events[2];
                return 1;
            },
            _ => 0);

        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs(source);

        // Act
        await _target.PublicGetLogEventsAsync("my-source-id", context.Response, context.RequestAborted);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);

        bodyStream.Position = 0;
        var writtenEvents = bodyStream.ParseJson<LogEvent[]>().OfType<LogEvent[]>().SelectMany(o => o).ToList();
        writtenEvents.Should().BeEquivalentTo(events);
    }

    [Fact]
    public async Task GetLogEventsAsync_SourceDoesNotExists_SetsTheHttpHeaders()
    {
        // Arrange
        var context = CreateContext();

        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs((ILogSource?)null);

        // Act
        await _target.PublicGetLogEventsAsync("my-source-id", context.Response, context.RequestAborted);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetLogEventsAsync_SourceDoesNotExists_DoesNotWriteToTheBody()
    {
        // Arrange
        var context = CreateContext(bodyStream: new MemoryStream());

        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs((ILogSource?)null);

        // Act
        await _target.PublicGetLogEventsAsync("my-source-id", context.Response, context.RequestAborted);

        // Assert
        context.Response.Body.Should().HaveLength(0);
    }

    [Fact]
    public async Task GetLogSources_Always_QueriesTheListOfSources()
    {
        // Arrange
        var context = CreateContext();

        // Act
        await _target.PublicGetLogSourcesAsync(context.Response, context.RequestAborted);

        // Assert
        await _sourceProvider.Received(1).GetLogSourcesAsync(context.RequestAborted);
    }

    [Fact]
    public async Task GetLogSources_SourcesFound_SetsTheHttpHeaders()
    {
        // Arrange
        var context = CreateContext();
        _sourceProvider.GetLogSourcesAsync(default).ReturnsForAnyArgs([new LogSourceInfo()]);

        // Act
        await _target.PublicGetLogSourcesAsync(context.Response, context.RequestAborted);

        // Assert
        context.Response.Headers.ContentType.Should().BeEquivalentTo("application/json; charset=utf-8");
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetLogSources_SourcesFound_WritesTheSourceInfos()
    {
        // Arrange
        var bodyStream = new MemoryStream();
        var context = CreateContext(bodyStream: bodyStream);

        LogSourceInfo[] sources =
        [
            new LogSourceInfo { Id = "my-source-1", IsLive = true, Name = "my-name-1" },
            new LogSourceInfo { Id = "my-source-2", IsLive = false, Name = "my-name-2" },
            new LogSourceInfo { Id = "my-source-3", IsLive = false, Name = "my-name-3" },
        ];
        _sourceProvider.GetLogSourcesAsync(default).ReturnsForAnyArgs(sources);

        // Act
        await _target.PublicGetLogSourcesAsync(context.Response, context.RequestAborted);

        // Assert
        bodyStream.Position = 0;
        var writtenEvents = bodyStream.ParseJson<LogSourceInfo[]>().OfType<LogSourceInfo[]>().SelectMany(o => o).ToList();
        writtenEvents.Should().BeEquivalentTo(sources);
    }

    [Fact]
    public async Task GetLogSources_NoSourcesFound_SetsTheHttpHeaders()
    {
        // Arrange
        var context = CreateContext();
        _sourceProvider.GetLogSourcesAsync(default).ReturnsForAnyArgs([]);

        // Act
        await _target.PublicGetLogSourcesAsync(context.Response, context.RequestAborted);

        // Assert
        context.Response.Headers.ContentType.Should().BeEmpty();
        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task GetLogSources_NoLogSources_DoesNotWriteToTheBody()
    {
        // Arrange
        var context = CreateContext(bodyStream: new MemoryStream());
        _sourceProvider.GetLogSourcesAsync(default).ReturnsForAnyArgs([]);

        // Act
        await _target.PublicGetLogSourcesAsync(context.Response, context.RequestAborted);

        // Assert
        context.Response.Body.Should().HaveLength(0);
    }

    #region Helpers

    // ReSharper disable all

    private static HttpContext CreateContext(string path = "", Stream? bodyStream = null)
    {
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature { Path = path, Method = HttpMethods.Get });
        features.Set<IHttpResponseFeature>(new HttpResponseFeature() { Body = bodyStream ?? Stream.Null });
        features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(bodyStream ?? Stream.Null));

        var context = new DefaultHttpContext(features)
        {
            RequestAborted = new CancellationTokenSource().Token,
        };

        return context;
    }

    internal class LogViewerMiddlewareDouble(ILogProvider sourceProvider) : LogViewerMiddleware(sourceProvider)
    {
        public virtual Task PublicGetLogEventsAsync(string sourceId, HttpResponse response, CancellationToken cancellationToken) => base.GetLogEventsAsync(sourceId, response, cancellationToken);
        protected sealed override Task GetLogEventsAsync(string sourceId, HttpResponse response, CancellationToken cancellationToken) => PublicGetLogEventsAsync(sourceId, response, cancellationToken);

        public virtual Task PublicGetLogSourcesAsync(HttpResponse response, CancellationToken cancellationToken) => base.GetLogSourcesAsync(response, cancellationToken);
        protected sealed override Task GetLogSourcesAsync(HttpResponse response, CancellationToken cancellationToken) => PublicGetLogSourcesAsync(response, cancellationToken);
    }

    // ReSharper restore all

    #endregion
}