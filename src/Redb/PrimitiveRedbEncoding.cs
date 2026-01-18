using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Redb;

public sealed class PrimitiveRedbEncoding : IRedbEncoding
{
    public static readonly PrimitiveRedbEncoding Instance = new();

    public static bool CanEncode<T>()
    {
        return typeof(T) == typeof(int)
            || typeof(T) == typeof(uint)
            || typeof(T) == typeof(long)
            || typeof(T) == typeof(ulong)
            || typeof(T) == typeof(short)
            || typeof(T) == typeof(ushort)
            || typeof(T) == typeof(byte)
            || typeof(T) == typeof(sbyte)
            || typeof(T) == typeof(char)
            || typeof(T) == typeof(bool)
            || typeof(T) == typeof(float)
            || typeof(T) == typeof(double)
            || typeof(T) == typeof(decimal)
            || typeof(T) == typeof(string)
            || typeof(T) == typeof(Guid)
            || typeof(T) == typeof(DateTime)
            || typeof(T) == typeof(DateTimeOffset);
    }

    public bool TryEncode<T>(T value, Span<byte> buffer, out int bytesWritten)
    {
        if (typeof(T) == typeof(int))
        {
            if (buffer.Length < 4)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, int>(ref value));
            bytesWritten = 4;
            return true;
        }
        else if (typeof(T) == typeof(uint))
        {
            if (buffer.Length < 4)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, uint>(ref value));
            bytesWritten = 4;
            return true;
        }
        else if (typeof(T) == typeof(long))
        {
            if (buffer.Length < 8)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, long>(ref value));
            bytesWritten = 8;
            return true;
        }
        else if (typeof(T) == typeof(ulong))
        {
            if (buffer.Length < 8)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, ulong>(ref value));
            bytesWritten = 8;
            return true;
        }
        else if (typeof(T) == typeof(short))
        {
            if (buffer.Length < 2)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, short>(ref value));
            bytesWritten = 2;
            return true;
        }
        else if (typeof(T) == typeof(ushort))
        {
            if (buffer.Length < 2)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, ushort>(ref value));
            bytesWritten = 2;
            return true;
        }
        else if (typeof(T) == typeof(byte))
        {
            if (buffer.Length < 1)
            {
                bytesWritten = 0;
                return false;
            }
            buffer[0] = Unsafe.As<T, byte>(ref value);
            bytesWritten = 1;
            return true;
        }
        else if (typeof(T) == typeof(sbyte))
        {
            if (buffer.Length < 1)
            {
                bytesWritten = 0;
                return false;
            }
            buffer[0] = (byte)Unsafe.As<T, sbyte>(ref value);
            bytesWritten = 1;
            return true;
        }
        else if (typeof(T) == typeof(char))
        {
            if (buffer.Length < 2)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, char>(ref value));
            bytesWritten = 2;
            return true;
        }
        else if (typeof(T) == typeof(bool))
        {
            if (buffer.Length < 1)
            {
                bytesWritten = 0;
                return false;
            }
            buffer[0] = Unsafe.As<T, bool>(ref value) ? (byte)1 : (byte)0;
            bytesWritten = 1;
            return true;
        }
        else if (typeof(T) == typeof(float))
        {
            if (buffer.Length < 4)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, float>(ref value));
            bytesWritten = 4;
            return true;
        }
        else if (typeof(T) == typeof(double))
        {
            if (buffer.Length < 8)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, double>(ref value));
            bytesWritten = 8;
            return true;
        }
        else if (typeof(T) == typeof(decimal))
        {
            if (buffer.Length < 16)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, decimal>(ref value));
            bytesWritten = 16;
            return true;
        }
        else if (typeof(T) == typeof(string))
        {
            var str = Unsafe.As<T, string>(ref value);
#if NET8_0_OR_GREATER
            return Encoding.UTF8.TryGetBytes(str, buffer, out bytesWritten);
#else
            var byteCount = Encoding.UTF8.GetByteCount(str);
            if (buffer.Length < byteCount)
            {
                bytesWritten = 0;
                return false;
            }
            bytesWritten = Encoding.UTF8.GetBytes(str, buffer);
            return true;
#endif
        }
        else if (typeof(T) == typeof(Guid))
        {
            if (buffer.Length < 16)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, Guid>(ref value));
            bytesWritten = 16;
            return true;
        }
        else if (typeof(T) == typeof(DateTime))
        {
            if (buffer.Length < 8)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, DateTime>(ref value).ToBinary());
            bytesWritten = 8;
            return true;
        }
        else if (typeof(T) == typeof(DateTimeOffset))
        {
            if (buffer.Length < 8)
            {
                bytesWritten = 0;
                return false;
            }
            Unsafe.WriteUnaligned(ref buffer[0], Unsafe.As<T, DateTimeOffset>(ref value).DateTime.ToBinary());
            bytesWritten = 8;
            return true;
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(T)} is not supported by {nameof(PrimitiveRedbEncoding)}.");
        }
    }

    public T Decode<T>(ReadOnlySpan<byte> data)
    {
        if (typeof(T) == typeof(int))
        {
            if (data.Length != 4) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<int>(data);
            return Unsafe.As<int, T>(ref result);
        }
        else if (typeof(T) == typeof(long))
        {
            if (data.Length != 8) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<long>(data);
            return Unsafe.As<long, T>(ref result);
        }
        else if (typeof(T) == typeof(uint))
        {
            if (data.Length != 4) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<uint>(data);
            return Unsafe.As<uint, T>(ref result);
        }
        else if (typeof(T) == typeof(ulong))
        {
            if (data.Length != 8) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<ulong>(data);
            return Unsafe.As<ulong, T>(ref result);
        }
        else if (typeof(T) == typeof(short))
        {
            if (data.Length != 2) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<short>(data);
            return Unsafe.As<short, T>(ref result);
        }
        else if (typeof(T) == typeof(ushort))
        {
            if (data.Length != 2) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<ushort>(data);
            return Unsafe.As<ushort, T>(ref result);
        }
        else if (typeof(T) == typeof(byte))
        {
            if (data.Length != 1) goto SIZE_MISMATCH;
            var result = data[0];
            return Unsafe.As<byte, T>(ref result);
        }
        else if (typeof(T) == typeof(sbyte))
        {
            if (data.Length != 1) goto SIZE_MISMATCH;
            var temp = (sbyte)data[0];
            return Unsafe.As<sbyte, T>(ref temp);
        }
        else if (typeof(T) == typeof(char))
        {
            if (data.Length != 2) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<char>(data);
            return Unsafe.As<char, T>(ref result);
        }
        else if (typeof(T) == typeof(bool))
        {
            if (data.Length != 1) goto SIZE_MISMATCH;
            var result = data[0] != 0;
            return Unsafe.As<bool, T>(ref result);
        }
        else if (typeof(T) == typeof(float))
        {
            if (data.Length != 4) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<float>(data);
            return Unsafe.As<float, T>(ref result);
        }
        else if (typeof(T) == typeof(double))
        {
            if (data.Length != 8) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<double>(data);
            return Unsafe.As<double, T>(ref result);
        }
        else if (typeof(T) == typeof(decimal))
        {
            if (data.Length != 16) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<decimal>(data);
            return Unsafe.As<decimal, T>(ref result);
        }
        else if (typeof(T) == typeof(string))
        {
            var str = Encoding.UTF8.GetString(data);
            return Unsafe.As<string, T>(ref str);
        }
        else if (typeof(T) == typeof(Guid))
        {
            if (data.Length != 16) goto SIZE_MISMATCH;
            var result = MemoryMarshal.Read<Guid>(data);
            return Unsafe.As<Guid, T>(ref result);
        }
        else if (typeof(T) == typeof(DateTime))
        {
            if (data.Length != 8) goto SIZE_MISMATCH;
            var binary = MemoryMarshal.Read<long>(data);
            var dt = DateTime.FromBinary(binary);
            return Unsafe.As<DateTime, T>(ref dt);
        }
        else if (typeof(T) == typeof(DateTimeOffset))
        {
            if (data.Length != 8) goto SIZE_MISMATCH;
            var binary = MemoryMarshal.Read<long>(data);
            var dto = new DateTimeOffset(DateTime.FromBinary(binary));
            return Unsafe.As<DateTimeOffset, T>(ref dto);
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(T)} is not supported by {nameof(PrimitiveRedbEncoding)}.");
        }

    SIZE_MISMATCH:
        throw new InvalidOperationException($"Data size {data.Length} does not match the size of type {typeof(T)}.");
    }
}