using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace MasterMemory
{
    public class TableTransaction<TValue> : ITableTransaction<TValue>, ITransactionObserver<TValue>
    {
        public event Action<TransactionType, CancellationToken>? OnTransaction;

        private readonly List<Operation<TValue>> _operations = new();

        public bool IsOpen { get; private set; } = true;

        public Operation<TValue> this[int index] => _operations[index];

        public int Count => _operations.Count;

        public ITransactionObserver<TValue> Observer => this;

        public void InsertOrReplace(TValue item)
        {
            AssertOpen();
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _operations.Add(new(OperationType.InsertOrReplace, item));
        }

        public void Remove(TValue key)
        {
            AssertOpen();
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _operations.Add(new(OperationType.Remove, key));
        }

        private void Squash()
        {
            // TODO: Implement squash
        }

        public void Execute(TransactionType type, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case TransactionType.BeginCommit:
                    AssertOpen();
                    IsOpen = false;
                    Squash();

                    break;
                case TransactionType.FinishCommit:
                    AssertClosed();

                    break;
                case TransactionType.Rollback:
                    AssertClosed();

                    break;
                case TransactionType.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            OnTransaction?.Invoke(type, cancellationToken);
            AssertClosed();
            switch (type)
            {
                case TransactionType.BeginCommit:
                    break;
                case TransactionType.FinishCommit:
                    IsOpen = true;
                    _operations.Clear();

                    break;
                case TransactionType.Rollback:
                    IsOpen = true;
                    _operations.Clear();

                    break;
            }
        }

        private void AssertClosed()
        {
            if (IsOpen)
            {
                throw new InvalidOperationException("Transaction is not closed.");
            }
        }

        public List<Operation<TValue>>.Enumerator GetEnumerator()
        {
            return _operations.GetEnumerator();
        }

        private void AssertOpen()
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("Transaction is closed.");
            }
        }

        IEnumerator<Operation<TValue>> IEnumerable<Operation<TValue>>.GetEnumerator()
        {
            return _operations.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}