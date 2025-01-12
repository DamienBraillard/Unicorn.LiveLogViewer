using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using FluentAssertions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using NSubstitute;
using Unicorn.LiveLogViewer.StaticContent;
using Unicorn.LiveLogViewer.Tests.Helpers;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit;

public class LogViewerOptionsTest
{
    private readonly LogViewerOptions _sut = new();

    [Fact]
    public void Title_Created_ReturnsDefaultTitle()
    {
        // Arrange

        // Act
        var title = _sut.Title;

        // Assert
        title.Should().Be("Log Viewer");
    }

    [Fact]
    public void Title_ValueSet_ReturnsValue()
    {
        // Arrange

        // Act
        _sut.Title = "my-new-title";

        // Assert
        _sut.Title.Should().Be("my-new-title");
    }

    [Fact]
    public void StaticContentProvider_Created_ReturnsAnEmbeddedFileProvider()
    {
        // Arrange

        // Act
        var provider = _sut.StaticContentProvider;

        // Assert
        provider.Should().BeOfType<EmbeddedFileProvider>();
    }

    [Theory]
    [MemberData(nameof(StaticFileTestCase.TheoryData), MemberType = typeof(StaticFileTestCase))]
    public void StaticContentProvider_Created_ServesAllTheStaticFiles(StaticFileTestCase testCase)
    {
        // Arrange

        // Act
        var provider = _sut.StaticContentProvider;

        // Assert
        provider.GetFileInfo(testCase.Name).Exists.Should().BeTrue();

        var embeddedFileContent = provider.GetFileInfo(testCase.Name).CreateReadStream();
        using var reader = new StreamReader(embeddedFileContent);
        var actualContent = reader.ReadToEnd();

        actualContent.Should().Be(testCase.Content);
    }

    [Fact]
    public void StaticContentProvider_ValueSet_ReturnsValue()
    {
        // Arrange
        var contentProvider = Substitute.For<IFileProvider>();

        // Act
        _sut.StaticContentProvider = contentProvider;

        // Assert
        _sut.StaticContentProvider.Should().BeSameAs(contentProvider);
    }

    [Fact]
    public void ContentTypeProvider_Created_ReturnsADefaultFileExtensionContentTypeProvider()
    {
        // Arrange

        // Act
        var contentTypeProvider = _sut.ContentTypeProvider;

        // Assert
        contentTypeProvider.Should().BeEquivalentTo(new FileExtensionContentTypeProvider());
    }

    [Fact]
    public void ContentTypeProvider_ValueSet_ReturnsValue()
    {
        // Arrange
        var contentTypeProvider = Substitute.For<IContentTypeProvider>();

        // Act
        _sut.ContentTypeProvider = contentTypeProvider;

        // Assert
        _sut.ContentTypeProvider.Should().BeSameAs(contentTypeProvider);
    }

    #region Test Data

    public class StaticFileTestCase(string name, string contentType, string content)
    {
        static StaticFileTestCase()
        {
            var fileNames = typeof(StaticFileNames).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => f.GetValue(null)).Cast<string>();
            var type = typeof(StaticFileNames);

            TheoryData = [];
            foreach (var fileName in fileNames)
            {
                var contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
                {
                    ".css" => "text/css",
                    ".js" => "text/javascript",
                    ".html" => "text/html",
                    _ => throw new NotSupportedException($"Content type not defined in test theory for file extension '{Path.GetExtension(fileName)}'. Please update the test")
                };

                var resourceFullName = $"{type.Namespace}.{fileName}";
                var content = type.Assembly.GetManifestResourceStream(resourceFullName)?.ToUtf8String();
                if (content == null)
                    throw new MissingManifestResourceException($"Resource for file '{fileName}' not found: '{resourceFullName}'");
                TheoryData.Add(new StaticFileTestCase(fileName, contentType, content));
            }
        }

        public static TheoryData<StaticFileTestCase> TheoryData { get; }

        public string Name { get; } = name;
        public string ContentType { get; } = contentType;
        public string Content { get; } = content;

        public override string ToString() => Name;
    }

    #endregion
}