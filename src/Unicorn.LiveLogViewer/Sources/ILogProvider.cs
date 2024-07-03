using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
    /// Opens log event source with the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name of the source to open.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ILogSource"/> that matches the specified <paramref name="name"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">No source with the specified <paramref name="name"/> exists.</exception>
    Task<ILogSource> OpenAsync(string name, CancellationToken cancellationToken);
}