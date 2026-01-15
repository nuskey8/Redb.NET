namespace Redb.Internal;

public ref struct ReadOnlySpanKeyValuePair
{
    public ReadOnlySpan<byte> Key;
    public ReadOnlySpan<byte> Value;
}