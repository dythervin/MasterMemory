using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using MasterMemory.Annotations;
using Microsoft.CodeAnalysis;

namespace MasterMemory.Generator.Core.Models;

public readonly record struct TableModel
{
    private static readonly KeyModelEqualityComparer EqualityComparer = new();

    public readonly string GlobalTypeName;
    public readonly ImmutableArray<KeyGroupModel> KeyGroups;
    public readonly ImmutableArray<KeyModel> Keys;
    public readonly ImmutableArray<KeyGroupModel> UniqueKeys;
    public readonly string TableName;
    public readonly string TableFullName;
    public readonly string TableGlobalName;
    public readonly string ITableGlobalName;
    public readonly string TableRawName;
    public readonly DbTableFlags Flags;
    public readonly int ThreadBatchSize;
    public readonly int ThreadCount;
    public readonly string TypeFullName;
    public readonly string Namespace;

    public DatabaseModel DatabaseModel { get; init; }

    public string TypeName { get; }

    public bool IsMultithreaded => IsMultithreadedInitialization || IsMultithreadedModifications;

    public bool IsMultithreadedInitialization =>
        Flags.HasFlagFast(DbTableFlags.MultithreadedInitialization) && DatabaseModel.IsMultithreaded;

    public bool IsMultithreadedModifications =>
        Flags.HasFlagFast(DbTableFlags.MultithreadedModifications) && DatabaseModel.IsMultithreaded;

    public KeyGroupModel PrimaryKey => KeyGroups[0];

    public TableModel(INamedTypeSymbol typeSymbol, ImmutableArray<KeyGroupModel> keys,
        ImmutableArray<KeyGroupModel> uniqueKeys, string tableName, DbTableFlags flags, int threadBatchSize)
    {
        TableRawName = tableName;
        TableName = Format(TableRawName);
        Flags = flags;
        TypeName = typeSymbol.Name;
        TypeFullName = typeSymbol.ToDisplayString();
        GlobalTypeName = "global::" + TypeFullName;
        Namespace = typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } ?
            typeSymbol.ContainingNamespace.ToDisplayString() :
            string.Empty;

        TableFullName = string.IsNullOrEmpty(Namespace) ? TableName : Namespace + "." + TableName;
        TableGlobalName = "global::" + TableFullName;
        ITableGlobalName =
            "global::" + (string.IsNullOrEmpty(Namespace) ? "I" + TableName : Namespace + ".I" + TableName);

        KeyGroups = keys;
        Keys = KeyGroups.SelectMany(x => x.Keys).Distinct(EqualityComparer).ToImmutableArray();

        UniqueKeys = uniqueKeys;

        ThreadCount =
            Math.Max((int)Math.Ceiling(KeyGroups.Length / (double)Math.Min(KeyGroups.Length, threadBatchSize)), 1);

        ThreadBatchSize = KeyGroups.Length / ThreadCount;
    }

    public bool Equals(TableModel other)
    {
        return GlobalTypeName == other.GlobalTypeName && Keys.SequenceEqual(other.Keys) &&
               UniqueKeys.SequenceEqual(other.UniqueKeys) && TableRawName == other.TableRawName &&
               Flags == other.Flags && ThreadBatchSize == other.ThreadBatchSize && ThreadCount == other.ThreadCount &&
               TypeFullName == other.TypeFullName && Namespace == other.Namespace &&
               DatabaseModel.Equals(other.DatabaseModel) && TypeName == other.TypeName;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = GlobalTypeName.GetHashCode();
            hashCode = (hashCode * 397) ^ Keys.GetHashCode();
            hashCode = (hashCode * 397) ^ UniqueKeys.GetHashCode();
            hashCode = (hashCode * 397) ^ TableRawName.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)Flags;
            hashCode = (hashCode * 397) ^ ThreadBatchSize;
            hashCode = (hashCode * 397) ^ ThreadCount;
            hashCode = (hashCode * 397) ^ TypeFullName.GetHashCode();
            hashCode = (hashCode * 397) ^ Namespace.GetHashCode();
            hashCode = (hashCode * 397) ^ DatabaseModel.GetHashCode();
            hashCode = (hashCode * 397) ^ TypeName.GetHashCode();
            return hashCode;
        }
    }

    private static string Format(string tableName)
    {
        StringBuilder sb = new(tableName);
        sb.Replace("_", string.Empty);
        sb[0] = char.ToUpperInvariant(sb[0]);
        return sb.Append("Table").ToString();
    }

    private class KeyModelEqualityComparer : IEqualityComparer<KeyModel>
    {
        public bool Equals(KeyModel x, KeyModel y)
        {
            return x.Name.Equals(y.Name, StringComparison.Ordinal);
        }

        public int GetHashCode(KeyModel obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}