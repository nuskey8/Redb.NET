using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Redb;

public unsafe sealed class RedbDatabase : IDisposable
{
    public IRedbEncoding Encoding { get; set; } = PrimitiveRedbEncoding.Instance;
    void* db;

    RedbDatabase(void* db)
    {
        this.db = db;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RedbDatabase Create(ReadOnlySpan<byte> utf8Path, RedbDatabaseOptions? options = null)
    {
        var pathBuffer = new NullTerminatedUtf8String(utf8Path);
        return CreateCore(pathBuffer, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RedbDatabase Create(ReadOnlySpan<char> path, RedbDatabaseOptions? options = null)
    {
        var pathBuffer = new NullTerminatedUtf8String(path);
        return CreateCore(pathBuffer, options);
    }

    static RedbDatabase CreateCore(NullTerminatedUtf8String path, RedbDatabaseOptions? options = null)
    {
        void* db;
        var opts = options?.ToNative() ?? default;

        fixed (byte* pathPtr = path)
        {
            int code = NativeMethods.redb_create_database(pathPtr, options == null ? null : &opts, &db);
            if (code != 0)
            {
                throw new RedbDatabaseException("Failed to create database", code);
            }

            Debug.Assert(db != null);
            return new RedbDatabase(db);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RedbDatabase Open(ReadOnlySpan<byte> utf8Path)
    {
        var pathBuffer = new NullTerminatedUtf8String(utf8Path);
        return OpenCore(pathBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RedbDatabase Open(ReadOnlySpan<char> path)
    {
        var pathBuffer = new NullTerminatedUtf8String(path);
        return OpenCore(pathBuffer);
    }

    static RedbDatabase OpenCore(NullTerminatedUtf8String path)
    {
        void* db;

        fixed (byte* pathPtr = path)
        {
            int code = NativeMethods.redb_open_database(pathPtr, &db);
            if (code != 0)
            {
                throw new RedbDatabaseException("Failed to open database", code);
            }

            Debug.Assert(db != null);
            return new RedbDatabase(db);
        }
    }

    public void Compact()
    {
        ThrowIfDisposed();

        int code = NativeMethods.redb_compact_database(db);
        if (code != 0)
        {
            throw new RedbDatabaseException("Failed to compact database", code);
        }
    }

    public WriteTransaction BeginWrite()
    {
        ThrowIfDisposed();

        void* tx;

        var code = NativeMethods.redb_begin_write(db, &tx);
        if (code != NativeMethods.REDB_OK)
        {
            throw new RedbDatabaseException("Failed to begin write transaction", code);
        }

        Debug.Assert(tx != null);
        return new WriteTransaction(this, tx);
    }

    public ReadTransaction BeginRead()
    {
        ThrowIfDisposed();

        void* tx;

        var code = NativeMethods.redb_begin_read(db, &tx);
        if (code != NativeMethods.REDB_OK)
        {
            throw new RedbDatabaseException("Failed to begin read transaction", code);
        }

        Debug.Assert(tx != null);
        return new ReadTransaction(this, tx);
    }

    public void Dispose()
    {
        if (db != null)
        {
            NativeMethods.redb_free_database(db);
            db = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ThrowIfDisposed()
    {
        ThrowHelper.ThrowIfDisposed(db == null, nameof(RedbDatabase));
    }
}