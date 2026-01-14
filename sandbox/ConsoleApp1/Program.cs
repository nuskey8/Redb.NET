using Redb;
using Redb.SystemTextJson;

try
{
    using var db = RedbDatabase.Create("test.redb", RedbDatabaseOptions.Default)
        .WithJsonSerializer();
    db.Compact();

    using (var writetx = db.BeginWrite())
    {
        using (var table = writetx.OpenTable<string, int>("my_table"))
        {
            table.Insert("foo", 12);
        }

        using (var table = writetx.OpenTable<string, Person>("persons"))
        {
            table.Insert("alice", new Person("Alice", 18));
            table.Insert("bob", new Person("Bob", 30));
        }

        writetx.Commit();
    }

    using (var readtx = db.BeginRead())
    {
        using (var table = readtx.OpenTable<string, int>("my_table"))
        {
            var value = table.Get("foo");
            Console.WriteLine($"foo: {value}");
        }

        using (var table = readtx.OpenTable<string, Person>("persons"))
        {
            var alice = table.Get("alice");
            var bob = table.Get("bob");
            Console.WriteLine(alice);
            Console.WriteLine(bob);
        }
    }
}
catch (RedbDatabaseException ex)
{
    Console.WriteLine($"RedbException({ex.Code}): {ex.Message}");
    return;
}

record Person(string Name, int Age);