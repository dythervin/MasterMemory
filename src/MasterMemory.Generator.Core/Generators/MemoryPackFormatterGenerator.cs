using System.Text;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;
using Microsoft.CodeAnalysis.CSharp;
using MasterMemory.Annotations;

namespace MasterMemory.Generator.Core;

internal static class MemoryPackFormatterGenerator
{
    public static void Execute(SourceGeneratorContext context, DatabaseModel database,
        ParseOptionsModel parseOptionsModel, StringBuilder sb)
    {
        if (!database.HasFlag(DatabaseFlags.MemoryPack))
            return;

        var name = database.Name;
        string scoped = parseOptionsModel.LanguageVersion >= (LanguageVersion)1100 ? "scoped " : string.Empty;
        sb.AppendUsings(database);
        using (sb.NamespaceScope(database.Namespace))
        {
            sb.Append(database.AccessibilityModifier);
            using (sb.Append(" class ").Append(name).Append("Formatter : MemoryPackFormatter<").Append(name).Append(">")
                       .BracketScope())
            {
                using (sb.AppendLine(
                               "public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ")
                           .Append(scoped).Append("ref ").Append(name).Append(" value)").BracketScope())
                {
                    sb.AppendLine($"{name}.Serialize(ref writer, ref value);");
                }

                using (sb.AppendLine("public override void Deserialize(ref MemoryPackReader reader, ").Append(scoped)
                           .Append(" ref ").Append(name).Append(" value)").BracketScope())
                {
                    sb.AppendLine($"{name}.Deserialize(ref reader, ref value);");
                }
            }
        }

        context.AddSource($"Db.{name}Formatter.g.cs", sb.ToStringAndClear());
    }
}