using System;
using System.Collections.Generic;
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
}