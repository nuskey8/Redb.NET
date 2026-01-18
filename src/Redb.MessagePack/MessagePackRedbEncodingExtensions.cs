using MessagePack;

namespace Redb.MessagePack;

public static class MessagePackRedbEncodingExtensions
{
    public static RedbDatabase WithMessagePackSerializer(this RedbDatabase database, MessagePackSerializerOptions? options = null)
    {
        database.Encoding = new MessagePackRedbEncoding(options);
        return database;
    }
}