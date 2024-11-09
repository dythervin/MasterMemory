using System.Collections.Immutable;
using System.Text;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;

namespace MasterMemory.Generator.Core;

internal static class TableContainerGenerator
{
    public const string Name = "TableContainer";

    internal static void Execute(SourceGeneratorContext context, ImmutableArray<TableModel> tableArray,
        DatabaseModel databaseModel, StringBuilder sb)
    {
        sb.AppendUsings(tableArray[0].DatabaseModel);
        using (sb.NamespaceScope(databaseModel.Namespace))
        {
            using (sb.AppendLine($"public class {Name} : IReadOnlyList<ITable>").BracketScope())
            {
                sb.AppendLine("public int Count => ").Append(tableArray.Length).AppendLine(";");
                using (sb.AppendLine("public ITable this[int index]").BracketScope())
                {
                    using (sb.AppendLine("get").BracketScope())
                    {
                        using (sb.AppendLine("switch (index) ").BracketScope())
                        {
                            for (int i = 0; i < tableArray.Length; i++)
                            {
                                TableModel model = tableArray[i];
                                sb.Append("case ").Append(i).Append(": return ").Append(model.TableName)
                                    .AppendLine(";");
                            }

                            sb.AppendLine("default: throw new System.IndexOutOfRangeException();");
                        }
                    }
                }

                using (sb.AppendLine("public IEnumerator<ITable> GetEnumerator()").BracketScope())
                {
                    foreach (TableModel model in tableArray)
                    {
                        sb.Append("yield return ").Append(model.TableName).AppendLine(";");
                    }
                }

                sb.AppendLine("IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();");

                using (sb.AppendLine("public ITable GetTable(string tableName)").BracketScope())
                {
                    using (sb.AppendLine("switch (tableName)").BracketScope())
                    {
                        foreach (TableModel model in tableArray)
                        {
                            sb.Append("case \"").Append(model.TableRawName).Append("\": return ").Append(model.TableName)
                                .AppendLine(";");
                        }

                        sb.AppendLine("default: throw new System.IndexOutOfRangeException();");
                    }
                }

                foreach (TableModel model in tableArray)
                {
                    sb.Append("public readonly ").Append(model.TableGlobalName).Append(" ").Append(model.TableName)
                        .AppendLine(";");
                }

                using (sb.Append($"public {Name}").Scope('('))
                {
                    sb.AppendJoin(", ",
                        tableArray,
                        static (sb, model) =>
                        {
                            sb.Append(model.TableGlobalName).Append(" ").AppendDecapitalized(model.TableName);
                        });
                }

                using (sb.BracketScope())
                {
                    foreach (TableModel model in tableArray)
                    {
                        sb.Append("this.").Append(model.TableName).Append(" = ").AppendDecapitalized(model.TableName)
                            .AppendLine(";");
                    }
                }
            }
        }

        context.AddSource($"{Name}.g.cs",sb);
    }
}