using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Unicorn.LiveLogViewer.Models;
using Unicorn.LiveLogViewer.Sources;
using LogViewerSerializerContext = Unicorn.LiveLogViewer.Models.LogViewerSerializerContext;

namespace Unicorn.LiveLogViewer;

/// <summary>
/// A middleware that serves the log entries for a source.
/// </summary>
internal class LogViewerMiddleware : ILogViewerMiddleware
{
    private static readonly TemplateMatcher GetLogEventsMatcher = new(TemplateParser.Parse("/sources/{name}"), []);
    private readonly ILogProvider _sourceProvider;

    /// <summary>
    /// Initialize a new instance of the <see cref="LogViewerMiddleware"/> class.
    /// </summary>
    /// <param name="sourceProvider">The <see cref="ILogProvider"/> that can provide <see cref="ILogSource"/> instances.</param>
    /// <exception cref="ArgumentNullException">Any argument is <c>null</c></exception>
    public LogViewerMiddleware(ILogProvider sourceProvider)
    {
        _sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
    }

    /// <inheritdoc/>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // GetLogEvents
        if (TryMatchGetLogEvents(context.Request, out var sourceName))
        {
            await HandleGetLogEventsAsync(sourceName, context.Response, context.RequestAborted);
            return;
        }

        // Invokes the next request delegate
        await next(context);
    }

    /// <summary>
    /// Handles the request for retrieving log events.
    /// </summary>
    /// <param name="sourceName">The name of the source whose log events to serve.</param>
    /// <param name="response">The <see cref="HttpResponse"/> to use to respond to the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    private async Task HandleGetLogEventsAsync(string sourceName, HttpResponse response, CancellationToken cancellationToken)
    {
        // Open the source
        await using var source = await _sourceProvider.OpenAsync(sourceName, cancellationToken);

        // Set the content type
        response.ContentType = "application/json; charset=utf-8";

        // Write the response in chunks until all events are read or the request is aborted
        var buffer = ArrayPool<LogEvent>.Shared.Rent(100);
        try
        {
            while (await source.ReadAsync(buffer, cancellationToken) is var eventCount and > 0)
            {
                await JsonSerializer.SerializeAsync(response.Body, buffer.Take(eventCount), LogViewerSerializerContext.Default.IEnumerableLogEvent, cancellationToken);
            }
        }
        finally
        {
            ArrayPool<LogEvent>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Attempts to match the path for retrieving log events and extract the requested source name.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> to match.</param>
    /// <param name="sourceName">When this method returns <c>true</c>, contains the name of the log source to serve; otherwise, <c>null</c>. This parameter is pass uninitialized.</param>
    /// <returns><c>true</c> if <paramref name="request"/> matched; otherwise, <c>false</c>.</returns>
    private static bool TryMatchGetLogEvents(HttpRequest request, [NotNullWhen(true)] out string? sourceName)
    {
        sourceName = null;

        // Verifies the method
        if (!HttpMethods.IsGet(request.Method))
            return false;

        // Matches the path
        var routeValues = new RouteValueDictionary();
        if (!GetLogEventsMatcher.TryMatch(request.Path, routeValues))
            return false;

        // Extract the parameters
        if ((sourceName = routeValues["name"] as string) is null)
            return false;

        // Done
        return true;
    }
}