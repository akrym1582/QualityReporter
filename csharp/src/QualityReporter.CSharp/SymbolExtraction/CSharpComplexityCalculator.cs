using Microsoft.CodeAnalysis; using Microsoft.CodeAnalysis.CSharp; using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace QualityReporter.CSharp.SymbolExtraction;
public static class CSharpComplexityCalculator
{
    public static int Calculate(SyntaxNode symbol,bool logicalOperators=true){var walker=new Walker(symbol,logicalOperators);walker.Visit(symbol);return 1+walker.Decisions;}
    private sealed class Walker(SyntaxNode root,bool logical):CSharpSyntaxWalker { public int Decisions{get;private set;} public override void Visit(SyntaxNode? node){if(node is null||node!=root&&node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax)return;if(node is IfStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax or WhileStatementSyntax or DoStatementSyntax or CatchClauseSyntax or ConditionalExpressionSyntax or CaseSwitchLabelSyntax||logical&&node is BinaryExpressionSyntax b&&(b.IsKind(SyntaxKind.LogicalAndExpression)||b.IsKind(SyntaxKind.LogicalOrExpression)||b.IsKind(SyntaxKind.CoalesceExpression)))Decisions++;base.Visit(node);}}
}
