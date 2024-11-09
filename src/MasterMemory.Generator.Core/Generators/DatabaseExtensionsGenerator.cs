using System.Collections.Immutable;
using System.Text;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;

namespace MasterMemory.Generator.Core;

internal static class DatabaseExtensionsGenerator
{
    public static void Execute(SourceGeneratorContext context, ImmutableArray<TableModel> tableArray,
        DatabaseModel database, StringBuilder sb)
    {
        string name = database.Name;
        sb.AppendUsings(tableArray[0].DatabaseModel);
        using (sb.NamespaceScope(database.Namespace))
        {
            sb.Append(database.AccessibilityModifier);
            using (sb.Append(" static class ").AppendCapitalized(name).Append("Extensions").AppendCapitalized(name)
                       .BracketScope())
            {
                using (sb.Append("public static void Transaction(this ").Append(name).Append(" db, System.Action<")
                           .Append(TransactionGenerator.Name).Append("> action)").BracketScope())
                {
                    sb.AppendLine("if (db == null) throw new System.ArgumentNullException(nameof(db));");
                    sb.AppendLine("var transaction = db.Transaction;");
                    using (sb.Append("try").BracketScope())
                    {
                        sb.AppendLine("transaction.BeginTransaction();");
                        sb.AppendLine("action(transaction);");
                        sb.AppendLine("transaction.Commit();");
                    }

                    using (sb.Append("catch").BracketScope())
                    {
                        sb.AppendLine("transaction.Rollback();");
                        sb.AppendLine("throw;");
                    }
                }

                using (sb.Append("public static void Transaction<T>(this ").Append(name).Append(" db, T state, System.Action<")
                           .Append(TransactionGenerator.Name).Append(", T> action)").BracketScope())
                {
                    sb.AppendLine("if (db == null) throw new System.ArgumentNullException(nameof(db));");
                    sb.AppendLine("var transaction = db.Transaction;");
                    using (sb.Append("try").BracketScope())
                    {
                        sb.AppendLine("transaction.BeginTransaction();");
                        sb.AppendLine("action(transaction, state);");
                        sb.AppendLine("transaction.Commit();");
                    }

                    using (sb.Append("catch").BracketScope())
                    {
                        sb.AppendLine("transaction.Rollback();");
                        sb.AppendLine("throw;");
                    }
                }
                
                using (sb.Append("public static bool TransactionSafe(this ").Append(name).Append(" db, System.Action<")
                           .Append(TransactionGenerator.Name).Append("> action)").BracketScope())
                {
                    using (sb.Append("try").BracketScope())
                    {
                        sb.AppendLine("db.Transaction(action);");
                        sb.AppendLine("return true;");
                    }

                    sb.AppendLine("catch");
                    using (sb.BracketScope())
                    {
                        sb.AppendLine("return false;");
                    }
                }

                using (sb.Append("public static bool TransactionSafe<T>(this ").Append(name).Append(" db, T state, System.Action<")
                           .Append(TransactionGenerator.Name).Append(", T> action)").BracketScope())
                {
                    using (sb.Append("try").BracketScope())
                    {
                        sb.AppendLine("db.Transaction(state, action);");
                        sb.AppendLine("return true;");
                    }

                    using (sb.Append("catch").BracketScope())
                    {
                        sb.AppendLine("return false;");
                    }
                }
            }
        }

        context.AddSource($"{name}Extensions.g.cs",sb);
    }
}