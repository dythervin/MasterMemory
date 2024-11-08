using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MasterMemory
{
    public abstract class DbTransactionBase : IDisposable
    {
        public event Action? OnBeforeCommit;

        public event Action? OnCommit;

        private bool _isDisposed;

        private uint _depth;

        private readonly Queue<int> _tableOperationsOrder = new();

        public bool IsCommitting { get; private set; }

        public bool IsInProgress => _depth > 0;

        public int Depth => (int)_depth;

        public void BeginTransaction()
        {
            AssertNotCommitting();
            _depth++;
        }

        public void Commit()
        {
            AssertNotDisposed();
            switch (_depth)
            {
                case 0:
                    throw new InvalidOperationException("No transaction to commit");
                case 1:
                    _depth = 0;
                    CommitInternal();
                    break;
                default:
                    _depth--;
                    return;
            }
        }

        public void Rollback()
        {
            if (_depth == 0)
            {
                return;
            }

            AssertNotDisposed();
            RollbackInternal();
            _depth = 0;
            ClearBuffers();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void OnTransaction(int tableIndex)
        {
            _tableOperationsOrder.Enqueue(tableIndex);
        }

        protected virtual void CommitInternal()
        {
            AssertNotDisposed();
            try
            {
                OnBeforeCommit?.Invoke();
                IsCommitting = true;
                while (_tableOperationsOrder.TryDequeue(out int tableIndex))
                {
                    CommitOperation(tableIndex);
                }

                OnCommit?.Invoke();
            }
            catch (Exception)
            {
                RollbackInternal();
                throw;
            }
            finally
            {
                ClearBuffers();
                IsCommitting = false;
            }
        }

        protected virtual void ClearBuffers()
        {
            _tableOperationsOrder.Clear();
        }

        protected abstract void CommitOperation(int tableIndex);

        protected abstract void RollbackInternal();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AssertCanExecuteOperations()
        {
            if (_depth == 0)
            {
                throw new InvalidOperationException("Transaction is not started");
            }

            AssertNotDisposed();
            AssertNotCommitting();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertNotCommitting()
        {
            if (IsCommitting)
            {
                throw new InvalidOperationException("Cannot perform this operation while committing");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
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
            if (_depth > 0)
            {
                _depth = 0;
                IsCommitting = false;
            }

            OnBeforeCommit = null;
            OnCommit = null;
        }
    }
}