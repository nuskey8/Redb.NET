using Redb;
using UnityEngine;

public class Sandbox : MonoBehaviour
{
    void Start()
    {
        using var db = RedbDatabase.Create("test.redb");

        using (var tx = db.BeginWrite())
        {
            using var table = tx.OpenTable<string, string>("my_table");

            table.Insert("key1", "value1");
            table.Insert("key2", "value2");

            tx.Commit();
        }

        using (var tx = db.BeginRead())
        {
            using var table = tx.OpenTable<string, string>("my_table");

            Debug.Log($"key1: {table.Get("key1")}");
            Debug.Log($"key2: {table.Get("key2")}");
        }
    }
}
