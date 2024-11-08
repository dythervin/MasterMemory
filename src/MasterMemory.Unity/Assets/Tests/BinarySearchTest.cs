using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace MasterMemory.Tests
{
    public class BinarySearchTest
    {
        public BinarySearchTest()
        {
            //MessagePackSerializer.DefaultOptions = MessagePackSerializer.DefaultOptions.WithResolver(MessagePackResolver.Instance);
        }

        [Fact]
        public void Find()
        {
            var rand = new Random();
            for (int iii = 0; iii < 30; iii++)
            {
                var seed = Enumerable.Range(1, 10);
                var randomSeed = seed.Where(x => rand.Next() % 2 == 0);
                var array = randomSeed.Concat(randomSeed).Concat(randomSeed).Concat(randomSeed).OrderBy(x => x).ToArray();

                for (int i = 1; i <= 10; i++)
                {
                    var firstIndex = Array.IndexOf(array, i);
                    var lastIndex = Array.LastIndexOf(array, i);

                    var f = BinarySearch.FindFirst(array, i, Comparer<int>.Default);
                    var l = BinarySearch.LowerBound(array, 0, array.Length, i, Comparer<int>.Default);
                    var u = BinarySearch.UpperBound(array, 0, array.Length, i, Comparer<int>.Default);

                    // not found
                    if (firstIndex == -1)
                    {
                        f.Should().Be(-1);
                        l.Should().Be(-1);
                        u.Should().Be(-1);
                        continue;
                    }

                    array[f].Should().Be(i);
                    array[l].Should().Be(i);
                    array[u].Should().Be(i);

                    l.Should().Be(firstIndex);
                    u.Should().Be(lastIndex);
                }
            }

            // and empty
            var emptyArray = Enumerable.Empty<int>().ToArray();
            BinarySearch.FindFirst(emptyArray, 0, Comparer<int>.Default).Should().Be(-1);
            BinarySearch.LowerBound(emptyArray, 0, emptyArray.Length, 0, Comparer<int>.Default).Should().Be(-1);
            BinarySearch.UpperBound(emptyArray, 0, emptyArray.Length, 0, Comparer<int>.Default).Should().Be(-1);
        }

        [Fact]
        public void Closest()
        {
            // empty
            var array = Enumerable.Empty<int>().ToArray();

            var near = BinarySearch.FindClosest(array, 0, 0, array.Length, Comparer<int>.Default, false);
            near.Should().Be(-1);

            // mid
            var source = new (int id, int bound)[] { (0, 0), (1, 100), (2, 200), (3, 300), (4, 500), (5, 1000), };

            var comparer = Comparer<(int id, int bound)>.Create((x, y) => x.bound.CompareTo(y.bound));

            BinarySearch.FindClosest(source, 0, source.Length, (default, -100), comparer, true).Should().Be(-1);
//          BinarySearch.FindClosest(source, 0, source.Length, (default, -100), comparer, true).Should().Be(0);
            BinarySearch.FindClosest(source, 0, source.Length, (default, 0), comparer, true).Should().Be(0);
            BinarySearch.FindClosest(source, 0, source.Length, (default, 10), comparer, true).Should().Be(0);
            BinarySearch.FindClosest(source, 0, source.Length, (default, 50), comparer, true).Should().Be(0);

            source[BinarySearch.FindClosest(source, 0, source.Length, (default, 100), comparer, true)].id.Should().Be(1);
            source[BinarySearch.FindClosest(source, 0, source.Length, (default, 100), comparer, false)].id.Should().Be(1);
            source[BinarySearch.FindClosest(source, 0, source.Length, (default, 150), comparer, true)].id.Should().Be(1);
            source[BinarySearch.FindClosest(source, 0, source.Length, (default, 300), comparer, true)].id.Should().Be(3);
            source[BinarySearch.FindClosest(source, 0, source.Length, (default, 999), comparer, true)].id.Should().Be(4);
            source[BinarySearch.FindClosest(source, 0, source.Length, (default, 1000), comparer, true)].id.Should().Be(5);
            source[BinarySearch.FindClosest(source, 0, source.Length, (default, 1001), comparer, true)].id.Should().Be(5);
            source[BinarySearch.FindClosest(source, 0, source.Length, (default, 10000), comparer, true)].id.Should().Be(5);
//          source[BinarySearch.FindClosest(source, 0, source.Length, (default, 10000), comparer, false)].id.Should().Be(5);

            BinarySearch.FindClosest(source, 0, source.Length, (default, 10000), comparer, false).Should().Be(6);
        }
    }
}