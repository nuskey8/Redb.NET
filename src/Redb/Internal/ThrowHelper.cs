using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Redb;

internal static class ThrowHelper
{
    [StackTraceHidden]
    public static void ThrowIfDisposed([DoesNotReturnIf(true)] bool condition, string name)
    {
        if (condition)
        {
            throw new ObjectDisposedException(name);
        }
    }

    [StackTraceHidden]
    public static void ThrowIfError(int code, string message)
    {
        if (code != NativeMethods.REDB_OK)
        {
            throw new RedbDatabaseException(message, code);
        }
    }
}