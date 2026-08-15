using System.Text.Json;
namespace QualityReporter.CSharp.Duplication;
public sealed class JscpdReportParser:IDuplicateCodeParser
{
 public IReadOnlyList<DuplicateGroup> Parse(string json){try{using var d=JsonDocument.Parse(json);if(!d.RootElement.TryGetProperty("duplicates",out var ds)||ds.ValueKind!=JsonValueKind.Array)return [];var result=new List<DuplicateGroup>();var i=0;foreach(var x in ds.EnumerateArray()){var fragments=new List<DuplicateFragment>();if(x.TryGetProperty("fragments",out var fs))foreach(var f in fs.EnumerateArray())fragments.Add(Fragment(f));else foreach(var name in new[]{"firstFile","secondFile"})if(x.TryGetProperty(name,out var f))fragments.Add(Fragment(f));var lines=Int(x,"lines");var tokens=Int(x,"tokens");var id=x.TryGetProperty("id",out var idp)?idp.GetString():null;result.Add(new(id??$"dup-{++i:000}",lines,tokens,fragments));}return result;}catch(JsonException e){throw new FormatException("Invalid jscpd report.",e);}}
 static DuplicateFragment Fragment(JsonElement f){var path=f.TryGetProperty("path",out var p)?p.GetString():f.TryGetProperty("name",out p)?p.GetString():"";return new(path!.Replace('\\','/'),Int(f,"startLine","start"),Int(f,"endLine","end"));}
 static int Int(JsonElement x,params string[] names){foreach(var n in names)if(x.TryGetProperty(n,out var p)&&p.TryGetInt32(out var v))return v;return 0;}
}
