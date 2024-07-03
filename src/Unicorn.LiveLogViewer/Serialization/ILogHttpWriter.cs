using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unicorn.LiveLogViewer.Sources;

namespace Unicorn.LiveLogViewer.Serialization;

/// <summary>
/// Defines the base functionalities of a class that can write an <see cref="LogEvent"/> as JSON HTTP chunks.
/// </summary>
/// <remarks>See https://datatracker.ietf.org/doc/html/rfc9112#section-7.1 for details about HTTP chunk encoding.</remarks>
internal interface ILogHttpWriter : IAsyncDisposable
{
    /// <summary>
    /// Writes the specified log <paramref name="events"/> as a HTTP data chunk which payload is the JSON representation of the entry.
    /// </summary>
    /// <param name="events">The collection of <see cref="LogEvent"/> to write.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is <c>null</c>.</exception>
    Task WriteAsync(IEnumerable<LogEvent> events, CancellationToken cancellationToken);
}