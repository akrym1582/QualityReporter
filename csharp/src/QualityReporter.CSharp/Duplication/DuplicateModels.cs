namespace QualityReporter.CSharp;
public sealed record DuplicateFragment(string Path,int StartLine,int EndLine);
public sealed record DuplicateGroup(string Id,int Lines,int Tokens,List<DuplicateFragment> Fragments);
public interface IDuplicateCodeParser { IReadOnlyList<DuplicateGroup> Parse(string json); }
