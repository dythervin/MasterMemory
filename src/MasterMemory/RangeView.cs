using System;
using System.Collections;
using System.Collections.Generic;

namespace MasterMemory
{
    public readonly struct RangeView<TKey, TMainKey, TValue> : IReadOnlyList<TValue>, IList<TValue>
    {
        private readonly Table<TMainKey, TValue> _table;
        private readonly (TKey key, TMainKey primaryKey)[] _orderedData;
        private readonly IComparer<(TKey key, TMainKey? primaryKey)> _comparerKeyOnly;
        private readonly int _left;
        private readonly int _right;
        private readonly bool _ascendant;
        private readonly bool _hasValue;
        private readonly int _version;
        public readonly string KeyName;

        public ITable<TMainKey, TValue> Table => _table;
        
        public string ElementName => _table.ElementName;

        public int Count => !_hasValue ? 0 : _right - _left + 1;

        public TValue First => this[0];

        public TValue Last => this[Count - 1];

        public RangeView<TKey, TMainKey, TValue> Reverse =>
            new RangeView<TKey, TMainKey, TValue>(_orderedData,
                _left,
                _right,
                !_ascendant,
                _table,
                _comparerKeyOnly,
                KeyName);

        internal int FirstIndex => _ascendant ? _left : _right;

        internal int LastIndex => _ascendant ? _right : _left;

        bool ICollection<TValue>.IsReadOnly => true;

        public (TKey key, TMainKey primaryKey) GetKeys(int index)
        {
            if (!_hasValue)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Empty");

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, "index < 0");

            if (Count <= index)
                throw new ArgumentOutOfRangeException(nameof(index), index, "index >= Count");

            if (_version != _table.Version)
                throw new InvalidOperationException("Table modified");

            return _orderedData[_ascendant ? _left + index : _right - index];
        }

        public TValue this[int index] => _table[GetKeys(index).primaryKey];

        public RangeView((TKey key, TMainKey primaryKey)[] orderedData, int left, int right, bool ascendant,
            Table<TMainKey, TValue> table, IComparer<(TKey key, TMainKey? primaryKey)> comparerKeyOnly, string keyName)
        {
            _hasValue = orderedData.Length != 0 && left <= right;

            _orderedData = orderedData;
            _left = left;
            _right = right;
            _ascendant = ascendant;
            KeyName = keyName;
            _comparerKeyOnly = comparerKeyOnly;
            _table = table;
            _version = table?.Version ?? 0;
        }

        public static RangeView<TKey, TMainKey, TValue> GetEmpty((TKey key, TMainKey primaryKey)[] orderedData,
            Table<TMainKey, TValue> table, string keyName)
        {
            return new RangeView<TKey, TMainKey, TValue>(orderedData, 0, -1, default, table, null!, keyName);
        }

        public RangeView<TKey, TMainKey, TValue> Slice(int start, int count)
        {
            return Slice(start, count, _ascendant);
        }

        public RangeView<TKey, TMainKey, TValue> Slice(int start, int count, bool ascendant)
        {
            if (count == 0)
                return GetEmpty(_orderedData, _table, KeyName);

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return new RangeView<TKey, TMainKey, TValue>(_orderedData,
                _left + start,
                _left + start + count - 1,
                ascendant,
                _table,
                _comparerKeyOnly,
                KeyName);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Any()
        {
            return Count != 0;
        }

        public int IndexOf(in TValue item)
        {
            var i = 0;
            foreach (var v in this)
            {
                if (EqualityComparer<TValue>.Default.Equals(v, item))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }

        /// <summary>
        /// O(N) search.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(in TValue item)
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                if (EqualityComparer<TValue>.Default.Equals(this[i], item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// O(log N) search.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int IndexOfFirst(TKey key)
        {
            int count = Count;
            if (count == 0)
                return -1;

            return BinarySearch.LowerBound(_orderedData!, 0, _orderedData.Length, (key, default!), _comparerKeyOnly);
        }

        /// <summary>
        /// O(log N) search.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int IndexOfLast(TKey key)
        {
            int count = Count;
            if (count == 0)
                return -1;

            return BinarySearch.UpperBound(_orderedData!, 0, _orderedData.Length, (key, default), _comparerKeyOnly);
        }

        /// <summary>
        /// O(log N) search.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return IndexOfFirst(key) >= 0;
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            var count = Count;
            for (int i = 0; i < count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        TValue IList<TValue>.this[int index]
        {
            get => this[index];
            set => throw new NotImplementedException();
        }

        bool ICollection<TValue>.Contains(TValue item)
        {
            var count = Count;
            for (int i = 0; i < count; i++)
            {
                var v = this[i];
                if (EqualityComparer<TValue>.Default.Equals(v, item))
                {
                    return true;
                }
            }

            return false;
        }

        int IList<TValue>.IndexOf(TValue item)
        {
            var i = 0;
            foreach (var v in this)
            {
                if (EqualityComparer<TValue>.Default.Equals(v, item))
                {
                    return i;
                }

                i++;
            }

            return -1;
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