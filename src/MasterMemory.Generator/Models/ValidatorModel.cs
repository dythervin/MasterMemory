using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MasterMemory.Generator.Models;

internal record ValidatorModel
{
    public readonly string FullName;
    public readonly string Name;
    public readonly string AccessibilityModifier;
    public readonly string TypeKind;
    public readonly string? Namespace;

    public DatabaseModel DatabaseModel { get; init; }

    public ValidatorModel(INamedTypeSymbol typeSymbol, TypeDeclarationSyntax declarationSyntax)
    {
        FullName = typeSymbol.ToDisplayString();
        Name = typeSymbol.Name;
        Namespace = typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } ?
            typeSymbol.ContainingNamespace.ToDisplayString() :
            string.Empty;

        AccessibilityModifier = typeSymbol.GetAccessibilityModifier();
        TypeKind = declarationSyntax.Kind() switch
        {
            SyntaxKind.ClassDeclaration => "class",
            SyntaxKind.RecordDeclaration => "record",
            SyntaxKind.StructDeclaration => "struct",
            SyntaxKind.RecordStructDeclaration => "record struct",
            _ => throw new NotSupportedException()
        };
    }
}