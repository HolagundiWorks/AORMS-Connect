// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Human Centric Works, Hospet

using Aorms.Bridge;

namespace AormsConnect.Services;

/// <summary>
/// Writes Connect-owned catalog.json (same path sibling apps read via ConnectCatalog).
/// </summary>
public sealed class ProjectCatalogStore
{
    readonly string _path;

    public ProjectCatalogStore()
    {
        _path = ConnectCatalog.DefaultPath();
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    public IReadOnlyList<CatalogProject> List() => ConnectCatalog.List(_path);

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
        var json = System.Text.Json.JsonSerializer.Serialize(
            rows,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
        return row;
    }
}
