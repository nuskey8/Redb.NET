using BenchmarkDotNet.Attributes;
using CsSqlite;
using LiteDB;
using Redb;

[Config(typeof(BenchmarkConfig))]
public class InsertBenchmark
{
    const int N = 10000;

    DirectoryInfo directory;
    SqliteConnection cssqliteConnection;
    RedbDatabase redbDatabase;
    LiteDatabase liteDatabase;

    [GlobalSetup]
    public void Setup()
    {
        directory = Directory.CreateTempSubdirectory("benchmarks");
        var sqlitePath = Path.Combine(directory.FullName, "bench.sqlite");
        var redbPath = Path.Combine(directory.FullName, "bench.redb");
        var litedbPath = Path.Combine(directory.FullName, "bench.litedb");

        // Setup Redb
        redbDatabase = RedbDatabase.Create(redbPath);

        // Setup LiteDB
        liteDatabase = new LiteDatabase(litedbPath);

        // Setup sqlite
        cssqliteConnection = new SqliteConnection(sqlitePath);
        cssqliteConnection.Open();
        cssqliteConnection.ExecuteNonQuery("DROP TABLE IF EXISTS items;");
        cssqliteConnection.ExecuteNonQuery(
            """
            CREATE TABLE IF NOT EXISTS items (
                id INTEGER NOT NULL PRIMARY KEY,
                data TEXT NOT NULL
            );
            """);
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

    [Benchmark(Description = "Insert - Redb.NET")]
    public void Redb_Insert()
    {
        using var tx = redbDatabase.BeginWrite();
        var table = tx.OpenTable<int, string>("items");

        for (var i = 0; i < N; i++)
        {
            table.Insert(i, $"val{i:D10}");
        }

        tx.Commit();
    }

    [Benchmark(Description = "Insert - LiteDB")]
    public void LiteDB_Insert()
    {
        var liteCollection = liteDatabase.GetCollection<Item>("items");
        liteCollection.InsertBulk(Enumerable.Range(0, N).Select(i => new Item { Data = $"val{i:D10}" }));
        liteDatabase.Commit();
    }

    [Benchmark(Description = "Insert - CsSqlite")]
    public void CsSqlite_Insert()
    {
        cssqliteConnection.ExecuteNonQuery("BEGIN TRANSACTION;");
        for (var i = 0; i < N; i++)
        {
            cssqliteConnection.ExecuteNonQuery(
                $"""
                 INSERT INTO items (id, data) VALUES ({i}, 'val{i:D10}');
                 """);
        }
        cssqliteConnection.ExecuteNonQuery("COMMIT;");
    }
}