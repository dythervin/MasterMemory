using System.Collections.Generic;
using MasterMemory.Generator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MasterMemory.Generator.Simple;

internal class SyntaxReceiver : ISyntaxReceiver
{
    public TypeDeclarationSyntax? DatabaseTypeDeclaration;
    public readonly List<(TypeDeclarationSyntax type, AttributeSyntax attribute)> RecordTypeDeclarations = new();
    public readonly List<(TypeDeclarationSyntax type, AttributeSyntax attribute)> ValidatorTypeDeclarations = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not AttributeSyntax attributeSyntax)
            return;

        switch (attributeSyntax.Name.ToString())
        {
            case DatabaseSourceGenerator.DatabaseAttribute.ShortName:
            case DatabaseSourceGenerator.DatabaseAttribute.Name:
                TryAddDatabase(attributeSyntax);
                break;
            case DatabaseSourceGenerator.TableAttribute.ShortName:
            case DatabaseSourceGenerator.TableAttribute.Name:
                TryAddTableRecord(attributeSyntax);
                break;
            case DatabaseSourceGenerator.ValidatorAttribute.ShortName:
            case DatabaseSourceGenerator.ValidatorAttribute.Name:
                TryAddValidator(attributeSyntax);
                break;
        }
    }

    private void TryAddValidator(AttributeSyntax attributeSyntax)
    {
        if (attributeSyntax.Parent?.Parent is TypeDeclarationSyntax typeDeclarationSyntax)
            ValidatorTypeDeclarations.Add((typeDeclarationSyntax, attributeSyntax));
    }

    private void TryAddDatabase(AttributeSyntax databaseAttribute)
    {
        DatabaseTypeDeclaration = databaseAttribute.Parent?.Parent as TypeDeclarationSyntax;
    }

    private void TryAddTableRecord(AttributeSyntax databaseAttribute)
    {
        if (databaseAttribute.Parent?.Parent is TypeDeclarationSyntax typeDeclarationSyntax)
            RecordTypeDeclarations.Add((typeDeclarationSyntax, databaseAttribute));
    }
}