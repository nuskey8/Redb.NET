using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Redb;

public unsafe sealed class WriteTransaction : IDisposable
{
    readonly RedbDatabase database;
    PooledList<Table> openedTables = new(8);
    void* tx;

    internal WriteTransaction(RedbDatabase database, void* tx)
    {
        this.database = database;
        this.tx = tx;
    }

    public void SetDurability(Durability durability)
    {
        ThrowIfDisposed();

        var code = NativeMethods.redb_write_tx_set_durability(tx, (redb_durability)durability);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to set transaction durability", code);
        }
    }

    public void SetTwoPhaseCommit(bool enable)
    {
        ThrowIfDisposed();

        var code = NativeMethods.redb_write_tx_set_two_phase_commit(tx, enable);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to call set_two_phase_commit()", code);
        }
    }

    public void SetQuickRepair(bool enable)
    {
        ThrowIfDisposed();

        var code = NativeMethods.redb_write_tx_set_quick_repair(tx, enable);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to call set_quick_repair()", code);
        }
    }

    public Table OpenTable(ReadOnlySpan<byte> name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore(nameBuffer);
    }

    public Table OpenTable(ReadOnlySpan<char> name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore(nameBuffer);
    }

    public Table<TKey, TValue> OpenTable<TKey, TValue>(ReadOnlySpan<byte> name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore<TKey, TValue>(nameBuffer);
    }

    public Table<TKey, TValue> OpenTable<TKey, TValue>(ReadOnlySpan<char> name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(name);
        return OpenTableCore<TKey, TValue>(nameBuffer);
    }

    Table OpenTableCore(NullTerminatedUtf8String name)
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
        var result = new Table(database, table);
        openedTables.Add(result);
        return result;
    }

    Table<TKey, TValue> OpenTableCore<TKey, TValue>(NullTerminatedUtf8String name)
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
        var result = new Table<TKey, TValue>(database, table);
        openedTables.Add(result.inner);
        return result;
    }

    public void DeleteTable(ReadOnlySpan<byte> utf8Name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(utf8Name);
        DeleteTableCore(nameBuffer);
    }

    public void DeleteTable(ReadOnlySpan<char> name)
    {
        ThrowIfDisposed();

        using var nameBuffer = new NullTerminatedUtf8String(name);
        DeleteTableCore(nameBuffer);
    }

    void DeleteTableCore(NullTerminatedUtf8String name)
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

    public void RenameTable(ReadOnlySpan<byte> oldUtf8Name, ReadOnlySpan<byte> newUtf8Name)
    {
        ThrowIfDisposed();

        using var oldNameBuffer = new NullTerminatedUtf8String(oldUtf8Name);
        using var newNameBuffer = new NullTerminatedUtf8String(newUtf8Name);
        RenameTableCore(oldNameBuffer, newNameBuffer);
    }

    public void RenameTable(ReadOnlySpan<char> oldName, ReadOnlySpan<char> newName)
    {
        ThrowIfDisposed();

        using var oldNameBuffer = new NullTerminatedUtf8String(oldName);
        using var newNameBuffer = new NullTerminatedUtf8String(newName);
        RenameTableCore(oldNameBuffer, newNameBuffer);
    }

    void RenameTableCore(NullTerminatedUtf8String oldName, NullTerminatedUtf8String newName)
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
        ThrowIfDisposed();

        DisposeTables();
        var code = NativeMethods.redb_write_tx_commit(tx);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to commit transaction", code);
        }
        tx = null;
    }

    public ulong PersistentSavepoint()
    {
        ThrowIfDisposed();

        ulong id;
        var code = NativeMethods.redb_write_tx_persistent_savepoint(tx, &id);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to persistent savepoint", code);
        }

        return id;
    }

    public Savepoint GetPersistentSavepoint(ulong id)
    {
        ThrowIfDisposed();

        void* savepoint;
        var code = NativeMethods.redb_write_tx_get_persistent_savepoint(tx, id, &savepoint);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to get persistent savepoint", code);
        }

        Debug.Assert(savepoint != null);
        return new Savepoint(savepoint);
    }

    public bool DeletePersistentSavepoint(ulong id)
    {
        ThrowIfDisposed();

        bool success;
        var code = NativeMethods.redb_write_tx_delete_persistent_savepoint(tx, id, &success);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to delete persistent savepoint", code);
        }

        return success;
    }

    public ulong[] ListPersistentSavepoints()
    {
        ThrowIfDisposed();

        ulong* idsPtr;
        nuint count;
        var code = NativeMethods.redb_write_tx_lists_persistent_savepoint(tx, &idsPtr, &count);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to list persistent savepoints", code);
        }

        try
        {
            var idsSpan = new ReadOnlySpan<ulong>(idsPtr, (int)count);
            return idsSpan.ToArray();
        }
        finally
        {
            NativeMethods.redb_free(idsPtr);
        }
    }

    public Savepoint EphemeralSavepoint()
    {
        ThrowIfDisposed();

        void* savepoint;
        var code = NativeMethods.redb_write_tx_ephemeral_savepoint(tx, &savepoint);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to create ephemeral savepoint", code);
        }

        Debug.Assert(savepoint != null);
        return new Savepoint(savepoint);
    }

    public void RestoreSavepoint(Savepoint savepoint)
    {
        ThrowIfDisposed();

        var code = NativeMethods.redb_write_tx_restore_savepoint(tx, savepoint.AsPtr());
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to restore to savepoint", code);
        }
    }

    public void Dispose()
    {
        if (tx != null)
        {
            DisposeTables();

            var code = NativeMethods.redb_write_tx_abort(tx);
            if (code != 0)
            {
                throw new RedbDatabaseException("Failed to abort transaction", code);
            }
            tx = null;
        }
    }

    void DisposeTables()
    {
        for (int i = 0; i < openedTables.Count; i++)
        {
            openedTables[i].Dispose();
        }
        openedTables.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ThrowIfDisposed()
    {
        ThrowHelper.ThrowIfDisposed(tx == null, nameof(WriteTransaction));
    }
}
