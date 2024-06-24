using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Unicorn.LiveLogViewer.StaticContent;

namespace Unicorn.LiveLogViewer;

/// <summary>
/// Provides options for the Live Log Viewer.
/// </summary>
public class LogViewerOptions
{
    /// <summary>
    /// Initialize a new instance of the <see cref="LogViewerOptions"/> class.
    /// </summary>
    public LogViewerOptions()
    {
        var staticRootType = typeof(StaticFiles);
        StaticContentProviders = [new EmbeddedFileProvider(staticRootType.Assembly, staticRootType.Namespace)];
    }

    /// <summary>
    /// Defines the collection of <see cref="IFileProvider"/> that will be used to locate static content.
    /// </summary>
    /// <remarks>
    /// Each <see cref="IFileProvider"/> will be queried first to last for the desired file.
    /// This allows to either fully replace the default provider or insert a provider that will
    /// only resolve one or more static files.
    /// </remarks>
    public ICollection<IFileProvider> StaticContentProviders { get; set; }

    /// <summary>
    /// The base path at which the log viewer middleware listens.
    /// </summary>
    public string BasePath { get; set; } = "";
}