using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NSubstitute;
using Unicorn.LiveLogViewer.StaticContent;
using Unicorn.LiveLogViewer.Tests.Helpers;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Integration;

public class LogViewerApplicationBuilderExtensionsTest
{
    private static readonly IFileProvider DefaultFiles = new EmbeddedFileProvider(typeof(StaticFiles).Assembly, typeof(StaticFiles).Namespace);

    private readonly TestServerBuilder _builder;

    public LogViewerApplicationBuilderExtensionsTest()
    {
        _builder = new TestServerBuilder();
    }

    [Theory]
    [CombinatorialData]
    public async Task UseLiveLogViewer_AllDefaultStaticFiles_ServesTheStaticFile(bool useCustomBasePath, [CombinatorialMemberData(nameof(AvailableStaticFiles))] string file)
    {
        // Arrange
        using var server = _builder
            .SetupWebApplicationBuilder(builder => builder.Services.AddLiveLogViewer())
            .SetupWebApplication(app => _ = useCustomBasePath ? app.UseLiveLogViewer(basePath: "/my-test-path") : app.UseLiveLogViewer())
            .StartServer();

        var requestPath = $"{(useCustomBasePath ? "/my-test-path" : LogViewerApplicationBuilderExtensions.DefaultBasePath)}/{file}";

        var client = server.CreateClient();

        // Act
        var actualContent = await client.GetStringAsync(requestPath);

        // Assert
        var expectedContent = DefaultFiles.GetFileInfo(file).CreateReadStream().ToUtf8String();
        actualContent.Should().Be(expectedContent);
    }

    [Theory]
    [CombinatorialData]
    public async Task UseLiveLogViewer_BasePathWithTrailingSlashRequested_ServesTheMainPage(bool useCustomBasePath)
    {
        // Arrange
        using var server = _builder
            .SetupWebApplicationBuilder(builder => builder.Services.AddLiveLogViewer())
            .SetupWebApplication(app => _ = useCustomBasePath ? app.UseLiveLogViewer(basePath: "/my-test-path") : app.UseLiveLogViewer())
            .StartServer();

        var requestPath = $"{(useCustomBasePath ? "/my-test-path" : LogViewerApplicationBuilderExtensions.DefaultBasePath)}/";

        var client = server.CreateClient();

        // Act
        var actualContent = await client.GetStringAsync(requestPath);

        // Assert
        var expectedContent = DefaultFiles.GetFileInfo(StaticFiles.Page).CreateReadStream().ToUtf8String();
        actualContent.Should().Be(expectedContent);
    }

    [Theory]
    [CombinatorialData]
    public async Task UseLiveLogViewer_BasePathWithoutTrailingSlashRequested_RedirectToBasePathWithTrailingSlash(bool useCustomBasePath)
    {
        // Arrange
        using var server = _builder
            .SetupWebApplicationBuilder(builder => builder.Services.AddLiveLogViewer())
            .SetupWebApplication(app => _ = useCustomBasePath ? app.UseLiveLogViewer(basePath: "/my-test-path") : app.UseLiveLogViewer())
            .StartServer();

        var requestPath = useCustomBasePath ? "/my-test-path" : LogViewerApplicationBuilderExtensions.DefaultBasePath;

        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync(requestPath);

        // Assert
        response.Headers.Location?.Should().BeEquivalentTo(new Uri(server.BaseAddress, $"{requestPath}/"));
        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
    }

    [Theory]
    [CombinatorialData]
    public async Task UseLiveLogViewer_NonFileUrlRequested_InvokesTheMiddleware(bool useCustomBasePath)
    {
        // Arrange
        string? receivedRequestPath = null;
        var middleware = Substitute.For<ILogViewerMiddleware>();
        middleware.InvokeAsync(default!, default!).ReturnsForAnyArgs(async ci =>
        {
            var context = ci.Arg<HttpContext>();
            var next = ci.Arg<RequestDelegate>();
            receivedRequestPath = context.Request.Path;
            await next(context);
        });

        using var server = _builder
            .SetupWebApplicationBuilder(builder => builder.Services.AddLiveLogViewer().AddSingleton(middleware))
            .SetupWebApplication(app => _ = useCustomBasePath ? app.UseLiveLogViewer(basePath: "/my-test-path") : app.UseLiveLogViewer())
            .StartServer();

        var requestPath = $"{(useCustomBasePath ? "/my-test-path" : LogViewerApplicationBuilderExtensions.DefaultBasePath)}/my/sub/path";

        var client = server.CreateClient();

        // Act
        await client.GetAsync(requestPath);

        // Assert
        await middleware.Received(1).InvokeAsync(Arg.Is<HttpContext>(x => x != null), Arg.Is<RequestDelegate>(x => x != null));
        receivedRequestPath.Should().Be("/my/sub/path");
    }

