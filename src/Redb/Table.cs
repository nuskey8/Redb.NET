using System.Buffers;
using System.Runtime.CompilerServices;

namespace Redb;

public unsafe struct Table : IDisposable
{
    internal readonly RedbDatabase database;
    void* table;

    internal Table(RedbDatabase database, void* table)
    {
        this.database = database;
        this.table = table;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Insert(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        ThrowIfDisposed();

        fixed (byte* keyPtr = key)
        fixed (byte* valuePtr = value)
        {
            var code = NativeMethods.redb_insert(table, keyPtr, (nuint)key.Length, valuePtr, (nuint)value.Length);
            ThrowIfError(code);
        }
    }

    static void ThrowIfError(int code)
    {
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to insert value to table.", code);
        }
    }

    readonly void ThrowIfDisposed()
    {
        if (table == null)
        {
            throw new ObjectDisposedException(nameof(Table));
        }
    }

    public void Dispose()
    {
        if (table != null)
        {
            NativeMethods.redb_free_table(table);
            table = null;
        }
    }
}

public unsafe struct Table<TKey, TValue> : IDisposable
{
    Table inner;

    internal Table(RedbDatabase database, void* table)
    {
        inner = new Table(database, table);
    }

    public readonly void Insert(TKey key, TValue value)
    {
        var encoding = inner.database.Encoding;

        var keyBuffer = ArrayPool<byte>.Shared.Rent(256);
        var valueBuffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            var keyBufferBytesWritten = 0;
            var valueBufferBytesWritten = 0;

            while (!encoding.TryEncode(key, keyBuffer, out keyBufferBytesWritten))
            {
                ArrayPool<byte>.Shared.Return(keyBuffer);
                keyBuffer = ArrayPool<byte>.Shared.Rent(keyBuffer.Length * 2);
            }

            while (!encoding.TryEncode(value, valueBuffer, out valueBufferBytesWritten))
            {
                ArrayPool<byte>.Shared.Return(valueBuffer);
                valueBuffer = ArrayPool<byte>.Shared.Rent(valueBuffer.Length * 2);
            }

            var keySpan = new ReadOnlySpan<byte>(keyBuffer, 0, keyBufferBytesWritten);
            var valueSpan = new ReadOnlySpan<byte>(valueBuffer, 0, valueBufferBytesWritten);

            inner.Insert(keySpan, valueSpan);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(keyBuffer);
            ArrayPool<byte>.Shared.Return(valueBuffer);
        }
    }

    public void Dispose()
    {
        inner.Dispose();
    }
}
