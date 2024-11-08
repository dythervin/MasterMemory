using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MasterMemory.Generator.Models;

internal record KeyGroupModel : IReadOnlyList<KeyModel>
{
    public bool IsGroupUnique { get; init; }

    public ImmutableArray<KeyModel> Keys { get; init; }

    public string Modifier { get; init; }

    public string Name { get; init; }

    public string Type { get; init; }

    public KeyModel this[int index] => Keys[index];

    public bool IsAnyKeyUnique => Keys.Any(k => k.IsUnique);

    public bool IsNonUnique => !IsAnyKeyUnique && !IsGroupUnique;

    public bool IsNullable => Keys.Length == 1 && Keys[0].CanBeNull;

    public bool IsSingle => Keys.Length == 1;

    public int Count => Keys.Length;

    public KeyGroupModel(params KeyModel[] keys)
    {
        if (keys.Length == 1)
        {
            KeyModel key = keys[0];
            Type = key.Type.ToDisplayString();
            Name = key.Symbol.Name;
        }
        else
        {
            Name = string.Join("And", keys.Select(k => k.Symbol.Name));
            Array.Sort(keys, (a, b) => a.KeyOrder.CompareTo(b.KeyOrder));

            Type = Concat(keys);
        }

        Keys = keys.ToImmutableArray();

        Modifier = GetModifier(Keys);
    }

    public virtual bool Equals(KeyGroupModel other)
    {
        return Keys.SequenceEqual(other.Keys) && Name == other.Name && Type == other.Type;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Keys.GetHashCode();
            hashCode = (hashCode * 397) ^ Name.GetHashCode();
            hashCode = (hashCode * 397) ^ Type.GetHashCode();
            return hashCode;
        }
    }

    private static string Concat(IEnumerable<KeyModel> keys)
    {
        return string.Concat("(",
            string.Join(", ", keys.Select(k => string.Concat(k.Type.ToDisplayString(), " ", k.Symbol.Name))),
            ")");
    }

    private static string GetModifier(ImmutableArray<KeyModel> keys)
    {
        int keySize = 0;
        foreach (KeyModel key in keys)
        {
            if (key.Type.SpecialType.TryGetSize(out int size))
            {
                keySize += size;
            }
            else
            {
                return "";
            }
        }

        return keySize <= 64 ? "" : "in ";
    }

    public bool IsNullableStruct(out INamedTypeSymbol typeArgument)
    {
        if (Keys.Length != 1)
        {
            typeArgument = null;
            return false;
        }

        return Keys[0].IsNullableStruct(out typeArgument);
    }

    public ImmutableArray<KeyModel>.Enumerator GetEnumerator()
    {
        return Keys.GetEnumerator();
    }

    IEnumerator<KeyModel> IEnumerable<KeyModel>.GetEnumerator()
    {
        return ((IEnumerable<KeyModel>)Keys).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Keys).GetEnumerator();
    }
}