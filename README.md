# Redb.NET

redb (An embedded key-value database for Rust) bindings for .NET and Unity.

[![NuGet](https://img.shields.io/nuget/v/Redb.svg)](https://www.nuget.org/packages/Redb)
[![Releases](https://img.shields.io/github/release/nuskey8/Redb.svg)](https://github.com/nuskey8/Redb/releases)
[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

[English](./README.md) | 日本語

## Overview

Redb.NET is a high-performance C# binding for [redb](https://github.com/cberner/redb), an embedded database implemented in Rust.

While SQLite is well-known as an embedded database and RocksDB as a key-value database, redb stands out as an excellent choice for its simplicity, high performance, and support for concurrency.

Redb.NET provides a high-level binding for redb and offers an easy-to-use API for C#. The binding layer is carefully tuned for performance, ensuring no overhead.

## Installation

### NuGet packages

Redb.NET requires .NET Standard 2.1 or later. The package is available on NuGet.

### .NET CLI

```ps1
dotnet add package Redb
```

### Package Manager

```ps1
Install-Package Redb
```

### Unity

WIP

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

    writetx.Commit();
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

In `Table<TKey, TValue>`/`ReadOnlyTable<TKey, TValue>`, the keys and values are strongly typed during read and write operations.

```cs
table1.Insert("foo", 1);
int value = readOnlyTable1.Get("foo");
```

Serialization is automatically handled by the `IRedbEncoding` set in `RedbDatabase`. By default, primitive types and some other types are supported, but you can connect a custom serializer to support any type. See the [C# Serialization](#c-serialization) section for details.

In `Table`/`ReadOnlyTable`, you can directly read and write untyped binary data.

```cs
table2.Insert("foo"u8, "bar"u8);
RedbBlob blob = readOnlyTable2.Get("foo"u8);
```

`RedbBlob` is a struct that wraps a pointer on the Rust side. It allows you to read values without overhead, but you must dispose of it using `Dispose()`.

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

By default, redb database files are larger than 1MB, but compaction can reduce them to around a few dozen KB.

## C# Serialization

By default, binary (`ReadOnlySpan<byte>`), primitive types, `Guid`, `DateTime`, and `TimeSpan` can be used as keys or values. By connecting a serializer to the database, you can use any object as a key or value.

Currently, System.Text.Json is supported, and support for MessagePack for C# and MemoryPack is planned.

```cs
using Redb;
using Redb.SystemTextJson;

using var db = RedbDatabase.Create("test.redb", RedbDatabaseOptions.Default)
    .WithJsonSerializer();

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

Redb.NET is currently in preview, and some features like Savepoint and MultimapTable are not yet implemented in the C# API. These are planned to be supported in the stable release.

## License

This library is provided under the [MIT License](LICENSE).

