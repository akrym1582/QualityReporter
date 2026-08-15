namespace QualityReporter.CSharp.SymbolExtraction;
public interface ISymbolExtractor { IReadOnlyList<SymbolResult> Extract(string path, string source); }
