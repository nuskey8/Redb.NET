using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Redb.Internal;

namespace Redb;

public unsafe sealed class ReadOnlyTable : IDisposable
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
    public RedbBlob Get(ReadOnlySpan<byte> key)
    {
        if (!TryGet(key, out var value))
        {
            throw new RedbDatabaseException($"Key `{Encoding.UTF8.GetString(key)}` not found", NativeMethods.REDB_ERROR_KEY_NOT_FOUND);
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(ReadOnlySpan<byte> key, out RedbBlob blob)
    {
        ThrowIfDisposed();

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

    public Enumerator GetEnumerator()
    {
        ThrowIfDisposed();

        void* iter;
        var code = NativeMethods.redb_iter(table, &iter);
        ThrowHelper.ThrowIfError(code, "Failed to create iterator for read-only table.");

        return new Enumerator(iter);
    }

    public RangeEnumerable GetRange(ReadOnlySpan<byte> startKey, ReadOnlySpan<byte> endKey)
    {
        ThrowIfDisposed();

        fixed (byte* startKeyPtr = startKey)
        fixed (byte* endKeyPtr = endKey)
        {
            void* iter;
            var code = NativeMethods.redb_range(table, startKeyPtr, (nuint)startKey.Length, endKeyPtr, (nuint)endKey.Length, &iter);
            ThrowHelper.ThrowIfError(code, "Failed to create range iterator for read-only table.");

            Debug.Assert(iter != null);
            return new RangeEnumerable(iter);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ThrowIfDisposed()
    {
        ThrowHelper.ThrowIfDisposed(table == null, nameof(ReadOnlyTable));
    }

    public ref struct RangeEnumerable
    {
        void* iter;

        internal RangeEnumerable(void* iter)
        {
            this.iter = iter;
        }

        public Enumerator GetEnumerator()
        {
            if (iter == null)
            {
                throw new InvalidOperationException("The range enumerable has already been enumerated.");
            }

            var e = new Enumerator(iter);
            iter = null;
            return e;
        }
    }

    public ref struct Enumerator
    {
        void* iter;
        RedbBlob keyBlob;
        RedbBlob valueBlob;

        ReadOnlySpanKeyValuePair current;

        internal Enumerator(void* iter)
        {
            this.iter = iter;
        }

        public ReadOnlySpanKeyValuePair Current => current;

        public bool MoveNext()
        {
            // dispose prev key/value blobs
            keyBlob.Dispose();
            valueBlob.Dispose();

            if (iter == null) return false;

            byte* keyPtr;
            nuint keyLen;
            byte* valuePtr;
            nuint valueLen;

            var code = NativeMethods.redb_iter_next(iter, &keyPtr, &keyLen, &valuePtr, &valueLen);
            if (code == NativeMethods.REDB_OK)
            {
                keyBlob = new RedbBlob(keyPtr, keyLen);
                valueBlob = new RedbBlob(valuePtr, valueLen);

                current = new ReadOnlySpanKeyValuePair
                {
                    Key = keyBlob.AsSpan(),
                    Value = valueBlob.AsSpan(),
                };

                return true;
            }
            else
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (iter != null)
            {
                keyBlob.Dispose();
                valueBlob.Dispose();

                NativeMethods.redb_free_iter(iter);
                iter = null;
            }
        }
    }
}

public unsafe sealed class ReadOnlyTable<TKey, TValue> : IDisposable
{
    internal readonly ReadOnlyTable inner;

    internal ReadOnlyTable(RedbDatabase database, void* table)
    {
        inner = new ReadOnlyTable(database, table);
    }

    public void Dispose()
    {
        inner.Dispose();
    }

    public TValue Get(TKey key)
    {
        if (!TryGet(key, out var value))
        {
            throw new RedbDatabaseException($"Key `{key}` not found", NativeMethods.REDB_ERROR_KEY_NOT_FOUND);
        }
        return value;
    }

    public bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value)
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

    public Enumerator GetEnumerator()
    {
        return new Enumerator(inner.GetEnumerator(), inner.database.Encoding);
    }

    public RangeEnumerable GetRange(TKey startKey, TKey endKey)
    {
        var encoding = inner.database.Encoding;
        var startBuffer = ArrayPool<byte>.Shared.Rent(256);
        var endBuffer = ArrayPool<byte>.Shared.Rent(256);

        try
        {
            int startBytesWritten;
            while (encoding.TryEncode(startKey, startBuffer, out startBytesWritten) == false)
            {
                ArrayPool<byte>.Shared.Return(startBuffer);
                startBuffer = ArrayPool<byte>.Shared.Rent(startBuffer.Length * 2);
            }

            int endBytesWritten;
            while (encoding.TryEncode(endKey, endBuffer, out endBytesWritten) == false)
            {
                ArrayPool<byte>.Shared.Return(endBuffer);
                endBuffer = ArrayPool<byte>.Shared.Rent(endBuffer.Length * 2);
            }

            var startKeySpan = new ReadOnlySpan<byte>(startBuffer, 0, startBytesWritten);
            var endKeySpan = new ReadOnlySpan<byte>(endBuffer, 0, endBytesWritten);

            var enumerable = inner.GetRange(startKeySpan, endKeySpan);
            return new RangeEnumerable(enumerable, encoding);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(startBuffer);
            ArrayPool<byte>.Shared.Return(endBuffer);
        }
    }

    public ref struct RangeEnumerable
    {
        ReadOnlyTable.RangeEnumerable inner;
        readonly IRedbEncoding encoding;

        internal RangeEnumerable(ReadOnlyTable.RangeEnumerable inner, IRedbEncoding encoding)
        {
            this.inner = inner;
            this.encoding = encoding;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(inner.GetEnumerator(), encoding);
        }
    }

    public ref struct Enumerator
    {
        ReadOnlyTable.Enumerator inner;
        readonly IRedbEncoding encoding;
        KeyValuePair<TKey, TValue> current;

        internal Enumerator(ReadOnlyTable.Enumerator inner, IRedbEncoding encoding)
        {
            this.inner = inner;
            this.encoding = encoding;
        }

        public KeyValuePair<TKey, TValue> Current => current;

        public bool MoveNext()
        {
            if (inner.MoveNext())
            {
                var kv = inner.Current;
                var key = encoding.Decode<TKey>(kv.Key);
                var value = encoding.Decode<TValue>(kv.Value);
                current = new KeyValuePair<TKey, TValue>(key, value);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            inner.Dispose();
        }
    }
}