using QualityReporter.Trend;
using System.Text.Json;
using Xunit;
namespace QualityReporter.Trend.Tests;
public sealed class TrendTests
{
    private static List<TrendPoint> Points(params double[] values) => values.Select((v, i) => new TrendPoint(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i * 7), v)).ToList();
    [Fact] public void Stable_series_is_stable() => Assert.Equal(TrendStatus.Stable, TrendClassifier.Classify(Points(40, 41, 40, 41, 40)).Status);
    [Fact] public void Continuous_deterioration_is_drift() { var result = TrendClassifier.Classify(Points(31, 36, 40, 46, 52, 58)); Assert.Equal(TrendStatus.RapidlyDeteriorating, result.Status); Assert.True(result.QualityDrift); }
    [Fact] public void Improvement_is_reported() => Assert.Equal(TrendStatus.Improving, TrendClassifier.Classify(Points(75, 68, 60, 52, 44)).Status);
    [Fact] public void Single_spike_is_not_drift() => Assert.False(TrendClassifier.Classify(Points(40, 42, 65, 43, 41)).QualityDrift);
    [Fact] public void Insufficient_samples_are_reported() => Assert.Equal(TrendStatus.InsufficientData, TrendClassifier.Classify(Points(10, 20, 30)).Status);
    [Fact] public void Regression_uses_irregular_elapsed_days() { var p = new[] { new TrendPoint(DateTimeOffset.Parse("2026-01-01Z"), 10), new TrendPoint(DateTimeOffset.Parse("2026-01-11Z"), 20), new TrendPoint(DateTimeOffset.Parse("2026-02-10Z"), 50), new TrendPoint(DateTimeOffset.Parse("2026-03-02Z"), 70) }; var result = TrendClassifier.Classify(p); Assert.InRange(result.SlopePer30Days, 29.9, 30.1); Assert.Equal(1, result.RSquared, 5); }
    [Fact] public void Policy_change_breaks_series() { var report = TrendAnalyzer.Analyze(Enumerable.Range(0, 4).Select(i => Snapshot(i, "old")).Append(Snapshot(4, "new")), new()); Assert.Contains(report.Warnings, x => x.StartsWith("POLICY_CHANGED")); Assert.All(report.Trends, x => Assert.Equal(1, x.Trend.Samples)); }
    [Fact] public void Invalid_history_is_ignored() { var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, "bad.json"), "{"); var (_, warnings) = TrendHistoryReader.Read(dir); Assert.Single(warnings); Directory.Delete(dir, true); }
    [Fact] public void Trend_status_uses_schema_casing() => Assert.Contains("\"status\":\"rapidly_deteriorating\"", JsonSerializer.Serialize(new TrendResult { Status = TrendStatus.RapidlyDeteriorating }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    [Fact] public void Snapshot_reverses_coverage_so_improvement_reduces_risk()
    {
        var snapshots = new[] { 31, 38, 45, 51, 58 }.Select((coverage, index) => BuildSnapshot(coverage, index)).ToList();
        var trend = TrendAnalyzer.Analyze(snapshots, new()).Trends.Single(x => x.Metric == "coverageRisk").Trend;
        Assert.Equal(TrendStatus.Improving, trend.Status);
        Assert.False(trend.QualityDrift);
    }
    [Fact] public void Snapshot_omits_optional_aggregate_counts_when_inputs_are_unavailable()
    {
        var snapshot = BuildSnapshot(null, 0);
        Assert.Null(snapshot.Repository.Metrics["untestedComplexCount"]);
        Assert.Null(snapshot.Repository.Metrics["duplicateHotspotCount"]);
    }
    [Fact] public void Save_history_action_exports_workflow_token()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../"));
        var action = File.ReadAllText(Path.Combine(root, "actions/save-history/action.yml"));
        Assert.Contains("GITHUB_TOKEN: \"${{ github.token }}\"", action);
    }
    private static QualitySnapshot BuildSnapshot(int? coverage, int index)
    {
        var path = Path.GetTempFileName();
        try
        {
            var lineCoverage = coverage.HasValue ? coverage.Value.ToString() : "null";
            File.WriteAllText(path, "{\"files\":[{\"path\":\"sample.cs\",\"scores\":{},\"metrics\":{\"lineCoverage\":" + lineCoverage + "}}]}");
            return SnapshotBuilder.Build([path], $"c{index}", DateTimeOffset.Parse("2026-01-01Z").AddDays(index * 7), "1.5", "2", null);
        }
        finally { File.Delete(path); }
    }
    private static QualitySnapshot Snapshot(int index, string policy) => new() { Metadata = new($"c{index}", DateTimeOffset.Parse("2026-01-01Z").AddDays(index), "1.5", "2", policy), Repository = new() { Scores = new() { ["overallQuality"] = index * 5 } } };
}
