using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Unicorn.LiveLogViewer.Tests;

/// <summary>
/// Provides useful extensions methods for testing.
/// </summary>
public static class TestExtensionMethods
{
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
}