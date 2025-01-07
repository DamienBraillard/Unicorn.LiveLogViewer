using System.Text;

namespace Unicorn.LiveLogViewer.Tests.Helpers;

/// <summary>
/// Provides test extension methods on the <see cref="string"/> type.
/// </summary>
public static class StringTestExtensions
{
    private static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Converts a string to a buffer containing the UTF8 binary representation of the string.
    /// </summary>
    /// <param name="str">The string to convert to a UTF-8 buffer .</param>
    /// <returns>A buffer that contains the UTF-8 binary representation of the string.</returns>
    public static byte[] ToUtf8Buffer(this string str) => Encoding.GetBytes(str);
}