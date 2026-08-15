namespace QualityReporter.CSharp.Analysis;
public static class HotspotPriorityCalculator { public static void Calculate(IEnumerable<FileResult> files,FileMaturityConfig cfg){foreach(var f in files){if(f.Risk.Score>=cfg.CriticalRiskBypass)f.Activity.EffectiveScore=f.Activity.Score;f.Hotspot.PriorityScore=Math.Round(f.Risk.Score*(.6+.4*f.Activity.EffectiveScore/100),1);}}}
