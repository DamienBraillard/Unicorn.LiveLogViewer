using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;
using Unicorn.LiveLogViewer.Sources;
using Unicorn.LiveLogViewer.Tests.Helpers;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit;

public class LogViewerMiddlewareTest
{
    private readonly ILogProvider _sourceProvider;
    private readonly RequestDelegate _next;
    private readonly LogViewerMiddleware _target;

    public LogViewerMiddlewareTest()
    {
        _sourceProvider = Substitute.For<ILogProvider>();
        _next = Substitute.For<RequestDelegate>();
        _target = new LogViewerMiddleware(_sourceProvider);
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

    [Fact]
    public async Task InvokeAsync_RouteMatches_SetsTheHttpHeaders()
    {
        // Arrange
        var context = CreateContext(path: "/sources/my-source");

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        context.Response.Headers.ContentType.Should().BeEquivalentTo("application/json; charset=utf-8");
    }

    [Theory]
    [InlineData("/sources/my-source", "my-source")]
    [InlineData("/sources/my-source/", "my-source")]
    public async Task InvokeAsync_RouteMatches_OpensTheSource(string requestPath, string expectedSourceName)
    {
        // Arrange
        var context = CreateContext(path: requestPath);

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        await _sourceProvider.Received(1).OpenAsync(expectedSourceName, context.RequestAborted);
    }

    [Fact]
    public async Task InvokeAsync_RouteMatches_DisposesTheSource()
    {
        // Arrange
        var context = CreateContext(path: "/sources/my-source");

        var source = Substitute.For<ILogSource>();
        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs(source);

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        await source.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task InvokeAsync_RouteMatches_ReadsTheLogEvents()
    {
        // Arrange
        var context = CreateContext(path: "/sources/my-source");

        var source = Substitute.For<ILogSource>();
        source.ReadAsync(default!, default).ReturnsForAnyArgs(0);

        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs(source);

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        await source.Received(1).ReadAsync(Arg.Is<ArraySegment<LogEvent>>(x => x.Count > 50), context.RequestAborted);
    }

    [Fact]
    public async Task InvokeAsync_RouteMatches_WritesTheLogEvents()
    {
        // Arrange
        var bodyStream = new MemoryStream();
        var context = CreateContext(path: "/sources/my-source", bodyStream: bodyStream);

        var entry1 = new LogEvent { Message = "entry-1" };
        var entry2 = new LogEvent { Message = "entry-2" };

        var source = Substitute.For<ILogSource>();
        source.ReadAsync(default!, default).ReturnsForAnyArgs(
            ci =>
            {
                var buffer = ci.Arg<ArraySegment<LogEvent>>();
                buffer[0] = entry1;
                buffer[1] = entry2;
                return 2;
            },
            _ => 0);

        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs(source);

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        bodyStream.Position = 0;
        var writtenEvents = bodyStream.ParseJson<LogEvent[]>().OfType<LogEvent[]>().SelectMany(o => o).ToList();
        writtenEvents.Should().BeEquivalentTo([entry1, entry2]);
    }

    [Fact]
    public async Task InvokeAsync_RouteMatches_DoesNotInvokeNext()
    {
        // Arrange
        var context = CreateContext(path: "/sources/my-source");

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        await _next.DidNotReceiveWithAnyArgs().Invoke(default!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/sources/")]
    [InlineData("/other/my-source")]
    public async Task InvokeAsync_RouteDoesNotMatch_InvokesNext(string requestPath)
    {
        // Arrange
        var context = CreateContext(path: requestPath);

        // Act
        await _target.InvokeAsync(context, _next);

        // Assert
        await _next.Received(1).Invoke(context);
    }

    #region Helpers

    // ReSharper disable all

    private static HttpContext CreateContext(string path, Stream? bodyStream = null)
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

    // ReSharper restore all

    #endregion
}