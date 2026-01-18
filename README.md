# Redb.NET

redb (An embedded key-value database) bindings for .NET and Unity.

[![NuGet](https://img.shields.io/nuget/v/Redb.svg)](https://www.nuget.org/packages/Redb)
[![Releases](https://img.shields.io/github/release/nuskey8/Redb.NET.svg)](https://github.com/nuskey8/Redb.NET/releases)
[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

English | [日本語](README.md)

## Overview

Redb.NET is a high-performance C# binding for [redb](https://github.com/cberner/redb), an embedded database implemented in Rust.

While SQLite is well-known as an embedded database, and LMDB or RocksDB as key-value databases, redb stands out as an excellent choice for its simplicity, stability, high performance, and support for concurrency.

Redb.NET provides a high-level binding for redb, offering an easy-to-use API for C#. The binding layer is carefully tuned for performance, ensuring no overhead.

## Installation

> [!WARNING]
> Redb.NET includes native libraries, so the installation process differs significantly between .NET and Unity. Be careful not to confuse the two.

### NuGet packages

Redb.NET requires .NET Standard 2.1 or higher. Packages are available on NuGet.

| Package             | Description                                                         | Latest Version                                                                                                         |
| ------------------- | ------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Redb                | The main package for Redb.NET.                                      | [![NuGet](https://img.shields.io/nuget/v/Redb.svg)](https://www.nuget.org/packages/Redb)                               |
| Redb.SystemTextJson | Extension package supporting serialization with System.Text.Json.   | [![NuGet](https://img.shields.io/nuget/v/Redb.SystemTextJson.svg)](https://www.nuget.org/packages/Redb.SystemTextJson) |
| Redb.MessagePack    | Extension package supporting serialization with MessagePack for C#. | [![NuGet](https://img.shields.io/nuget/v/Redb.MessagePack.svg)](https://www.nuget.org/packages/Redb.MessagePack)       |

### Unity

For Unity, all packages, including extension packages, can be installed via the Package Manager.

1. Open the Package Manager from Window > Package Manager.
2. Click the "+" button > Add package from git URL.
3. Enter the URL for the corresponding package.

| Package             | URL                                                                                     |
| ------------------- | --------------------------------------------------------------------------------------- |
| Redb                | https://github.com/nuskey8/Redb.NET.git?path=src/Redb.Unity/Assets/Redb`                |
| Redb.SystemTextJson | https://github.com/nuskey8/Redb.NET.git?path=src/Redb.Unity/Assets/Redb.SystemTextJson` |
| Redb.MessagePack    | https://github.com/nuskey8/Redb.NET.git?path=src/Redb.Unity/Assets/Redb.MessagePack`    |

To use the extension packages, you need to separately add `System.Text.Json` or `MessagePack` using tools like [NugetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).

> [!WARNING]
> Due to differences in the binding implementation, the NuGet version of Redb.NET cannot be used in Unity. Always install it using the above method.

## Platforms

Redb.NET supports the following platforms:

| Platform | Architecture          | Supported    | Notes        |
| -------- | --------------------- | ------------ | ------------ |
| Windows  | x64                   | ✅            |              |
|          | arm64                 | ✅            |              |
| macOS    | x64                   | ✅            |              |
|          | arm64 (Apple Silicon) | ✅            |              |
| Linux    | x64                   | ✅            |              |
|          | arm64                 | ✅            |              |
| iOS      | arm64                 | ✅ (untested) | (Unity only) |
|          | x64                   | ✅ (untested) | (Unity only) |
| Android  | arm64                 | ✅ (untested) | (Unity only) |
|          | armv7                 | ✅ (untested) | (Unity only) |
|          | x86_64                | ✅ (untested) | (Unity only) |

## Quick Start

```cs
using Redb;

using var db = RedbDatabase.Create("test.redb", RedbDatabaseOptions.Default);

using (var tx = db.BeginWrite())
{
    using (var table = tx.OpenTable<string, int>("my_table"))
    {
        table.Insert("foo", 12);
        table.Insert("bar", 30);
    }

    tx.Commit();
}

using (var tx = db.BeginRead())
{
    using (var table = tx.OpenTable<string, int>("my_table"))
    {
        var foo = table.Get("foo");
        Console.WriteLine($"foo: {foo}");

        if (table.TryGet("bar", out var bar))
        {
            Console.WriteLine($"bar: {bar}");
        }
    }
}
```

## Table API

You can open `Table`/`ReadOnlyTable` using `OpenTable()`. Both generic and non-generic types are available.

```cs
// generic table
using var table1 = tx.OpenTable<string, int>("my_table");

// non-generic table
using var table2 = tx.OpenTable("my_table");
```

In `Table<TKey, TValue>`/`ReadOnlyTable<TKey, TValue>`, the keys and values are strongly typed when reading and writing values.

```cs
table1.Insert("foo", 1);
int value = readOnlyTable1.Get("foo");
```

This is automatically serialized by the `IRedbEncoding` set in `RedbDatabase`. By default, primitive types and some other types are supported, but you can support any type by connecting a serializer. See the [C# Serialization](#c-serialization) section for details.

In `Table`/`ReadOnlyTable`, you can directly read and write untyped binary data.

```cs
table2.Insert("foo"u8, "bar"u8);
RedbBlob blob = readOnlyTable2.Get("foo"u8);
```

`RedbBlob` is a struct that wraps a pointer on the Rust side. You can read values without overhead through this, but you must dispose of it with `Dispose()`.

```cs
var span = blob.AsSpan();
span.CopyTo(buffer);
blob.Dispose();
```

## Compaction

You can perform compaction by calling `Compact()`.

```cs
db.Compact();
```

The default size of a redb database file is over 1MB, but compaction can reduce it to a few dozen KB.

## C# Serialization

By default, binary (`ReadOnlySpan<byte>`), primitive types, `Guid`, `DateTime`, and `TimeSpan` can be used as keys or values. By connecting a serializer to the database, you can use any object as a key or value.

Currently, System.Text.Json and MessagePack for C# are supported.

```cs
using Redb;
using Redb.SystemTextJson;
// using Redb.MessagePack;

using var db = RedbDatabase.Create("test.redb", RedbDatabaseOptions.Default)
    .WithJsonSerializer(); // or .WithMessagePackSerializer();

using (var tx = db.BeginWrite())
{
    using (var table = tx.OpenTable<string, Person>("persons"))
    {
        table.Insert("alice", new Person("Alice", 18));
        table.Insert("bob", new Person("Bob", 30));
    }

    tx.Commit();
}

using (var tx = db.BeginRead())
{
    using (var table = tx.OpenTable<string, Person>("persons"))
    {
        var alice = table.Get("alice");
        var bob = table.Get("bob");
        Console.WriteLine(alice);
        Console.WriteLine(bob);
    }
}

record Person(string Name, int Age);
```

## Supported Features

Redb.NET is currently in preview, and some features like MultimapTable are not yet implemented in the C# API. These are planned to be supported in the stable release.

## License

This library is provided under the [MIT License](LICENSE).