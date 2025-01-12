namespace Unicorn.LiveLogViewer.Models;

/// <summary>
/// Provides information about a log source.
/// </summary>
public class LogSourceInfo
{
    /// <summary>
    /// The unique identifier for the source.
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    /// Defines whether the source is the "live" source that gathers log events directly from the running application logger.
    /// </summary>
    public bool IsLive { get; init; }

    /// <summary>
    /// The user-friendly name of the source. For example the name of the log file or <c>"Live"</c> for a live log source.
    /// </summary>
    public string Name { get; init; } = "";
}