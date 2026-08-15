using QualityReporter.CSharp;
using QualityReporter.CSharp.Git;
using QualityReporter.CSharp.Reporting;
using Xunit;

public sealed class GitAndReportingTests
{
    [Fact]
    public void GitParser_handles_rename_delete_and_binary_numstat()
    {
        var commits = GitHistoryAnalyzer.Parse("""
            @@abc	2026-08-01T00:00:00Z	dev@example.com
            2	1	src/{Old => New}.cs
            0	4	src/Deleted.cs
            -	-	asset.png
            """);

        var commit = Assert.Single(commits);
        Assert.Equal((2, 1), commit.Files["src/New.cs"]);
        Assert.Equal((0, 4), commit.Files["src/Deleted.cs"]);
        Assert.DoesNotContain("asset.png", commit.Files.Keys);
    }

    [Fact]
    public void Coupling_excludes_large_commits_and_applies_thresholds()
    {
        var at = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var commits = new[]
        {
            Change("1", at, "a.cs", "b.cs"),
            Change("2", at.AddDays(1), "a.cs", "b.cs"),
            Change("3", at.AddDays(2), "a.cs", "b.cs", "generated.cs"),
        };
        var cfg = new Config { Coupling = new() { MaxFilesPerCommit = 2, MinCount = 2, MinRatio = .5 } };

        var (_, couplings) = GitHistoryAnalyzer.Aggregate(commits, cfg);

        Assert.Contains(couplings, x => x.File == "a.cs" && x.CoupledWith == "b.cs" && x.Count == 2);
        Assert.DoesNotContain(couplings, x => x.CoupledWith == "generated.cs");
    }

    [Fact]
    public void Markdown_and_json_report_include_hotspots_warnings_and_functions()
    {
        var report = new Report
        {
            Warnings = ["Coverage: Not Available"],
            Files = [new FileResult
            {
                Path = "src/A.cs",
                Risk = new() { Score = 82, Level = "critical" },
                History = new() { CommitCount = 3, ReworkRate = .5 },
                Metrics = new() { Complexity = 17 },
                Functions = [new("Run", "method", 1, 20, 17)],
                Couplings = [new("src/A.cs", "src/B.cs", 3, .75)]
            }]
        };
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"quality-report-{Guid.NewGuid()}");
        var json = System.IO.Path.Combine(directory, "report.json");
        var markdown = System.IO.Path.Combine(directory, "report.md");
        try
        {
            ReportWriter.WriteJson(report, json);
            ReportWriter.WriteMarkdown(report, markdown);
            var roundTrip = ReportWriter.Read(json);
            var text = File.ReadAllText(markdown);

            Assert.Equal("src/A.cs", Assert.Single(roundTrip.Files).Path);
            Assert.Contains("Coverage: Not Available", text);
            Assert.Contains("src/B.cs", text);
            Assert.Contains("Run", text);
            Assert.DoesNotContain("Knowledge", text);
            Assert.DoesNotContain("\"knowledge\"", File.ReadAllText(json), StringComparison.OrdinalIgnoreCase);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static CommitChange Change(string hash, DateTimeOffset at, params string[] files) =>
        new(hash, at, "dev@example.com", false, files.ToDictionary(x => x, _ => (1, 0)));
}
