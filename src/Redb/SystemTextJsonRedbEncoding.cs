#if NET8_0_OR_GREATER || REDB_SYSTEM_TEXT_JSON

using System.Buffers;
using System.Text.Json;

namespace Redb.SystemTextJson;

public sealed class SystemTextJsonRedbEncoding(JsonSerializerOptions? options) : IRedbEncoding
{
    [ThreadStatic] static (Utf8JsonWriter? jsonWriter, ArrayBufferWriter<byte>? bufferWriter) writers;
    static (Utf8JsonWriter, ArrayBufferWriter<byte>) GetWriters()
    {
        var (jw, bw) = writers;
        if (jw == null || bw == null)
        {
            bw = new ArrayBufferWriter<byte>(256);
            jw = new Utf8JsonWriter(bw);
            writers = (jw, bw);
        }
        else
        {
            bw.Clear();
            jw.Reset();
        }
        return (jw, bw);
    }

    public bool TryEncode<T>(T value, Span<byte> buffer, out int bytesWritten)
    {
        if (PrimitiveRedbEncoding.CanEncode<T>())
        {
            return PrimitiveRedbEncoding.Instance.TryEncode(value, buffer, out bytesWritten);
        }

        var (writer, bufferWriter) = GetWriters();
        JsonSerializer.Serialize(writer, value, options);
        var jsonBytes = bufferWriter.WrittenSpan;
        if (buffer.Length < jsonBytes.Length)
        {
            bytesWritten = 0;
            return false;
        }
        jsonBytes.CopyTo(buffer);
        bytesWritten = jsonBytes.Length;
        return true;
    }

    public T Decode<T>(ReadOnlySpan<byte> data)
    {
        if (PrimitiveRedbEncoding.CanEncode<T>())
        {
            return PrimitiveRedbEncoding.Instance.Decode<T>(data);
        }

        return JsonSerializer.Deserialize<T>(data, options)!;
    }
}

public static class SystemTextJsonRedbEncodingExtensions
{
    public static RedbDatabase WithJsonSerializer(this RedbDatabase database, JsonSerializerOptions? options = null)
    {
        database.Encoding = new SystemTextJsonRedbEncoding(options);
        return database;
    }
}

#endif