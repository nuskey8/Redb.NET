# Redb.NET

redb (An embedded key-value database) bindings for .NET and Unity.

[![NuGet](https://img.shields.io/nuget/v/Redb.svg)](https://www.nuget.org/packages/Redb)
[![Releases](https://img.shields.io/github/release/nuskey8/Redb.NET.svg)](https://github.com/nuskey8/Redb.NET/releases)
[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE)

[English](./README.md) | 日本語

## 概要

Redb.NETはRust実装の組み込みDBである[redb](https://github.com/cberner/redb)のハイパフォーマンスなC#バインディングです。

組み込みDBとして有名なのはSQLiteや、KVデータベースとしてはLMDBやRocksDBなどが挙げられますが、redbは仕様のシンプルさや安定性、十分に高いパフォーマンス、並行処理のサポートなど、組み込みDBとして優れた選択肢の一つです。

Redb.NETはredbの高レベルなバインディングであり、C#向けの扱いやすいAPIを提供します。バインディング層のパフォーマンスも入念にチューニングされているため、パフォーマンス上のオーバーヘッドはありません。

## ベンチマーク

### Insert bulk (1000 items)

![](/docs/images/img-bench-insert.png)

### Find by integer key (10000 items, 1000 reads)

![](/docs/images/img-bench-findbykey.png)

## インストール

> [!WARNING]
> Redb.NETはネイティブライブラリを含むため、.NETとUnityで導入手順が大きく異なります。混同しないように注意してください。

### NuGet packages

Redb.NETを利用するには.NET Standard2.1以上が必要です。パッケージはNuGetから入手できます。

| パッケージ          | 説明                                                               | 最新バージョン                                                                                                         |
| ------------------- | ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------- |
| Redb                | Redb.NETのメインパッケージ。                                       | [![NuGet](https://img.shields.io/nuget/v/Redb.svg)](https://www.nuget.org/packages/Redb)                               |
| Redb.SystemTextJson | System.Text.Jsonによるシリアライズをサポートする拡張パッケージ。   | [![NuGet](https://img.shields.io/nuget/v/Redb.SystemTextJson.svg)](https://www.nuget.org/packages/Redb.SystemTextJson) |
| Redb.MessagePack    | MessagePack for C#によるシリアライズをサポートする拡張パッケージ。 | [![NuGet](https://img.shields.io/nuget/v/Redb.MessagePack.svg)](https://www.nuget.org/packages/Redb.MessagePack)       |

### Unity

Unityの場合、拡張パッケージを含むすべてのパッケージはPackage Managerからのインストールが可能です。

1. Window > Package ManagerからPackage Managerを開く
2. 「+」ボタン > Add package from git URL
3. 対応するパッケージのURLを入力する

| パッケージ          | URL                                                                                     |
| ------------------- | --------------------------------------------------------------------------------------- |
| Redb                | https://github.com/nuskey8/Redb.NET.git?path=src/Redb.Unity/Assets/Redb`                |
| Redb.SystemTextJson | https://github.com/nuskey8/Redb.NET.git?path=src/Redb.Unity/Assets/Redb.SystemTextJson` |
| Redb.MessagePack    | https://github.com/nuskey8/Redb.NET.git?path=src/Redb.Unity/Assets/Redb.MessagePack`    |

拡張パッケージを利用するには、それぞれ別に`System.Text.Json`や`MessagePack`の導入が必要です。これらは[NugetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)などで追加できます。

> [!WARNING]
> バインディングの実装が異なるため、Redb.NETのNuGet版をUnityで利用することはできません。必ず上の方法で導入を行なってください。

## プラットフォーム

Redb.NETは以下のプラットフォームに対応しています。

| プラットフォーム | アーキテクチャ         | サポート    | 備考        |
| ---------------- | ---------------------- | ----------- | ----------- |
| Windows          | x64                    | ✅           |             |
|                  | arm64                  | ✅           |             |
| macOS            | x64                    | ✅           |             |
|                  | arm64  (Apple Silicon) | ✅           |             |
| Linux            | x64                    | ✅           |             |
|                  | arm64                  | ✅           |             |
| iOS              | arm64                  | ✅  (未検証) | (Unityのみ) |
|                  | x64                    | ✅  (未検証) | (Unityのみ) |
| Android          | arm64                  | ✅  (未検証) | (Unityのみ) |
|                  | armv7                  | ✅  (未検証) | (Unityのみ) |
|                  | x86_64                 | ✅  (未検証) | (Unityのみ) |

## クイックスタート

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

`OpenTable()`で`Table`/`ReadOnlyTable`を開くことができます。これにはそれぞれ、ジェネリックな型とそうでない型が存在します。

```cs
// generic table
using var table1 = tx.OpenTable<string, int>("my_table");

// non-generic table
using var table2 = tx.OpenTable("my_table");
```

`Table<TKey, TValue>`/`ReadOnlyTable<TKey, TValue>`では、値の書き込み・読み取り時のキーと値は型付けされています。

```cs
table1.Insert("foo", 1);
int value = readOnlyTable1.Get("foo");
```

これは`RedbDatabase`にセットされた`IRedbEncoding`によって自動でシリアライズされます。デフォルトではプリミティブ型とその他いくつかの型に対応していますが、シリアライザを接続することで任意の型に対応できます。詳細は[C# Serialization](#c-serialization)の項目を参照してください

`Table`/`ReadOnlyTable`では、型付けされていないバイナリを直接読み書きすることができます。

```cs
table2.Insert("foo"u8, "bar"u8);
RedbBlob blob = readOnlyTable2.Get("foo"u8);
```

`RedbBlob`はRust側のポインタをラップした構造体です。これを介してオーバーヘッドなしで値を読み取ることができますが、必ず`Dispose()`で破棄する必要があります。

```cs
var span = blob.AsSpan();
span.CopyTo(buffer);
blob.Dispose();
```

## Compaction

`Compact()`を呼び出すことでCompactionを実行できます。

```cs
db.Compact();
```

redbのDBファイルはデフォルトでは1MB以上のサイズがありますが、Compactionによってこれを数十KB程度まで削減することができます。

## C# Serialization

デフォルトではバイナリ(`ReadOnlySpan<byte>`)とプリミティブ型、`Guid`、`DateTime`、`TimeSpan`をキーや値として利用できますが、DBにシリアライザを接続することで任意のオブジェクトをキーや値として利用することが可能になります。

現在はSystem.Text.JsonとMessagePack for C#に対応しています。

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

## サポートされる機能

Redb.NETは現在プレビューであり、MultimapTableなどの機能に対応するC# APIが実装されていません。これは正式版までにサポートされる予定です。

## ライセンス

このライブラリは[MITライセンス](LICENSE)の下で提供されています。