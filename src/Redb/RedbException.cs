namespace Redb;

public class RedbException(string message) : Exception(message);

public class RedbEncodingException(string message) : RedbException(message)
{
    public static void Throw(string message) => throw new RedbEncodingException(message);
}

public class RedbDatabaseException(string message, int code) : RedbException(message)
{
    public int Code { get; } = code;
}