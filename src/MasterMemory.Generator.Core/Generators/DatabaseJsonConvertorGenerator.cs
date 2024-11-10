using System.Collections.Immutable;
using System.Text;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;
using MasterMemory.Annotations;

namespace MasterMemory.Generator.Core;

internal static class DatabaseJsonConvertorGenerator
{
    public static void Execute(SourceGeneratorContext context, ImmutableArray<TableModel> tableArray,
        DatabaseModel database, StringBuilder sb)
    {
        if (!database.HasFlag(DatabaseFlags.SystemTextJson) && !database.HasFlag(DatabaseFlags.NewtonsoftJson))
            return;

        var name = database.Name;
        sb.AppendUsings(tableArray[0].DatabaseModel);
        using (sb.NamespaceScope(database.Namespace))
        {
            sb.Append(database.AccessibilityModifier);
            sb.Append(" class ").Append(name).Append("JsonConvertor : ");
            if (database.HasFlag(DatabaseFlags.SystemTextJson))
            {
                sb.Append("System.Text.Json.Serialization.JsonConverter<").Append(name).Append(">");
            }
            else
            {
                sb.Append("Newtonsoft.Json.JsonConverter");
            }

            using (sb.BracketScope())
            {
                if (database.HasFlag(DatabaseFlags.NewtonsoftJson))
                    sb.Append("public override bool CanConvert(System.Type objectType) => objectType == typeof(")
                        .Append(name).Append(");");

                sb.AppendRead(tableArray, database, name);

                sb.AppendWrite(tableArray, database, name);

                sb.AppendAssertJsonTokenType(database);
            }
        }

        if (!database.HasFlag(DatabaseFlags.SystemTextJson))
        {
            sb.Replace("JsonTokenType", "JsonToken");
            sb.Replace("reader.GetString()", "reader.Value.ToString()");
        }

        context.AddSource($"Db.{name}JsonConvertor.g.cs", sb.ToStringAndClear());
    }

