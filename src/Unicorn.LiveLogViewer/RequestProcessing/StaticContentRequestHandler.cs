using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Unicorn.LiveLogViewer.StaticContent;

namespace Unicorn.LiveLogViewer.RequestProcessing;

/// <summary>
/// A <see cref="ILogViewerRequestHandler"/> that serves the static content.
/// </summary>
public class StaticContentRequestHandler : ILogViewerRequestHandler
{
    private readonly LogViewerOptions _options;

    /// <summary>
    /// Initialize a new instance of the <see cref="StaticContentRequestHandler"/> class.
    /// </summary>
    /// <param name="options">The <see cref="LogViewerOptions"/> that stores the options for the middleware.</param>
    public StaticContentRequestHandler(LogViewerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public async Task<bool> TryHandleRequestAsync(HttpContext context)
    {
        // If the address is empty, redirect to the base path with a trailing slash (required so that the relative addreses within the HTML work as expected)
        var path = context.Request.Path.ToString();
        if (path.Length == 0)
        {
            context.Response.Redirect($"{context.Request.PathBase}/", permanent: true);
            return true;
        }

        // Serve the log viewer page as root
        if (path == "/")
            path = StaticFiles.Page;

        // Locate and serve the file (from the first file provider that has it)
        foreach (var fileProvider in _options.StaticContentProviders)
        {
            if (fileProvider.GetFileInfo(path) is { Exists: true } foundFile)
            {
                await ServeFileAsync(foundFile, context.Response, context.RequestAborted);
                return true;
            }
        }

        // Not found
        return false;
    }

    /// <summary>
    /// Serves the specified <paramref name="file"/>.
    /// </summary>
    /// <param name="file">The <see cref="IFileInfo"/> that represents the file to serve.</param>
    /// <param name="response">The <see cref="HttpResponse"/> to populate.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    private async Task ServeFileAsync(IFileInfo file, HttpResponse response, CancellationToken cancellationToken)
    {
        // Sets the headers
        response.ContentType = Path.GetExtension(file.Name).ToLowerInvariant() switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "text/javascript",
            var extension => throw new NotSupportedException($"Unsupported file extension ({extension}). Supported extensions are \".html\", \".css\" and \".js\"."),
        };
        response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true, MustRevalidate = true };
        response.StatusCode = StatusCodes.Status200OK;

        // Starts the response
        await response.StartAsync(cancellationToken);

        // Writes the body
        await using var stream = file.CreateReadStream();
        while (await stream.ReadAsync(response.BodyWriter.GetMemory(), cancellationToken) is var readCount && readCount > 0)
        {
            response.BodyWriter.Advance(readCount);
        }

        await response.BodyWriter.FlushAsync(cancellationToken);
    }
}