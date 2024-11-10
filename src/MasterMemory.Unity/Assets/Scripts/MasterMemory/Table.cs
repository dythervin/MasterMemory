using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MasterMemory
{
    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
    public abstract partial class Table<TMainKey, TElement> : ITable<TMainKey, TElement>
    {
        public event OnOperationChange<TElement>? OnChange;

        private readonly Dictionary<TMainKey, TElement> _dictionary;
        private readonly Stack<OperationChange<TElement>> _rollbackStack = new();
        private bool _isRollingBack;

        public Type ItemType => typeof(TElement);

        public int Version { get; private set; }

        public KeySelector<TElement, TMainKey> KeySelector { get; }

        public TElement this[TMainKey sortedKey]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dictionary[sortedKey];
        }

        public int Count => _dictionary.Count;

        public abstract string ElementName { get; }

        public Type KeyType => typeof(TMainKey);

        public abstract string KeyName { get; }

        public abstract string TableName { get; }

        private bool CanPushToRollback => !_isRollingBack;

        protected Table(IReadOnlyList<TElement> data, KeySelector<TElement, TMainKey> keySelector)
        {
            KeySelector = keySelector;
            _dictionary = new(data.Count);
            for (int i = 0; i < data.Count; i++)
            {
                _dictionary.Add(keySelector(data[i]), data[i]);
            }
        }

        public bool ContainsKey(TMainKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public abstract RangeView<TMainKey, TMainKey, TElement> GetAllSorted(bool ascendant = true);

        public bool TryGetValue(TMainKey key, out TElement element)
        {
            return _dictionary.TryGetValue(key, out element);
        }

        public Dictionary<TMainKey, TElement>.ValueCollection.Enumerator GetEnumerator()
        {
            return _dictionary.Values.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TMainKey keys)
        {
            if (!RemoveInternal(keys))
            {
                return false;
            }

            UpdateVersion();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in TMainKey keys)
        {
            if (!RemoveInternal(keys))
            {
                return false;
            }

            UpdateVersion();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in TElement element)
        {
            return Remove(KeySelector(element));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Execute(in Operation<TElement> operation)
        {
            if (!ExecuteOperationInternal(operation))
            {
                return false;
            }

            UpdateVersion();
            return true;
        }

        public void Rollback()
        {
            if (_isRollingBack)
            {
                return;
            }

            _isRollingBack = true;
            while (_rollbackStack.TryPop(out var change))
            {
                ExecuteOperationInternal(change);
            }

            _isRollingBack = false;
            UpdateVersion();
        }

        public void ClearRollback()
        {
            _rollbackStack.Clear();
        }

        public void Clear()
        {
            if (_dictionary.Count == 0)
            {
                return;
            }

            OnOperation(new(OperationType.Clear, default!));
            if (CanPushToRollback)
            {
                foreach (TElement value in _dictionary.Values)
                {
                    _rollbackStack.Push(new(OperationType.Insert, value));
                }
            }

            _dictionary.Clear();
            UpdateVersion();
        }

        protected abstract void ApplyToKeyCollections(in OperationChange<TElement> operation);

        protected Dictionary<TMainKey, TElement>.ValueCollection GetAll()
        {
            return _dictionary.Values;
        }

        protected abstract bool CanInsert(in TElement item);

        protected void OnOperation(in OperationChange<TElement> operation)
        {
            ApplyToKeyCollections(operation);
            OnChange?.Invoke(operation);
        }

        private void PushToRollbackStack(OperationType operationType, in TElement item, in TElement key)
        {
            if (CanPushToRollback)
            {
                _rollbackStack.Push(new(operationType, item, key));
            }
        }

        private void PushToRollbackStack(OperationType operationType, in TElement item)
        {
            if (CanPushToRollback)
            {
                _rollbackStack.Push(new(operationType, item));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ExecuteOperationInternal(in Operation<TElement> operation)
        {
            TMainKey key = KeySelector(operation.Value);
            switch (operation.Type)
            {
                case OperationType.Insert:
                {
                    if (!CanInsert(operation.Value))
                    {
                        return false;
                    }

                    _dictionary.Add(key, operation.Value);
                    OnOperation(new(OperationType.Insert, operation.Value));
                    PushToRollbackStack(OperationType.Remove, operation.Value);
                    return true;
                }
                case OperationType.Replace:
                {
                    TElement? rollbackItem = _dictionary[key];
                    if (EqualityComparer<TElement>.Default.Equals(rollbackItem, operation.Value))
                    {
                        return false;
                    }

                    _dictionary[key] = operation.Value;
                    OnOperation(new(OperationType.Replace, rollbackItem, operation.Value));
                    PushToRollbackStack(OperationType.Replace, operation.Value, rollbackItem);
                    return true;
                }
                case OperationType.InsertOrReplace:
                {
                    if (_dictionary.TryGetValue(key, out TElement? rollbackItem))
                    {
                        if (EqualityComparer<TElement>.Default.Equals(rollbackItem, operation.Value))
                        {
                            return false;
                        }

                        _dictionary[key] = operation.Value;
                        OnOperation(new(OperationType.InsertOrReplace, rollbackItem, operation.Value));
                        PushToRollbackStack(OperationType.Replace, operation.Value, rollbackItem);
                    }
                    else
                    {
                        _dictionary.Add(key, operation.Value);
                        OnOperation(new(OperationType.InsertOrReplace, operation.Value));
                        PushToRollbackStack(OperationType.Remove, operation.Value);
                    }

                    return true;
                }

                case OperationType.Remove:
                {
                    return RemoveInternal(key);
                }

                case OperationType.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool RemoveInternal(TMainKey key)
        {
            if (!_dictionary.Remove(key, out TElement? rollbackItem))
            {
                return false;
            }

            OnOperation(new(OperationType.Remove, rollbackItem));
            PushToRollbackStack(OperationType.Insert, rollbackItem);
            return true;
        }

        private void UpdateVersion()
        {
            Version++;
        }
    }
}