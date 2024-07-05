using Microsoft.Extensions.FileProviders;
using Unicorn.LiveLogViewer.StaticContent;

namespace Unicorn.LiveLogViewer;

/// <summary>
/// Provides options for the Live Log Viewer.
/// </summary>
public class LogViewerOptions
{
    /// <summary>
    /// Defines the <see cref="IFileProvider"/> that will be used to serve static content.
    /// </summary>
    public LogViewerOptions()
    {
        var staticRootType = typeof(StaticFileNames);
        StaticContentProvider = new EmbeddedFileProvider(staticRootType.Assembly, staticRootType.Namespace);
    }

    /// <summary>
    /// Defines the collection of <see cref="IFileProvider"/> that will be used to locate static content.
    /// </summary>
    /// <remarks>
    /// To replace a static file, use the <see cref="CompositeFileProvider"/> by inserting your own file provider before the default one.
    /// <code>
    /// services.AddLiveLogViewer((serviceProvider, options) =>
    /// {
    ///     IFileProvider customFileProvider = …;
    ///     options.StaticContentProvider = new CompositeFileProvider(customFileProvider, options.StaticContentProvider);
    /// });
    /// </code>
    /// </remarks>
    public IFileProvider StaticContentProvider { get; set; } = new EmbeddedFileProvider(typeof(StaticFiles).Assembly, typeof(StaticFiles).Namespace);
}