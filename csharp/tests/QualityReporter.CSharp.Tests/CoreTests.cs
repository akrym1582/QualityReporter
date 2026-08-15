using Xunit;using QualityReporter.CSharp;using QualityReporter.CSharp.Analysis;using QualityReporter.CSharp.Git;
public class CoreTests { [Fact] public void Git_aggregates_churn_rework_and_coupling(){var now=DateTimeOffset.UtcNow;var cs=new[]{new CommitChange("1",now,"a@x",false,new(){{"a.cs",(2,1)},{"b.cs",(1,0)}}),new CommitChange("2",now.AddDays(2),"b@x",false,new(){{"a.cs",(3,2)},{"b.cs",(1,1)}}),new CommitChange("3",now.AddDays(40),"a@x",false,new(){{"a.cs",(1,0)},{"b.cs",(1,0)}})};var cfg=new Config{Coupling=new(){MinCount=3,MinRatio=.3}};var(h,c)=GitHistoryAnalyzer.Aggregate(cs,cfg);Assert.Equal(3,h["a.cs"].CommitCount);Assert.Equal(9,h["a.cs"].Churn);Assert.Equal(2,h["a.cs"].AuthorCount);Assert.Equal(1d/3,h["a.cs"].ReworkRate,5);Assert.Contains(c,x=>x.File=="a.cs"&&x.CoupledWith=="b.cs");}[Fact]public void Risk_normalizes_missing_coverage(){var fs=new List<FileResult>{F("a",1),F("b",9)};RiskCalculator.Calculate(fs,new());Assert.Equal(0,fs[0].Risk.Score);Assert.Equal(94.1,fs[1].Risk.Score);Assert.Equal("critical",fs[1].Risk.Level);}[Fact]public void Baseline_finds_new_and_resolved(){var old=new Report{Files=[F("old",9),F("new",1)]};var now=new Report{Files=[F("old",1),F("new",9)]};old.Files[0].Risk.Score=90;old.Files[1].Risk.Score=10;now.Files[0].Risk.Score=10;now.Files[1].Risk.Score=90;var json=System.Text.Json.JsonSerializer.Serialize(BaselineComparer.Compare(now,old));Assert.Contains("new",json);Assert.Contains("old",json);}static FileResult F(string p,int n)=>new(){Path=p,Metrics=new(){Complexity=n},History=new(){CommitCount=n,Churn=n,ReworkRate=n/10d}};}

public sealed class RiskBoundaryTests
{
    [Fact]
    public void Risk_levels_include_all_boundaries()
    {
        var files = Enumerable.Range(0, 6).Select(i => new FileResult
        {
            Path = $"{i}.cs",
            History = new() { CommitCount = i }
        }).ToList();
        var weights = new RiskWeights { Change = 1, Churn = 0, Rework = 0, Complexity = 0, Coverage = 0, Issues = 0 };

        RiskCalculator.Calculate(files, weights);

        Assert.Equal(new[] { "low", "low", "medium", "high", "critical", "critical" }, files.Select(x => x.Risk.Level));
        Assert.Equal(new[] { 0d, 20d, 40d, 60d, 80d, 100d }, files.Select(x => x.Risk.Score));
    }

    [Fact]
    public void Missing_coverage_weight_is_removed_instead_of_scored_as_zero()
    {
        var files = new List<FileResult>
        {
            new() { Path = "low.cs", History = new() { CommitCount = 1 } },
            new() { Path = "high.cs", History = new() { CommitCount = 2 } }
        };
        var weights = new RiskWeights { Change = 25, Churn = 0, Rework = 0, Complexity = 0, Coverage = 75, Issues = 0 };

        RiskCalculator.Calculate(files, weights);

        Assert.Equal(100, files[1].Risk.Score);
        Assert.Null(files[1].Risk.CoverageRiskPercentile);
    }
}
