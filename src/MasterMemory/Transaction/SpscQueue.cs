using System;
using System.Threading;

namespace MasterMemory
{
    /// <summary>
    ///     Enqueue and Clear operations are called only by one thread (producer).
    ///     Dequeue operation is called by another thread (consumer).
    /// </summary>
    /// <typeparam name="T">The type of items stored in the queue.</typeparam>
    public class SpscQueue<T>
    {
        private T[] _buffer;
        private int _capacity;

        private int _head; // Written by producer, read by consumer
        private int _tail; // Written by consumer, read by producer

        private SpinLock _spinLock;

        /// <summary>
        ///     Indicates whether the queue is empty.
        /// </summary>
        public bool IsEmpty => Volatile.Read(ref _head) == Volatile.Read(ref _tail);

        /// <summary>
        ///     Initializes a new instance of the <see cref="SpscQueue{T}" /> class with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the queue.</param>
        public SpscQueue(int initialCapacity = 4)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            _capacity = initialCapacity;
            _buffer = new T[_capacity];
            _head = 0;
            _tail = 0;
        }

        /// <summary>
        ///     Enqueues an item to the queue. This method is called only by the producer thread.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        public void Enqueue(T item)
        {
            int tail = Volatile.Read(ref _tail);
            int nextHead = (_head + 1) % _capacity;

            if (nextHead == tail)
            {
                // Queue is full; resize is needed
                Resize();
                nextHead = (_head + 1) % _capacity;
            }

            _buffer[_head] = item;
            // Ensure the write to the buffer happens before updating _head
            Volatile.Write(ref _head, nextHead);
        }

        /// <summary>
        ///     Attempts to dequeue an item from the queue. This method is called only by the consumer thread.
        /// </summary>
        /// <param name="item">
        ///     When this method returns, contains the item removed from the queue, if it was successful; otherwise,
        ///     the default value.
        /// </param>
        /// <returns>True if an item was dequeued successfully; false if the queue is empty.</returns>
        public bool TryDequeue(out T? item)
        {
            bool lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);

                int tail = Volatile.Read(ref _tail);
                int head = Volatile.Read(ref _head);

                if (tail == head)
                {
                    // Queue is empty
                    item = default;
                    return false;
                }

                item = _buffer[tail];
                int nextTail = (tail + 1) % _capacity;
                // Ensure the read from the buffer happens before updating _tail
                Volatile.Write(ref _tail, nextTail);
                return true;
            }
            finally
            {
                if (lockTaken)
                {
                    _spinLock.Exit();
                }
            }
        }

        /// <summary>
        ///     Dequeues an item from the queue. Throws an exception if the queue is empty.
        /// </summary>
        /// <returns>The dequeued item.</returns>
        public T Dequeue()
        {
            if (TryDequeue(out T? item))
            {
                return item!;
            }

            throw new InvalidOperationException("Cannot dequeue from an empty queue.");
        }

        /// <summary>
        ///     Clears the queue. This method is called only by the producer thread.
        /// </summary>
        public void Clear()
        {
            bool lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                Volatile.Write(ref _head, 0);
                Volatile.Write(ref _tail, 0);
            }
            finally
            {
                if (lockTaken)
                {
                    _spinLock.Exit();
                }
            }
        }

        /// <summary>
        ///     Resizes the internal buffer to accommodate more items.
        /// </summary>
        private void Resize()
        {
            int newCapacity = _capacity * 2;
            var newBuffer = new T[newCapacity];

            int initialTail = Volatile.Read(ref _tail);
            if (_head >= initialTail)
            {
                Array.Copy(_buffer, initialTail, newBuffer, 0, _head - initialTail);
            }
            else
            {
                int headChunkLength = _capacity - initialTail;
                Array.Copy(_buffer, initialTail, newBuffer, 0, headChunkLength);
                Array.Copy(_buffer, 0, newBuffer, headChunkLength, _head);
            }

            bool lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                bool isEmpty = _tail == _head;

                int newTail = isEmpty || _tail == initialTail ? 0 :
                    _tail > initialTail ? _tail - initialTail : _tail + _capacity - initialTail;

                int newHead = isEmpty ? 0 : _head > initialTail ? _head - initialTail : _head + _capacity - initialTail;

                //Validate(isEmpty, newBuffer, newTail, newHead, newCapacity);

                _buffer = newBuffer;

                _tail = newTail;
                _head = newHead;
                _capacity = newCapacity;
            }
            finally
            {
                if (lockTaken)
                {
                    _spinLock.Exit();
                }
            }
        }
    }
}