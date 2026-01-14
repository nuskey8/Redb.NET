using System.Diagnostics;

namespace Redb;

public unsafe struct WriteTransaction : IDisposable
{
    RedbDatabase database;
    void* tx;

    internal WriteTransaction(RedbDatabase database, void* tx)
    {
        this.database = database;
        this.tx = tx;
    }

    public readonly void SetDurability(RedbDurability durability)
    {
        var code = NativeMethods.redb_write_tx_set_durability(tx, (redb_durability)durability);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to set transaction durability", code);
        }
    }

    public readonly Table OpenTable(ReadOnlySpan<byte> name)
    {
        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore(nameBuffer);
    }

    public readonly Table OpenTable(ReadOnlySpan<char> name)
    {
        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore(nameBuffer);
    }

    public readonly Table<TKey, TValue> OpenTable<TKey, TValue>(ReadOnlySpan<byte> name)
    {
        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore<TKey, TValue>(nameBuffer);
    }

    public readonly Table<TKey, TValue> OpenTable<TKey, TValue>(ReadOnlySpan<char> name)
    {
        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore<TKey, TValue>(nameBuffer);
    }

    readonly Table OpenTableCore(NullTerminatedUtf8String name)
    {
        void* table;

        fixed (byte* namePtr = name)
        {
            int code = NativeMethods.redb_write_tx_open_table(tx, namePtr, &table);
            if (code != 0)
            {
                throw new RedbDatabaseException("Failed to open table for write", code);
            }
        }

        Debug.Assert(table != null);
        return new Table(database, table);
    }

    readonly Table<TKey, TValue> OpenTableCore<TKey, TValue>(NullTerminatedUtf8String name)
    {
        void* table;

        fixed (byte* namePtr = name)
        {
            int code = NativeMethods.redb_write_tx_open_table(tx, namePtr, &table);
            if (code != 0)
            {
                throw new RedbDatabaseException("Failed to open table for write", code);
            }
        }

        Debug.Assert(table != null);
        return new Table<TKey, TValue>(database, table);
    }

    public void Commit()
    {
        var code = NativeMethods.redb_commit(tx);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to commit transaction", code);
        }
        tx = null;
    }

    public void Dispose()
    {
        if (tx != null)
        {
            NativeMethods.redb_free_write_transaction(tx);
            tx = null;
        }
    }
}
