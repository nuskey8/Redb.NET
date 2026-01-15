using System.Buffers;
using System.Text;

namespace Redb.Internal;

internal struct NullTerminatedUtf8String : IDisposable
{
    byte[] buffer;
    int length;

    public NullTerminatedUtf8String(ReadOnlySpan<byte> str)
    {
        buffer = ArrayPool<byte>.Shared.Rent(str.Length + 1);
        str.CopyTo(buffer);
        buffer[str.Length] = 0; // Null-terminate
        length = str.Length + 1;
    }

    public NullTerminatedUtf8String(ReadOnlySpan<char> str)
    {
        var byteCount = Encoding.UTF8.GetByteCount(str);
        buffer = ArrayPool<byte>.Shared.Rent(byteCount + 1);
        var bytesWritten = Encoding.UTF8.GetBytes(str, buffer);
        buffer[bytesWritten] = 0; // Null-terminate
        length = bytesWritten + 1;
    }

    public int Length => length;

    public readonly ref readonly byte GetPinnableReference()
    {
        return ref buffer[0];
    }

    public void Dispose()
    {
        if (buffer != null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        buffer = null!;
    }
}