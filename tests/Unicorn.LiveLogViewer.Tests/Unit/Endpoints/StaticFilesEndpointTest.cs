using System;
using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using NSubstitute;
using NSubstitute.Extensions;
using Unicorn.LiveLogViewer.Endpoints;
using Unicorn.LiveLogViewer.Tests.Helpers;
using Unicorn.LiveLogViewer.Tests.Helpers.AspNet;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit.Endpoints;

public class StaticFilesEndpointTest
{
    private readonly LogViewerOptions _options = new()
    {
        StaticContentProvider = Substitute.For<IFileProvider>(),
        ContentTypeProvider = Substitute.For<IContentTypeProvider>(),
    };

    [Fact]
    public void Map_NoState_MapsTheEndpoint()
    {
        // Arrange
        var endpointBuilder = new EndpointRegistrationTestBuilder();

        // Act
        StaticFilesEndpoint.Map(endpointBuilder);

        // Assert
        endpointBuilder.GetRouteDetails().Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new RouteDetails("/{*fileName}", ["GET"], StaticFilesEndpoint.Handle));
    }

    [Fact]
    public void Map_NoState_ReturnsAConventionBuilderThatCanBeUsedToConfigureTheEndpoint()
    {
        // Arrange
        var endpointBuilder = new EndpointRegistrationTestBuilder();

        // Act
        var conventionBuilder = StaticFilesEndpoint.Map(endpointBuilder);

        // Assert
        conventionBuilder.Add(b => b.Metadata.Add(new TestMetadata()));

        endpointBuilder.RouteEndpoints.Should().ContainSingle()
            .Which.Metadata.GetMetadata<TestMetadata>().Should().BeOfType<TestMetadata>();
    }

    [Fact]
    public void Handle_ValidFileName_UsesTheFileProviderToLocateTheFile()
    {
        // Arrange
        _options.StaticContentProvider.ReturnsForAll(GetFileInfo("my-file.txt"));

        // Act
        StaticFilesEndpoint.Handle("/data/my-file.txt", _options);

        // Assert
        _options.StaticContentProvider.Received().GetFileInfo("/data/my-file.txt");
    }

    [Fact]
    public void Handle_FileFound_DeterminesTheContentType()
    {
        // Arrange
        _options.StaticContentProvider.ReturnsForAll(GetFileInfo("my-file.txt"));

        // Act
        StaticFilesEndpoint.Handle("/data/my-file.txt", _options);

        // Assert
        _options.ContentTypeProvider.Received(1).TryGetContentType("my-file.txt", out Arg.Any<string>()!);
    }

    [Fact]
    public void Handle_FileFound_ReturnsAValidFileResponse()
    {
        // Arrange
        var fileLastModified = new DateTimeOffset(2000, 1, 2, 3, 4, 5, TimeSpan.FromHours(6));
        var fileData = "my-file-content".ToUtf8Buffer();

        _options.StaticContentProvider.ReturnsForAll(GetFileInfo("my-file.txt", lastModified: fileLastModified, content: fileData));

        _options.ContentTypeProvider.TryGetContentType("my-file.txt", out Arg.Any<string>()!).ReturnsForAnyArgs(ci =>
        {
            ci[1] = "text/plain";
            return true;
        });

        // Act
        var results = StaticFilesEndpoint.Handle("/data/my-file.txt", _options);

        // Assert
        var result = results.Result.Should().BeOfType<FileStreamHttpResult>().Subject;

        result.ContentType.Should().Be("text/plain");
        result.FileDownloadName.Should().Be("my-file.txt");
        result.FileStream.ToUtf8String().Should().Be("my-file-content");
        result.EntityTag.Should().BeEquivalentTo(new EntityTagHeaderValue($"\"{fileLastModified.Ticks ^ fileData.Length:x}\"", isWeak: false));
        result.LastModified.Should().Be(fileLastModified);
        result.FileLength.Should().Be(fileData.Length);
        result.EnableRangeProcessing.Should().BeFalse();
    }

    [Theory]
    [InlineData("File does not exist")]
    [InlineData("File is a directory")]
    public void Handle_FileNotFound_DoesNotTryToDeterminesTheContentType(string reason)
    {
        // Arrange
        _options.StaticContentProvider.ReturnsForAll(reason switch
        {
            "File does not exist" => GetFileInfo("my-file.txt", exists: false),
            "File is a directory" => GetFileInfo("my-file.txt", isDirectory: true),
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        });

        var contentTypeProvider = Substitute.For<IContentTypeProvider>();

        // Act
        StaticFilesEndpoint.Handle("/data/my-file.txt", _options);

        // Assert
        contentTypeProvider.DidNotReceiveWithAnyArgs().TryGetContentType(default!, out Arg.Any<string>()!);
    }

    [Theory]
    [InlineData("File does not exist")]
    [InlineData("File is a directory")]
    public void Handle_FileNotFound_ReturnsNotFoundResponse(string reason)
    {
        // Arrange
        _options.StaticContentProvider.ReturnsForAll(reason switch
        {
            "File does not exist" => GetFileInfo("my-file.txt", exists: false),
            "File is a directory" => GetFileInfo("my-file.txt", isDirectory: true),
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        });

        // Act
        var results = StaticFilesEndpoint.Handle("/data/my-file.txt", _options);

        // Assert
        results.Result.Should().BeOfType<NotFound>();
    }

    #region Helpers

    private static IFileInfo GetFileInfo(string name, bool exists = true, bool isDirectory = false, DateTimeOffset lastModified = default, byte[]? content = null)
    {
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Exists.Returns(exists);
        fileInfo.IsDirectory.Returns(isDirectory);
        fileInfo.Name.Returns(name);
        fileInfo.LastModified.Returns(lastModified);
        fileInfo.Length.Returns(content?.Length ?? 0);
        fileInfo.CreateReadStream().Returns(content?.Length > 0 ? new MemoryStream(content, writable: false) : Stream.Null);
        return fileInfo;
    }

    #endregion
}