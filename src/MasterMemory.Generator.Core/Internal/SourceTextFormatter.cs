using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MasterMemory.Generator.Core.Internal;

public static class SourceTextFormatter
{
    public static CompilationUnitSyntax FormatCompilationUnit(string sourceText, bool sort = true)
    {
        CompilationUnitSyntax compilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(sourceText);
        if (sort)
        {
            compilationUnitSyntax = compilationUnitSyntax.WithSortedMembers();
        }

        return compilationUnitSyntax.NormalizeWhitespace();
    }
}