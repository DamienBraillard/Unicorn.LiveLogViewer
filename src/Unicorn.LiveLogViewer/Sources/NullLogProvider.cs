﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unicorn.LiveLogViewer.Models;

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
    public Task<IReadOnlyCollection<LogSourceInfo>> GetLogSourcesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<LogSourceInfo>>([new LogSourceInfo { Id = "Null", IsLive = false, Name = "Null" }]);
    }

    /// <inheritdoc/>
    public Task<ILogSource?> OpenAsync(string sourceId, CancellationToken cancellationToken)
    {
        return Task.FromResult<ILogSource?>(sourceId == "Null" ? NullLogSource.Default : null);
    }
}