using System.Text;
using MasterMemory.Generator.Internal;
using MasterMemory.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MasterMemory.Generator;

internal static class ValidatorGenerator
{
    internal static void Execute(SourceProductionContext context, DatabaseModel database, StringBuilder sb)
    {
        sb.AppendUsings(database);
        string name = database.Name;

        using (sb.NamespaceScope(database.Namespace))
        {
            string accessModifier = database.AccessibilityModifier;

            using (sb.Append(accessModifier).Append(" interface IDatabaseValidator").BracketScope())
            {
                sb.Append("void Validate(I").Append(name).Append(" db, IValidator validator);");
            }
        }

        context.AddSource("DbValidator.g.cs",
            SourceText.From(SourceTextFormatter.FormatCompilationUnit(sb.ToStringAndClear()).ToFullString(),
                Encoding.UTF8));
    }

    public static void Execute(SourceProductionContext context, ValidatorModel model)
    {
        var sb = new StringBuilder();
        sb.AppendUsings(model.DatabaseModel);
        using (sb.NamespaceScope(model.Namespace))
        {
            using (sb.Append(model.AccessibilityModifier).Append(" partial ").Append(model.TypeKind)
                       .Append(" ").Append(model.Name).Append(" : IDatabaseValidator").BracketScope())
            {
            }
        }

        context.AddSource($"Validators/{model.Name}.g.cs",
            SourceText.From(SourceTextFormatter.FormatCompilationUnit(sb.ToStringAndClear()).ToFullString(),
                Encoding.UTF8));
    }
}