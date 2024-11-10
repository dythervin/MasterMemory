using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MasterMemory.Generator.Core.Internal;

public static class SourceTextFormatter
{
    public static string FormatCompilationUnit(string sourceText, SourceGeneratorFlags flags)
    {
        if (flags == SourceGeneratorFlags.None)
        {
            return sourceText;
        }

        CompilationUnitSyntax compilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(sourceText);
        if (flags.HasFlagFast(SourceGeneratorFlags.SortMembers))
        {
            compilationUnitSyntax = compilationUnitSyntax.WithSortedMembers();
        }

        if (flags.HasFlagFast(SourceGeneratorFlags.NormalizeWhitespace))
        {
            compilationUnitSyntax = compilationUnitSyntax.NormalizeWhitespace();
        }

        return compilationUnitSyntax.ToFullString();
    }
}