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

    public readonly void SetTwoPhaseCommit(bool enable)
    {
        var code = NativeMethods.redb_write_tx_set_two_phase_commit(tx, enable);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to call set_two_phase_commit()", code);
        }
    }

    public readonly void SetQuickRepair(bool enable)
    {
        var code = NativeMethods.redb_write_tx_set_quick_repair(tx, enable);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to call set_quick_repair()", code);
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

    public readonly void DeleteTable(ReadOnlySpan<byte> utf8Name)
    {
        using var nameBuffer = new NullTerminatedUtf8String(utf8Name);
        DeleteTableCore(nameBuffer);
    }

    public readonly void DeleteTable(ReadOnlySpan<char> name)
    {
        using var nameBuffer = new NullTerminatedUtf8String(name);
        DeleteTableCore(nameBuffer);
    }

    readonly void DeleteTableCore(NullTerminatedUtf8String name)
    {
        fixed (byte* namePtr = name)
        {
            int code = NativeMethods.redb_write_tx_delete_table(tx, namePtr);
            if (code != 0)
            {
                throw new RedbDatabaseException("Failed to delete table", code);
            }
        }
    }

    public readonly void RenameTable(ReadOnlySpan<byte> oldUtf8Name, ReadOnlySpan<byte> newUtf8Name)
    {
        using var oldNameBuffer = new NullTerminatedUtf8String(oldUtf8Name);
        using var newNameBuffer = new NullTerminatedUtf8String(newUtf8Name);
        RenameTableCore(oldNameBuffer, newNameBuffer);
    }

    public readonly void RenameTable(ReadOnlySpan<char> oldName, ReadOnlySpan<char> newName)
    {
        using var oldNameBuffer = new NullTerminatedUtf8String(oldName);
        using var newNameBuffer = new NullTerminatedUtf8String(newName);
        RenameTableCore(oldNameBuffer, newNameBuffer);
    }

    readonly void RenameTableCore(NullTerminatedUtf8String oldName, NullTerminatedUtf8String newName)
    {
        fixed (byte* oldNamePtr = oldName)
        fixed (byte* newNamePtr = newName)
        {
            int code = NativeMethods.redb_write_tx_rename_table(tx, oldNamePtr, newNamePtr);
            if (code != 0)
            {
                throw new RedbDatabaseException("Failed to rename table", code);
            }
        }
    }

    public void Commit()
    {
        var code = NativeMethods.redb_write_tx_commit(tx);
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
            var code = NativeMethods.redb_write_tx_abort(tx);
            if (code != 0)
            {
                throw new RedbDatabaseException("Failed to abort transaction", code);
            }
            tx = null;
        }
    }
}
