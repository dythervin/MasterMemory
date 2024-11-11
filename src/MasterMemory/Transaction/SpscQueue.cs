using System;
using System.Threading;

namespace MasterMemory
{
    /// <summary>
    ///     Enqueue and Clear operations are called only by one thread.
    ///     Dequeue operation is called by another thread.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SpscQueue<T>
    {
        private T[] _buffer;
        private int _capacity;

        private int _head;
        private int _tail;
        private int _resizeLock;

        public bool IsEmpty => _head == _tail;

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

        public bool Enqueue(T item)
        {
            int nextHead = (_head + 1) % _capacity;

            if (nextHead == _tail)
            {
                Resize();
                nextHead = (_head + 1) % _capacity;
            }

            _buffer[_head] = item;
            _head = nextHead;

            return true;
        }

        public bool TryDequeue(out T item)
        {
            AcquireLock();

            if (_tail == _head)
            {
                item = default;
                _resizeLock = 0;
                return false;
            }

            item = _buffer[_tail];
            _tail = (_tail + 1) % _capacity;
            _resizeLock = 0;

            return true;
        }

        public T Dequeue()
        {
            if (TryDequeue(out T item))
            {
                return item;
            }

            throw new InvalidOperationException("Queue is empty.");
        }

        public void Clear()
        {
            _head = _tail = 0;
        }

        private void AcquireLock()
        {
            var spinWait = new SpinWait();
            while (Interlocked.CompareExchange(ref _resizeLock, 1, 0) == 1)
            {
                spinWait.SpinOnce();
            }
        }

        private void Resize()
        {
            int tail = _tail;
            if ((_head + 1) % _capacity != tail)
            {
                return;
            }

            int newCapacity = _capacity * 2;
            T[] newBuffer = new T[newCapacity];

            if (_head >= tail)
            {
                Array.Copy(_buffer, tail, newBuffer, 0, _head - tail);
            }
            else
            {
                int headChunkLength = _capacity - tail;
                Array.Copy(_buffer, tail, newBuffer, 0, headChunkLength);
                Array.Copy(_buffer, 0, newBuffer, headChunkLength, _head);
            }

            AcquireLock();

            _buffer = newBuffer;
            _head = _capacity - 1;

            _tail = _tail == tail ? 0 : _tail > tail ? _tail - tail : _tail + _capacity - tail;
            _capacity = newCapacity;
            _resizeLock = 0;
        }
    }
}