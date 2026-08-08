// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Human Centric Works, Hospet

namespace AormsConnect.Services;

/// <summary>
/// Shared project catalog under LocalAppData\AORMS-Connect\catalog.json.
/// Sibling apps will read this in C2 — keep id/ref/title stable.
/// </summary>
public sealed class ProjectCatalogStore
{
    readonly string _path;

    public ProjectCatalogStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AORMS-Connect");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "catalog.json");
    }

    public IReadOnlyList<CatalogProject> List()
    {
        if (!File.Exists(_path)) return Array.Empty<CatalogProject>();
        try
        {
            var json = File.ReadAllText(_path);
            var rows = System.Text.Json.JsonSerializer.Deserialize<List<CatalogProject>>(json);
            return rows ?? new List<CatalogProject>();
        }
        catch
        {
            return Array.Empty<CatalogProject>();
        }
    }

    public CatalogProject Add(string title)
    {
        var rows = List().ToList();
        var id = Guid.NewGuid().ToString("D");
        var n = rows.Count + 1;
        var row = new CatalogProject
        {
            Id = id,
            Ref = $"PRJ-{n:D4}",
            Title = title.Trim(),
            Status = "ACTIVE",
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O"),
        };
        rows.Add(row);
        Save(rows);
        return row;
    }

    void Save(List<CatalogProject> rows)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            rows,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}

public sealed class CatalogProject
{
    public string Id { get; set; } = "";
    public string Ref { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "ACTIVE";
    public string UpdatedAt { get; set; } = "";
}
