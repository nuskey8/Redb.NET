namespace Redb;

public interface IRedbEncoding
{
    bool TryEncode<T>(T value, Span<byte> buffer, out int bytesWritten)
#if NET10_0_OR_GREATER
    where T : allows ref struct
#endif
    ;
    T Decode<T>(ReadOnlySpan<byte> data)
#if NET10_0_OR_GREATER
    where T : allows ref struct
#endif
    ;
}
