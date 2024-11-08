using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using MasterMemory.Tests2;
using MasterMemory.Tests2.TestStructures;
using MemoryPack;
using Xunit;
using Xunit.Abstractions;

namespace MasterMemory.Tests
{
    public class SerializationTest
    {
        private readonly ITestOutputHelper _output;

        public SerializationTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private Database2 ToDatabase2(Database database)
        {
            var sampleArray = database.SampleTable.GetAllSorted()
                .Select(x => new Sample2(x.Id, x.Age, x.FirstName, x.LastName)).ToArray();

            var peopleArray = database.PeopleTable.GetAllSorted().Select(x => new PersonModel2
            {
                RandomId = x.RandomId,
                FirstName = x.FirstName,
                LastName = x.LastName
            }).ToArray();

            return new Database2(sampleTable: sampleArray, peopleTable: peopleArray);
        }
        Database CreateData()
        {
            // Id = Unique, PK
            // FirstName + LastName = Unique
            var sampleArray = new[]
            {
                new Sample
                {
                    Id = 5,
                    Age = 19,
                    FirstName = "aaa",
                    LastName = "foo"
                },
                new Sample
                {
                    Id = 6,
                    Age = 29,
                    FirstName = "bbb",
                    LastName = "foo"
                },
                new Sample
                {
                    Id = 7,
                    Age = 39,
                    FirstName = "ccc",
                    LastName = "foo"
                },
                new Sample
                {
                    Id = 8,
                    Age = 49,
                    FirstName = "ddd",
                    LastName = "foo"
                },
                new Sample
                {
                    Id = 1,
                    Age = 59,
                    FirstName = "eee",
                    LastName = "foo"
                },
                new Sample
                {
                    Id = 2,
                    Age = 89,
                    FirstName = "aaa",
                    LastName = "bar"
                },
                new Sample
                {
                    Id = 3,
                    Age = 79,
                    FirstName = "be",
                    LastName = "de"
                },
                new Sample
                {
                    Id = 4,
                    Age = 89,
                    FirstName = "aaa",
                    LastName = "tako"
                },
                new Sample
                {
                    Id = 9,
                    Age = 99,
                    FirstName = "aaa",
                    LastName = "ika"
                },
                new Sample
                {
                    Id = 10,
                    Age = 9,
                    FirstName = "eee",
                    LastName = "baz"
                },
            };

            var personArray = new[]
            {
                new PersonModel
                {
                    RandomId = "1",
                    FirstName = "John",
                    LastName = "Doe"
                },
                new PersonModel
                {
                    RandomId = "2",
                    FirstName = "Jane",
                    LastName = "Doe"
                },
                new PersonModel
                {
                    RandomId = "3",
                    FirstName = "John",
                    LastName = "Smith"
                },
                new PersonModel
                {
                    RandomId = "4",
                    FirstName = "Jane",
                    LastName = "Smith"
                }
            };

            return new Database(sampleTable: sampleArray, peopleTable: personArray);
        }

        [Fact]
        public void MemoryPackSerialize()
        {
            using var db = CreateData();
            var bytes = MemoryPackSerializer.Serialize(db);
            using var db2 = MemoryPackSerializer.Deserialize<Database>(bytes);
            AssertEqual(db, db2);
        }

        [Fact]
        public void JsonSerialize()
        {
            using var db = CreateData();
            var json = db.ToJson(true);
            _output.WriteLine(json);
            var db2 = Database.FromJson(json);
            AssertEqual(db, db2);
        }

        [Fact]
        public void NewtonSoftJsonSerialize()
        {
            using var tempDb = CreateData();
            using var db = ToDatabase2(tempDb);
            var json = db.ToJson(true);
            _output.WriteLine(json);
            var db2 = Database2.FromJson(json);
            AssertEqual(db, db2);
        }

        private void AssertEqual(Database db, Database db2)
        {
            AssertEqual(db.SampleTable, db2.SampleTable);
            AssertEqual(db.PeopleTable, db2.PeopleTable);
        }

        private void AssertEqual(Database2 db, Database2 db2)
        {
            AssertEqual(db.SampleTable, db2.SampleTable);
            AssertEqual(db.PeopleTable, db2.PeopleTable);
        }

        private void AssertEqual<TKey, TValue>(ITable<TKey, TValue> table, ITable<TKey, TValue> table2)
        {
            table.Count.Should().Be(table2.Count);
            table.GetAllSorted().SequenceEqual(table2.GetAllSorted()).Should().BeTrue();
        }
    }
}