using System;
using System.Collections;
using System.Collections.Generic;

namespace MasterMemory
{
    public readonly struct RangeView<TKey, TMainKey, TValue> : IReadOnlyList<TValue>, IList<TValue>
    {
        private readonly (TKey key, TMainKey primaryKey)[] _orderedData;

        private readonly int _left;
        private readonly int _right;
        private readonly bool _ascendant;
        private readonly bool _hasValue;
        private readonly int _version;

        private readonly Table<TMainKey, TValue>.KeyCollectionData<TKey> _keyCollectionData;

        public TValue this[int index] => _keyCollectionData.Table[GetKeys(index).primaryKey];

        TValue IList<TValue>.this[int index]
        {
            get => this[index];
            set => throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                AssertVersion();
                return CountUnsafe;
            }
        }

        public ITable<TMainKey, TValue> Table => _keyCollectionData.Table;

        public RangeView<TKey, TMainKey, TValue> Reverse =>
            new(_orderedData, _left, _right, !_ascendant, _keyCollectionData);

        public string ElementName => _keyCollectionData.Table.ElementName;

        public string KeyName => _keyCollectionData.KeyName;

        public TValue First => this[0];

        public TValue Last => this[CountUnsafe - 1];

        internal int FirstIndex => _ascendant ? _left : _right;

        internal int LastIndex => _ascendant ? _right : _left;

        bool ICollection<TValue>.IsReadOnly => true;

        private int CountUnsafe => !_hasValue ? 0 : _right - _left + 1;

        public RangeView((TKey key, TMainKey primaryKey)[] orderedData, int left, int right, bool ascendant,
            Table<TMainKey, TValue>.KeyCollectionData<TKey> keyCollectionData)
        {
            _keyCollectionData = keyCollectionData;
            _hasValue = orderedData.Length != 0 && left <= right;

            _orderedData = orderedData;
            _left = left;
            _right = right;
            _ascendant = ascendant;
            _version = _keyCollectionData.Table.Version;
        }

        public static RangeView<TKey, TMainKey, TValue> GetEmpty(
            Table<TMainKey, TValue>.KeyCollectionData<TKey> keyCollectionData)
        {
            return new(Array.Empty<(TKey key, TMainKey primaryKey)>(), 0, -1, default, keyCollectionData);
        }

        public (TKey key, TMainKey primaryKey) GetKeys(int index)
        {
            if (!_hasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Empty");
            }

            AssertVersion();

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "index < 0");
            }

            if (CountUnsafe <= index)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "index >= Count");
            }

            return _orderedData[_ascendant ? _left + index : _right - index];
        }

        public RangeView<TKey, TMainKey, TValue> Slice(int start, int count)
        {
            return Slice(start, count, _ascendant);
        }

        public RangeView<TKey, TMainKey, TValue> Slice(int start, int count, bool ascendant)
        {
            if (count == 0)
            {
                return GetEmpty(_keyCollectionData);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return new(_orderedData, _left + start, _left + start + count - 1, ascendant, _keyCollectionData);
        }

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        public bool Any()
        {
            AssertVersion();
            return CountUnsafe != 0;
        }

        public int IndexOfFirst(in TValue item)
        {
            AssertVersion();
            int count = CountUnsafe;
            if (count == 0)
            {
                return -1;
            }

            return BinarySearch.LowerBound<(TKey key, TMainKey? primaryKey)>(_orderedData!,
                _left,
                _right + 1,
                (_keyCollectionData.KeySelector(item), default!),
                _keyCollectionData.ComparerKeyOnly);
        }

        public int IndexOfLast(in TValue item)
        {
            AssertVersion();
            int count = CountUnsafe;
            if (count == 0)
            {
                return -1;
            }

            return BinarySearch.UpperBound<(TKey key, TMainKey? primaryKey)>(_orderedData!,
                _left,
                _right + 1,
                (_keyCollectionData.KeySelector(item), default!),
                _keyCollectionData.ComparerKeyOnly);
        }

        /// <summary>
        ///     O(N) search.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(in TValue item)
        {
            return CountUnsafe != 0 && ContainsKey(_keyCollectionData.KeySelector(item));
        }

        /// <summary>
        ///     O(log N) search.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int IndexOfFirst(TKey key)
        {
            AssertVersion();
            int count = CountUnsafe;
            if (count == 0)
            {
                return -1;
            }

            return BinarySearch.LowerBound<(TKey key, TMainKey? primaryKey)>(_orderedData!,
                _left,
                _right + 1,
                (key, default),
                _keyCollectionData.ComparerKeyOnly);
        }

        /// <summary>
        ///     O(log N) search.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int IndexOfLast(TKey key)
        {
            AssertVersion();
            int count = CountUnsafe;
            if (count == 0)
            {
                return -1;
            }

            return BinarySearch.UpperBound<(TKey key, TMainKey? primaryKey)>(_orderedData!,
                _left,
                _right + 1,
                (key, default),
                _keyCollectionData.ComparerKeyOnly);
        }

        /// <summary>
        ///     O(log N) search.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return IndexOfFirst(key) >= 0;
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            AssertVersion();
            int count = CountUnsafe;
            for (int i = 0; i < count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        private void AssertVersion()
        {
            if (_version != _keyCollectionData.Table.Version)
            {
                throw new InvalidOperationException("Table modified");
            }
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        bool ICollection<TValue>.Contains(TValue item)
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                TValue v = this[i];
                if (EqualityComparer<TValue>.Default.Equals(v, item))
                {
                    return true;
                }
            }

            return false;
        }

        int IList<TValue>.IndexOf(TValue item)
        {
            return IndexOfFirst(item);
        }

        void IList<TValue>.Insert(int index, TValue item)
        {
            throw new NotSupportedException();
        }

        void IList<TValue>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void ICollection<TValue>.Add(TValue item)
        {
            throw new NotSupportedException();
        }

        void ICollection<TValue>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<TValue>.Remove(TValue item)
        {
            throw new NotSupportedException();
        }

        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly RangeView<TKey, TMainKey, TValue> _rangeView;
            private int _index;

            public TValue Current => _rangeView[_index];

            object IEnumerator.Current => Current!;

            public Enumerator(in RangeView<TKey, TMainKey, TValue> rangeView)
            {
                _rangeView = rangeView;
                _index = -1;
            }

            public bool MoveNext()
            {
                return ++_index < _rangeView.Count;
            }

            public void Reset()
            {
                _index = -1;
            }

            public void Dispose()
            {
            }
        }
    }
}