namespace QualityReporter.CSharp.Analysis;

internal static class Percentiles
{
    internal static void Set<T>(
        List<FileResult> files,
        Func<FileResult, T> value,
        Action<FileResult, double> set)
        where T : IComparable<T>
    {
        if (files.Count == 0)
        {
            return;
        }

        var sorted = files.Select(value).Order().ToList();
        var allValuesMatch = sorted.Count < 2 || sorted[0].CompareTo(sorted[^1]) == 0;

        foreach (var file in files)
        {
            var fileValue = value(file);
            var valuesBelow = sorted.Count(candidate => candidate.CompareTo(fileValue) < 0);
            var matchingValues = sorted.Count(candidate => candidate.CompareTo(fileValue) == 0);
            var percentile = allValuesMatch
                ? 0
                : (valuesBelow + (matchingValues - 1) / 2d) / (sorted.Count - 1);
            set(file, percentile);
        }
    }

    internal static double Weighted(params (double? Value, double Weight)[] terms)
    {
        var availableTerms = terms.Where(term => term.Value.HasValue && term.Weight > 0).ToList();
        return availableTerms.Count == 0
            ? 0
            : Math.Round(
                availableTerms.Sum(term => term.Value!.Value * term.Weight)
                    / availableTerms.Sum(term => term.Weight)
                    * 100,
                1);
    }
}

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

public static class BaselineComparer
{
    public static object Compare(Report current, Report old)
    {
        var currentFiles = current.Files.ToDictionary(file => file.Path);
        var previousFiles = old.Files.ToDictionary(file => file.Path);

        return new
        {
            newCritical = CurrentPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => IsCritical(currentFile) && !IsCritical(previousFile)),
            resolvedCritical = PreviousPaths(currentFiles, previousFiles,
                (previousFile, currentFile) => IsCritical(previousFile) && !IsCritical(currentFile)),
            newHotspots = CurrentPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => IsHotspot(currentFile) && !IsHotspot(previousFile)),
            resolvedHotspots = PreviousPaths(currentFiles, previousFiles,
                (previousFile, currentFile) => IsHotspot(previousFile) && !IsHotspot(currentFile)),
            riskIncreased = ChangedPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => currentFile.Risk.Score > previousFile.Risk.Score),
            riskDecreased = ChangedPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => currentFile.Risk.Score < previousFile.Risk.Score),
            priorityIncreased = ChangedPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => currentFile.Hotspot.PriorityScore > previousFile.Hotspot.PriorityScore),
            priorityDecreased = ChangedPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => currentFile.Hotspot.PriorityScore < previousFile.Hotspot.PriorityScore),
            newUntestedComplexMethods = CurrentPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => HasClassification(currentFile, "untested_complex")
                    && !HasClassification(previousFile, "untested_complex")),
            newDuplicateHotspots = CurrentPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => HasClassification(currentFile, "duplicated")
                    && currentFile.Hotspot.PriorityScore >= 80
                    && !HasClassification(previousFile, "duplicated"))
        };
    }

    private static IEnumerable<string> CurrentPaths(
        IReadOnlyDictionary<string, FileResult> currentFiles,
        IReadOnlyDictionary<string, FileResult> previousFiles,
        Func<FileResult, FileResult?, bool> predicate) =>
        currentFiles.Values
            .Where(currentFile => predicate(currentFile, Find(previousFiles, currentFile.Path)))
            .Select(file => file.Path);

    private static IEnumerable<string> PreviousPaths(
        IReadOnlyDictionary<string, FileResult> currentFiles,
        IReadOnlyDictionary<string, FileResult> previousFiles,
        Func<FileResult, FileResult?, bool> predicate) =>
        previousFiles.Values
            .Where(previousFile => predicate(previousFile, Find(currentFiles, previousFile.Path)))
            .Select(file => file.Path);

    private static IEnumerable<string> ChangedPaths(
        IReadOnlyDictionary<string, FileResult> currentFiles,
        IReadOnlyDictionary<string, FileResult> previousFiles,
        Func<FileResult, FileResult, bool> predicate) =>
        currentFiles.Values
            .Where(currentFile => previousFiles.TryGetValue(currentFile.Path, out var previousFile)
                && predicate(currentFile, previousFile))
            .Select(file => file.Path);

    private static FileResult? Find(IReadOnlyDictionary<string, FileResult> files, string path) =>
        files.TryGetValue(path, out var file) ? file : null;

    private static bool IsCritical(FileResult? file) => file?.Risk.Level == "critical";

    private static bool IsHotspot(FileResult? file) => file?.Hotspot.PriorityScore >= 60;

    private static bool HasClassification(FileResult? file, string classification) =>
        file?.Hotspot.Classifications.Contains(classification) == true;
}
