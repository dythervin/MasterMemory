using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MasterMemory.Tests
{
    public static class RangeViewTest
    {
        [Fact]
        public static void Range()
        {
            // 4 -> 8
            {
                var range = GetRange(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 4, 8, true);

                range.Count.Should().Be(5);
                range[0].Should().Be(4);
                range[1].Should().Be(5);
                range[2].Should().Be(6);
                range[3].Should().Be(7);
                range[4].Should().Be(8);

                Assert.Throws<ArgumentOutOfRangeException>(() => range[-1]);
                Assert.Throws<ArgumentOutOfRangeException>(() => range[5]);

                var begin = 4;
                foreach (var item in range)
                {
                    item.Should().Be(begin++);
                }

                var xs = new int[10];
                range.CopyTo(xs, 3);
                xs[3].Should().Be(4);
                xs[4].Should().Be(5);
                xs[5].Should().Be(6);
                xs[6].Should().Be(7);
                xs[7].Should().Be(8);
                xs[8].Should().Be(0);

                range.IndexOf(5).Should().Be(1);
                range.IndexOf(9).Should().Be(-1);

                range.Contains(5).Should().BeTrue();
                range.Contains(9).Should().BeFalse();
            }

            {
                // 7 -> 2
                var range = GetRange(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, 7, false);

                range.Count.Should().Be(6);
                range[0].Should().Be(7);
                range[1].Should().Be(6);
                range[2].Should().Be(5);
                range[3].Should().Be(4);
                range[4].Should().Be(3);
                range[5].Should().Be(2);

                Assert.Throws<ArgumentOutOfRangeException>(() => range[-1]);
                Assert.Throws<ArgumentOutOfRangeException>(() => range[6]);

                var begin = 7;
                foreach (var item in range)
                {
                    item.Should().Be(begin--);
                }

                var xs = new int[10];
                range.CopyTo(xs, 3);
                xs[3].Should().Be(7);
                xs[4].Should().Be(6);
                xs[5].Should().Be(5);
                xs[6].Should().Be(4);
                xs[7].Should().Be(3);
                xs[8].Should().Be(2);

                range.IndexOf(5).Should().Be(2);
                range.IndexOf(9).Should().Be(-1);

                range.Contains(5).Should().BeTrue();
                range.Contains(9).Should().BeFalse();
            }

            var empty = GetRange(Enumerable.Empty<int>().ToArray(), 0, 0, true);
            empty.Count.Should().Be(0);

            var same = GetRange(Enumerable.Range(1, 1000).ToArray(), 100, 100, true);
            same.Count.Should().Be(1);
            same[0].Should().Be(101);
        }

        private static List<int> GetRange(int[] ids, int left, int right, bool ascendant)
        {
            var table = new Database(userLevelTable: ids.Select(id => new UserLevel()
            {
                Level = id,
            }).ToArray()).UserLevelTable;

            if (right <= 0)
            {
                left = -1;
                right = -1;
            }

            var all = table.GetAllSortedByLevel();
            int count = right - left + 1;
            return all.Slice(left, count, ascendant).Select(x => x.Level).ToList();
        }
    }
}