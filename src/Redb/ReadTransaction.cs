using System.Diagnostics;
using System.Runtime.CompilerServices;
using Redb.Internal;

namespace Redb;

public unsafe sealed class ReadTransaction : IDisposable
{
    readonly RedbDatabase database;
    PooledList<ReadOnlyTable> openedTables = new(8);
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
            foreach (var table in openedTables.AsSpan())
            {
                table.Dispose();
            }
            openedTables.Dispose();

            NativeMethods.redb_free_read_transaction(tx);
            tx = null;
        }
    }

    public ulong Length
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
    public ReadOnlyTable OpenTable(ReadOnlySpan<byte> utf8Name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(utf8Name);
        return OpenTableCore(nameBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyTable OpenTable(ReadOnlySpan<char> name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore(nameBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyTable<TKey, TValue> OpenTable<TKey, TValue>(ReadOnlySpan<byte> utf8Name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(utf8Name);
        return OpenTableCore<TKey, TValue>(nameBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyTable<TKey, TValue> OpenTable<TKey, TValue>(ReadOnlySpan<char> name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore<TKey, TValue>(nameBuffer);
    }

    ReadOnlyTable OpenTableCore(NullTerminatedUtf8String name)
    {
        void* t;

        fixed (byte* namePtr = name)
        {
            var code = NativeMethods.redb_read_tx_open_table(tx, namePtr, &t);
            if (code != NativeMethods.REDB_OK)
            {
                throw new RedbDatabaseException("Failed to open table", code);
            }
        }

        Debug.Assert(t != null);

        var table = new ReadOnlyTable(database, t);
        openedTables.Add(table);
        return table;
    }

    ReadOnlyTable<TKey, TValue> OpenTableCore<TKey, TValue>(NullTerminatedUtf8String name)
    {
        void* t;

        fixed (byte* namePtr = name)
        {
            var code = NativeMethods.redb_read_tx_open_table(tx, namePtr, &t);
            if (code != NativeMethods.REDB_OK)
            {
                throw new RedbDatabaseException("Failed to open table", code);
            }
        }

        Debug.Assert(t != null);

        var table = new ReadOnlyTable<TKey, TValue>(database, t);
        openedTables.Add(table.inner);
        return table;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ThrowIfDisposed()
    {
        ThrowHelper.ThrowIfDisposed(tx == null, nameof(ReadTransaction));
    }
}
