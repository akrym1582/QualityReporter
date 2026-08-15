namespace QualityReporter.Trend;

public static class LinearTrendCalculator
{
    public static (double SlopePer30Days, double RSquared) Calculate(IReadOnlyList<TrendPoint> points)
    {
        if (points.Count < 2) return (0, 0);
        var origin = points[0].At;
        var xs = points.Select(p => (p.At - origin).TotalDays).ToArray();
        var ys = points.Select(p => p.Value).ToArray();
        var xMean = xs.Average(); var yMean = ys.Average();
        var denominator = xs.Sum(x => Math.Pow(x - xMean, 2));
        if (denominator == 0) return (0, 0);
        var slope = xs.Zip(ys).Sum(p => (p.First - xMean) * (p.Second - yMean)) / denominator;
        var intercept = yMean - slope * xMean;
        var total = ys.Sum(y => Math.Pow(y - yMean, 2));
        var residual = xs.Zip(ys).Sum(p => Math.Pow(p.Second - (slope * p.First + intercept), 2));
        return (slope * 30, total == 0 ? 1 : Math.Max(0, 1 - residual / total));
    }
}
