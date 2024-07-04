using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;
using Unicorn.LiveLogViewer.RequestProcessing;
using Unicorn.LiveLogViewer.Sources;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.RequestProcessing;

public class LogEntriesRequestHandlerTest
{
    private readonly ILogProvider _sourceProvider;
    private readonly LogEntriesRequestHandler _target;

    public LogEntriesRequestHandlerTest()
    {
        _sourceProvider = Substitute.For<ILogProvider>();
        _target = new LogEntriesRequestHandler(_sourceProvider);
    }

    [Fact]
    public void Constructor_ProviderIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => new LogEntriesRequestHandler(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("sourceProvider");
    }

    [Fact]
    public async Task TryHandleRequestAsync_RouteMatches_SetsTheHttpHeaders()
    {
        // Arrange
        var context = CreateContext(path: "/sources/my-source");

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        context.Response.Headers.ContentType.Should().BeEquivalentTo("application/json; charset=utf-8");
    }

    [Theory]
    [InlineData("/sources/my-source", "my-source")]
    [InlineData("/sources/my-source/", "my-source")]
    public async Task TryHandleRequestAsync_RouteMatches_OpensTheSource(string requestPath, string expectedSourceName)
    {
        // Arrange
        var context = CreateContext(path: requestPath);

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        await _sourceProvider.Received(1).OpenAsync(expectedSourceName, context.RequestAborted);
    }

    [Fact]
    public async Task TryHandleRequestAsync_RouteMatches_DisposesTheSource()
    {
        // Arrange
        var context = CreateContext(path: "/sources/my-source");

        var source = Substitute.For<ILogSource>();
        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs(source);

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        await source.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task TryHandleRequestAsync_RouteMatches_ReadsTheLogEvents()
    {
        // Arrange
        var context = CreateContext(path: "/sources/my-source");

        var source = Substitute.For<ILogSource>();
        source.ReadAsync(default!, default).ReturnsForAnyArgs(0);

        _sourceProvider.OpenAsync(default!, default!).ReturnsForAnyArgs(source);

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        await source.Received(1).ReadAsync(Arg.Is<ArraySegment<LogEvent>>(x => x.Count > 50), context.RequestAborted);
    }

    [Fact]
    public async Task TryHandleRequestAsync_RouteMatches_WritesTheLogEvents()
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
        await _target.TryHandleRequestAsync(context);

        // Assert
        bodyStream.Position = 0;
        var writtenEvents = bodyStream.ParseJson<LogEvent[]>().OfType<LogEvent[]>().SelectMany(o => o).ToList();
        writtenEvents.Should().BeEquivalentTo([entry1, entry2]);
    }

    [Fact]
    public async Task TryHandleRequestAsync_RouteMatches_ReturnsTrue()
    {
        // Arrange
        var context = CreateContext(path: "/sources/my-source");

        // Act
        var result = await _target.TryHandleRequestAsync(context);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("/sources/")]
    [InlineData("/other/my-source")]
    public async Task TryHandleRequestAsync_RouteDoesNotMatch_ReturnsFalse(string requestPath)
    {
        // Arrange
        var context = CreateContext(path: requestPath);

        // Act
        var result = await _target.TryHandleRequestAsync(context);

        // Assert
        result.Should().BeFalse();
    }

    #region Helpers

    // ReSharper disable all

    private static HttpContext CreateContext(string path, Stream? bodyStream = null)
    {
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature { Path = path });
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