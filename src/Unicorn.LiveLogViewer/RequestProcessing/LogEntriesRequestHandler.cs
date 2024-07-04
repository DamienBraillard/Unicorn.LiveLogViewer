using System;
using System.Buffers;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Unicorn.LiveLogViewer.Sources;

namespace Unicorn.LiveLogViewer.RequestProcessing;

/// <summary>
/// A <see cref="ILogViewerRequestHandler"/> that serves the log entries for a source.
/// </summary>
internal class LogEntriesRequestHandler : ILogViewerRequestHandler
{
    private static readonly TemplateMatcher RouteMatcher = new(TemplateParser.Parse("/sources/{name}"), []);
    private readonly ILogProvider _sourceProvider;

    /// <summary>
    /// Initialize a new instance of the <see cref="LogEntriesRequestHandler"/> class.
    /// </summary>
    /// <param name="sourceProvider">The <see cref="ILogProvider"/> that can provide <see cref="ILogSource"/> instances.</param>
    /// <exception cref="ArgumentNullException">Any argument is <c>null</c></exception>
    public LogEntriesRequestHandler(ILogProvider sourceProvider)
    {
        _sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
    }

    /// <inheritdoc/>
    public async Task<bool> TryHandleRequestAsync(HttpContext context)
    {
        // Parse the route
        var routeValues = new RouteValueDictionary();
        if (!RouteMatcher.TryMatch(context.Request.Path, routeValues))
            return false;
        if (routeValues["name"] is not string name)
            return false;

        // Open the generator
        await using var source = await _sourceProvider.OpenAsync(name, context.RequestAborted);

        // Set the content type
        context.Response.ContentType = "application/json; charset=utf-8";

        // Write the response in chunks until all events are read or the request is aborted
        var buffer = ArrayPool<LogEvent>.Shared.Rent(100);
        try
        {
            while (await source.ReadAsync(buffer, context.RequestAborted) is var eventCount and > 0)
            {
                await JsonSerializer.SerializeAsync(context.Response.Body, buffer.Take(eventCount), LogViewerSerializerContext.Default.IEnumerableLogEvent, context.RequestAborted);
            }
        }
        finally
        {
            ArrayPool<LogEvent>.Shared.Return(buffer);
        }

        return true;
    }
}