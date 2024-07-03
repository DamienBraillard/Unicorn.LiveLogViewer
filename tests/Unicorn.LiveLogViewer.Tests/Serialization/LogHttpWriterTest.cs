using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Unicorn.LiveLogViewer.Serialization;
using Unicorn.LiveLogViewer.Sources;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Serialization;

public class LogHttpWriterTest
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly MemoryStream _stream;
    private readonly LogHttpWriter _target;

    public LogHttpWriterTest()
    {
        _stream = Substitute.ForPartsOf<MemoryStream>();
        _target = new LogHttpWriter(_stream);
    }

    [Fact]
    public void Constructor_StreamIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => new LogHttpWriter(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public async Task WriteAsync_EventsIsEmpty_WritesNothing()
    {
        // Arrange

        // Act
        await _target.WriteAsync([], default);

        // Assert
        _stream.Length.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public async Task WriteAsync_EventsWritten_WritesACorrectHttpChunk(int eventCount)
    {
        // Arrange
        var events = GenerateEvents(eventCount);

        // Act
        await _target.WriteAsync(events, default);

        // Assert
        VerifyHttpChunk(events);
    }

    [Fact]
    public async Task WriteAsync_EventsWritten_WritesToAndFlushesTheStream()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var events = GenerateEvents(1);

        // Act
        await _target.WriteAsync(events, cancellationToken);

        // Assert
        await _stream.Received().WriteAsync(Arg.Any<ReadOnlyMemory<byte>>(), cancellationToken);
        await _stream.Received(1).FlushAsync(cancellationToken);
    }

    [Theory]
    [CombinatorialData]
    public async Task DisposeAsync_Always_WritesFinalChunk(bool eventsWritten)
    {
        // Arrange
        if (eventsWritten)
            await _target.WriteAsync(GenerateEvents(1), default);

        _stream.SetLength(0);

        // Act
        await _target.DisposeAsync();

        // Assert
        VerifyHttpChunk([]);
    }

    [Fact]
    public async Task WriteAsync_EventsIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _target.WriteAsync(null!, default);

        // Assert
        await action.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("events");
    }

    #region Helpers

    private static LogEvent[] GenerateEvents(int count)
    {
        var baseDate = new DateTime(2000, 1, 1);
        var result = new LogEvent[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = new LogEvent
            {
                LogLevel = i,
                Logger = $"my-logger-{i}",
                Message = $"my-message-{i}",
                Timestamp = baseDate.AddSeconds(i),
                Values = { ["param"] = $"value-{i}" }
            };
        }

        return result;
    }

    private void VerifyHttpChunk(ICollection<LogEvent> expectedContent)
    {
        var dataStream = new MemoryStream();
        if (expectedContent.Count > 0)
            JsonSerializer.Serialize(dataStream, expectedContent, SerializerOptions);

        var writtenChunk = Encoding.UTF8.GetString(_stream.GetBuffer().AsSpan(0, (int)_stream.Length)).Replace('\r', '␍').Replace('\n', '␊');
        var expectedChunk = $"{dataStream.Length:X}␍␊{Encoding.UTF8.GetString(dataStream.GetBuffer().AsSpan(0, (int)dataStream.Length))}␍␊";

        writtenChunk.Should().Be(expectedChunk);
    }

    #endregion
}