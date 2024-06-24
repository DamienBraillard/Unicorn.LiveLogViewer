using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using Unicorn.LiveLogViewer.RequestProcessing;
using Unicorn.LiveLogViewer.StaticContent;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.RequestProcessing;

public class StaticContentRequestHandlerTest
{
    private readonly IFileProvider _fileProviderA;
    private readonly IFileProvider _fileProviderB;
    private readonly IFileProvider _fileProviderC;
    private readonly LogViewerOptions _options;
    private readonly StaticContentRequestHandler _target;

    public StaticContentRequestHandlerTest()
    {
        _fileProviderA = Substitute.For<IFileProvider>();
        _fileProviderB = Substitute.For<IFileProvider>();
        _fileProviderC = Substitute.For<IFileProvider>();

        _options = new LogViewerOptions
        {
            StaticContentProviders = [_fileProviderA, _fileProviderB, _fileProviderC]
        };

        _target = new StaticContentRequestHandler(_options);
    }

    [Fact]
    public void Constructor_OptionsIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => new StaticContentRequestHandler(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task TryHandleRequestAsync_PathIsEmpty_RedirectsToSlash()
    {
        // Arrange
        var context = CreateContext("");

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status301MovedPermanently);
        context.Response.Headers.Location.Should().BeEquivalentTo("/");
    }

    [Fact]
    public async Task TryHandleRequestAsync_PathIsSlash_ServeTheHtmlPage()
    {
        // Arrange
        var context = CreateContext("/");

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        _fileProviderA.Received(1).GetFileInfo(StaticFiles.Page);
        _fileProviderB.Received(1).GetFileInfo(StaticFiles.Page);
        _fileProviderC.Received(1).GetFileInfo(StaticFiles.Page);
    }

    [Theory]
    [InlineData('A', "A")]
    [InlineData('B', "A,B")]
    [InlineData('C', "A,B,C")]
    public async Task TryHandleRequestAsync_OneFileProviderHasTheFile_StopAtFirstProvider(char successProvider, string expectedProviders)
    {
        // Arrange
        var context = CreateContext("/my-file.html");

        var calledProviders = new List<string>();
        _fileProviderA.GetFileInfo(default!).ReturnsForAnyArgs(ci =>
        {
            calledProviders.Add("A");
            return CreateFileInfo(ci.Arg<string>(), exists: successProvider == 'A');
        });
        _fileProviderB.GetFileInfo(default!).ReturnsForAnyArgs(ci =>
        {
            calledProviders.Add("B");
            return CreateFileInfo(ci.Arg<string>(), exists: successProvider == 'B');
        });
        _fileProviderC.GetFileInfo(default!).ReturnsForAnyArgs(ci =>
        {
            calledProviders.Add("C");
            return CreateFileInfo(ci.Arg<string>(), exists: successProvider == 'C');
        });

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        calledProviders.Should().BeEquivalentTo(expectedProviders.Split(','), opts => opts.WithStrictOrdering());
    }

    [Fact]
    public async Task TryHandleRequestAsync_FileNotFound_ReturnsFalse()
    {
        // Arrange
        var context = CreateContext("/my-file.html");
        _fileProviderA.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html", exists: false));
        _fileProviderB.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html", exists: false));
        _fileProviderC.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html", exists: false));

        // Act
        var result = await _target.TryHandleRequestAsync(context);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleRequestAsync_FileNotFound_DoesNotSetTheStatusCode()
    {
        // Arrange
        var context = CreateContext("/my-file.html");
        context.Response.StatusCode = 999;
        _fileProviderA.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html", exists: false));
        _fileProviderB.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html", exists: false));
        _fileProviderC.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html", exists: false));

        // Act
        var result = await _target.TryHandleRequestAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(999);
    }

    [Fact]
    public async Task TryHandleRequestAsync_FileNotFound_ReturnsTrue()
    {
        // Arrange
        var context = CreateContext("/my-file.html");
        _fileProviderA.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html", exists: true));
        _fileProviderB.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html", exists: true));
        _fileProviderC.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html", exists: true));

        // Act
        var result = await _target.TryHandleRequestAsync(context);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("my-file.html", "text/html")]
    [InlineData("my-file.js", "text/javascript")]
    [InlineData("my-file.css", "text/css")]
    [InlineData("my-file.HTML", "text/html")]
    [InlineData("my-file.JS", "text/javascript")]
    [InlineData("my-file.CSS", "text/css")]
    public async Task TryHandleAsync_FileFoundWithValidContentType_SetsTheResponseContentType(string fileName, string expected)
    {
        // Arrange
        var context = CreateContext($"/{fileName}");
        _fileProviderA.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo(fileName));

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        context.Response.ContentType.Should().Be(expected);
    }

    [Fact]
    public async Task TryHandleAsync_FileFoundWithInvalidContentType_Throws()
    {
        // Arrange
        var context = CreateContext("/my-file.txt");
        _fileProviderA.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.txt"));

        // Act
        var action = () => _target.TryHandleRequestAsync(context);

        // Assert
        await action.Should().ThrowExactlyAsync<NotSupportedException>().WithMessage("*unsupported*(.txt)*supported*\".html\", \".css\" and \".js\"*");
    }

    [Fact]
    public async Task TryHandleAsync_FileFound_DisablesBrowserCaching()
    {
        // Arrange
        var context = CreateContext("/my-file.html");
        _fileProviderA.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html"));

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        context.Response.GetTypedHeaders().CacheControl.Should().BeEquivalentTo(new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MustRevalidate = true,
        });
    }

    [Fact]
    public async Task TryHandleAsync_FileFound_SetsTheResponseStatus()
    {
        // Arrange
        var context = CreateContext("/my-file.html");
        _fileProviderA.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html"));

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task TryHandleAsync_FileFound_WritesTheResponse()
    {
        // Arrange
        var bodyStream = new MyMagicalStream();
        var context = CreateContext("/my-file.html", bodyStream: bodyStream);
        var data = new MemoryStream("<my-response>"u8.ToArray());
        _fileProviderA.GetFileInfo(default!).ReturnsForAnyArgs(_ => CreateFileInfo("my-file.html", content: data));

        // Act
        await _target.TryHandleRequestAsync(context);

        // Assert
        var actualContent = Encoding.UTF8.GetString(bodyStream.ToArray());
        actualContent.Should().Be("<my-response>");
    }

    #region Helpers

    // ReSharper disable all

    private class MyMagicalStream : MemoryStream;

    private static HttpContext CreateContext(string path, Stream? bodyStream = null)
    {
        var features = new FeatureCollection();
        features.Set<IHttpRequestFeature>(new HttpRequestFeature { Path = path });
        features.Set<IHttpResponseFeature>(new HttpResponseFeature() { Body = bodyStream ?? Stream.Null });
        features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(bodyStream ?? Stream.Null));

        var context = new DefaultHttpContext(features);

        return context;
    }

    private static IFileInfo CreateFileInfo(string name, bool exists = true, Stream? content = null)
    {
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Name.Returns(name);
        fileInfo.Exists.Returns(exists);
        fileInfo.CreateReadStream().Returns(content ?? Stream.Null);
        return fileInfo;
    }

    // ReSharper restore all

    #endregion
}