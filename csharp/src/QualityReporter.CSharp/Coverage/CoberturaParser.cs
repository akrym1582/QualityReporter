using System.Globalization;
using System.Xml.Linq;

namespace QualityReporter.CSharp.Coverage;

public static class CoberturaParser
{
    public static Dictionary<string, (double Line, double? Branch)> Parse(string path)
    {
        var document = XDocument.Load(path);
        var result = new Dictionary<string, (double, double?)>();
        var classes = document.Descendants("class").Where(x => x.Attribute("filename") is not null);

        foreach (var group in classes.GroupBy(x => x.Attribute("filename")!.Value.Replace('\\', '/')))
        {
            var lines = group.SelectMany(x => x.Descendants("line")).ToList();
            var hitLines = lines.Count(x => (int?)x.Attribute("hits") > 0);
            var branchPercentages = lines
                .Where(x => (bool?)x.Attribute("branch") == true)
                .Select(x => ParsePercentage(x.Attribute("condition-coverage")?.Value))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            result[group.Key] = (
                lines.Count == 0 ? 0 : 100d * hitLines / lines.Count,
                branchPercentages.Count == 0 ? null : branchPercentages.Average());
        }

        return result;
    }

    private static double? ParsePercentage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var percentIndex = value.IndexOf('%');
        return percentIndex > 0 && double.TryParse(value[..percentIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentage)
            ? percentage
            : null;
    }
}
