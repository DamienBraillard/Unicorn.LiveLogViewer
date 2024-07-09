using Microsoft.AspNetCore.Http;

namespace Unicorn.LiveLogViewer;

/// <summary>
/// A middleware that serves the log entries for a source.
/// </summary>
internal interface ILogViewerMiddleware : IMiddleware
{
}