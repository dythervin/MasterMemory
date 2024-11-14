using System;
using System.Collections.Generic;

namespace MasterMemory
{
    public partial class Table<TMainKey, TElement>
    {
        public sealed class KeyCollectionData<TKey>
        {
            public readonly Table<TMainKey, TElement> Table;
            public readonly KeySelector<TElement, (TKey key, TMainKey mainKey)> KeysSelector;
            public readonly IComparer<(TKey key, TMainKey mainKey)> Comparer;
            public readonly IComparer<(TKey key, TMainKey? mainKey)> ComparerKeyOnly;
            public readonly string KeyName;
            public readonly KeySelector<TElement, TKey> KeySelector;

            public KeyCollectionData(Table<TMainKey, TElement> table,
                KeySelector<TElement, (TKey key, TMainKey mainKey)> keysSelector,
                IComparer<(TKey key, TMainKey mainKey)> comparer,
                IComparer<(TKey key, TMainKey? mainKey)> comparerKeyOnly, string keyName,
                KeySelector<TElement, TKey> keySelector)
            {
                Table = table;
                KeysSelector = keysSelector;
                Comparer = comparer;
                ComparerKeyOnly = comparerKeyOnly;
                KeyName = keyName;
                KeySelector = keySelector;
            }
        }

        protected class KeyCollection<TKey>
        {
            private (TKey key, TMainKey mainKey)[] _keys;
            private int _count;
            private readonly KeyCollectionData<TKey> _data;

            public KeyCollection(IReadOnlyList<TElement> items, Table<TMainKey, TElement> table,
                KeySelector<TElement, TKey> keySelector,
                KeySelector<TElement, (TKey key, TMainKey mainKey)> keysSelector,
                IComparer<(TKey key, TMainKey mainKey)> comparer,
                IComparer<(TKey key, TMainKey? mainKey)> comparerKeyOnly, string keyName)
            {
                _keys = new (TKey key, TMainKey mainKey)[items.Count];
                _data = new KeyCollectionData<TKey>(table,
                    keysSelector,
                    comparer,
                    comparerKeyOnly,
                    keyName,
                    keySelector);

                for (int i = 0; i < items.Count; i++)
                {
                    _keys[i] = _data.KeysSelector(items[i]);
                }

                _count = items.Count;
            }

            public RangeView<TKey, TMainKey, TElement> GetAll(bool ascendant = true)
            {
                return new(_keys, 0, _count - 1, ascendant, _data);
            }

            public void Sort()
            {
                Array.Sort(_keys, 0, _count, _data.Comparer);
            }

