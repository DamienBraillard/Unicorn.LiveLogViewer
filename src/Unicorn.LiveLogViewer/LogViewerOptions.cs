using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Unicorn.LiveLogViewer.StaticContent;

namespace Unicorn.LiveLogViewer;

/// <summary>
/// Provides options for the Live Log Viewer.
/// </summary>
public class LogViewerOptions
{
    /// <summary>
    /// Defines the title to show on the Live Log Viewer page.
    /// </summary>
    public string Title { get; set; } = "Log Viewer";

    /// <summary>
    /// Defines the <see cref="IFileProvider"/> that will be used to locate static content.
    /// </summary>
    /// <remarks>
    ///     <para>The default value is an <see cref="EmbeddedFileProvider"/> that serves the default static files (<see cref="StaticFileNames"/>) embedded with the nuget package.</para>
    ///     <para>
    ///     To replace a static file, use the <see cref="CompositeFileProvider"/> by inserting your own file provider before the default one.
    ///     The names of the files used by the default HTML page is exposed by the <see cref="StaticFileNames"/> class.
    ///     <code>
    /// services.AddLiveLogViewer((serviceProvider, options) =>
    /// {
    ///     IFileProvider customFileProvider = …;
    ///     options.StaticContentProvider = new CompositeFileProvider(customFileProvider, options.StaticContentProvider);
    /// });
    /// </code>
    ///     </para>
    /// </remarks>
    public IFileProvider StaticContentProvider { get; set; } = new EmbeddedFileProvider(typeof(StaticFileNames).Assembly, typeof(StaticFileNames).Namespace);

    /// <summary>
    /// Defines the <see cref="IContentTypeProvider"/> that resolves the content-type of the static files served by the <see cref="StaticContentProvider"/>.
    /// </summary>
    /// <remarks>The default value is a <see cref="FileExtensionContentTypeProvider"/> instance.</remarks>
    public IContentTypeProvider ContentTypeProvider { get; set; } = new FileExtensionContentTypeProvider();
}