using System;
using System.Buffers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Unicorn.LiveLogViewer.Serialization;
using Unicorn.LiveLogViewer.Sources;

namespace Unicorn.LiveLogViewer.RequestProcessing;

/// <summary>
/// A <see cref="ILogViewerRequestHandler"/> that serves the log entries for a source.
/// </summary>
internal class LogEntriesRequestHandler : ILogViewerRequestHandler
{
    private static readonly TemplateMatcher RouteMatcher = new(TemplateParser.Parse("/sources/{name}"), []);
    private readonly ILogProvider _sourceProvider;
    private readonly ILogHttpWriterFactory _writerFactory;

    /// <summary>
    /// Initialize a new instance of the <see cref="LogEntriesRequestHandler"/> class.
    /// </summary>
    /// <param name="sourceProvider">The <see cref="ILogProvider"/> that can provide <see cref="ILogSource"/> instances.</param>
    /// <param name="writerFactory">The <see cref="ILogHttpWriterFactory"/> that can create <see cref="ILogHttpWriter"/> instances to write the log entries to the http response.</param>
    /// <exception cref="ArgumentNullException">Any argument is <c>null</c></exception>
    public LogEntriesRequestHandler(ILogProvider sourceProvider, ILogHttpWriterFactory writerFactory)
    {
        _sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
        _writerFactory = writerFactory ?? throw new ArgumentNullException(nameof(writerFactory));
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

        // Open the generator & create a writer
        await using var source = await _sourceProvider.OpenAsync(name, context.RequestAborted);
        await using var writer = _writerFactory.Create(context.Response.Body);

        // Sets the response headers
        context.Response.Headers.TransferEncoding = "chunked";
        context.Response.Headers.ContentType = $"{MediaTypeNames.Application.Json}; charset={Encoding.UTF8.WebName}";

        // Write the response in chunks until all events are read or the request is aborted
        var buffer = ArrayPool<LogEvent>.Shared.Rent(100);
        try
        {
            while (await source.ReadAsync(buffer, context.RequestAborted) is var eventCount and > 0)
            {
                await writer.WriteAsync(new ArraySegment<LogEvent>(buffer, 0, eventCount), context.RequestAborted);
            }
        }
        finally
        {
            ArrayPool<LogEvent>.Shared.Return(buffer);
        }

        return true;
    }
}