using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MasterMemory.Generator.Core.Internal;

public static class CompilationUnitSorter
{
    public static CompilationUnitSyntax WithSortedMembers(this CompilationUnitSyntax compilationUnit)
    {
        return compilationUnit.WithMembers(
            new SyntaxList<MemberDeclarationSyntax>(compilationUnit.Members.Select(SortMembers).ToArray()));
    }

    private static MemberDeclarationSyntax SortMembers(MemberDeclarationSyntax member)
    {
        return member switch
        {
            NamespaceDeclarationSyntax namespaceDeclarationSyntax => namespaceDeclarationSyntax.WithMembers(
                SyntaxFactory.List(namespaceDeclarationSyntax.Members.Select(SortMembers).ToArray())),
            TypeDeclarationSyntax typeDeclarationSyntax => typeDeclarationSyntax.WithMembers(
                SyntaxFactory.List(Sort(typeDeclarationSyntax.Members).ToArray())),
            _ => member
        };
    }

    private static IOrderedEnumerable<MemberDeclarationSyntax> Sort(IEnumerable<MemberDeclarationSyntax> members)
    {
        return members.OrderBy(delegate(MemberDeclarationSyntax m)
        {
            return m switch
            {
                FieldDeclarationSyntax => 0,
                PropertyDeclarationSyntax => 1,
                ConstructorDeclarationSyntax => 2,
                _ => m is MethodDeclarationSyntax ? 3 : 4
            };
        }).ThenBy(delegate(MemberDeclarationSyntax m)
        {
            SyntaxToken val2 = m.Modifiers.FirstOrDefault();
            return val2.ValueText switch
            {
                "public" => 0,
                "internal" => 1,
                "protected" => 2,
                "private" => 4,
                _ => 3,
            };
        }).ThenBy(delegate(MemberDeclarationSyntax m)
        {
            int num = 5;
            int num2 = 1;
            while (true)
            {
                int num3 = num2;
                SyntaxTokenList modifiers = m.Modifiers;
                if (num3 >= modifiers.Count)
                {
                    break;
                }

                modifiers = m.Modifiers;
                SyntaxToken val = modifiers[num2];
                if (val.IsKind(SyntaxKind.ConstKeyword))
                {
                    return 0;
                }

                if (val.IsKind(SyntaxKind.ReadOnlyKeyword))
                {
                    num--;
                }

                if (val.IsKind(SyntaxKind.StaticKeyword))
                {
                    num -= 2;
                }

                num2++;
            }

            return num;
        });
    }
}