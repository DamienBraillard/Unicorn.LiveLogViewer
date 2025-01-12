using System.Threading.Tasks;
using FluentAssertions;
using Unicorn.LiveLogViewer.Models;
using Unicorn.LiveLogViewer.Sources;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit.Sources;

public class NullLogProviderTest
{
    private readonly NullLogProvider _sut = NullLogProvider.Default;

    [Fact]
    public void Default_Created_ReturnsSingletonInstance()
    {
        // Arrange

        // Act
        var other = NullLogProvider.Default;

        // Assert
        _sut.Should().BeSameAs(other);
    }

    [Fact]
    public void LogLevels_Created_ReturnsEmptyList()
    {
        // Arrange

        // Act
        var logLevels = _sut.LogLevels;

        // Assert
        logLevels.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLogSourcesAsync_Created_ReturnsSingleNullSource()
    {
        // Arrange

        // Act
        var sources = await _sut.GetLogSourcesAsync(default);

        // Assert
        sources.Should().BeEquivalentTo([new LogSourceInfo { Id = "Null", IsLive = false, Name = "Null" }]);
    }

    [Fact]
    public async Task OpenAsync_SourceIdIsValid_ReturnsDefaultNullSource()
    {
        // Arrange

        // Act
        var source = await _sut.OpenAsync("Null", default);

        // Assert
        source.Should().BeOfType<NullLogSource>().And.BeSameAs(NullLogSource.Default);
    }

    [Fact]
    public async Task OpenAsync_SourceIsInvalid_ReturnsNull()
    {
        // Arrange

        // Act
        var source = await _sut.OpenAsync("Invalid", default);

        // Assert
        source.Should().BeNull();
    }
}