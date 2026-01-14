namespace Redb;

public enum RedbBackend : int
{
    File = 0,
    InMemory = 1,
}

public record RedbDatabaseOptions
{
    public static readonly RedbDatabaseOptions Default = new();

    public nuint CacheSize { get; init; } = 64 * 1024 * 1024;
    public RedbBackend Backend { get; init; } = RedbBackend.File;

    internal redb_database_options ToNative()
    {
        return new redb_database_options
        {
            cache_size = CacheSize,
            backend = (redb_backend)(int)Backend,
        };
    }
}