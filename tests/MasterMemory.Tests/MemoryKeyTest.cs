﻿using Xunit;
using System.Linq;

using FluentAssertions;
using System.Collections.Generic;

namespace MasterMemory.Tests
{
    public class MemoryKeyMemoryTest
    {
        Sample[] CreateData()
        {
            // Id = Unique, PK
            // FirstName + LastName = Unique
            var data = new[]
            {
                new Sample { Id = 5, Age = 19, FirstName = "aaa", LastName = "foo" },
                new Sample { Id = 6, Age = 29, FirstName = "bbb", LastName = "foo" },
                new Sample { Id = 7, Age = 39, FirstName = "ccc", LastName = "foo" },
                new Sample { Id = 8, Age = 49, FirstName = "ddd", LastName = "foo" },
                new Sample { Id = 1, Age = 59, FirstName = "eee", LastName = "foo" },
                new Sample { Id = 2, Age = 89, FirstName = "aaa", LastName = "bar" },
                new Sample { Id = 3, Age = 79, FirstName = "be", LastName = "de" },
                new Sample { Id = 4, Age = 89, FirstName = "aaa", LastName = "tako" },
                new Sample { Id = 9, Age = 99, FirstName = "aaa", LastName = "ika" },
                new Sample { Id = 10, Age = 9, FirstName = "eee", LastName = "baz" },
            };
            return data;
        }

        ISampleTable CreateTable()
        {
            return new Database(sampleTable: CreateData()).SampleTable;
        }

        [Fact]
        public void Unique()
        {
            var table = CreateTable();

            table.GetById(8).Id.Should().Be(8);
            Assert.Throws<KeyNotFoundException>(() => table.GetById(100));

            table.FindByIdAndAge((4, 89)).Id.Should().Be(4);

            Assert.Throws<KeyNotFoundException>(() => table.FindByIdAndAge((4, 899)));
            Assert.Throws<KeyNotFoundException>(() => table.FindByIdAndAge((5, 89)));

            table.FindByIdAndAgeAndFirstName((6, 29, "bbb")).Id.Should().Be(6);
            Assert.Throws<KeyNotFoundException>(() => table.FindByIdAndAgeAndFirstName((6, 29, "bbbz")));
        }

        [Fact]
        public void Range()
        {
            var table = CreateTable();

            var test = table.FindByFirstName("eee");

            table.FindByFirstName("eee").Select(x => x.Id).OrderBy(x => x).Should().BeEquivalentTo(new[] { 1, 10 });
            table.FindByFirstName("eeee").Count.Should().Be(0);

            table.FindClosestByFirstNameAndLastName(("aaa", "takz")).Id.Should().Be(4);
        }
    }
}
