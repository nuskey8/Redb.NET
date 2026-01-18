using BenchmarkDotNet.Attributes;
using CsSqlite;
using LiteDB;
using Redb;


[Config(typeof(BenchmarkConfig))]
public class ReadBenchmark
{
    const int N = 10000;
    const int READ_COUNT = 1000;

    DirectoryInfo directory;
    SqliteConnection cssqliteConnection;
    RedbDatabase redbDatabase;
    LiteDatabase liteDatabase;

    ZipfDistribution zipf;
    int[] readKeys;

    [GlobalSetup]
    public async Task CreateDB()
    {
        directory = Directory.CreateTempSubdirectory("benchmarks");
        var sqlitePath = Path.Combine(directory.FullName, "bench.sqlite");
        var redbPath = Path.Combine(directory.FullName, "bench.redb");
        var litedbPath = Path.Combine(directory.FullName, "bench.litedb");

        zipf = new ZipfDistribution(N, 0.5);

        readKeys = new int[READ_COUNT];
        for (int i = 0; i < READ_COUNT; i++)
        {
            readKeys[i] = zipf.Next();
        }

        // Setup Redb
        redbDatabase = RedbDatabase.Create(redbPath);
        using (var tx = redbDatabase.BeginWrite())
        {
            var table = tx.OpenTable<int, string>("items");

            for (var i = 0; i < N; i++)
            {
                table.Insert(i, $"val{i:D10}");
            }

            tx.Commit();
        }

        // Setup LiteDB
        liteDatabase = new LiteDatabase(litedbPath);
        var liteCollection = liteDatabase.GetCollection<Item>("items");
        for (var i = 0; i < N; i++)
        {
            liteCollection.Insert(new Item { Data = $"val{i:D10}" });
            liteCollection.EnsureIndex(x => x.Id);
        }
        liteDatabase.Commit();

        // Setup sqlite
        using (var sqlite = new SqliteConnection(sqlitePath))
        {
            sqlite.Open();
            sqlite.ExecuteNonQuery("DROP TABLE IF EXISTS items;");
            sqlite.ExecuteNonQuery(
                """
                CREATE TABLE IF NOT EXISTS items (
                    id INTEGER NOT NULL PRIMARY KEY,
                    data TEXT NOT NULL
                );
                """);

            for (var i = 0; i < N; i++)
            {
                sqlite.ExecuteNonQuery(
                    $"""
                     INSERT INTO items (id, data) VALUES ({i}, 'val{i:D10}');
                     """);
            }
        }

        cssqliteConnection = new SqliteConnection(sqlitePath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        cssqliteConnection.Dispose();
        liteDatabase.Dispose();
        redbDatabase.Dispose();

        try
        {
            directory.Delete(true);
        }
        catch (DirectoryNotFoundException) { }
    }

    [Benchmark(Description = "FindByKey - Redb.NET")]
    public void Redb_FindByKey()
    {
        using var tx = redbDatabase.BeginRead();
        var table = tx.OpenTable<int, string>("items");

        for (var i = 0; i < READ_COUNT; i++)
        {
            _ = table.Get(readKeys[i]);
        }
    }

    [Benchmark(Description = "FindByKey - LiteDB")]
    public void LiteDB_FindByKey()
    {
        var liteCollection = liteDatabase.GetCollection<Item>("items");
        for (var i = 0; i < READ_COUNT; i++)
        {
            _ = liteCollection.Find(Query.EQ("Id", readKeys[i]));
        }
    }

    [Benchmark(Description = "FindByKey - CsSqlite")]
    public void CsSqlite_FindByKey()
    {
        for (var i = 0; i < READ_COUNT; i++)
        {
            using var command = cssqliteConnection.CreateCommand(
                "SELECT data FROM items WHERE id = @id"u8);

            command.Parameters.Add("@id"u8, readKeys[i]);
            using var reader = command.ExecuteReader();
            reader.Read();
            _ = reader.GetString(0);
        }
    }
}
