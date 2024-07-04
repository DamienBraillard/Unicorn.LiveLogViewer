using System;
using System.Collections.Generic;

namespace Unicorn.LiveLogViewer.Sources;

/// <summary>
/// Represents a logged event.
/// </summary>
#if NET6_0
// The type 'LogEvent' defines init-only properties, deserialization of which is currently not supported in source generation mode. => LogEvent is never deserialized.
#pragma warning disable SYSLIB1037
#endif
public class LogEvent
{
    /// <summary>
    /// The zero-based index of the log level.
    /// </summary>
    /// <remarks>
    /// The value must match the <see cref="ILogProvider.LogLevels"/> property of the <see cref="ILogProvider"/>
    /// that created the <see cref="ILogSource"/> that emitted the event.
    /// </remarks>
    public int LogLevel { get; init; }

    /// <summary>
    /// The date and time the log entry was emitted in the local timezone of the server.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The name of the logger that emitted the entry.
    /// </summary>
    public string Logger { get; init; } = "";

    /// <summary>
    /// The log message.
    /// </summary>
    public string Message { get; init; } = "";

    /// <summary>
    /// The data associated with the log event. Usually gathered through semantic logging.
    /// </summary>
    public IDictionary<string, string?> Values { get; init; } = new Dictionary<string, string?>();
}