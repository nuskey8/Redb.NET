using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Redb;

public unsafe struct ReadOnlyTable : IDisposable
{
    internal RedbDatabase database;
    void* table;

    internal ReadOnlyTable(RedbDatabase database, void* table)
    {
        this.database = database;
        this.table = table;
    }

    public void Dispose()
    {
        if (table != null)
        {
            NativeMethods.redb_free_readonly_table(table);
            table = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly RedbBlob Get(ReadOnlySpan<byte> key)
    {
        if (!TryGet(key, out var value))
        {
            throw new RedbDatabaseException($"Key `{Encoding.UTF8.GetString(key)}` not found", NativeMethods.REDB_ERROR_KEY_NOT_FOUND);
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGet(ReadOnlySpan<byte> key, out RedbBlob blob)
    {
        fixed (byte* keyPtr = key)
        {
            byte* ptr;
            nuint written;
            var code = NativeMethods.redb_get(table, keyPtr, (nuint)key.Length, &ptr, &written);
            if (code == NativeMethods.REDB_OK)
            {
                blob = new RedbBlob(ptr, written);
                return true;
            }
            else
            {
                blob = default;
                return false;
            }
        }
    }
}

public unsafe struct ReadOnlyTable<TKey, TValue> : IDisposable
{
    ReadOnlyTable inner;

    internal ReadOnlyTable(RedbDatabase database, void* table)
    {
        inner = new ReadOnlyTable(database, table);
    }

    public void Dispose()
    {
        inner.Dispose();
    }

    public readonly TValue Get(TKey key)
    {
        if (!TryGet(key, out var value))
        {
            throw new RedbDatabaseException($"Key `{key}` not found", NativeMethods.REDB_ERROR_KEY_NOT_FOUND);
        }
        return value;
    }

    public readonly bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        var encoding = inner.database.Encoding;

        var buffer = ArrayPool<byte>.Shared.Rent(256);
        try
        {
            int bytesWritten;
            while (encoding.TryEncode(key, buffer, out bytesWritten) == false)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                buffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
            }
            var keySpan = new ReadOnlySpan<byte>(buffer, 0, bytesWritten);

            if (inner.TryGet(keySpan, out var blob))
            {
                var valueSpan = blob.AsSpan();
                value = encoding.Decode<TValue>(valueSpan)!;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}