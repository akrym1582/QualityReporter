namespace QualityReporter.CSharp.Analysis;
public static class UntestedComplexityCalculator
{
    public static double? Calculate(int complexity, double? lineCoverage, double? branchCoverage = null, string coverageMode = "line")
    {
        var coverage = coverageMode switch
        {
            "branch" => branchCoverage,
            "combined" when lineCoverage.HasValue && branchCoverage.HasValue => lineCoverage * .6 + branchCoverage * .4,
            _ => lineCoverage
        };
        return coverage.HasValue ? Math.Round(complexity * (1 - Math.Clamp(coverage.Value / 100, 0, 1)), 2) : null;
    }
}
