using System;
using System.IO;
using System.Threading.Tasks;

namespace Unicorn.LiveLogViewer.Infrastructure;

/// <summary>
/// <para>
/// A read-only non-seekable <see cref="Stream"/> that reads from an inner <see cref="Stream"/> and blocks when the end of the inner stream has been reached.
/// </para>
/// <para>
/// The pending reads are unlocked when the <see cref="TryUnblock"/> method is called and determines that the size of the stream has changed.
/// </para>
/// </summary>
public class TailStream : Stream
{
    private Stream _innerStream;

    /// <summary>
    /// Initialize a new instance of the <see cref="TailStream"/> class.
    /// </summary>
    /// <param name="innerStream">The inner <see cref="Stream"/> to read from.</param>
    public TailStream(Stream innerStream)
    {
        _innerStream = innerStream;
    }

    /// <summary>
    /// Verifies whether the length of the inner stream has changed. If it is the case, allows further reading.
    /// </summary>
    /// <returns><c>true</c> if the length of the inner stream has changed and further reading is allowed; otherwise, <c>false</c>.</returns>
    public bool TryUnblock()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _innerStream.Length;

    /// <inheritdoc />
    public override void Flush()
    {
        _innerStream.Flush();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        return _innerStream.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown, the stream does not support seeking.</exception>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown, the stream does not support length changes.</exception>
    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown, the stream does not support writes.</exception>
    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown, the stream does not support seeking.</exception>
    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _innerStream.Dispose();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await  base.DisposeAsync();
        await _innerStream.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}