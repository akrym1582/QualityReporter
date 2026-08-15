using System.Text.Json.Serialization;

namespace QualityReporter.Trend;

public sealed record SnapshotMetadata(string SourceCommit, DateTimeOffset GeneratedAt, string QualityReporterVersion, string ScoreModelVersion, string PolicyHash);
public sealed class SnapshotEntity
{
    public string? Path { get; init; }
    public string? SymbolId { get; init; }
    public string? Name { get; init; }
    public Dictionary<string, double?> Scores { get; init; } = [];
    public Dictionary<string, double?> Metrics { get; init; } = [];
}
public sealed class QualitySnapshot
{
    public int SchemaVersion { get; init; } = 1;
    public required SnapshotMetadata Metadata { get; init; }
    public SnapshotEntity Repository { get; init; } = new();
    public List<SnapshotEntity> Files { get; init; } = [];
}
public enum TrendStatus { Improving, Stable, Deteriorating, RapidlyDeteriorating, InsufficientData, PolicyChanged }
public sealed record TrendPoint(DateTimeOffset At, double Value);
public sealed class TrendResult
{
    [JsonConverter(typeof(JsonStringEnumConverter<TrendStatus>))]
    public TrendStatus Status { get; init; }
    public int Samples { get; init; }
    public double Current { get; init; }
    public double Oldest { get; init; }
    public double Change { get; init; }
    public double SlopePer30Days { get; init; }
    public double RSquared { get; init; }
    public int ConsecutiveWorsening { get; init; }
    public bool QualityDrift { get; init; }
}
public sealed record NamedTrend(string Scope, string EntityId, string Metric, TrendResult Trend);
public sealed class TrendReport
{
    public int SchemaVersion { get; init; } = 1;
    public required DateTimeOffset GeneratedAt { get; init; }
    public List<string> Warnings { get; init; } = [];
    public List<NamedTrend> Trends { get; init; } = [];
}
public sealed class TrendOptions { public int WindowRuns { get; init; } = 8; public int MinimumSamples { get; init; } = 4; }
