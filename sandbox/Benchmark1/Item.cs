using LiteDB;

record Item
{
    [BsonId]
    public int Id { get; set; }
    public string Data { get; set; }
}