using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Unicorn.LiveLogViewer.Endpoints;

/// <summary>
/// Implements the API endpoint that serves the static files.
/// </summary>
internal static class StaticFilesEndpoint
{
    /// <summary>
    /// Maps the Live Log Viewer endpoint.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointRouteBuilder"/> to configure.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/> that allows further configuration of the mapped endpoint.</returns>
    public static IEndpointConventionBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/{*fileName}", Handle);
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="fileName">The name or relative path of the file to serve.</param>
    /// <param name="options">The <see cref="LogViewerOptions"/> exposing the <see cref="IFileProvider"/> that will provide the static files.</param>
    /// <returns>
    /// If the file was found, a <see cref="FileStreamHttpResult"/> result with the file content; otherwise, a <see cref="NotFound"/> result.
    /// </returns>
    public static Results<FileStreamHttpResult, NotFound> Handle(string fileName, LogViewerOptions options)
    {
        var fileInfo = options.StaticContentProvider.GetFileInfo(fileName);

        if (fileInfo is { Exists: true, IsDirectory: false })
        {
            if (!options.ContentTypeProvider.TryGetContentType(fileInfo.Name, out var contentType))
                contentType = "application/octet-stream";

            var fileStream = fileInfo.CreateReadStream();
            return TypedResults.File(
                fileStream: fileStream,
                contentType: contentType,
                fileDownloadName: fileInfo.Name,
                lastModified: fileInfo.LastModified,
                entityTag: new EntityTagHeaderValue($"\"{fileInfo.LastModified.Ticks ^ fileInfo.Length:x}\""),
                enableRangeProcessing: false);
        }

        return TypedResults.NotFound();
    }
}