using FluentAssertions;
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
}