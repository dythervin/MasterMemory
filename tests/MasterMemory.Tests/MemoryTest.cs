﻿using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MasterMemory.Tests
{
    public class MemoryTest
    {
        public MemoryTest()
        {
            // MessagePackSerializer.DefaultOptions = MessagePackSerializer.DefaultOptions.WithResolver(MessagePackResolver.Instance);
        }

        Sample[] CreateData()
        {
            // Id = Unique, PK
            // FirstName + LastName = Unique
            var data = new[]
            {
                new Sample { Id = 5, Age = 19, FirstName = "aaa", LastName = "foo" },
                new Sample { Id = 6, Age = 29, FirstName = "bbb", LastName = "fo1" },
                new Sample { Id = 7, Age = 39, FirstName = "ccc", LastName = "fo2" },
                new Sample { Id = 8, Age = 49, FirstName = "ddd", LastName = "fo3" },
                new Sample { Id = 1, Age = 59, FirstName = "eee", LastName = "fo4" },
                new Sample { Id = 2, Age = 89, FirstName = "aaa", LastName = "bar" },
                new Sample { Id = 3, Age = 79, FirstName = "be", LastName = "de" },
                new Sample { Id = 4, Age = 89, FirstName = "aaa", LastName = "tako" },
                new Sample { Id = 9, Age = 99, FirstName = "aaa", LastName = "ika" },
                new Sample { Id = 10, Age = 9, FirstName = "eee", LastName = "baz" },
            };
            return data;
        }

        ISampleTable CreateTable(Sample[] data)
        {
            return new Database(sampleTable: data).SampleTable;
        }

        [Fact]
        public void Count()
        {
            var data = CreateData();
            var table = CreateTable(data);

            table.Count.Should().Be(data.Length);
        }

        [Fact]
        public void Find()
        {
            var data = CreateData();
            var table = CreateTable(data);

            foreach (var item in data)
            {
                var f = table.GetById(item.Id);
                item.Id.Should().Be(f.Id);
            }

            Assert.Throws<KeyNotFoundException>(() => table.GetById(110));
            table.TryGetValue(110, out _).Should().BeFalse();
        }

        [Fact]
        public void MultiKeyGet()
        {
            var data = CreateData();
            var table = CreateTable(data);

            foreach (var item in data)
            {
                var f = table.GetByFirstNameAndLastName((item.FirstName, item.LastName));
                item.Id.Should().Be(f.Id);
            }

            Assert.Throws<KeyNotFoundException>(() => table.GetByFirstNameAndLastName(("aaa", "___")));
            Assert.Throws<KeyNotFoundException>(() => table.GetByFirstNameAndLastName(("___", "foo")));
            table.TryGetByFirstNameAndLastName(("aaa", "___"), out _).Should().BeFalse();
            table.TryGetByFirstNameAndLastName(("___", "foo"), out _).Should().BeFalse();
        }

        [Fact]
        public void FindClosest()
        {
            var data = CreateData();
            var table = CreateTable(data);

            {
                table.FindClosestByAge(56, true).First.Age.Should().Be(49);
                table.FindClosestByAge(56, false).First.Age.Should().Be(59);
            }
            {
                // first
                for (int i = 0; i < 9; i++)
                {
                    table.FindClosestByAge(i, selectLower: true).Count.Should().Be(0);
//                  table.FindClosestByAge(i, selectLower: true).First.Age.Should().Be(9);
                }

                var lastAge = 9;
                foreach (var item in data.OrderBy(x => x.Age))
                {
                    for (int i = lastAge + 1; i < item.Age; i++)
                    {
                        table.FindClosestByAge(i, selectLower: true).First.Age.Should().Be(lastAge);
                    }

                    lastAge = item.Age;
                }

                // last
                table.FindClosestByAge(99, selectLower: false).First.Age.Should().Be(99);

                for (int i = 100; i < 120; i++)
                {
                    table.FindClosestByAge(i, selectLower: false).Count.Should().Be(0);
//                  table.FindClosestByAge(i, selectLower: true).First.Age.Should().Be(99);
                }
            }
            {
                // first
                for (int i = 0; i < 9; i++)
                {
                    table.FindClosestByAge(i, selectLower: false).First.Age.Should().Be(9);
                }

                var xss = data.OrderBy(x => x.Age).ToArray();
                for (int j = 1; j < xss.Length - 1; j++)
                {
                    var item = xss[j];
                    for (int i = xss[j - 1].Age + 1; i < item.Age; i++)
                    {
                        table.FindClosestByAge(i, selectLower: false).First.Age.Should().Be(xss[j].Age);
                    }
                }

                // last
                table.FindClosestByAge(99, selectLower: false).First.Age.Should().Be(99);

                for (int i = 100; i < 120; i++)
                {
                    table.FindClosestByAge(i, selectLower: false).Count.Should().Be(0);
                }
            }
        }

        [Fact]
        public void FindClosestMultiKey()
        {
            var data = CreateData();
            var table = CreateTable(data);

            // Age of aaa
            //new Sample { Id = 5, Age = 19, FirstName = "aaa", LastName = "foo" },
            //new Sample { Id = 2, Age = 89, FirstName = "aaa", LastName = "bar" },
            //new Sample { Id = 4, Age = 89, FirstName = "aaa", LastName = "tako" },
            //new Sample { Id = 9, Age = 99, FirstName = "aaa", LastName = "ika" },

            table.FindClosestByAgeAndFirstName(("aaa", 10), true).Count.Should().Be(0);
            table.FindClosestByAgeAndFirstName(("aaa", 10), false).First.Age.Should().Be(19);
            table.FindClosestByAgeAndFirstName(("aaa", 92), true).First.Age.Should().Be(89);
            table.FindClosestByAgeAndFirstName(("aaa", 120), true).First.Age.Should().Be(99);
            table.FindClosestByAgeAndFirstName(("aaa", 10), false).First.Age.Should().Be(19);
            table.FindClosestByAgeAndFirstName(("aaa", 73), false).First.Age.Should().Be(89);
        }

        [Fact]
        public void FindMany()
        {
            var data = CreateData();
            var table = CreateTable(data);

            table.FindByFirstName("aaa").OrderBy(x => x.Id).Select(x => x.Id).Should().BeEquivalentTo(new[] { 2, 4, 5, 9 });
        }

        [Fact]
        public void FindManyMultiKey()
        {
            var data = CreateData();
            var table = CreateTable(data);

            table.FindByAgeAndFirstName(("aaa", 89)).Select(x => x.Id).Should().BeEquivalentTo(new[] { 2, 4 });
            table.FindByAgeAndFirstName(("aaa", 89)).Reverse().Select(x => x.Id).Should().BeEquivalentTo(new[] { 4, 2 });
        }
    }
}