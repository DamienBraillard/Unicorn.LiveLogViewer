using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unicorn.LiveLogViewer.Sources;

/// <summary>
/// A class that allows to obtain <see cref="LogEvent"/> from an infrastructure that emits logs.
/// </summary>
public class NullLogProvider : ILogProvider
{
    /// <summary>
    /// Provides the singleton default <see cref="NullLogProvider"/> instance.
    /// </summary>
    public static readonly NullLogProvider Default = new();

    /// <summary>
    /// Prevents the creation of the <see cref="NullLogProvider"/> class.
    /// </summary>
    private NullLogProvider()
    {
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> LogLevels => [];

    /// <inheritdoc/>
    public Task<ILogSource> OpenAsync(string name, CancellationToken cancellationToken) => throw new NotImplementedException();
}