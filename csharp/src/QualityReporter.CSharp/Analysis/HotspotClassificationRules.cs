namespace QualityReporter.CSharp.Analysis;

internal static class HotspotClassificationRules
{
    internal static bool IsComplex(FileResult file, Config config) =>
        file.Risk.ComplexityPercentile >= config.Classification.HighComplexityPercentile
        || file.Functions.Any(function => function.Complexity >= config.Functions.ComplexityWarning);

    internal static bool HasRework(FileResult file, Config config) =>
        file.Risk.ReworkPercentile >= config.Classification.HighReworkPercentile
        || file.History.ReworkRate >= .5;

    internal static bool IsUntested(
        FileResult file,
        Config config,
        bool isComplex,
        bool hasRework) =>
        file.Metrics.LineCoverage < config.Classification.LowCoverage
        && (file.Activity.EffectiveScore >= config.Classification.HighActivity || isComplex || hasRework);

    internal static bool IsCoupled(FileResult file, Config config) =>
        file.Couplings.Any(coupling =>
            coupling.Count >= config.Coupling.MinCount && coupling.Ratio >= config.Coupling.MinRatio);

    internal static bool IsUntestedComplex(FileResult file, Config config) =>
        file.Metrics.UntestedComplexity is { } untestedComplexity
        && ((file.Metrics.Complexity >= config.UntestedComplexity.ComplexityWarning
                && file.Metrics.LineCoverage / 100 < config.UntestedComplexity.LowCoverage)
            || untestedComplexity.Percentile >= .8);

    internal static bool IsDuplicated(FileResult file, Config config) =>
        file.Metrics.Duplication is { } duplication
        && (duplication.DuplicatedPercentage >= config.Duplication.WarningPercentage
            || duplication.RiskPercentile >= .8);

    internal static bool IsNewAndActive(FileResult file) =>
        file.History.FileAgeDays <= 30 && file.Activity.Score >= 70 && file.Risk.Score < 60;

    internal static bool IsActive(FileResult file) =>
        file.Activity.EffectiveScore >= 70 && file.Risk.Score < 40;
}