    private static void AppendRead(this StringBuilder sb, ImmutableArray<TableModel> tableArray, DatabaseModel database,
        string name)
    {
        sb.Append("public override ");
        if (database.HasFlag(DatabaseFlags.SystemTextJson))
        {
            sb.Append(name)
                .Append(" Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)");
        }
        else
        {
            sb.Append(
                "object ReadJson(JsonReader reader, System.Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)");
        }

        using (sb.BracketScope())
        {
            sb.Append("AssertJsonTokenType(reader.TokenType, JsonTokenType.StartObject);");

            foreach (TableModel model in tableArray)
            {
                sb.Append("var ").AppendDecapitalized(model.TableName).Append("Array = System.Array.Empty<")
                    .Append(model.GlobalTypeName).Append(">();");
            }

            sb.Append("AssertReadJsonTokenType(").Append(database.HasFlag(DatabaseFlags.SystemTextJson), " ref ")
                .Append("reader, JsonTokenType.PropertyName);");

            using (sb.Append("if (reader.GetString() == \"tables\")").BracketScope())
            {
                sb.Append("AssertReadJsonTokenType(").Append(database.HasFlag(DatabaseFlags.SystemTextJson), " ref ")
                    .Append("reader, JsonTokenType.StartObject);");

                using (sb.BracketScope())
                {
                    using (sb.Append("while (reader.Read())").BracketScope())
                    {
                        sb.Append("if (reader.TokenType == JsonTokenType.EndObject)");
                        sb.Append("break;");

                        using (sb.Append("if (reader.TokenType == JsonTokenType.PropertyName)").BracketScope())
                        {
                            sb.Append("var tableName = reader.GetString();");
                            using (sb.Append("if (tableName == null)").BracketScope())
                            {
                                sb.Append("reader.Skip();");
                                sb.Append("continue;");
                            }

                            using (sb.Append("switch (tableName)").BracketScope())
                            {
                                foreach (TableModel model in tableArray)
                                {
                                    using (sb.Append("case \"").Append(model.TableRawName).Append("\":").BracketScope())
                                    {
                                        AppendReadTable(sb, model, database);
                                    }
                                }

                                sb.Append("default:");
                                sb.Append("reader.Skip();");
                                sb.Append("break;");
                            }
                        }
                    }
                }
            }

            sb.Append("AssertReadJsonTokenType(").Append(database.HasFlag(DatabaseFlags.SystemTextJson), " ref ")
                .Append("reader, JsonTokenType.EndObject);");

            if (!database.HasFlag(DatabaseFlags.SystemTextJson))
            {
                using (sb.Append("if (existingValue is ").Append(name).Append(" existing)").BracketScope())
                {
                    string varName = tableArray.Length == 1 ? tableArray[0].TableName : "tables";
                    using (sb.Append("existing.Transaction((")
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

                    sb.Append("return existing;");
                }
            }

            sb.Append("return new ").Append(name).Append('(').AppendJoin(", ",
                tableArray,
                static (sb, model) => sb.AppendDecapitalized(model.TableName).Append("Array")).Append(");");
        }

        sb.Append("private T[] ReadItems<T>(").Append(database.HasFlag(DatabaseFlags.SystemTextJson) ?
            "ref Utf8JsonReader reader, JsonSerializerOptions options)" :
            "JsonReader reader, Newtonsoft.Json.JsonSerializer serializer)");

        using (sb.BracketScope())
        {
            sb.Append("AssertReadJsonTokenType(").Append(database.HasFlag(DatabaseFlags.SystemTextJson), " ref ")
                .Append("reader, JsonTokenType.StartObject);");

            sb.Append("AssertReadJsonTokenType(").Append(database.HasFlag(DatabaseFlags.SystemTextJson), " ref ")
                .Append("reader, JsonTokenType.PropertyName);");

            sb.Append("if (reader.GetString() != \"items\")");
            sb.Append("throw new JsonException(\"Expected items property.\");");

            sb.Append("AssertReadJsonTokenType(").Append(database.HasFlag(DatabaseFlags.SystemTextJson), " ref ")
                .Append("reader, JsonTokenType.StartArray);");

            sb.Append(database.HasFlag(DatabaseFlags.SystemTextJson) ?
                "return JsonSerializer.Deserialize<T[]>(ref reader, options);" :
                "return serializer.Deserialize<T[]>(reader);");
        }
    }

    private static void AppendWrite(this StringBuilder sb, ImmutableArray<TableModel> tableArray,
        DatabaseModel database, string name)
    {
        sb.Append("public override void ");
        if (database.HasFlag(DatabaseFlags.SystemTextJson))
        {
            sb.Append("Write(Utf8JsonWriter writer, ").Append(name).Append(" database, JsonSerializerOptions options)");
        }
        else
        {
            sb.Append("WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)");
        }

        using (sb.BracketScope())
        {
            if (!database.HasFlag(DatabaseFlags.SystemTextJson))
            {
                using (sb.Append("if (value == null)").BracketScope())
                {
                    sb.Append("writer.WriteNull();");
                    sb.Append("return;");
                }

                sb.Append("var database = (").Append(name).Append(")value;");
            }

            sb.Append("if (database.Transaction.IsInProgress)");
            sb.Append(
                "throw new System.InvalidOperationException(\"Cannot serialize database while transaction is in progress.\");");

            sb.Append("writer.WriteStartObject();");
            sb.AppendWriteStart(database, "Object", "tables");
            foreach (var table in tableArray)
            {
                using (sb.BracketScope())
                {
                    sb.AppendWriteStart(database, "Object", table.TableRawName);
                    sb.Append("var allSorted = database.").AppendCapitalized(table.TableName)
                        .Append(".GetAllSorted();");

                    sb.Append("WriteItems(").Append(database.HasFlag(DatabaseFlags.SystemTextJson) ?
                        "writer, allSorted, options);" :
                        "writer, allSorted, serializer);");

                    sb.Append("writer.WriteEndObject();");
                }
            }

            sb.Append("writer.WriteEndObject();");
            sb.Append("writer.WriteEndObject();");
        }

        sb.Append("private void WriteItems<T>(").Append(database.HasFlag(DatabaseFlags.SystemTextJson) ?
            "Utf8JsonWriter writer, IEnumerable<T> items,  JsonSerializerOptions options)" :
            "JsonWriter writer, IEnumerable<T> items, Newtonsoft.Json.JsonSerializer serializer)");

        using (sb.BracketScope())
        {
            sb.AppendWriteStart(database, "Array", "items");
            using (sb.Append("foreach (var item in items)").BracketScope())
            {
                sb.Append(database.HasFlag(DatabaseFlags.SystemTextJson) ?
                    "JsonSerializer.Serialize(writer, item, options);" :
                    "serializer.Serialize(writer, item);");
            }

            sb.Append("writer.WriteEndArray();");
        }
    }

    private static void AppendAssertJsonTokenType(this StringBuilder sb, DatabaseModel database)
    {
        using (sb.Append("private void AssertJsonTokenType(JsonTokenType actual, JsonTokenType expected)")
                   .BracketScope())
        {
            sb.Append("if (actual != expected)");
            sb.Append("throw new JsonException($\"Expected {expected}. Got {actual}\");");
        }

        using (sb.Append("private void AssertReadJsonTokenType(")
                   .Append(database.HasFlag(DatabaseFlags.SystemTextJson) ?
                       "ref Utf8JsonReader reader" :
                       "JsonReader reader").Append(", JsonTokenType expected)").BracketScope())
        {
            sb.Append("if (!reader.Read())");
            sb.Append("throw new JsonException(\"Unexpected end of JSON input.\");");

            sb.Append("AssertJsonTokenType(reader.TokenType, expected);");
        }
    }

    private static StringBuilder AppendWriteStart(this StringBuilder sb, DatabaseModel database, string type,
        string? value = null, bool withQuotes = true)
    {
        if (value == null)
        {
            sb.Append("writer.WriteStart").Append(type).Append("();");
            return sb;
        }

        sb.Append("writer.WritePropertyName(");
        using (sb.Scope(withQuotes, "\"", "\""))
        {
            sb.Append(value);
        }

        sb.Append(");");
        return sb.Append("writer.WriteStart").Append(type).Append("();");
    }

    private static void AppendReadTable(StringBuilder sb, TableModel model, DatabaseModel database)
    {
        sb.AppendDecapitalized(model.TableName).Append("Array = ReadItems<").Append(model.GlobalTypeName).Append(">(");
        sb.Append(database.HasFlag(DatabaseFlags.SystemTextJson) ? "ref reader, options);" : "reader, serializer);");

        sb.Append("AssertReadJsonTokenType(").Append(database.HasFlag(DatabaseFlags.SystemTextJson), " ref ")
            .Append("reader, JsonTokenType.EndObject);");

        sb.Append("break;");
    }
}