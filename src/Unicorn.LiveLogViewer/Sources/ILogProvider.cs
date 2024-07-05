using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unicorn.LiveLogViewer.Models;

namespace Unicorn.LiveLogViewer.Sources;

/// <summary>
/// Defines the base functionalities of a class that allows to obtain <see cref="LogEvent"/> from an infrastructure that emits logs.
/// </summary>
/// <remarks>Implementations are specific to each logging provider.</remarks>
public interface ILogProvider
{
    /// <summary>
    /// The names of the log levels, that this provider can emit.
    /// The returned log levels are ordered from least critical (e.g. Verbose) first and most critical last (e.g. fatal).
    /// </summary>
    IReadOnlyCollection<string> LogLevels { get; }

    /// <summary>
    /// Enumerates the available log sources.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>A collection of <see cref="LogSourceInfo"/> that describes the available log sources.</returns>
    Task<IReadOnlyCollection<LogSourceInfo>> GetLogSourcesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens log event source with the specified <paramref name="sourceId"/>.
    /// </summary>
    /// <param name="sourceId">A source ID provided by a <see cref="LogSourceInfo"/> that uniquely identifies the source to open.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ILogSource"/> that matches the specified <paramref name="sourceId"/>; <c>null</c> if no matching log source was found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceId"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sourceId"/> is empty.</exception>
    Task<ILogSource?> OpenAsync(string sourceId, CancellationToken cancellationToken);
}