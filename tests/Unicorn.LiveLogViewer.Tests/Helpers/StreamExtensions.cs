using System.IO;
using System.Text;

namespace Unicorn.LiveLogViewer.Tests.Helpers;

/// <summary>
/// Provides test extension methods on the <see cref="Stream"/> type.
/// </summary>
public static class StreamTestExtensions
{
    private static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Converts a stream to an UTF8 string that contains the text read from the stream current position.
    /// </summary>
    /// <param name="stream">The stream to read into the returned string.</param>
    /// <param name="dispose"><c>true</c> to dispose the stream (default); otherwise, <c>false</c>.</param>
    /// <returns>A <see cref="string"/> that contains the text that could be read from the stream current position.</returns>
    public static string ToUtf8String(this Stream stream, bool dispose = true)
    {
        using var reader = new StreamReader(stream, Encoding, leaveOpen: !dispose);
        var str = reader.ReadToEnd();
        return str;
    }
}