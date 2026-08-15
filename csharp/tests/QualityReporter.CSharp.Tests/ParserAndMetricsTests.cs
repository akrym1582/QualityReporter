using QualityReporter.CSharp;
using QualityReporter.CSharp.Coverage;
using QualityReporter.CSharp.Metrics;
using Xunit;

public sealed class ParserAndMetricsTests
{
    [Fact]
    public void CoberturaParser_aggregates_files_and_branch_coverage()
    {
        using var temp = new TempFile("""
            <coverage><packages><package><classes>
              <class filename="src/A.cs"><lines>
                <line number="1" hits="1" branch="true" condition-coverage="50% (1/2)" />
                <line number="2" hits="0" />
              </lines></class>
              <class filename="src/A.cs"><lines><line number="3" hits="1" branch="true" condition-coverage="100% (2/2)" /></lines></class>
            </classes></package></packages></coverage>
            """);

        var result = CoberturaParser.Parse(temp.Path)["src/A.cs"];

        Assert.Equal(66.67, result.Line, 2);
        Assert.Equal(75, result.Branch);
    }

    [Fact]
    public void AnalyzerParser_reads_supported_severities_and_ignores_other_lines()
    {
        using var temp = new TempFile("""
            /repo/A.cs(12,3): warning CA1502: complex
            /repo/B.cs(4,1): error CS1000: broken
            Build succeeded.
            """);

        var issues = AnalyzerResultParser.Parse(temp.Path);

        Assert.Collection(issues,
            x => { Assert.Equal("CA1502", x.RuleId); Assert.Equal("warning", x.Severity); Assert.Equal(12, x.Line); },
            x => { Assert.Equal("CS1000", x.RuleId); Assert.Equal("error", x.Severity); Assert.Equal(4, x.Line); });
        Assert.Empty(AnalyzerResultParser.Parse(temp.Path + ".missing"));
    }

    [Fact]
    public void CodeMetrics_finds_methods_local_functions_and_decision_points()
    {
        using var temp = new TempFile("""
            class Sample {
              int Run(bool a, bool b) {
                int Local(int x) => x > 0 ? 1 : 0;
                if (a && b) return Local(1);
                for (var i = 0; i < 2; i++) { }
                return 0;
              }
            }
            """, ".cs");

        var (metric, functions) = new CodeMetricsAnalyzer().Analyze(temp.Path);

        Assert.True(metric.Loc >= 8);
        Assert.True(metric.Complexity >= 5);
        Assert.Contains(functions, x => x.Name == "Run" && x.Kind == "method" && x.Complexity >= 4);
        Assert.Contains(functions, x => x.Name == "Local" && x.Kind == "local-function" && x.Complexity == 2);
    }

    private sealed class TempFile : IDisposable
    {
        public TempFile(string content, string extension = ".tmp")
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"quality-reporter-{Guid.NewGuid()}{extension}");
            File.WriteAllText(Path, content);
        }
        public string Path { get; }
        public void Dispose() => File.Delete(Path);
    }
}
