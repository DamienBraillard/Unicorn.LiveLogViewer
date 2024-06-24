using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Unicorn.LiveLogViewer.RequestProcessing;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.RequestProcessing;

public class LogViewerRequestDispatcherTest
{
    private readonly ILogViewerRequestHandler _handlerA;
    private readonly ILogViewerRequestHandler _handlerB;
    private readonly ILogViewerRequestHandler _handlerC;
    private readonly LogViewerOptions _options;
    private readonly LogViewerRequestDispatcher _target;

    public LogViewerRequestDispatcherTest()
    {
        _handlerA = Substitute.For<ILogViewerRequestHandler>();
        _handlerB = Substitute.For<ILogViewerRequestHandler>();
        _handlerC = Substitute.For<ILogViewerRequestHandler>();
        _options = new LogViewerOptions();
        _target = new LogViewerRequestDispatcher(_options, new[] { _handlerA, _handlerB, _handlerC });
    }

    [Fact]
    public void Constructor_OptionsIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => new LogViewerRequestDispatcher(null!, [_handlerA]);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_HandlersIsEmpty_Throws()
    {
        // Arrange

        // Act
        var action = () => new LogViewerRequestDispatcher(_options, []);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithParameterName("handlers").WithMessage("*empty*");
    }

    [Fact]
    public void Constructor_HandlersIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => new LogViewerRequestDispatcher(_options, null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("handlers");
    }

    [Theory]
    [InlineData("/log/viewer")]
    [InlineData("/log/viewer/")]
    public void BasePath_Created_ReturnsTheOptionsValue(string value)
    {
        // Arrange
        _options.BasePath = value;

        // Act
        var basePath = _target.BasePath;

        // Assert
        basePath.Should().Be(value);
    }

    [Theory]
    [InlineData("/log/viewer/sub-path", "/log/viewer", true)]
    [InlineData("/log/viewer/", "/log/viewer", true)]
    [InlineData("/log/viewer", "/log/viewer", true)]
    [InlineData("/LOG/viewer/sub-path", "/log/VIEWER", true)]
    [InlineData("/LOG/viewer/", "/log/VIEWER", true)]
    [InlineData("/LOG/viewer", "/log/VIEWER", true)]
    [InlineData("/something-else", "/log/viewer", false)]
    public async Task DispatchAsync_VariousConfigurations_ProcessesIfBasePathMatches(string requestPath, string basePath, bool shouldProcess)
    {
        // Arrange
        _options.BasePath = basePath;
        var context = CreateContext(requestPath);

        // Act
        await _target.DispatchAsync(context);

        // Assert
        if (shouldProcess)
        {
            await _handlerA.Received(1).TryHandleRequestAsync(context);
            await _handlerB.Received(1).TryHandleRequestAsync(context);
            await _handlerC.Received(1).TryHandleRequestAsync(context);
        }
        else
        {
            await _handlerA.DidNotReceiveWithAnyArgs().TryHandleRequestAsync(context);
            await _handlerB.DidNotReceiveWithAnyArgs().TryHandleRequestAsync(context);
            await _handlerC.DidNotReceiveWithAnyArgs().TryHandleRequestAsync(context);
        }
    }

    [Fact]
    public async Task DispatchAsync_PathMatches_AltersRequestPathAndPathBaseForRequestProcessing()
    {
        // Arrange
        _options.BasePath = "/log/viewer";
        var context = CreateContext(path: "/log/viewer/x/y/z", basePath: "/root");

        (string PathBase, string Path)? valuesDuringProcessing = null;

        await _handlerA.TryHandleRequestAsync(Arg.Do<HttpContext>(x => valuesDuringProcessing = (x.Request.PathBase, x.Request.Path)));

        // Act
        await _target.DispatchAsync(context);

        // Assert
        valuesDuringProcessing?.PathBase.Should().Be("/root/log/viewer");
        valuesDuringProcessing?.Path.Should().Be("/x/y/z");
    }

    [Theory]
    [CombinatorialData]
    public async Task DispatchAsync_PathMatches_RestoresRequestPathAndPathBaseAfterRequestProcessing(bool handlerThrows)
    {
        // Arrange
        _options.BasePath = "/log/viewer";
        var context = CreateContext(path: "/log/viewer/x/y/z", basePath: "/root");

        if (handlerThrows)
            _handlerA.TryHandleRequestAsync(default!).ThrowsAsyncForAnyArgs(new Exception("Kaboom"));

        // Act
        var action = () => _target.DispatchAsync(context);

        // Assert
        if (handlerThrows)
            await action.Should().ThrowAsync<Exception>();
        else
            await action.Should().NotThrowAsync();

        context.Request.PathBase.ToString().Should().Be("/root");
        context.Request.Path.ToString().Should().Be("/log/viewer/x/y/z");
    }

    [Theory]
    [InlineData('A', "A")]
    [InlineData('B', "A,B")]
    [InlineData('C', "A,B,C")]
    public async Task DispatchAsync_OneHandlerMatches_StopAtFirstHandler(char successHandler, string expectedHandlers)
    {
        // Arrange
        _options.BasePath = "/log/viewer";
        var context = CreateContext("/log/viewer");

        var calledHandlers = new List<string>();
        _handlerA.TryHandleRequestAsync(default!).ReturnsForAnyArgs(_ =>
        {
            calledHandlers.Add("A");
            return successHandler == 'A';
        });
        _handlerB.TryHandleRequestAsync(default!).ReturnsForAnyArgs(_ =>
        {
            calledHandlers.Add("B");
            return successHandler == 'B';
        });
        _handlerC.TryHandleRequestAsync(default!).ReturnsForAnyArgs(_ =>
        {
            calledHandlers.Add("C");
            return successHandler == 'C';
        });

        // Act
        await _target.DispatchAsync(context);

        // Assert
        calledHandlers.Should().BeEquivalentTo(expectedHandlers.Split(','), opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task DispatchAsync_NoHandlerMatches_SetsResponseTo404()
    {
        // Arrange
        _options.BasePath = "/log/viewer";
        var context = CreateContext("/log/viewer");

        // Act
        await _target.DispatchAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DispatchAsync_HttpContextIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _target.DispatchAsync(null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    #region Helpers

    // ReSharper disable all

    private static HttpContext CreateContext(string path, string basePath = "")
    {
        var context = new DefaultHttpContext()
        {
            Request = { PathBase = basePath, Path = path }
        };
        return context;
    }

    // ReSharper restore all

    #endregion
}