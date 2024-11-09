using System.Collections.Immutable;
using System.Text;
using MasterMemory.Annotations;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;

namespace MasterMemory.Generator.Core;

internal static class DatabaseGenerator
{
    public static void Execute(SourceGeneratorContext context, ImmutableArray<TableModel> tableArray,
        ImmutableArray<ValidatorModel> validatorArray, DatabaseModel database, StringBuilder sb)
    {
        string name = database.Name;
        sb.AppendUsings(tableArray[0].DatabaseModel);
        using (sb.NamespaceScope(database.Namespace))
        {
            sb.Append(database.AccessibilityModifier);
            using (sb.Append(" interface I").Append(name).BracketScope())
            {
                sb.AppendMembers(tableArray, true);
            }
        }

        context.AddSource($"I{name}.g.cs", sb);

        sb.AppendUsings(tableArray[0].DatabaseModel);
        using (sb.NamespaceScope(database.Namespace))
        {
            bool isMultithreaded = database.IsMultithreaded;
            sb.Append(database.AccessibilityModifier);

            using (sb.Append(" partial class ").AppendCapitalized(name).Append(" : DatabaseBase, I")
                       .AppendCapitalized(name).BracketScope())
            {
                sb.AppendLine($"private readonly {TableContainerGenerator.Name} _container;");
                sb.Append("public readonly ").Append(TransactionGenerator.Name).Append(" Transaction;");

                sb.Append("public const int ThreadedComplexityThreshold = ")
                    .Append(database.ThreadedComplexityThreshold).AppendLine(";");

                sb.AppendMembers(tableArray, false);

                sb.AppendConstructor(tableArray, name, isMultithreaded);
                sb.AppendStaticConstructor(database);

                sb.AppendLine("partial void OnConstruct();");
                sb.AppendLine("partial void OnDisposePartial();");

                sb.AppendValidation(validatorArray);

                using (sb.AppendLine("protected override void OnDispose()").BracketScope())
                {
                    sb.AppendLine("this.Transaction.Dispose();");
                    sb.AppendLine("this.OnDisposePartial();");
                    sb.AppendLine("base.OnDispose();");
                }
            }
        }

        context.AddSource($"{name}.g.cs", sb);
    }

    private static StringBuilder AppendValidation(this StringBuilder sb, ImmutableArray<ValidatorModel> validatorArray)
    {
        using (sb.AppendLine("public ValidateResult Validate()").BracketScope())
        {
            sb.AppendLine("var result = new ValidateResult();");
            if (validatorArray.Length > 0)
            {
                sb.AppendLine("var validator = new Validator(result);");

                foreach (ValidatorModel validatorModel in validatorArray)
                {
                    sb.Append("var ").AppendDecapitalized(validatorModel.Name).Append(" = new ")
                        .Append(validatorModel.FullName).Append("();");

                    sb.AppendDecapitalized(validatorModel.Name).Append(".Validate(this, validator);");
                }
            }

            sb.AppendLine("return result;");
        }

        return sb;
    }

    private static void AppendStaticConstructor(this StringBuilder sb, DatabaseModel database)
    {
        if (!database.HasFlag(DatabaseFlags.MemoryPack))
        {
            return;
        }

        using (sb.AppendLine("static ").Append(database.Name).Append("()").BracketScope())
        {
            if (database.HasFlag(DatabaseFlags.MemoryPack))
            {
                sb.Append("if(!MemoryPackFormatterProvider.IsRegistered<").Append(database.Name).AppendLine(">())");
                sb.Append("MemoryPackFormatterProvider.Register(new ").Append(database.Name).Append("Formatter());");
            }

            sb.Append("OnStaticConstruct();");
        }

        sb.AppendLine("static partial void OnStaticConstruct();");
    }

