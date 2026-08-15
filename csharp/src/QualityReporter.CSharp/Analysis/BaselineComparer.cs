namespace QualityReporter.CSharp.Analysis;

public static class BaselineComparer
{
    public static object Compare(Report current, Report old)
    {
        var currentFiles = current.Files.ToDictionary(file => file.Path);
        var previousFiles = old.Files.ToDictionary(file => file.Path);

        return new
        {
            newCritical = CurrentPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => IsCritical(currentFile) && !IsCritical(previousFile)),
            resolvedCritical = PreviousPaths(currentFiles, previousFiles,
                (previousFile, currentFile) => IsCritical(previousFile) && !IsCritical(currentFile)),
            newHotspots = CurrentPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => IsHotspot(currentFile) && !IsHotspot(previousFile)),
            resolvedHotspots = PreviousPaths(currentFiles, previousFiles,
                (previousFile, currentFile) => IsHotspot(previousFile) && !IsHotspot(currentFile)),
            riskIncreased = ChangedPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => currentFile.Risk.Score > previousFile.Risk.Score),
            riskDecreased = ChangedPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => currentFile.Risk.Score < previousFile.Risk.Score),
            priorityIncreased = ChangedPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => currentFile.Hotspot.PriorityScore > previousFile.Hotspot.PriorityScore),
            priorityDecreased = ChangedPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => currentFile.Hotspot.PriorityScore < previousFile.Hotspot.PriorityScore),
            newUntestedComplexMethods = CurrentPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => HasClassification(currentFile, "untested_complex")
                    && !HasClassification(previousFile, "untested_complex")),
            newDuplicateHotspots = CurrentPaths(currentFiles, previousFiles,
                (currentFile, previousFile) => HasClassification(currentFile, "duplicated")
                    && currentFile.Hotspot.PriorityScore >= 80
                    && !HasClassification(previousFile, "duplicated"))
        };
    }

    private static IEnumerable<string> CurrentPaths(
        IReadOnlyDictionary<string, FileResult> currentFiles,
        IReadOnlyDictionary<string, FileResult> previousFiles,
        Func<FileResult, FileResult?, bool> predicate) =>
        currentFiles.Values
            .Where(currentFile => predicate(currentFile, Find(previousFiles, currentFile.Path)))
            .Select(file => file.Path);

    private static IEnumerable<string> PreviousPaths(
        IReadOnlyDictionary<string, FileResult> currentFiles,
        IReadOnlyDictionary<string, FileResult> previousFiles,
        Func<FileResult, FileResult?, bool> predicate) =>
        previousFiles.Values
            .Where(previousFile => predicate(previousFile, Find(currentFiles, previousFile.Path)))
            .Select(file => file.Path);

    private static IEnumerable<string> ChangedPaths(
        IReadOnlyDictionary<string, FileResult> currentFiles,
        IReadOnlyDictionary<string, FileResult> previousFiles,
        Func<FileResult, FileResult, bool> predicate) =>
        currentFiles.Values
            .Where(currentFile => previousFiles.TryGetValue(currentFile.Path, out var previousFile)
                && predicate(currentFile, previousFile))
            .Select(file => file.Path);

    private static FileResult? Find(IReadOnlyDictionary<string, FileResult> files, string path) =>
        files.TryGetValue(path, out var file) ? file : null;

    private static bool IsCritical(FileResult? file) => file?.Risk.Level == "critical";

    private static bool IsHotspot(FileResult? file) => file?.Hotspot.PriorityScore >= 60;

    private static bool HasClassification(FileResult? file, string classification) =>
        file?.Hotspot.Classifications.Contains(classification) == true;
}
