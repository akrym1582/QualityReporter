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
