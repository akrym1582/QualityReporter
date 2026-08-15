using System.Text.Json;
using System.Text.Json.Serialization;
using QualityReporter.Trend;

var json = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) } };
if (args.Length == 0) return Usage();
var options = new Dictionary<string, string>();
for (var i = 1; i + 1 < args.Length; i += 2) options[args[i]] = args[i + 1];
try
{
    if (args[0] == "snapshot")
    {
        if (!options.TryGetValue("--reports", out var reports) || !options.TryGetValue("--output", out var output) || !options.TryGetValue("--source-commit", out var commit)) return Usage();
        var snapshot = SnapshotBuilder.Build(reports.Split(',', StringSplitOptions.RemoveEmptyEntries), commit,
            options.TryGetValue("--generated-at", out var at) ? DateTimeOffset.Parse(at) : DateTimeOffset.UtcNow,
            options.GetValueOrDefault("--version", "1.5.0"), options.GetValueOrDefault("--score-model-version", "2"), options.GetValueOrDefault("--config"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!); File.WriteAllText(output, JsonSerializer.Serialize(snapshot, json)); return 0;
    }
    if (args[0] == "analyze")
    {
        if (!options.TryGetValue("--current", out var currentPath) || !options.TryGetValue("--output", out var output)) return Usage();
        var (history, warnings) = TrendHistoryReader.Read(options.GetValueOrDefault("--history", "quality-history"));
        var current = JsonSerializer.Deserialize<QualitySnapshot>(File.ReadAllText(currentPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        history.RemoveAll(x => x.Metadata.SourceCommit == current.Metadata.SourceCommit && x.Metadata.ScoreModelVersion == current.Metadata.ScoreModelVersion && x.Metadata.PolicyHash == current.Metadata.PolicyHash);
        history.Add(current);
        var report = TrendAnalyzer.Analyze(history, new() { WindowRuns = int.Parse(options.GetValueOrDefault("--window-runs", "8")), MinimumSamples = int.Parse(options.GetValueOrDefault("--minimum-samples", "4")) }, warnings);
        File.WriteAllText(output, JsonSerializer.Serialize(report, json));
        if (options.TryGetValue("--markdown", out var markdown)) File.WriteAllText(markdown, Markdown(report));
        return 0;
    }
    return Usage();
}
catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }

static int Usage() { Console.Error.WriteLine("quality-trend snapshot --reports <json[,json]> --source-commit <sha> --output <file> | analyze --current <snapshot> --history <dir> --output <file> [--markdown <file>]"); return 2; }
static string Markdown(TrendReport report)
{
    var repository = report.Trends.Where(x => x.Scope == "repository").ToList(); var drift = report.Trends.Where(x => x.Trend.QualityDrift).OrderByDescending(x => x.Trend.Change).Take(20).ToList();
    var lines = new List<string> { "# Quality Report", "", "Quality risk trends are prioritization signals, not definitive quality verdicts.", "", "## Repository trends", "", "| Signal | Current | Trend | Change / 30 days |", "|---|---:|---|---:|" };
    lines.AddRange(repository.Select(x => $"| {x.Metric} | {x.Trend.Current:F1} | {x.Trend.Status} | {x.Trend.SlopePer30Days:+0.0;-0.0;0.0} |"));
    lines.AddRange(["", "## Quality Drift Detected", ""]); lines.AddRange(drift.Count == 0 ? ["None."] : drift.Select(x => $"- **{x.Scope}: {x.EntityId} / {x.Metric}** — {x.Trend.Oldest:F1} → {x.Trend.Current:F1}; slope {x.Trend.SlopePer30Days:+0.0;-0.0;0.0} / 30 days"));
    return string.Join(Environment.NewLine, lines) + Environment.NewLine;
}
