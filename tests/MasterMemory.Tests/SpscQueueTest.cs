using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace MasterMemory.Tests
{
    public class SpscQueueTest
    {
        [Fact]
        public void Enqueue_ShouldStoreItemsInOrder()
        {
            var queue = new SpscQueue<int>(4);
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            queue.Dequeue().Should().Be(1);
            queue.Dequeue().Should().Be(2);
            queue.Dequeue().Should().Be(3);
            queue.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void TryDequeue_ShouldReturnFalseWhenQueueIsEmpty()
        {
            var queue = new SpscQueue<int>(4);

            queue.TryDequeue(out var item).Should().BeFalse();
            item.Should().Be(default(int));
        }

        [Fact]
        public void Enqueue_ShouldResizeWhenFull()
        {
            var queue = new SpscQueue<int>(2);
            queue.Enqueue(1);
            queue.Enqueue(2);

            //queue.Capacity.Should().Be(2);

            queue.Enqueue(3); // Triggers resize
            //queue.Capacity.Should().Be(4);

            queue.Dequeue().Should().Be(1);
            queue.Dequeue().Should().Be(2);
            queue.Dequeue().Should().Be(3);
            queue.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void Clear_ShouldEmptyQueue()
        {
            var queue = new SpscQueue<int>(4);
            queue.Enqueue(1);
            queue.Enqueue(2);

            queue.Clear();

            queue.IsEmpty.Should().BeTrue();
            //queue.Capacity.Should().Be(4);
            queue.TryDequeue(out _).Should().BeFalse();
        }

        [Fact]
        public void Resize_ShouldDoubleCapacity()
        {
            var queue = new SpscQueue<int>(2);
            queue.Enqueue(1);
            queue.Enqueue(2);

            queue.Enqueue(3); // Triggers resize
            //queue.Capacity.Should().Be(4);
        }

        [Fact]
        public void EnqueueAndDequeue_MultiThreaded_ShouldNotLoseData()
        {
            var queue = new SpscQueue<int>(4);
            int itemCount = 1000;
            List<int> dequeuedItems = new List<int>();

            // Producer Task
            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < itemCount; i++)
                {
                    queue.Enqueue(i);
                }
            });

            // Consumer Task
            var dequeueTask = Task.Run(() =>
            {
                int count = 0;
                while (count < itemCount)
                {
                    if (queue.TryDequeue(out var item))
                    {
                        dequeuedItems.Add(item);
                        count++;
                    }
                }
            });

            Task.WaitAll(enqueueTask, dequeueTask);

            dequeuedItems.Count.Should().Be(itemCount);
            dequeuedItems.Should().BeInAscendingOrder();
            queue.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void ResizeAndDequeue_MultiThreaded_ShouldPreserveDataOrder()
        {
            var queue = new SpscQueue<int>(2);
            List<int> dequeuedItems = new List<int>();
            int itemCount = 5000;

            // Producer Task: Enqueue items and trigger multiple resizes
            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < itemCount; i++)
                {
                    queue.Enqueue(i);
                }
            });

            // Consumer Task: Continuously dequeue items while resizing occurs
            var dequeueTask = Task.Run(() =>
            {
                int count = 0;
                while (count < itemCount)
                {
                    if (queue.TryDequeue(out var item))
                    {
                        dequeuedItems.Add(item);
                        count++;
                    }
                }
            });

            Task.WaitAll(enqueueTask, dequeueTask);

            // Validate results
            dequeuedItems.Count.Should().Be(itemCount);
            dequeuedItems.Should().BeInAscendingOrder();
            queue.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void Dequeue_ShouldThrowWhenQueueIsEmpty()
        {
            var queue = new SpscQueue<int>(4);

            Action action = () => queue.Dequeue();

            action.Should().Throw<InvalidOperationException>().WithMessage("Queue is empty.");
        }
    }
}