namespace QualityReporter.CSharp.Analysis;

public static class HotspotClassifier
{
    private static readonly string[] ClassificationOrder =
    [
        "critical",
        "untested_complex",
        "duplicated",
        "rework",
        "complex",
        "coupled",
        "untested",
        "new_active",
        "active"
    ];

    private static readonly HashSet<string> CriticalSignals =
    [
        "complex",
        "rework",
        "untested_complex",
        "coupled",
        "duplicated"
    ];

    public static void Classify(IEnumerable<FileResult> files, Config config)
    {
        foreach (var file in files)
        {
            Classify(file, config);
        }
    }

    private static void Classify(FileResult file, Config config)
    {
        var classifications = file.Hotspot.Classifications;
        classifications.Clear();

        var isComplex = HotspotClassificationRules.IsComplex(file, config);
        var hasRework = HotspotClassificationRules.HasRework(file, config);

        AddIf(classifications, "complex", isComplex);
        AddIf(classifications, "rework", hasRework);
        AddIf(classifications, "untested_complex", HotspotClassificationRules.IsUntestedComplex(file, config));
        AddIf(classifications, "duplicated", HotspotClassificationRules.IsDuplicated(file, config));
        AddIf(classifications, "untested", HotspotClassificationRules.IsUntested(file, config, isComplex, hasRework));
        AddIf(classifications, "coupled", HotspotClassificationRules.IsCoupled(file, config));
        AddIf(classifications, "new_active", HotspotClassificationRules.IsNewAndActive(file));
        AddIf(classifications, "active", HotspotClassificationRules.IsActive(file));
        AddIf(classifications, "critical", IsCritical(file, config, classifications));

        file.Hotspot.PrimaryClassification = ClassificationOrder.FirstOrDefault(classifications.Contains);
    }

    private static bool IsCritical(FileResult file, Config config, IEnumerable<string> classifications) =>
        file.Hotspot.PriorityScore >= config.Classification.CriticalPriority
        && classifications.Count(CriticalSignals.Contains) >= 2;

    private static void AddIf(ICollection<string> classifications, string classification, bool condition)
    {
        if (condition)
        {
            classifications.Add(classification);
        }
    }
}
