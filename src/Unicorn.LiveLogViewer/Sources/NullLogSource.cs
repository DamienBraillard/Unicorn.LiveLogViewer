using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unicorn.LiveLogViewer.Sources;

/// <summary>
/// A <see cref="ILogSource"/> that emits no events.
/// </summary>
public class NullLogSource : ILogSource
{
    /// <summary>
    /// Provides the singleton default <see cref="NullLogSource"/> instance.
    /// </summary>
    public static readonly NullLogSource Default = new();

    /// <summary>
    /// Prevents the creation of the <see cref="NullLogSource"/> class.
    /// </summary>
    private NullLogSource()
    {
    }

    /// <inheritdoc/>
    public Task<int> ReadAsync(ArraySegment<LogEvent> buffer, CancellationToken cancellationToken) => Task.FromResult(0);

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => throw new NotImplementedException();
}