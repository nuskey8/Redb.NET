using MessagePack;
using Redb;
using Redb.MessagePack;
using UnityEngine;

public class Sandbox : MonoBehaviour
{
    void Start()
    {
        using var db = RedbDatabase.Create("test.redb")
            .WithMessagePackSerializer();

        using (var tx = db.BeginWrite())
        {
            using var table = tx.OpenTable<string, Person>("my_table");

            table.Insert("key1", new Person { Name = "Alice", Age = 30 });
            table.Insert("key2", new Person { Name = "Bob", Age = 25 });

            tx.Commit();
        }

        using (var tx = db.BeginRead())
        {
            using var table = tx.OpenTable<string, Person>("my_table");

            Debug.Log($"key1: {table.Get("key1")}");
            Debug.Log($"key2: {table.Get("key2")}");
        }
    }
}

[MessagePackObject]
public record Person
{
    [Key(0)]
    public string Name { get; set; }

    [Key(1)]
    public int Age { get; set; }
}