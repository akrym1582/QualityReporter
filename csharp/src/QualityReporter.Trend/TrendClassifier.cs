namespace QualityReporter.Trend;

public static class TrendClassifier
{
    public static TrendResult Classify(IReadOnlyList<TrendPoint> points, int minimumSamples = 4)
    {
        var ordered = points.OrderBy(p => p.At).ToArray();
        if (ordered.Length == 0) return new() { Status = TrendStatus.InsufficientData };
        var change = ordered[^1].Value - ordered[0].Value;
        var (slope, r2) = LinearTrendCalculator.Calculate(ordered);
        var consecutive = 0;
        for (var i = ordered.Length - 1; i > 0 && ordered[i].Value > ordered[i - 1].Value; i--) consecutive++;
        var status = TrendStatus.Stable;
        if (ordered.Length < minimumSamples) status = TrendStatus.InsufficientData;
        else if (slope >= 8 && change >= 10 && r2 >= .45) status = TrendStatus.RapidlyDeteriorating;
        else if (slope >= 3 && change >= 5 && r2 >= .35) status = TrendStatus.Deteriorating;
        else if (slope <= -8 && change <= -10 && r2 >= .45) status = TrendStatus.Improving;
        else if (slope <= -3 && change <= -5 && r2 >= .35) status = TrendStatus.Improving;
        var maxStep = ordered.Zip(ordered.Skip(1)).Select(p => Math.Max(0, p.Second.Value - p.First.Value)).DefaultIfEmpty().Max();
        var spikeDominated = change > 0 && maxStep / change >= .7;
        var drift = ordered.Length >= 5 && change >= 10 && !spikeDominated && consecutive >= 2 &&
                    status is TrendStatus.Deteriorating or TrendStatus.RapidlyDeteriorating;
        return new() { Status = status, Samples = ordered.Length, Oldest = ordered[0].Value, Current = ordered[^1].Value,
            Change = change, SlopePer30Days = slope, RSquared = r2, ConsecutiveWorsening = consecutive, QualityDrift = drift };
    }
}
