using System.Runtime.CompilerServices;
using Redb.Internal;
using Redb.Interop;

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

    public readonly ReadOnlySpan<byte> AsSpan()
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
