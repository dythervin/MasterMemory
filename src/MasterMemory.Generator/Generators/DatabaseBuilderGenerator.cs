using System.Collections.Immutable;
using System.Text;
using MasterMemory.Generator.Internal;
using MasterMemory.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MasterMemory.Generator;

internal static class DatabaseBuilderGenerator
{
    internal static void Execute(SourceProductionContext context, ImmutableArray<TableModel> tableArray,
        DatabaseModel database, StringBuilder sb)
    {
        sb.AppendUsings(tableArray);
        sb.AppendLine("using System.Linq;");
        string name = database.Name + "Builder";

        using (sb.NamespaceScope(database.Namespace))
        {
            sb.Append(database.AccessibilityModifier);
            sb.Append(" partial class ").Append(name).AppendLine(" : DatabaseBuilderBase");
            using (sb.BracketScope())
            {
                foreach (TableModel model in tableArray)
                {
                    sb.Append("private List<").Append(model.GlobalTypeName).Append("> _")
                        .AppendDecapitalized(model.TableName).AppendLine(";");

                    using (sb.Append("public ").Append(name).Append(" Append(IEnumerable<").Append(model.GlobalTypeName)
                               .Append("> items)").BracketScope())
                    {
                        using (sb.Append("if (this._").AppendDecapitalized(model.TableName).Append(" is null)")
                                   .BracketScope())
                        {
                            sb.Append("this._").AppendDecapitalized(model.TableName).Append(" = new List<")
                                .Append(model.GlobalTypeName).AppendLine(">(items);");
                        }

                        using (sb.Append("else").BracketScope())
                        {
                            sb.Append("this._").AppendDecapitalized(model.TableName).Append(".AddRange(items);");
                        }

                        sb.AppendLine("return this;");
                    }
                }

                using (sb.Append("public ").Append(database.Name).Append(" Build()").BracketScope())
                {
                    using (sb.Append("var database = new ").Append(database.Name).Scope('('))
                    {
                        sb.AppendJoin(", ",
                            tableArray,
                            (sb, model) =>
                            {
                                sb.Append("this._").AppendDecapitalized(model.TableName);
                            });
                    }

                    sb.AppendLine(";");
                    sb.AppendLine("this.Clear();");
                    sb.AppendLine("return database;");
                }

                using (sb.AppendLine("public void Clear()").BracketScope())
                {
                    foreach (TableModel model in tableArray)
                    {
                        sb.Append("this._").AppendDecapitalized(model.TableName).Append("?.Clear();");
                    }
                }

                using (sb.AppendLine("public override void AppendDynamic(System.Type type, IEnumerable<object> items)")
                           .BracketScope())
                {
                    foreach (TableModel model in tableArray)
                    {
                        using (sb.Append("if (type == typeof(").Append(model.GlobalTypeName).Append("))").BracketScope())
                        {
                            sb.Append("this.Append(items.Cast<").Append(model.GlobalTypeName).AppendLine(">());");

                            sb.AppendLine("return;");
                        }
                    }
                }
            }
        }

        context.AddSource($"{name}.g.cs",
            SourceText.From(SourceTextFormatter.FormatCompilationUnit(sb.ToStringAndClear()).ToFullString(),
                Encoding.UTF8));
    }
}