    private static void AppendConstructor(this StringBuilder sb, ImmutableArray<TableModel> tableArray, string name,
        bool isMultithreaded)
    {
        using (sb.AppendLine("public ").AppendCapitalized(name).Scope('('))
        {
            sb.AppendJoin(", ",
                tableArray,
                (sb, model) => sb.Append("IReadOnlyList<").Append(model.GlobalTypeName).Append(">? ")
                    .AppendDecapitalized(model.TableName).Append(" = null"));
        }

        if (isMultithreaded)
        {
            sb.Append(" : this(").AppendDefaultMaxDegreeOfParallelism(tableArray).Append(", ").AppendJoin(", ",
                tableArray,
                (sb, model) => sb.AppendDecapitalized(model.TableName)).AppendLine(")").AppendLine("{ }");

            using (sb.AppendLine("public ").AppendCapitalized(name).Scope('('))
            {
                sb.AppendLine("int maxDegreeOfParallelism, ");

                sb.AppendJoin(", ",
                    tableArray,
                    (sb, model) => sb.Append("IReadOnlyList<").Append(model.GlobalTypeName).Append(">? ")
                        .AppendDecapitalized(model.TableName).Append(" = null"));
            }
        }

        using (sb.Append(": base").Scope('('))
        {
            sb.Append(tableArray.Length);
        }

        using (sb.BracketScope())
        {
            using (sb.Append("this._container = new ").Append(TableContainerGenerator.Name).Scope('('))
            {
                sb.AppendJoin(",",
                    tableArray,
                    (sb, model) =>
                    {
                        using (sb.Append("new ").Append(model.TableGlobalName).Scope('('))
                        {
                            sb.Append("(IReadOnlyList<").Append(model.GlobalTypeName).Append(">)(")
                                .AppendDecapitalized(model.TableName).Append(" ?? System.Array.Empty<")
                                .Append(model.GlobalTypeName).Append(">())");
                        }
                    });
            }

            sb.AppendLine(";");
            foreach (TableModel model in tableArray)
            {
                sb.Append("this.RegisterTable(this._container.").Append(model.TableName).Append(");");
            }

            sb.AppendLine("this.Transaction = new(this._container").Append(isMultithreaded, ", maxDegreeOfParallelism")
                .AppendLine(");");

            sb.Append("this.OnConstruct();");
        }
    }

    private static StringBuilder AppendMembers(this StringBuilder sb, ImmutableArray<TableModel> tableArray,
        bool isInterface)
    {
        foreach (TableModel model in tableArray)
        {
            sb.Append("public ").Append(model.ITableGlobalName).Append(' ').Append(model.TableName);
            if (isInterface)
            {
                sb.AppendLine(" { get; }");
            }
            else
            {
                sb.AppendLine(" => this._container.").Append(model.TableName).AppendLine(";");
            }
        }

        sb.Append("public IReadOnlyList<ITable> Tables ");
        sb.AppendLine(isInterface ? "{ get; }" : "=> this._container;");

        sb.AppendLine("public ITable GetTable(string tableName)");

        if (isInterface)
        {
            sb.AppendLine(";");
        }
        else
        {
            using (sb.BracketScope())
            {
                sb.AppendLine("return this._container.GetTable(tableName);");
            }
        }

        if (isInterface)
        {
            return sb;
        }

        {
            sb.AppendLine("private static MetaDatabase metaTable;");
        }

        {
            sb.AppendLine("public static MetaDatabase GetMetaDatabase()");
            using (sb.BracketScope())
            {
                sb.AppendLine("if (metaTable != null) return metaTable;");
                sb.AppendLine("var dict = new Dictionary<string, MetaTable>(").Append(tableArray.Length)
                    .AppendLine(");");

                foreach (TableModel model in tableArray)
                {
                    sb.Append("dict.Add(\"").Append(model.TableRawName).Append("\", ").Append(model.TableGlobalName)
                        .Append(".CreateMetaTable());");
                }

                sb.AppendLine("metaTable = new MetaDatabase(dict);");
                sb.AppendLine("return metaTable;");
            }
        }

        return sb;
    }
}