using QualityReporter.Trend;
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
    private static QualitySnapshot Snapshot(int index, string policy) => new() { Metadata = new($"c{index}", DateTimeOffset.Parse("2026-01-01Z").AddDays(index), "1.5", "2", policy), Repository = new() { Scores = new() { ["overallQuality"] = index * 5 } } };
}
