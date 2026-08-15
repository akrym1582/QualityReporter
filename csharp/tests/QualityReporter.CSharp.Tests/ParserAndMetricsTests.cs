using QualityReporter.CSharp;
using QualityReporter.CSharp.Coverage;
using QualityReporter.CSharp.Analysis;
using QualityReporter.CSharp.Metrics;
using QualityReporter.CSharp.SymbolExtraction;
using Xunit;

public sealed class ParserAndMetricsTests
{
    [Fact]
    public void Project_discovery_uses_repository_relative_paths()
    {
        var repository = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var project = Path.Combine(repository, "src", "Nested");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "Program.cs"), "class Program {}");

        try
        {
            var file = Assert.Single(SourceFileDiscovery.Discover(project, repository, new()));
            Assert.Equal("src/Nested/Program.cs", file.RepositoryPath);
        }
        finally
        {
            Directory.Delete(repository, true);
        }
    }

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

public class V13MetricTests
{
 [Theory][InlineData(34,42,19.72)][InlineData(34,100,0)][InlineData(3,0,3)] public void Untested_complexity_uses_uncovered_fraction(int complexity,double coverage,double expected)=>Assert.Equal(expected,QualityReporter.CSharp.Analysis.UntestedComplexityCalculator.Calculate(complexity,coverage));
 [Fact] public void Untested_complexity_is_missing_without_coverage()=>Assert.Null(QualityReporter.CSharp.Analysis.UntestedComplexityCalculator.Calculate(34,null));
 [Fact] public void Combined_coverage_is_supported()=>Assert.Equal(4.4,QualityReporter.CSharp.Analysis.UntestedComplexityCalculator.Calculate(10,40,80,"combined"));
 [Fact] public void Duplication_risk_prioritizes_activity()=>Assert.True(QualityReporter.CSharp.Analysis.DuplicationRiskCalculator.Calculate(.4,90)>QualityReporter.CSharp.Analysis.DuplicationRiskCalculator.Calculate(.4,10));
 [Fact] public void Jscpd_parser_and_symbol_mapper_support_partial_overlap(){var groups=new QualityReporter.CSharp.Duplication.JscpdReportParser().Parse("""{"duplicates":[{"lines":6,"tokens":60,"firstFile":{"name":"src/A.cs","start":5,"end":10},"secondFile":{"name":"src/B.cs","start":1,"end":6}}]}""");var file=new FileResult{Path="src/A.cs",Metrics=new(){Loc=20},Symbols=[new SymbolResult{SymbolId=new string('a',64),SymbolKey="A.M",Name="M",Kind="method",Location=new(8,15),Metrics=new(){Loc=8}}]};QualityReporter.CSharp.Duplication.DuplicateSymbolMapper.Map(groups,[file]);Assert.Equal(6,file.Metrics.Duplication!.DuplicatedLines);Assert.Equal(3,file.Symbols[0].Metrics.Duplication!.DuplicatedLines);}
 [Fact] public void Invalid_jscpd_report_is_rejected()=>Assert.Throws<FormatException>(()=>new QualityReporter.CSharp.Duplication.JscpdReportParser().Parse("{"));
}
