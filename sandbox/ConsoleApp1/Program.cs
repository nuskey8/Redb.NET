using Redb;
using Redb.SystemTextJson;

try
{
    using var db = RedbDatabase.Create("test.redb", RedbDatabaseOptions.Default)
        .WithJsonSerializer();
    db.Compact();

    using (var tx = db.BeginWrite())
    {
        var table = tx.OpenTable<string, Person>("persons");

        table.Insert("alice", new Person("Alice", 18));
        table.Insert("bob", new Person("Bob", 30));
        table.Insert("carol", new Person("Carol", 25));
        table.Insert("dave", new Person("Dave", 40));
        table.Insert("eve", new Person("Eve", 22));

        tx.Commit();
    }

    using (var tx = db.BeginRead())
    {
        var table = tx.OpenTable<string, Person>("persons");

        foreach (var kv in table)
        {
            Console.WriteLine($"{kv.Key}: {kv.Value}");
        }

        foreach (var kv in table.GetRange("alice", "carol"))
        {
            Console.WriteLine($"{kv.Key}: {kv.Value}");
        }
    }
}
catch (RedbDatabaseException ex)
{
    Console.WriteLine($"RedbException({ex.Code}): {ex.Message}");
    return;
}

record Person(string Name, int Age);