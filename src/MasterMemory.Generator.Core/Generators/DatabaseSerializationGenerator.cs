using System.Collections.Immutable;
using System.Text;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;
using MasterMemory.Annotations;

namespace MasterMemory.Generator.Core;

internal static class DatabaseSerializationGenerator
{
    public static void Execute(SourceGeneratorContext context, ImmutableArray<TableModel> tableArray,
        DatabaseModel database, StringBuilder sb)
    {
        const DatabaseFlags serializationFlags = DatabaseFlags.SystemTextJson | DatabaseFlags.NewtonsoftJson |
                                                 DatabaseFlags.MemoryPack;

        if ((serializationFlags & database.Flags) == 0)
        {
            return;
        }

        sb.AppendUsings(database);
        using (sb.NamespaceScope(database.Namespace))
        {
            if (database.HasFlag(DatabaseFlags.SystemTextJson))
            {
                sb.Append("[System.Text.Json.Serialization.JsonConverter(typeof(").Append(database.Name)
                    .Append("JsonConvertor))]");
            }
            else if (database.HasFlag(DatabaseFlags.NewtonsoftJson))
            {
                sb.Append("[Newtonsoft.Json.JsonConverter(typeof(").Append(database.Name).Append("JsonConvertor))]");
            }

            using (sb.Append(database.AccessibilityModifier).Append(" partial class ").Append(database.Name)
                       .BracketScope())
            {
                if (database.HasFlag(DatabaseFlags.SystemTextJson) || database.HasFlag(DatabaseFlags.NewtonsoftJson))
                {
                    sb.AppendJsonSerialization(database);
                }

                sb.AppendBinarySerialization(tableArray, database);
            }
        }

        context.AddSource($"{database.Name}.Serialization.g.cs",sb);
    }

    private static void AppendJsonSerialization(this StringBuilder sb, DatabaseModel database)
    {
        string name = database.Name;
        using (sb.Append("public string ").Append("ToJson(bool indented = false)").BracketScope())
        {
            sb.Append("return ToJson(this, indented);");
        }

        using (sb.Append("public static string ").Append("ToJson(").Append(name)
                   .Append(" value, bool indented = false)").BracketScope())
        {
            if (database.HasFlag(DatabaseFlags.SystemTextJson))
            {
                sb.Append(
                    "return System.Text.Json.JsonSerializer.Serialize(value, new System.Text.Json.JsonSerializerOptions { WriteIndented = indented });");
            }
            else if (database.HasFlag(DatabaseFlags.NewtonsoftJson))
            {
                sb.Append(
                    "return Newtonsoft.Json.JsonConvert.SerializeObject(value, indented ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);");
            }
        }

        using (sb.Append("public static ").Append(name).Append(" ").Append("FromJson(string json)").BracketScope())
        {
            if (database.HasFlag(DatabaseFlags.SystemTextJson))
            {
                sb.AppendLine("return System.Text.Json.JsonSerializer.Deserialize<").Append(name).Append(">(json);");
            }
            else if (database.HasFlag(DatabaseFlags.NewtonsoftJson))
            {
                sb.AppendLine("return Newtonsoft.Json.JsonConvert.DeserializeObject<").Append(name).Append(">(json);");
            }
        }
    }

