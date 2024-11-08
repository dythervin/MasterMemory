using System;
using System.Collections.Generic;

namespace MasterMemory
{
    public partial class Table<TMainKey, TElement>
    {
        protected class KeyCollection<TKey>
        {
            private (TKey key, TMainKey mainKey)[] _keys;
            private int _count;
            private readonly IComparer<(TKey key, TMainKey mainKey)> _comparer;
            private readonly Table<TMainKey, TElement> _table;
            private readonly KeySelector<TElement, (TKey key, TMainKey mainKey)> _keySelector;
            private readonly IComparer<(TKey key, TMainKey? mainKey)> _comparerKeyOnly;
            private readonly string _keyName;

            public KeyCollection(IReadOnlyList<TElement> items,
                Table<TMainKey, TElement> table,
                KeySelector<TElement, (TKey key, TMainKey mainKey)> keySelector,
                IComparer<(TKey key, TMainKey mainKey)> comparer, IComparer<(TKey key, TMainKey? mainKey)> comparerKeyOnly, string keyName)
            {
                _comparer = comparer;
                _comparerKeyOnly = comparerKeyOnly;
                _keyName = keyName;
                _table = table;
                _keySelector = keySelector;
                _keys = new (TKey key, TMainKey mainKey)[items.Count];
                for (int i = 0; i < items.Count; i++)
                {
                    _keys[i] = _keySelector(items[i]);
                }

                _count = items.Count;
            }

            public RangeView<TKey, TMainKey, TElement> GetAll(bool ascendant = true)
            {
                return new(_keys, 0, _count - 1, ascendant, _table, _comparerKeyOnly, _keyName);
            }

            public void Sort()
            {
                Array.Sort(_keys, _comparer);
            }

            public void Execute(in OperationChange<TElement> operationChange)
            {
                (TKey key, TMainKey mainKey) key = _keySelector(operationChange.Item);
                switch (operationChange.Type)
                {
                    case OperationType.Insert:
                    {
                        int index = Array.BinarySearch(_keys, 0, _count, key, _comparer);
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
                        int index = Array.BinarySearch(_keys, 0, _count, key, _comparer);
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
                            _keySelector(operationChange.Previous),
                            _comparer);

                        if (index < 0)
                        {
                            throw new KeyNotFoundException($"Key not found: {_keySelector(operationChange.Previous)}");
                        }

                        _keys[index] = key;
                        break;
                    }
                    case OperationType.Remove:
                    {
                        int index = Array.BinarySearch(_keys, 0, _count, key, _comparer);
                        if (index < 0)
                        {
                            throw new KeyNotFoundException($"Key not found: {key}");
                        }

                        _count--;

                        if (index != _count)
                            Array.Copy(_keys, index + 1, _keys, index, _count - index);

                        break;
                    }
                    case OperationType.Clear:
                    {
                        _count = 0;
                        break;
                    }
                    case OperationType.None:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private void EnsureCapacity(int capacity)
            {
                if (_keys.Length < capacity)
                {
                    int newCapacity = _keys.Length == 0 ? 4 : _keys.Length * 2;
                    if (newCapacity < capacity)
                        newCapacity = capacity;

                    Array.Resize(ref _keys, newCapacity);
                }
            }

            public TElement FindUnique(TKey key)
            {
                int index = BinarySearch.FindFirst(_keys!, (key, default), _comparerKeyOnly);
                if (index != -1)
                {
                    return _table[_keys[index].mainKey];
                }

                throw new KeyNotFoundException("DataType: " + typeof(TElement).FullName + ", Key: " + key);
            }

            public bool TryFindUnique(TKey key, out TElement? result)
            {
                int index = BinarySearch.FindFirst(_keys!, (key, default), _comparerKeyOnly);
                if (index != -1)
                {
                    result = _table[_keys[index].mainKey];
                    return true;
                }

                result = default;
                return false;
            }

            public RangeView<TKey, TMainKey, TElement> FindUniqueRange(TKey min, TKey max, bool ascendant)
            {
                int lo = BinarySearch.FindClosest(_keys!, 0, _keys.Length, (min, default), _comparerKeyOnly, false);
                int hi = BinarySearch.FindClosest(_keys!, 0, _keys.Length, (max, default), _comparerKeyOnly, true);

                if (lo == -1)
                    lo = 0;

                if (hi == _keys.Length)
                    hi -= 1;

                return new(_keys, lo, hi, ascendant, _table, _comparerKeyOnly, _keyName);
            }

            public TElement? FindUniqueClosest(TKey key, bool selectLower)
            {
                int index = BinarySearch.FindClosest(_keys!,
                    0,
                    _keys.Length,
                    (key, default),
                    _comparerKeyOnly,
                    selectLower);

                return index != -1 ? _table[_keys[index].mainKey] : default;
            }

            public RangeView<TKey, TMainKey, TElement> FindMany(TKey key, bool ascendant)
            {
                int lo = BinarySearch.LowerBound(_keys!, 0, _keys.Length, (key, default!), _comparerKeyOnly);
                if (lo == -1)
                    return RangeView<TKey, TMainKey, TElement>.GetEmpty(_keys, _table, _keyName);

                int hi = BinarySearch.UpperBound(_keys!, 0, _keys.Length, (key, default), _comparerKeyOnly);
                if (hi == -1)
                    return RangeView<TKey, TMainKey, TElement>.GetEmpty(_keys, _table, _keyName);

                return new RangeView<TKey, TMainKey, TElement>(_keys,
                    lo,
                    hi,
                    ascendant,
                    _table,
                    _comparerKeyOnly,
                    _keyName);
            }

            public RangeView<TKey, TMainKey, TElement> FindManyClosest(TKey key, bool selectLower, bool ascendant)
            {
                int closest = BinarySearch.FindClosest(_keys!,
                    0,
                    _keys.Length,
                    (key, default),
                    _comparerKeyOnly,
                    selectLower);

                if (closest == -1 || closest >= _keys.Length)
                    return RangeView<TKey, TMainKey, TElement>.GetEmpty(_keys, _table, _keyName);

                return FindMany(_keys[closest].key, ascendant);
            }

            public RangeView<TKey, TMainKey, TElement> FindManyRange(TKey min, TKey max, bool ascendant)
            {
                //... Empty set when min > max
                //... Alternatively, could treat this as between and swap min and max.

                if (Comparer<TKey>.Default.Compare(min, max) > 0)
                    return RangeView<TKey, TMainKey, TElement>.GetEmpty(_keys, _table, _keyName);

                //... want lo to be the lowest  index of the values >= than min.
                //... lo should be in the range [0,arraylength]

                int lo = BinarySearch.LowerBoundClosest(_keys!, 0, _keys.Length, (min, default), _comparerKeyOnly);

                //... want hi to be the highest index of the values <= than max
                //... hi should be in the range [-1,arraylength-1]

                int hi = BinarySearch.UpperBoundClosest(_keys!, 0, _keys.Length, (max, default), _comparerKeyOnly);
                if (lo < 0)
                    throw new InvalidOperationException("lo is less than 0");

                if (hi >= _keys.Length)
                    throw new InvalidOperationException("hi is greater than or equal to keys length");

                if (hi < lo)
                    return RangeView<TKey, TMainKey, TElement>.GetEmpty(_keys, _table, _keyName);

                return new RangeView<TKey, TMainKey, TElement>(_keys,
                    lo,
                    hi,
                    ascendant,
                    _table,
                    _comparerKeyOnly,
                    _keyName);
            }
        }
    }
}