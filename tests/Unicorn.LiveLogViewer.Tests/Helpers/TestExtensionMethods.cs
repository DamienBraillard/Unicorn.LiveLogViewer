using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Unicorn.LiveLogViewer.Tests.Helpers;

/// <summary>
/// Provides useful extensions methods for testing.
/// </summary>
public static class TestExtensionMethods
{
    #region IEnumerable<T>

    /// <summary>
    /// Converts a <see cref="IEnumerable{T}"/> instance to an <see cref="IAsyncEnumerable{T}"/> instance.
    /// </summary>
    /// <param name="enumerable">The <see cref="IEnumerable{T}"/> instance to convert.</param>
    /// <typeparam name="T">The type of the items to enumerate.</typeparam>
    /// <returns>A <see cref="IAsyncEnumerable{T}"/> instance that yields the items of the specified <paramref name="enumerable"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="enumerable"/> is <c>null</c>.</exception>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        // Fake await to make the method async.
        await Task.CompletedTask;

        // Yield the items
        foreach (var item in enumerable)
        {
            yield return item;
        }
    }

    #endregion

    #region IAsyncEnumerable<T>

    /// <summary>
    /// Consumes all the values from the specified <paramref name="asyncEnumerable"/>.
    /// </summary>
    /// <param name="asyncEnumerable">The <see cref="IAsyncEnumerable{T}"/> to drain.</param>
    /// <typeparam name="T">The type of items that the specified <paramref name="asyncEnumerable"/> yields.</typeparam>
    public static async Task Consume<T>(this IAsyncEnumerable<T> asyncEnumerable)
    {
        await foreach (var _ in asyncEnumerable)
        {
        }
    }

    #endregion

    #region Stream

    /// <summary>
    /// Reads multiple JSON objects of the same type concatenated together.
    /// </summary>
    /// <param name="memoryStream">The <see cref="MemoryStream"/> to read from.</param>
    /// <typeparam name="T">The type of the JSON objects to deserialize.</typeparam>
    /// <returns>A collection of deserialized JSON objects.</returns>
    public static IEnumerable<T?> ParseJson<T>(this MemoryStream memoryStream)
    {
        var buffer = new ReadOnlySequence<byte>(memoryStream.ToArray());
        while (true)
        {
            var jsonReader = new Utf8JsonReader(buffer, isFinalBlock: false, default);
            if (!JsonDocument.TryParseValue(ref jsonReader, out var jsonDocument))
            {
                break;
            }

            buffer = buffer.Slice(jsonReader.BytesConsumed);
            yield return jsonDocument.Deserialize<T>();
        }
    }

    /// <summary>
    /// Converts a stream to a buffer that contains the data read from the stream current position.
    /// </summary>
    /// <param name="stream">The stream to read into the returned buffer.</param>
    /// <param name="dispose"><c>true</c> to dispose the stream (default); otherwise, <c>false</c>.</param>
    /// <returns>A buffer that contains the data that could be read from the stream current position.</returns>
    public static byte[] ToBuffer(this Stream stream, bool dispose = true)
    {
        var memoryStream = new MemoryStream(stream.CanSeek ? (int)(stream.Length - stream.Position) : 0);
        stream.CopyTo(memoryStream);
        if (dispose)
            stream.Dispose();
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Converts a stream to an UTF8 string that contains the text read from the stream current position.
    /// </summary>
    /// <param name="stream">The stream to read into the returned string.</param>
    /// <param name="dispose"><c>true</c> to dispose the stream (default); otherwise, <c>false</c>.</param>
    /// <returns>A <see cref="string"/> that contains the text that could be read from the stream current position.</returns>
    public static string ToUtf8String(this Stream stream, bool dispose = true)
    {
        using var reader = new StreamReader(stream, leaveOpen: !dispose);
        var str = reader.ReadToEnd();
        return str;
    }

    #endregion
}