using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MasterMemory
{
    public abstract class DatabaseBase : IDatabase
    {
        private readonly Dictionary<Type, ITable> _tables;
        private bool _isDisposed;

        protected DatabaseBase(int capacity)
        {
            _tables = new(capacity);
        }

        public ITable<TKey, T> GetTable<TKey, T>()
        {
            if (_tables.TryGetValue(typeof(T), out ITable table))
            {
                return (ITable<TKey, T>)table;
            }

            throw new InvalidOperationException($"Table of type {typeof(T).Name} not found.");
        }

        public bool TryGetTable<TKey, T>([NotNullWhen(true)] out ITable<TKey, T>? table)
        {
            if (_tables.TryGetValue(typeof(T), out ITable t))
            {
                table = (ITable<TKey, T>)t;
                return true;
            }

            table = null;
            return false;
        }

        protected void RegisterTable<TKey, T>(ITable<TKey, T> table)
        {
            _tables.Add(typeof(T), table);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            OnDispose();
        }

        protected virtual void OnDispose()
        {
        }
    }
}