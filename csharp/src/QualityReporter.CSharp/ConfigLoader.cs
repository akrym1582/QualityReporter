using System.Text.Json;
using System.Text.Json.Serialization;

namespace QualityReporter.CSharp;

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static Config Read(string path) =>
        JsonSerializer.Deserialize<Config>(File.ReadAllText(path), Json) ?? new Config();
}
