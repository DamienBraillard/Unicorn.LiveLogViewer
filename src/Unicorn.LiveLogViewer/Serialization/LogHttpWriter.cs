using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unicorn.LiveLogViewer.Sources;

namespace Unicorn.LiveLogViewer.Serialization;

/// <summary>
/// A class that can write an <see cref="LogEvent"/> as JSON HTTP chunks.
/// </summary>
internal class LogHttpWriter : ILogHttpWriter
{
    private readonly Stream _stream;
    private readonly Utf8JsonWriter _writer;
    private byte[] _buffer;

    /// <summary>
    /// Initialize a new instance of the <see cref="LogHttpWriter"/> class.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
    /// <remarks>
    /// We need a <see cref="Stream"/> here to write to rather than a <see cref="IBufferWriter{T}"/>.
    /// This is because the <see cref="Utf8JsonWriter"/> buffers the written data when created with a stream rather than writing directly
    /// to the underlying <see cref="IBufferWriter{T}"/> when created with one.
    /// We use this buffering to know the size of the HTTP chunk to write once the JSON data has been serialized.
    /// As the <see cref="Utf8JsonWriter"/> uses an efficient internal buffer based on <see cref="ArrayPool{T}"/> doing so, we don't have
    /// to reimplement our own efficient <see cref="IBufferWriter{T}"/>.
    /// </remarks>
    public LogHttpWriter(Stream stream)
    {
        var options = new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = false,
            SkipValidation = true
        };

        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _writer = new Utf8JsonWriter(stream, options);
        _buffer = ArrayPool<byte>.Shared.Rent(10);
    }

    /// <inheritdoc/>
    public async Task WriteAsync(IEnumerable<LogEvent> events, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(events);

        // Writes all the events in the writer buffer
        _writer.Reset();
        var isEmpty = true;
        _writer.WriteStartArray();
        {
            foreach (var logEvent in events)
            {
                _writer.WriteStartObject();
                _writer.WriteNumber("logLevel", logEvent.LogLevel);
                _writer.WriteString("timestamp", logEvent.Timestamp.ToString("s"));
                _writer.WriteString("logger", logEvent.Logger);
                _writer.WriteString("message", logEvent.Message);
                _writer.WritePropertyName("values");
                _writer.WriteStartObject();
                {
                    foreach (var value in logEvent.Values)
                    {
                        _writer.WriteString(value.Key, value.Value);
                    }
                }
                _writer.WriteEndObject();

                _writer.WriteEndObject();

                isEmpty = false;
            }
        }
        _writer.WriteEndArray();

        // Don't write anything if no events were written
        if (isEmpty)
            return;

        // Writes the header
        await WriteChunkHeaderAsync(_writer.BytesPending, cancellationToken);
        // Flushes the writer to the underlying data storage
        await _writer.FlushAsync(cancellationToken);
        // Writes the footer
        await WriteChunkFooterAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await WriteChunkHeaderAsync(0, default);
        await WriteChunkFooterAsync(default);
        await _writer.DisposeAsync();

        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = [];
    }

    /// <summary>
    /// Writes the HTTP chunk header that consists of the data length as an hexadecimal string followed by a CRLF separator.
    /// </summary>
    /// <param name="length">The length of the chunk data.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    private async Task WriteChunkHeaderAsync(int length, CancellationToken cancellationToken)
    {
        // Converts the length to a Hexadecimal string
        var lengthHex = length.ToString("X");

        // Builds the header in the buffer (length in HEX plus CRLF)
        var count = Encoding.ASCII.GetBytes(lengthHex, _buffer);
        _buffer[count++] = 13;
        _buffer[count++] = 10;

        // Write the header
        await _stream.WriteAsync(_buffer.AsMemory(0, count), cancellationToken);
    }

    /// <summary>
    /// Writes the final CRLF footer of the chunk.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    private async Task WriteChunkFooterAsync(CancellationToken cancellationToken)
    {
        _buffer[0] = 13;
        _buffer[1] = 10;
        await _stream.WriteAsync(_buffer.AsMemory(0, 2), cancellationToken);
    }
}