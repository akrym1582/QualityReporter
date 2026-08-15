using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace QualityReporter.Trend;

public static class SnapshotBuilder
{
    private static readonly string[] ScoreNames = ["activity", "maintainability", "testability", "architecture", "overallQuality", "hotspotPriority"];
    public static QualitySnapshot Build(IEnumerable<string> reports, string sourceCommit, DateTimeOffset generatedAt, string version, string modelVersion, string? policyPath)
    {
        var files = new List<SnapshotEntity>();
        foreach (var path in reports)
        {
            var root = JsonNode.Parse(File.ReadAllText(path))!;
            foreach (var file in root["files"]?.AsArray() ?? [])
            {
                if (file is null) continue;
                files.Add(Entity(file, file["path"]?.GetValue<string>()));
                foreach (var symbol in file["symbols"]?.AsArray() ?? []) if (symbol is not null)
                    files.Add(Entity(symbol, file["path"]?.GetValue<string>(), symbol["symbolId"]?.GetValue<string>(), symbol["name"]?.GetValue<string>()));
            }
        }
        var actualFiles = files.Where(x => x.SymbolId is null).ToList();
        double? Median(string key) { var v = actualFiles.Select(x => x.Scores.GetValueOrDefault(key)).OfType<double>().Order().ToArray(); return v.Length == 0 ? null : v[v.Length / 2]; }
        double? P90(string key) { var v = actualFiles.Select(x => x.Scores.GetValueOrDefault(key)).OfType<double>().Order().ToArray(); return v.Length == 0 ? null : v[(int)Math.Ceiling((v.Length - 1) * .9)]; }
        double? CountWhenAvailable(string key, Func<double, bool> predicate)
        {
            var values = actualFiles.Select(x => x.Metrics.GetValueOrDefault(key)).OfType<double>().ToList();
            return values.Count == 0 ? null : values.Count(predicate);
        }
        var repository = new SnapshotEntity { Scores = ScoreNames.ToDictionary(x => x, Median), Metrics = new()
        {
            ["overallQualityP90"] = P90("overallQuality"), ["testabilityP90"] = P90("testability"),
            ["criticalCount"] = actualFiles.Count(x => x.Scores.GetValueOrDefault("overallQuality") >= 80),
            ["hotspotCount"] = actualFiles.Count(x => x.Scores.GetValueOrDefault("hotspotPriority") >= 60),
            ["untestedComplexCount"] = CountWhenAvailable("untestedComplexity", value => value >= 15),
            ["duplicateHotspotCount"] = CountWhenAvailable("duplicatedPercentage", value => value >= .2)
        }};
        return new() { Metadata = new(sourceCommit, generatedAt, version, modelVersion, HashPolicy(policyPath)), Repository = repository, Files = files };
    }
    private static SnapshotEntity Entity(JsonNode node, string? path, string? symbolId = null, string? name = null) => new()
    {
        Path = path, SymbolId = symbolId, Name = name,
        Scores = ScoreNames.ToDictionary(x => x, x => node["scores"]?[x]?["score"]?.GetValue<double?>()),
        Metrics = new() { ["complexity"] = Number(node["metrics"]?["complexity"]), ["coverageRisk"] = CoverageRisk(node["metrics"]?["lineCoverage"]),
            ["reworkRate"] = Number(node["history"]?["reworkRate"]), ["duplicatedPercentage"] = Number(node["metrics"]?["duplication"]?["duplicatedPercentage"]),
            ["untestedComplexity"] = Number(node["metrics"]?["untestedComplexity"]?["raw"]) }
    };
    private static double? Number(JsonNode? n) => n?.GetValue<double?>();
    private static double? Percent(JsonNode? n) { var value = Number(n); return value is <= 1 ? value * 100 : value; }
    private static double? CoverageRisk(JsonNode? n) { var coverage = Percent(n); return coverage.HasValue ? 100 - coverage.Value : null; }
    public static string HashPolicy(string? path) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path is not null && File.Exists(path) ? File.ReadAllText(path) : "default"))).ToLowerInvariant();
}
