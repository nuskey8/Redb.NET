using System.Runtime.CompilerServices;

namespace Redb;

public unsafe struct RedbBlob : IDisposable
{
    byte* ptr;
    nuint length;

    internal RedbBlob(byte* ptr, nuint length)
    {
        this.ptr = ptr;
        this.length = length;
    }

    public ReadOnlySpan<byte> AsSpan()
    {
        ThrowIfDisposed();
        return new ReadOnlySpan<byte>(ptr, (int)length);
    }

    public void Dispose()
    {
        if (ptr != null)
        {
            NativeMethods.redb_free_blob(ptr);
            ptr = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly void ThrowIfDisposed()
    {
        ThrowHelper.ThrowIfDisposed(ptr == null, nameof(RedbBlob));
    }
}

public struct RedbBlobKeyValuePair : IDisposable
{
    public RedbBlob Key;
    public RedbBlob Value;

    public void Dispose()
    {
        Key.Dispose();
        Value.Dispose();
    }
}