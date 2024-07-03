using System.IO;

namespace Unicorn.LiveLogViewer.Serialization;

/// <summary>
/// Defines the base functionalities of a class that can create <see cref="ILogHttpWriter"/> instances.
/// </summary>
internal interface ILogHttpWriterFactory
{
    /// <summary>
    /// Creates a new <see cref="ILogHttpWriter"/> instance that writes to the specified <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <returns>A new <see cref="ILogHttpWriter"/>.</returns>
    ILogHttpWriter Create(Stream stream);
}