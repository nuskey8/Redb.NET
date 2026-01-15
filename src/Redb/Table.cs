using System.Buffers;
using System.Runtime.CompilerServices;
using Redb.Internal;

namespace Redb;

public unsafe sealed class Table : IDisposable
{
    internal readonly RedbDatabase database;
    void* table;

    internal Table(RedbDatabase database, void* table)
    {
        this.database = database;
        this.table = table;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value)
    {
        ThrowIfDisposed();

        fixed (byte* keyPtr = key)
        fixed (byte* valuePtr = value)
        {
            var code = NativeMethods.redb_insert(table, keyPtr, (nuint)key.Length, valuePtr, (nuint)value.Length);
            ThrowHelper.ThrowIfError(code, "Failed to insert value to table.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ThrowIfDisposed()
    {
        ThrowHelper.ThrowIfDisposed(table == null, nameof(Table));
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

public unsafe sealed class Table<TKey, TValue> : IDisposable
{
    internal readonly Table inner;

    internal Table(RedbDatabase database, void* table)
    {
        inner = new Table(database, table);
    }

    public void Insert(TKey key, TValue value)
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
