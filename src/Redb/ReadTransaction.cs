using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Redb;

public unsafe struct ReadTransaction : IDisposable
{
    readonly RedbDatabase database;
    void* tx;

    internal ReadTransaction(RedbDatabase database, void* tx)
    {
        this.database = database;
        this.tx = tx;
    }

    public void Dispose()
    {
        if (tx != null)
        {
            NativeMethods.redb_free_read_transaction(tx);
            tx = null;
        }
    }

    public readonly ulong Length
    {
        get
        {
            ThrowIfDisposed();

            ulong length;
            var code = NativeMethods.redb_table_len(tx, &length);
            if (code != NativeMethods.REDB_OK)
            {
                throw new RedbDatabaseException("Failed to get database length", code);
            }

            return length;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlyTable OpenTable(ReadOnlySpan<byte> utf8Name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(utf8Name);
        return OpenTableCore(nameBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlyTable OpenTable(ReadOnlySpan<char> name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore(nameBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlyTable<TKey, TValue> OpenTable<TKey, TValue>(ReadOnlySpan<byte> utf8Name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(utf8Name);
        return OpenTableCore<TKey, TValue>(nameBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlyTable<TKey, TValue> OpenTable<TKey, TValue>(ReadOnlySpan<char> name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore<TKey, TValue>(nameBuffer);
    }

    readonly ReadOnlyTable OpenTableCore(NullTerminatedUtf8String name)
    {
        void* table;

        fixed (byte* namePtr = name)
        {
            var code = NativeMethods.redb_read_tx_open_table(tx, namePtr, &table);
            if (code != NativeMethods.REDB_OK)
            {
                throw new RedbDatabaseException("Failed to open table", code);
            }
        }

        Debug.Assert(table != null);
        return new ReadOnlyTable(database, table);
    }

    readonly ReadOnlyTable<TKey, TValue> OpenTableCore<TKey, TValue>(NullTerminatedUtf8String name)
    {
        void* table;

        fixed (byte* namePtr = name)
        {
            var code = NativeMethods.redb_read_tx_open_table(tx, namePtr, &table);
            if (code != NativeMethods.REDB_OK)
            {
                throw new RedbDatabaseException("Failed to open table", code);
            }
        }

        Debug.Assert(table != null);
        return new ReadOnlyTable<TKey, TValue>(database, table);
    }

    readonly void ThrowIfDisposed()
    {
        if (tx == null)
        {
            throw new ObjectDisposedException(nameof(ReadTransaction));
        }
    }
}
