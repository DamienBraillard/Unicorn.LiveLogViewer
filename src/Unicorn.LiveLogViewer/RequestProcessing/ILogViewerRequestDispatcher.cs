using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Unicorn.LiveLogViewer.RequestProcessing;

/// <summary>
/// Provides a <see cref="RequestDelegate"/> that handles the requests for the log viewer.
/// </summary>
public interface ILogViewerRequestDispatcher
{
    /// <summary>
    /// The base path of the requests that will be handled by the dispatcher.
    /// </summary>
    string BasePath { get; }

    /// <summary>
    /// Dispatches a logviewer request to the correct <see cref="ILogViewerRequestHandler"/>.
    /// </summary>
    Task DispatchAsync(HttpContext context);
}