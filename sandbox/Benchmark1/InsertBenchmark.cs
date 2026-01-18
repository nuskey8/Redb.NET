using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using BenchmarkDotNet.Attributes;
using CsSqlite;
using LiteDB;
using Redb;

[Config(typeof(BenchmarkConfig))]
public class InsertBenchmark
{
    const int N = 10000;

    Item[] items = Enumerable.Range(0, N)
        .Select(i => new Item { Id = i + 1, Data = $"val{i:D10}" })
        .ToArray();

    DirectoryInfo directory;
    string sqlitePath;
    string redbPath;
    string litedbPath;
    SqliteConnection cssqliteConnection;
    RedbDatabase redbDatabase;
    LiteDatabase liteDatabase;
    ILiteCollection<Item> liteCollection;

    [GlobalSetup]
    public void Setup()
    {
        directory = Directory.CreateTempSubdirectory("benchmarks");
        sqlitePath = Path.Combine(directory.FullName, "bench.sqlite");
        redbPath = Path.Combine(directory.FullName, "bench.redb");
        litedbPath = Path.Combine(directory.FullName, "bench.litedb");

        // Setup Redb
        redbDatabase = RedbDatabase.Create(redbPath);
        using var tx = redbDatabase.BeginWrite();
        tx.OpenTable("items");
        tx.Commit();

        // Setup LiteDB
        liteDatabase = new LiteDatabase(litedbPath);
        liteCollection = liteDatabase.GetCollection<Item>("items");
        liteCollection.EnsureIndex(x => x.Id);

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

    [IterationSetup]
    public void IterationSetup()
    {
        liteCollection.DeleteAll();
        liteDatabase.Commit();

        cssqliteConnection.ExecuteNonQuery("DELETE FROM items;");

        using var tx = redbDatabase.BeginWrite();
        tx.DeleteTable("items");
        tx.Commit();
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
        var table = tx.OpenTable("items");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AddItem(Table table, int id)
        {
            Span<byte> keySpan = stackalloc byte[sizeof(int)];
            MemoryMarshal.Write(keySpan, id);
            Span<byte> dataSpan = stackalloc byte[20];
            Utf8.TryWrite(dataSpan, $"val{id - 1:D10}", out var bytesWritten);
            var data = dataSpan[..bytesWritten];
            table.Insert(keySpan, data);
        }

        for (var i = 0; i < N; i++)
        {
            AddItem(table, i + 1);
        }

        tx.Commit();
    }

    [Benchmark(Description = "Insert - LiteDB")]
    public void LiteDB_Insert()
    {
        liteCollection.InsertBulk(items, items.Length);
        liteDatabase.Commit();
    }

    [Benchmark(Description = "Insert - CsSqlite")]
    public void CsSqlite_Insert()
    {
        cssqliteConnection.ExecuteNonQuery("BEGIN TRANSACTION;"u8);
        for (var i = 0; i < N; i++)
        {
            cssqliteConnection.ExecuteNonQuery(
                $"""
                 INSERT INTO items (id, data) VALUES ({i + 1}, 'val{i:D10}');
                 """);
        }
        cssqliteConnection.ExecuteNonQuery("COMMIT;"u8);
    }
}