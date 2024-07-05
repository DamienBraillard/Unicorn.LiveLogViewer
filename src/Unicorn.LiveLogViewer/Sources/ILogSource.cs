using System;
using System.Threading;
using System.Threading.Tasks;
using Unicorn.LiveLogViewer.Models;

namespace Unicorn.LiveLogViewer.Sources;

/// <summary>
/// A class that can provide log entries.
/// </summary>
public interface ILogSource : IAsyncDisposable
{
    /// <summary>
    /// Reads the log entries from the current source.
    /// </summary>
    /// <param name="buffer">The buffer to fill with log events.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>The number of <see cref="LogEvent"/> that were read into the specified <paramref name="buffer"/>.</returns>
    Task<int> ReadAsync(ArraySegment<LogEvent> buffer, CancellationToken cancellationToken);
}