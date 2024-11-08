using System;

namespace MasterMemory
{
    public interface ITable
    {
        int Count { get; }

        string ElementName { get; }

        string KeyName { get; }

        string TableName { get; }

        Type ItemType { get; }

        Type KeyType { get; }
    }

    public interface ITable<TKey, TValue> : ITable
    {
        TValue this[TKey sortedKey] { get; }

        KeySelector<TValue, TKey> KeySelector { get; }

        RangeView<TKey, TKey, TValue> GetAllSorted(bool ascendant = true);

        bool TryGetValue(TKey key, out TValue value);

        bool ContainsKey(TKey key);
    }
}