namespace Redb.MessagePack;

public static class MessagePackRedbEncodingExtensions
{
    public static RedbDatabase WithMessagePackSerializer(this RedbDatabase database)
    {
        database.Encoding = MessagePackRedbEncoding.Instance;
        return database;
    }
}