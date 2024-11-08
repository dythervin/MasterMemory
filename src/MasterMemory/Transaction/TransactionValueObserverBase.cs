using System;
using System.Collections.Generic;

namespace MasterMemory
{
    public abstract class TransactionValueObserverBase<TValue> : Queue<Operation<TValue>>,
        ITransactionValueObserver,
        ICollection<Operation<TValue>>, IDisposable
    {
        bool ICollection<Operation<TValue>>.IsReadOnly => false;
        
        private bool _isDisposed;

        public void Add(Operation<TValue> item)
        {
            Enqueue(item);
        }

        bool ICollection<Operation<TValue>>.Remove(Operation<TValue> item)
        {
            throw new NotSupportedException();
        }

        public void PublishNext()
        {
            PublishNext(Dequeue());
        }

        protected abstract void PublishNext(in Operation<TValue> operation);

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