#if NET8_0_OR_GREATER || REDB_SYSTEM_TEXT_JSON

using System.Buffers;
using System.Text.Json;

namespace Redb.SystemTextJson;

public sealed class SystemTextJsonRedbEncoding : IRedbEncoding
{
    public static readonly SystemTextJsonRedbEncoding Instance = new();

    public bool TryEncode<T>(T value, Span<byte> buffer, out int bytesWritten)
    {
        if (PrimitiveRedbEncoding.CanEncode<T>())
        {
            return PrimitiveRedbEncoding.Instance.TryEncode(value, buffer, out bytesWritten);
        }

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value);
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

        return JsonSerializer.Deserialize<T>(data)!;
    }
}

public static class SystemTextJsonRedbEncodingExtensions
{
    public static RedbDatabase WithJsonSerializer(this RedbDatabase database)
    {
        database.Encoding = SystemTextJsonRedbEncoding.Instance;
        return database;
    }
}

#endif