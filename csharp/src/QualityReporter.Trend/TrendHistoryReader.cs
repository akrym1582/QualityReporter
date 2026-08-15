using System.Text.Json;

namespace QualityReporter.Trend;

public static class TrendHistoryReader
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    public static (List<QualitySnapshot> Snapshots, List<string> Warnings) Read(string directory)
    {
        var snapshots = new List<QualitySnapshot>(); var warnings = new List<string>();
        if (!Directory.Exists(directory)) return (snapshots, ["History is not available; this is the first snapshot."]);
        foreach (var file in Directory.EnumerateFiles(directory, "*.json").Order())
            try { var value = JsonSerializer.Deserialize<QualitySnapshot>(File.ReadAllText(file), Json); if (value is not null) snapshots.Add(value); else warnings.Add($"Invalid snapshot ignored: {file}"); }
            catch (Exception) { warnings.Add($"Invalid snapshot ignored: {file}"); }
        return (snapshots.OrderBy(x => x.Metadata.GeneratedAt).ToList(), warnings);
    }
}
