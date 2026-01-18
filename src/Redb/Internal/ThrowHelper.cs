using System.Diagnostics.CodeAnalysis;
using Redb.Interop;

namespace Redb.Internal;

internal static class ThrowHelper
{
    public static void ThrowIfDisposed([DoesNotReturnIf(true)] bool condition, string name)
    {
        if (condition)
        {
            throw new ObjectDisposedException(name);
        }
    }

    public static void ThrowIfError(int code, string message)
    {
        if (code != NativeMethods.REDB_OK)
        {
            throw new RedbDatabaseException(message, code);
        }
    }
}