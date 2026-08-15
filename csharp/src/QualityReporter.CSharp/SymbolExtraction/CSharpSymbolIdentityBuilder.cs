using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
namespace QualityReporter.CSharp.SymbolExtraction;
public static class CSharpSymbolIdentityBuilder
{
    public static string BuildKey(IMethodSymbol symbol) => symbol.ToDisplayString(new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces, genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters, memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters, parameterOptions: SymbolDisplayParameterOptions.IncludeType, miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes));
    public static string BuildId(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();
}
