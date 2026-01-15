using System.Runtime.CompilerServices;

namespace Redb;

public unsafe class RedbBlob : IDisposable
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
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    ~RedbBlob()
    {
        DisposeCore();
    }

    void DisposeCore()
    {
        if (ptr != null)
        {
            NativeMethods.redb_free_blob(ptr);
            ptr = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ThrowIfDisposed()
    {
        ThrowHelper.ThrowIfDisposed(ptr == null, nameof(RedbBlob));
    }
}