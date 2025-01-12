using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Unicorn.LiveLogViewer.Models;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit.Models;

public class LogEventTest
{
    [Fact]
    public async Task SerializeToJson_Created_DoesNotThrow()
    {
        // Arrange
        var sut = new LogEvent
        {
            LogLevel = 0,
            Timestamp = DateTime.Now,
            Logger = "TestLogger",
            Message = "TestMessage",
            Values = new Dictionary<string, string?> { { "Key", "Value" } }
        };

        // Act
        var action = () => JsonSerializer.SerializeAsync(Stream.Null, sut, LogViewerSerializerContext.Default.LogEvent);

        // Assert
        await action.Should().NotThrowAsync();
    }
}