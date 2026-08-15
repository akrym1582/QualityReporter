namespace QualityReporter.CSharp.Analysis;

public static class SourceFileDiscovery
{
    public static List<(string AbsolutePath, string RepositoryPath)> Discover(
        string analysisRoot,
        string repositoryRoot,
        Config config)
    {
        return Directory.EnumerateFiles(analysisRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => (
                AbsolutePath: path,
                RepositoryPath: Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/')))
            .Where(file => !config.Exclude.Any(pattern => Glob(file.RepositoryPath, pattern)))
            .ToList();
    }

    private static bool Glob(string path, string pattern)
    {
        path = path.Replace('\\', '/');
        var regex = System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*");
        return System.Text.RegularExpressions.Regex.IsMatch(
            "/" + path,
            "^/?" + regex + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
