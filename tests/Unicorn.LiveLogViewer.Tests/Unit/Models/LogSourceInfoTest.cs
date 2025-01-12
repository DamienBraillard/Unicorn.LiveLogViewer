using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Unicorn.LiveLogViewer.Models;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit.Models;

public class LogSourceInfoTest
{
    [Fact]
    public async Task SerializeToJson_Created_DoesNotThrow()
    {
        // Arrange
        var sut = new LogSourceInfo
        {
            Id = "Test",
            IsLive = true,
            Name = "Test"
        };

        // Act
        var action = () => JsonSerializer.SerializeAsync(Stream.Null, sut, LogViewerSerializerContext.Default.LogSourceInfo);

        // Assert
        await action.Should().NotThrowAsync();
    }
}