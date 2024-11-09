using System;
using Microsoft.CodeAnalysis;

namespace MasterMemory.Generator.Core.Models;

public readonly struct KeyModel : IEquatable<KeyModel>
{
    public bool IsUnique { get; init; }

    public int KeyOrder { get; init; }

    public ISymbol Symbol { get; init; }

    public uint? GroupIndex { get; init; }

    public bool CanBeNull =>
        Type.NullableAnnotation == NullableAnnotation.Annotated || Type.IsReferenceType ||
        Type.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;

    public INamedTypeSymbol Type =>
        Symbol switch
        {
            IFieldSymbol field => (INamedTypeSymbol)field.Type,
            IPropertySymbol property => (INamedTypeSymbol)property.Type,
            _ => throw new NotSupportedException()
        };

    public string Name => Symbol.Name;

    public KeyModel(ISymbol symbol, uint? groupIndex, bool isUnique, int keyOrder)
    {
        Symbol = symbol;
        GroupIndex = groupIndex;
        IsUnique = isUnique;
        KeyOrder = keyOrder;
    }

    public bool Equals(KeyModel other)
    {
        return GroupIndex == other.GroupIndex && SymbolEqualityComparer.Default.Equals(Symbol, other.Symbol) && KeyOrder == other.KeyOrder &&
               IsUnique == other.IsUnique;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Symbol != null ? Symbol.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ GroupIndex.GetHashCode();
            return hashCode;
        }
    }

    public bool IsNullableStruct(out INamedTypeSymbol typeArgument)
    {
        INamedTypeSymbol type = Type;
        if (type.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            typeArgument = (INamedTypeSymbol)type.TypeArguments[0];
            return true;
        }

        typeArgument = null!;
        return false;
    }
}