    private static void AppendBinarySerialization(this StringBuilder sb, ImmutableArray<TableModel> tableArray,
        DatabaseModel database)
    {
        string name = database.Name;
        if (!database.HasFlag(DatabaseFlags.MemoryPack))
        {
            return;
        }

        sb.AppendLine("private const int Version = 0;");

        using (sb.AppendLine(
                       "public static void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref ")
                   .Append(name).Append(" value) where TBufferWriter : class, IBufferWriter<byte>").BracketScope())
        {
            using (sb.Append("if (value == null)").BracketScope())
            {
                sb.AppendLine("writer.WriteNullObjectHeader();");
                sb.AppendLine("return;");
            }

            using (sb.Append("if (value.Transaction.IsInProgress)").BracketScope())
            {
                sb.AppendLine("throw new System.InvalidOperationException(\"Transaction is in progress\");");
            }

            sb.AppendLine("writer.WriteObjectHeader(2);");
            sb.Append("writer.WriteVarInt(").Append("Version").AppendLine(");");
            sb.Append("writer.WriteCollectionHeader(").Append(tableArray.Length).AppendLine(");");
            sb.AppendLine("var tempBuffer = global::MemoryPack.Internal.ReusableLinkedArrayBufferWriterPool.Rent();");

            using (sb.AppendLine("try").BracketScope())
            {
                foreach (TableModel model in tableArray)
                {
                    using (sb.BracketScope())
                    {
                        sb.Append("writer.WriteString(\"").Append(model.TableRawName).AppendLine("\");");
                        sb.Append("var ").Append(model.TypeName).Append("Formatter = writer.GetFormatter<")
                            .Append(model.GlobalTypeName).AppendLine(">();");

                        sb.AppendLine(
                            "var tempWriter = new MemoryPackWriter<global::MemoryPack.Internal.ReusableLinkedArrayBufferWriter>(ref tempBuffer, writer.OptionalState);");

                        sb.Append("tempWriter").Append(".WriteCollectionHeader(value._container.")
                            .Append(model.TableName).Append(".Count);");

                        using (sb.Append("foreach (var item in value._container.").Append(model.TableName)
                                   .AppendLine(")").BracketScope())
                        {
                            sb.Append("var itemCopy = item;");
                            sb.Append(model.TypeName).Append("Formatter.Serialize(ref ").Append("tempWriter")
                                .Append(", ref itemCopy);");
                        }

                        sb.AppendLine("writer.WriteVarInt(tempWriter.WrittenCount);");
                        sb.AppendLine("tempWriter.Flush();");
                        sb.AppendLine("tempBuffer.WriteToAndReset(ref writer);");
                    }
                }
            }

            using (sb.AppendLine("finally").BracketScope())
            {
                sb.AppendLine("global::MemoryPack.Internal.ReusableLinkedArrayBufferWriterPool.Return(tempBuffer);");
            }
        }

        using (sb.AppendLine("public static ").Append(database.GlobalName)
                   .Append(" Deserialize(ref MemoryPackReader reader)").BracketScope())
        {
            sb.Append(database.GlobalName).Append("? value = null;");
            sb.AppendLine("Deserialize(ref reader, ref value);");
            sb.AppendLine("return value;");
        }

        using (sb.AppendLine("public static void Deserialize(ref MemoryPackReader reader, ref ").Append(name)
                   .Append(" value)").BracketScope())
        {
            using (sb.AppendLine("if (!reader.TryReadObjectHeader(out _))").BracketScope())
            {
                sb.AppendLine("value = null!;");
                sb.AppendLine("return;");
            }

            sb.AppendLine("int version = reader.ReadVarIntInt32();");
            using (sb.Append("if (!reader.TryReadCollectionHeader(out var count))").BracketScope())
            {
                sb.AppendLine("value = null!;");
                sb.AppendLine("return;");
            }

            foreach (TableModel model in tableArray)
            {
                sb.AppendLine(model.GlobalTypeName).Append("[] ").AppendDecapitalized(model.TableName)
                    .Append("Array = System.Array.Empty<").Append(model.GlobalTypeName).Append(">();");
            }

            using (sb.AppendLine("for (int i = 0; i < count; i++)").BracketScope())
            {
                sb.AppendLine("string tableName = reader.ReadString();");
                sb.AppendLine("switch (tableName)");
                using (sb.BracketScope())
                {
                    foreach (TableModel model in tableArray)
                    {
                        using (sb.Append("case \"").Append(model.TableRawName).AppendLine("\":").BracketScope())
                        {
                            sb.AppendLine("int byteCount = reader.ReadVarIntInt32();");
                            sb.Append("if (!reader.TryReadCollectionHeader(out var itemCount)) break;");
                            {
                                sb.AppendDecapitalized(model.TableName).Append("Array").Append(" = new ")
                                    .Append(model.GlobalTypeName).Append("[itemCount];");

                                sb.Append("var formatter = reader.GetFormatter<").Append(model.GlobalTypeName)
                                    .AppendLine(">();");

                                using (sb.Append("for (int j = 0; j < itemCount; j++)").BracketScope())
                                {
                                    sb.Append("formatter.Deserialize(ref reader, ref ")
                                        .AppendDecapitalized(model.TableName).Append("Array").Append("[j]);");
                                }
                            }

                            sb.AppendLine("break;");
                        }
                    }

                    using (sb.AppendLine("default:").BracketScope())
                    {
                        sb.AppendLine("int byteCount = reader.ReadVarIntInt32();");
                        sb.AppendLine("reader.Advance(byteCount);");
                        sb.AppendLine("System.Diagnostics.Debug.WriteLine($\"Skipping unknown table: {tableName}\");");

                        sb.AppendLine("break;");
                    }
                }
            }

            using (sb.Append("if (value == null)").BracketScope())
            {
                sb.Append("value = new ").Append(name).Append('(');
                sb.AppendJoin(", ", tableArray, (sb, model) => sb.AppendDecapitalized(model.TableName).Append("Array"));

                sb.AppendLine(");");
            }

            using (sb.AppendLine("else").BracketScope())
            {
                string varName = tableArray.Length == 1 ? tableArray[0].TableName : "tables";
                using (sb.Append("value.Transaction((")
                           .AppendJoin(",",
                               tableArray,
                               (sb, model) => sb.AppendDecapitalized(model.TableName).Append("Array"))
                           .AppendLine("), (transaction, ").Append(varName).Append(") =>").BracketScope())
                {
                    sb.Append("transaction.Clear();");
                    foreach (TableModel model in tableArray)
                    {
                        sb.Append("transaction.Insert(").Append(tableArray.Length > 1, "tables.")
                            .AppendDecapitalized(model.TableName).Append("Array);");
                    }
                }

                sb.Append(");");
            }
        }
    }
}