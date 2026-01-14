namespace Redb;

public interface IRedbEncoding
{
    bool TryEncode<T>(T value, Span<byte> buffer, out int bytesWritten);
    T Decode<T>(ReadOnlySpan<byte> data);
}
