namespace QualityReporter.Trend;

public static class TrendAnalyzer
{
    public static TrendReport Analyze(IEnumerable<QualitySnapshot> input, TrendOptions options, IEnumerable<string>? warnings = null)
    {
        var all = input.OrderBy(x => x.Metadata.GeneratedAt).ToList();
        var report = new TrendReport { GeneratedAt = all.LastOrDefault()?.Metadata.GeneratedAt ?? DateTimeOffset.UtcNow };
        if (warnings is not null) report.Warnings.AddRange(warnings);
        if (all.Count == 0) return report;
        var current = all[^1];
        var compatible = all.Where(x => x.Metadata.ScoreModelVersion == current.Metadata.ScoreModelVersion && x.Metadata.PolicyHash == current.Metadata.PolicyHash).TakeLast(options.WindowRuns).ToList();
        if (compatible.Count != all.TakeLast(options.WindowRuns).Count()) report.Warnings.Add("POLICY_CHANGED: incompatible policy or score model snapshots were excluded.");
        AddEntity("repository", "repository", compatible.Select(x => (x, x.Repository)), report, options);
        foreach (var entity in current.Files)
        {
            var id = entity.SymbolId ?? entity.Path!;
            var history = compatible.Select(x => (x, x.Files.FirstOrDefault(f => entity.SymbolId is null ? f.SymbolId is null && f.Path == entity.Path : f.SymbolId == entity.SymbolId))).Where(x => x.Item2 is not null).Select(x => (x.x, x.Item2!));
            AddEntity(entity.SymbolId is null ? "file" : "method", id, history, report, options);
        }
        return report;
    }
    private static void AddEntity(string scope, string id, IEnumerable<(QualitySnapshot Snapshot, SnapshotEntity Entity)> source, TrendReport report, TrendOptions options)
    {
        var rows = source.ToList();
        var keys = rows.SelectMany(x => x.Entity.Scores.Keys.Concat(x.Entity.Metrics.Keys)).Distinct();
        foreach (var key in keys)
        {
            var points = rows.Select(x => new { x.Snapshot.Metadata.GeneratedAt, Value = x.Entity.Scores.GetValueOrDefault(key) ?? x.Entity.Metrics.GetValueOrDefault(key) })
                .Where(x => x.Value.HasValue).Select(x => new TrendPoint(x.GeneratedAt, x.Value!.Value)).ToList();
            if (points.Count > 0) report.Trends.Add(new(scope, id, key, TrendClassifier.Classify(points, options.MinimumSamples)));
        }
    }
}
