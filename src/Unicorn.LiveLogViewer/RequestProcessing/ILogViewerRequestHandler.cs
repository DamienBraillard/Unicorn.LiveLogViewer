using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Unicorn.LiveLogViewer.RequestProcessing;

/// <summary>
/// Defines the base functionalities of a class that can handle requests.
/// </summary>
public interface ILogViewerRequestHandler
{
    /// <summary>
    /// Tries to handle the request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> that represents the request to handle.</param>
    /// <returns><c>true</c> if the request was handled successfully; <c>false</c> if the handler did not process the request.</returns>
    Task<bool> TryHandleRequestAsync(HttpContext context);
}