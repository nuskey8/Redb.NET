using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Redb;

internal static class ThrowHelper
{
    [StackTraceHidden]
    internal static void ThrowIfDisposed([DoesNotReturnIf(true)] bool condition, string name)
    {
        if (condition)
        {
            throw new ObjectDisposedException(name);
        }
    }
}