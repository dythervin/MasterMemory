using System.Collections.Immutable;
using System.Text;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;

namespace MasterMemory.Generator.Core;

internal static class DatabaseReactiveExtensionsGenerator
{
    public static void Execute(SourceGeneratorContext context, ImmutableArray<TableModel> tableArray,
        DatabaseModel database, StringBuilder sb)
    {
        string name = database.Name;
        sb.AppendUsings(tableArray[0].DatabaseModel);
        using (sb.NamespaceScope(database.Namespace))
        {
            sb.Append(database.AccessibilityModifier);
            using (sb.Append(" static partial class ").AppendCapitalized(name).Append("ReactiveExtensions")
                       .AppendCapitalized(name).BracketScope())
            {
                foreach (TableModel model in tableArray)
                {
                    using (sb.Append("public static ").Append(ObservableDbValueGenerator.GetAbstractName(database))
                               .Append('<').Append(model.GlobalTypeName).Append("> Observe(this ").Append(name)
                               .Append(" db, ").Append(model.GlobalTypeName).Append(" value)").BracketScope())
                    {
                        sb.AppendLine("if (db == null) throw new System.ArgumentNullException(nameof(db));");
                        sb.AppendLine("return new ").Append(ObservableDbValueGenerator.Name).Append("<")
                            .Append(model.PrimaryKey.Type).Append(", ").Append(model.GlobalTypeName)
                            .Append(">(db, value);");
                    }

                    using (sb.Append("public static ").Append(ObservableDbValueGenerator.GetAbstractName(database))
                               .Append('<').Append(model.GlobalTypeName).Append("> Observe").Append(model.TypeName)
                               .Append("(this ").Append(name).Append(" db, ").Append(model.PrimaryKey.Type)
                               .Append(" key)").BracketScope())
                    {
                        sb.AppendLine("if (db == null) throw new System.ArgumentNullException(nameof(db));");
                        sb.AppendLine("return new ").Append(ObservableDbValueGenerator.Name).Append("<")
                            .Append(model.PrimaryKey.Type).Append(", ").Append(model.GlobalTypeName)
                            .Append(">(db, key);");
                    }
                }
            }
        }

        context.AddSource($"Db.{name}ReactiveExtensions.g.cs", sb.ToStringAndClear());
    }
}