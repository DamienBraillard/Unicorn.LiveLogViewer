using System.IO;

namespace Unicorn.LiveLogViewer.Serialization;

/// <summary>
/// A class that can create <see cref="ILogHttpWriter"/> instances.
/// </summary>
internal class LogHttpWriterFactory : ILogHttpWriterFactory
{
    /// <inheritdoc/>
    public ILogHttpWriter Create(Stream stream) => new LogHttpWriter(stream);
}