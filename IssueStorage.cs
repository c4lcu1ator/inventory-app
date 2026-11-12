using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace InventoryApp;

/// <summary>
/// Сохранение и загрузка записей в локальный файл issues.json.
/// </summary>
public static class IssueStorage
{
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "issues.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static List<Issue> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new List<Issue>();
            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<Issue>>(json, Options) ?? new List<Issue>();
        }
        catch
        {
            // битый файл не должен ронять программу
            return new List<Issue>();
        }
    }

    public static void Save(List<Issue> issues)
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(issues, Options));
    }
}