    [Theory]
    [CombinatorialData]
    public async Task MapLiveLogViewer_AllDefaultStaticFiles_ServesTheStaticFile(bool useCustomBasePath, [CombinatorialMemberData(nameof(AvailableStaticFiles))] string file)
    {
        // Arrange
        using var server = _builder
            .SetupWebApplicationBuilder(builder => builder.Services.AddLiveLogViewer())
            .SetupWebApplication(app => _ = useCustomBasePath ? app.MapLiveLogViewer(basePath: "/my-test-path") : app.MapLiveLogViewer())
            .StartServer();

        var requestPath = $"{(useCustomBasePath ? "/my-test-path" : LogViewerApplicationBuilderExtensions.DefaultBasePath)}/{file}";

        var client = server.CreateClient();

        // Act
        var actualContent = await client.GetStringAsync(requestPath);

        // Assert
        var expectedContent = DefaultFiles.GetFileInfo(file).CreateReadStream().ToUtf8String();
        actualContent.Should().Be(expectedContent);
    }

    [Theory]
    [CombinatorialData]
    public async Task MapLiveLogViewer_BasePathWithTrailingSlashRequested_ServesTheMainPage(bool useCustomBasePath)
    {
        // Arrange
        using var server = _builder
            .SetupWebApplicationBuilder(builder => builder.Services.AddLiveLogViewer())
            .SetupWebApplication(app => _ = useCustomBasePath ? app.MapLiveLogViewer(basePath: "/my-test-path") : app.MapLiveLogViewer())
            .StartServer();

        var requestPath = $"{(useCustomBasePath ? "/my-test-path" : LogViewerApplicationBuilderExtensions.DefaultBasePath)}/";

        var client = server.CreateClient();

        // Act
        var actualContent = await client.GetStringAsync(requestPath);

        // Assert
        var expectedContent = DefaultFiles.GetFileInfo(StaticFiles.Page).CreateReadStream().ToUtf8String();
        actualContent.Should().Be(expectedContent);
    }

    [Theory]
    [CombinatorialData]
    public async Task MapLiveLogViewer_BasePathWithoutTrailingSlashRequested_RedirectToBasePathWithTrailingSlash(bool useCustomBasePath)
    {
        // Arrange
        using var server = _builder
            .SetupWebApplicationBuilder(builder => builder.Services.AddLiveLogViewer())
            .SetupWebApplication(app => _ = useCustomBasePath ? app.MapLiveLogViewer(basePath: "/my-test-path") : app.MapLiveLogViewer())
            .StartServer();

        var requestPath = useCustomBasePath ? "/my-test-path" : LogViewerApplicationBuilderExtensions.DefaultBasePath;

        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync(requestPath);

        // Assert
        response.Headers.Location?.Should().BeEquivalentTo(new Uri(server.BaseAddress, $"{requestPath}/"));
        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
    }

    [Theory]
    [CombinatorialData]
    public async Task MapLiveLogViewer_NonFileUrlRequested_InvokesTheMiddleware(bool useCustomBasePath)
    {
        // Arrange
        string? receivedRequestPath = null;
        var middleware = Substitute.For<ILogViewerMiddleware>();
        middleware.InvokeAsync(default!, default!).ReturnsForAnyArgs(async ci =>
        {
            var context = ci.Arg<HttpContext>();
            var next = ci.Arg<RequestDelegate>();
            receivedRequestPath = context.Request.Path;
            await next(context);
        });

        using var server = _builder
            .SetupWebApplicationBuilder(builder => builder.Services.AddLiveLogViewer().AddSingleton(middleware))
            .SetupWebApplication(app => _ = useCustomBasePath ? app.MapLiveLogViewer(basePath: "/my-test-path") : app.MapLiveLogViewer())
            .StartServer();

        var requestPath = $"{(useCustomBasePath ? "/my-test-path" : LogViewerApplicationBuilderExtensions.DefaultBasePath)}/my/sub/path";

        var client = server.CreateClient();

        // Act
        await client.GetAsync(requestPath);

        // Assert
        await middleware.Received(1).InvokeAsync(Arg.Is<HttpContext>(x => x != null), Arg.Is<RequestDelegate>(x => x != null));
        receivedRequestPath.Should().Be("/my/sub/path");
    }

    #region Test Data

    public static IEnumerable<string> AvailableStaticFiles() => DefaultFiles.GetDirectoryContents("").Select(x => x.Name);

    #endregion
}