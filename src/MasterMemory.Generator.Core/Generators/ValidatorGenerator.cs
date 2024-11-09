using System.Text;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;

namespace MasterMemory.Generator.Core;

internal static class ValidatorGenerator
{
    internal static void Execute(SourceGeneratorContext context, DatabaseModel database, StringBuilder sb)
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

        context.AddSource("DbValidator.g.cs",sb);
    }

    public static void Execute(SourceGeneratorContext context, ValidatorModel model)
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

        context.AddSource($"Validators_{model.Name}.g.cs",sb);
    }
}