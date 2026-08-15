using QualityReporter.CSharp;
using QualityReporter.CSharp.Coverage;
using QualityReporter.CSharp.Metrics;
using QualityReporter.CSharp.SymbolExtraction;
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

    [Fact]
    public void SymbolExtractor_builds_stable_semantic_identities_and_excludes_nested_complexity()
    {
        const string source = """
            namespace Billing;
            class Service {
              Service() { }
              int Run<T>(int value) { int Local() => value > 0 ? 1 : 0; if (value > 1) return Local(); return 0; }
              int Run(string value) => value.Length;
              int Value { get { return 1; } set { } }
            }
            """;
        var symbols = new CSharpSymbolExtractor(new() { IncludeAccessors = true }).Extract("Service.cs", source);

        Assert.Equal(6, symbols.Count);
        Assert.Equal(2, symbols.Single(x => x.Name == "Run" && x.SymbolKey.Contains("int")).Metrics.Complexity);
        Assert.Equal(2, symbols.Single(x => x.Name == "Local").Metrics.Complexity);
        Assert.All(symbols, x => Assert.Matches("^[0-9a-f]{64}$", x.SymbolId));
        Assert.Equal(symbols.Count, symbols.Select(x => x.SymbolId).Distinct().Count());
        Assert.Contains(symbols, x => x.Kind == "constructor");
        Assert.Contains(symbols, x => x.Kind == "getter");
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
