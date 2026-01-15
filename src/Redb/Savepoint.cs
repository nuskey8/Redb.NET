using Redb.Internal;

namespace Redb;

public unsafe sealed class Savepoint : IDisposable
{
    void* ptr;

    internal Savepoint(void* ptr)
    {
        this.ptr = ptr;
    }

    internal void* AsPtr()
    {
        ThrowHelper.ThrowIfDisposed(ptr == null, nameof(Savepoint));
        return ptr;
    }

    void DisposeCore()
    {
        if (ptr != null)
        {
            NativeMethods.redb_free_savepoint(ptr);
            ptr = null;
        }
    }

    ~Savepoint()
    {
        DisposeCore();
    }

    void IDisposable.Dispose()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
    }
}