            public void Execute(in OperationChange<TElement> operationChange)
            {
                if (operationChange.Type == OperationType.Clear)
                {
                    _count = 0;
                    return;
                }

                (TKey key, TMainKey mainKey) key = _data.KeysSelector(operationChange.Value);
                switch (operationChange.Type)
                {
                    case OperationType.Insert:
                    {
                        int index = Array.BinarySearch(_keys, 0, _count, key, _data.Comparer);
                        if (index >= 0)
                        {
                            throw new InvalidOperationException($"Key already exists: {key}");
                        }

                        index = ~index;
                        EnsureCapacity(_count + 1);
                        if (index < _count)
                            Array.Copy(_keys, index, _keys, index + 1, _count - index);

                        _keys[index] = key;
                        _count++;
                        break;
                    }
                    case OperationType.InsertOrReplace:
                    {
                        int index = Array.BinarySearch(_keys, 0, _count, key, _data.Comparer);
                        if (index >= 0)
                        {
                            _keys[index] = key;
                        }
                        else
                        {
                            index = ~index;
                            EnsureCapacity(_count + 1);
                            if (index < _count)
                                Array.Copy(_keys, index, _keys, index + 1, _count - index);

                            _keys[index] = key;
                            _count++;
                        }

                        break;
                    }
                    case OperationType.Replace:
                    {
                        int index = Array.BinarySearch(_keys,
                            0,
                            _count,
                            _data.KeysSelector(operationChange.Previous),
                            _data.Comparer);

                        if (index < 0)
                        {
                            throw new KeyNotFoundException(
                                $"Key not found: {_data.KeysSelector(operationChange.Previous)}");
                        }

                        _keys[index] = key;
                        break;
                    }
                    case OperationType.Remove:
                    {
                        int index = Array.BinarySearch(_keys, 0, _count, key, _data.Comparer);
                        if (index < 0)
                        {
                            throw new KeyNotFoundException($"Key not found: {key}");
                        }

                        _count--;

                        if (index != _count)
                            Array.Copy(_keys, index + 1, _keys, index, _count - index);

                        break;
                    }
                    case OperationType.None:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private void EnsureCapacity(int capacity)
            {
                if (_keys.Length >= capacity)
                    return;

                int newCapacity = _keys.Length == 0 ? 4 : _keys.Length * 2;
                if (newCapacity < capacity)
                    newCapacity = capacity;

                Array.Resize(ref _keys, newCapacity);
            }

            public TElement FindUnique(TKey key)
            {
                int index = BinarySearch.FindFirst(_keys!, (key, default), _data.ComparerKeyOnly);
                if (index != -1)
                {
                    return _data.Table[_keys[index].mainKey];
                }

                throw new KeyNotFoundException("DataType: " + typeof(TElement).FullName + ", Key: " + key);
            }

            public bool TryFindUnique(TKey key, out TElement? result)
            {
                int index = BinarySearch.FindFirst(_keys!, (key, default), _data.ComparerKeyOnly);
                if (index != -1)
                {
                    result = _data.Table[_keys[index].mainKey];
                    return true;
                }

                result = default;
                return false;
            }

            public RangeView<TKey, TMainKey, TElement> FindUniqueRange(TKey min, TKey max, bool ascendant)
            {
                int lo = BinarySearch.FindClosest(_keys!, 0, _count, (min, default), _data.ComparerKeyOnly, false);
                int hi = BinarySearch.FindClosest(_keys!, 0, _count, (max, default), _data.ComparerKeyOnly, true);

                if (lo == -1)
                    lo = 0;

                if (hi == _count)
                    hi -= 1;

                return new(_keys, lo, hi, ascendant, _data);
            }

            public TElement? FindUniqueClosest(TKey key, bool selectLower)
            {
                int index = BinarySearch.FindClosest(_keys!,
                    0,
                    _count,
                    (key, default),
                    _data.ComparerKeyOnly,
                    selectLower);

                return index != -1 ? _data.Table[_keys[index].mainKey] : default;
            }

            public RangeView<TKey, TMainKey, TElement> FindMany(TKey key, bool ascendant)
            {
                int lo = BinarySearch.LowerBound(_keys!, 0, _count, (key, default!), _data.ComparerKeyOnly);
                if (lo == -1)
                    return RangeView<TKey, TMainKey, TElement>.GetEmpty(_data);

                int hi = BinarySearch.UpperBound(_keys!, 0, _count, (key, default), _data.ComparerKeyOnly);
                if (hi == -1)
                    return RangeView<TKey, TMainKey, TElement>.GetEmpty(_data);

                return new RangeView<TKey, TMainKey, TElement>(_keys, lo, hi, ascendant, _data);
            }

            public RangeView<TKey, TMainKey, TElement> FindManyClosest(TKey key, bool selectLower, bool ascendant)
            {
                int closest = BinarySearch.FindClosest(_keys!,
                    0,
                    _count,
                    (key, default),
                    _data.ComparerKeyOnly,
                    selectLower);

                if (closest == -1 || closest >= _count)
                    return RangeView<TKey, TMainKey, TElement>.GetEmpty(_data);

                return FindMany(_keys[closest].key, ascendant);
            }

            public RangeView<TKey, TMainKey, TElement> FindManyRange(TKey min, TKey max, bool ascendant)
            {
                //... Empty set when min > max
                //... Alternatively, could treat this as between and swap min and max.

                if (Comparer<TKey>.Default.Compare(min, max) > 0)
                    return RangeView<TKey, TMainKey, TElement>.GetEmpty(_data);

                //... want lo to be the lowest  index of the values >= than min.
                //... lo should be in the range [0,arraylength]

                int lo = BinarySearch.LowerBoundClosest(_keys!, 0, _count, (min, default), _data.ComparerKeyOnly);

                //... want hi to be the highest index of the values <= than max
                //... hi should be in the range [-1,arraylength-1]

                int hi = BinarySearch.UpperBoundClosest(_keys!, 0, _count, (max, default), _data.ComparerKeyOnly);
                if (lo < 0)
                    throw new InvalidOperationException("lo is less than 0");

                if (hi >= _count)
                    throw new InvalidOperationException("hi is greater than or equal to keys length");

                if (hi < lo)
                    return RangeView<TKey, TMainKey, TElement>.GetEmpty(_data);

                return new RangeView<TKey, TMainKey, TElement>(_keys, lo, hi, ascendant, _data);
            }
        }
    }
}