using System;
using System.Threading.Tasks;
using FluentAssertions;
using Unicorn.LiveLogViewer.Models;
using Unicorn.LiveLogViewer.Sources;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit.Sources;

public class NullLogSourceTest
{
    private readonly NullLogSource _sut = NullLogSource.Default;

    [Fact]
    public void Default_Created_ReturnsSingletonInstance()
    {
        // Arrange

        // Act
        var other = NullLogSource.Default;

        // Assert
        _sut.Should().BeSameAs(other);
    }

    [Fact]
    public async Task ReadAsync_Created_ReturnsZero()
    {
        // Arrange

        // Act
        var result = await _sut.ReadAsync(ArraySegment<LogEvent>.Empty, default);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task DisposeAsync_Created_DoesNotThrow()
    {
        // Arrange

        // Act
        var action = () => _sut.DisposeAsync().AsTask();

        // Assert
        await action.Should().NotThrowAsync();
    }
}