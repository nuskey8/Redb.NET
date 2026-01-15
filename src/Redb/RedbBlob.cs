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
        if (ptr == null)
        {
            throw new ObjectDisposedException(nameof(RedbBlob));
        }

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
}