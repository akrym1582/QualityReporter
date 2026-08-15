namespace QualityReporter.CSharp.Analysis;

public static class HotspotClassifier
{
    private static readonly string[] ClassificationOrder =
    [
        "critical",
        "untested_complex",
        "duplicated",
        "rework",
        "complex",
        "coupled",
        "untested",
        "new_active",
        "active"
    ];

    private static readonly HashSet<string> CriticalSignals =
    [
        "complex",
        "rework",
        "untested_complex",
        "coupled",
        "duplicated"
    ];

    public static void Classify(IEnumerable<FileResult> files, Config config)
    {
        foreach (var file in files)
        {
            Classify(file, config);
        }
    }

    private static void Classify(FileResult file, Config config)
    {
        var classifications = file.Hotspot.Classifications;
        classifications.Clear();

        var isComplex = IsComplex(file, config);
        var hasRework = HasRework(file, config);

        AddIf(classifications, "complex", isComplex);
        AddIf(classifications, "rework", hasRework);
        AddIf(classifications, "untested_complex", IsUntestedComplex(file, config));
        AddIf(classifications, "duplicated", IsDuplicated(file, config));
        AddIf(classifications, "untested", IsUntested(file, config, isComplex, hasRework));
        AddIf(classifications, "coupled", IsCoupled(file, config));
        AddIf(classifications, "new_active", IsNewAndActive(file));
        AddIf(classifications, "active", IsActive(file));
        AddIf(classifications, "critical", IsCritical(file, config, classifications));

        file.Hotspot.PrimaryClassification = ClassificationOrder.FirstOrDefault(classifications.Contains);
    }

    private static bool IsComplex(FileResult file, Config config) =>
        file.Risk.ComplexityPercentile >= config.Classification.HighComplexityPercentile
        || file.Functions.Any(function => function.Complexity >= config.Functions.ComplexityWarning);

    private static bool HasRework(FileResult file, Config config) =>
        file.Risk.ReworkPercentile >= config.Classification.HighReworkPercentile
        || file.History.ReworkRate >= .5;

    private static bool IsUntested(
        FileResult file,
        Config config,
        bool isComplex,
        bool hasRework) =>
        file.Metrics.LineCoverage < config.Classification.LowCoverage
        && (file.Activity.EffectiveScore >= config.Classification.HighActivity || isComplex || hasRework);

    private static bool IsCoupled(FileResult file, Config config) =>
        file.Couplings.Any(coupling =>
            coupling.Count >= config.Coupling.MinCount && coupling.Ratio >= config.Coupling.MinRatio);

    private static bool IsUntestedComplex(FileResult file, Config config) =>
        file.Metrics.UntestedComplexity is { } untestedComplexity
        && ((file.Metrics.Complexity >= config.UntestedComplexity.ComplexityWarning
                && file.Metrics.LineCoverage / 100 < config.UntestedComplexity.LowCoverage)
            || untestedComplexity.Percentile >= .8);

    private static bool IsDuplicated(FileResult file, Config config) =>
        file.Metrics.Duplication is { } duplication
        && (duplication.DuplicatedPercentage >= config.Duplication.WarningPercentage
            || duplication.RiskPercentile >= .8);

    private static bool IsNewAndActive(FileResult file) =>
        file.History.FileAgeDays <= 30 && file.Activity.Score >= 70 && file.Risk.Score < 60;

    private static bool IsActive(FileResult file) =>
        file.Activity.EffectiveScore >= 70 && file.Risk.Score < 40;

    private static bool IsCritical(FileResult file, Config config, IEnumerable<string> classifications) =>
        file.Hotspot.PriorityScore >= config.Classification.CriticalPriority
        && classifications.Count(CriticalSignals.Contains) >= 2;

    private static void AddIf(ICollection<string> classifications, string classification, bool condition)
    {
        if (condition)
        {
            classifications.Add(classification);
        }
    }
}
