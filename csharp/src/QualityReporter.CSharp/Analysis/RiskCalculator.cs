namespace QualityReporter.CSharp.Analysis;

public static class RiskCalculator
{
    public static void Calculate(
        List<FileResult> files,
        RiskWeights weights,
        bool issuesAvailable = true)
    {
        SetAlwaysAvailablePercentiles(files);
        SetOptionalPercentiles(files, issuesAvailable);

        foreach (var file in files)
        {
            CalculateScores(file, weights);
        }
    }

    private static void SetAlwaysAvailablePercentiles(List<FileResult> files)
    {
        Percentiles.Set(files, file => file.History.ReworkRate,
            (file, percentile) => file.Risk.ReworkPercentile = percentile);
        Percentiles.Set(files, file => file.Metrics.Complexity,
            (file, percentile) => file.Risk.ComplexityPercentile = percentile);
        Percentiles.Set(files, MaximumCouplingRatio,
            (file, percentile) => file.Risk.CouplingRiskPercentile = percentile);
    }

    private static void SetOptionalPercentiles(List<FileResult> files, bool issuesAvailable)
    {
        if (issuesAvailable)
        {
            Percentiles.Set(files, IssueCount,
                (file, percentile) => file.Risk.IssuePercentile = percentile);
        }

        var filesWithCoverage = files.Where(file => file.Metrics.LineCoverage.HasValue).ToList();
        Percentiles.Set(filesWithCoverage, file => -file.Metrics.LineCoverage!.Value,
            (file, percentile) => file.Risk.CoverageRiskPercentile = percentile);

        var filesWithUntestedComplexity = files
            .Where(file => file.Metrics.UntestedComplexity is not null)
            .ToList();
        Percentiles.Set(filesWithUntestedComplexity, file => file.Metrics.UntestedComplexity!.Raw,
            (file, percentile) => file.Metrics.UntestedComplexity!.Percentile = percentile);

        var filesWithDuplication = files.Where(file => file.Metrics.Duplication is not null).ToList();
        Percentiles.Set(filesWithDuplication, DuplicationRisk, SetDuplicationPercentile);
    }

    private static double MaximumCouplingRatio(FileResult file) =>
        file.Couplings.Select(coupling => coupling.Ratio).DefaultIfEmpty().Max();

    private static int IssueCount(FileResult file) =>
        file.Metrics.Issues.Error + file.Metrics.Issues.Warning + file.Metrics.Issues.Info;

    private static double DuplicationRisk(FileResult file) =>
        DuplicationRiskCalculator.Calculate(
            file.Metrics.Duplication!.DuplicatedPercentage,
            file.Activity.EffectiveScore);

    private static void SetDuplicationPercentile(FileResult file, double percentile)
    {
        file.Metrics.Duplication!.RiskPercentile = percentile;
        file.Risk.DuplicationRiskPercentile = percentile;
    }

    private static void CalculateScores(FileResult file, RiskWeights weights)
    {
        file.Risk.MaintainabilityScore = Percentiles.Weighted(
            (file.Risk.ComplexityPercentile, weights.Complexity),
            (file.Risk.ReworkPercentile, weights.Rework),
            (file.Risk.DuplicationRiskPercentile, weights.Duplication),
            (file.Risk.CouplingRiskPercentile, weights.Coupling),
            (file.Risk.IssuePercentile, weights.Issues));

        file.Risk.TestScore = file.Risk.CoverageRiskPercentile.HasValue
            ? Percentiles.Weighted(
                (file.Risk.CoverageRiskPercentile, weights.CoverageRisk),
                (file.Metrics.UntestedComplexity?.Percentile, weights.UntestedComplexity))
            : null;
        file.Risk.ArchitectureScore = Percentiles.Weighted((file.Risk.CouplingRiskPercentile, 1));
        file.Risk.Score = Percentiles.Weighted(
            (file.Risk.MaintainabilityScore / 100, .5),
            (file.Risk.TestScore / 100, .3),
            (file.Risk.ArchitectureScore / 100, .2));
        file.Risk.Level = RiskLevel(file.Risk.Score);
    }

    private static string RiskLevel(double score) => score switch
    {
        >= 80 => "critical",
        >= 60 => "high",
        >= 40 => "medium",
        _ => "low"
    };
}
