using System.Buffers;
using MessagePack;

namespace Redb.MessagePack;

public sealed class MessagePackRedbEncoding : IRedbEncoding
{
    public static readonly MessagePackRedbEncoding Instance = new();

    [ThreadStatic] static ArrayBufferWriter<byte>? bufferWriter;

    static ArrayBufferWriter<byte> GetBufferWriter()
    {
        var writer = bufferWriter;
        if (writer == null)
        {
            writer = new ArrayBufferWriter<byte>(256);
            bufferWriter = writer;
        }
        else
        {
            writer.Clear();
        }

        return writer;
    }

    MessagePackRedbEncoding()
    {
    }

    public T Decode<T>(ReadOnlySpan<byte> data)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(data.Length);
        try
        {
            data.CopyTo(buffer);
            return MessagePackSerializer.Deserialize<T>(new ReadOnlySequence<byte>(buffer, 0, data.Length));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public bool TryEncode<T>(T value, Span<byte> buffer, out int bytesWritten)
    {
        var writer = GetBufferWriter();
        MessagePackSerializer.Serialize(writer, value);
        var writtenSpan = writer.WrittenSpan;

        if (writtenSpan.Length <= buffer.Length)
        {
            writtenSpan.CopyTo(buffer);
            bytesWritten = writtenSpan.Length;
            return true;
        }
        else
        {
            bytesWritten = 0;
            return false;
        }
    }
}
