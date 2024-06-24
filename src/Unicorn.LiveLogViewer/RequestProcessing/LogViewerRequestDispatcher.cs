using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Unicorn.LiveLogViewer.RequestProcessing;

/// <inheritdoc/>
public class LogViewerRequestDispatcher : ILogViewerRequestDispatcher
{
    private readonly ILogViewerRequestHandler[] _handlers;
    private readonly LogViewerOptions _options;

    /// <summary>
    /// Initialize a new instance of the <see cref="LogViewerRequestDispatcher"/> class.
    /// </summary>
    /// <param name="options">The <see cref="LogViewerOptions"/> that stores the options for the middleware.</param>
    /// <param name="handlers">The collection of <see cref="ILogViewerRequestHandler"/> that can process the requests.</param>
    public LogViewerRequestDispatcher(LogViewerOptions options, IEnumerable<ILogViewerRequestHandler> handlers)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _handlers = handlers?.ToArray() ?? throw new ArgumentNullException(nameof(handlers));

        if (_handlers.Length == 0)
            throw new ArgumentException("The collection of request handlers cannot be empty.", nameof(handlers));
    }

    /// <inheritdoc/>
    public string BasePath => _options.BasePath;

    /// <inheritdoc/>
    public async Task DispatchAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Path.StartsWithSegments(_options.BasePath, StringComparison.OrdinalIgnoreCase, out var matchedPath, out var remainingPath))
        {
            var pathBase = context.Request.PathBase;
            var path = context.Request.Path;
            try
            {
                context.Request.PathBase = pathBase.Add(matchedPath);
                context.Request.Path = remainingPath;

                await ProcessRequest(context);
            }
            finally
            {
                context.Request.PathBase = pathBase;
                context.Request.Path = path;
            }
        }
    }

    /// <summary>
    /// Processes the request with the request paths rooted to the base url.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> that represents the current request.</param>
    private async Task ProcessRequest(HttpContext context)
    {
        foreach (var handler in _handlers)
        {
            if (await handler.TryHandleRequestAsync(context))
                return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }
}