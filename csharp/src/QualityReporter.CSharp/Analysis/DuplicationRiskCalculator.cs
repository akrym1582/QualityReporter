namespace QualityReporter.CSharp.Analysis;
public static class DuplicationRiskCalculator { public static double Calculate(double duplicatedPercentage,double activityScore)=>duplicatedPercentage*(.60+.40*Math.Clamp(activityScore,0,100)/100); }
