using System;
using FluentAssertions;
using R3;
using Xunit;

namespace MasterMemory.Tests
{
    public class TransactionTest
    {
        [Fact]
        public void Transaction()
        {
            using var db = new Database();

            db.Transaction(transaction =>
            {
                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "ccc",
                    LastName = "ddd",
                    RandomId = "2"
                });

                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "aaa",
                    LastName = "bbb",
                    RandomId = "1"
                });

                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "eee",
                    LastName = "fff",
                    RandomId = "3"
                });
            });

            var sortedById = db.PeopleTable.GetAllSortedByRandomId();
            sortedById.Count.Should().Be(3);
            sortedById[0].RandomId.Should().Be("1");
            sortedById[1].RandomId.Should().Be("2");
            sortedById[2].RandomId.Should().Be("3");
        }

        [Fact]
        public void Transaction2()
        {
            using var db = new Database();

            db.Transaction(transaction =>
            {
                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "ccc",
                    LastName = "ddd",
                    RandomId = "2"
                });

                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "aaa",
                    LastName = "bbb",
                    RandomId = "1"
                });

                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "eee",
                    LastName = "fff",
                    RandomId = "1"
                });
            });

            var sortedById = db.PeopleTable.GetAllSortedByRandomId();
            sortedById.Count.Should().Be(2);
            sortedById[0].RandomId.Should().Be("1");
            sortedById[0].FirstName.Should().Be("eee");
            sortedById[1].RandomId.Should().Be("2");
        }

        [Fact]
        public void TransactionFail()
        {
            using var db = new Database();

            db.TransactionSafe(transaction =>
            {
                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "ccc",
                    LastName = "ddd",
                    RandomId = "2"
                });

                transaction.InsertOrReplace(new Sample()
                {
                    Age = 21,
                    FirstName = "aaa",
                    Id = 10,
                    LastName = "lll"
                });

                transaction.InsertOrReplace(new Sample()
                {
                    Age = 26,
                    FirstName = "fff",
                    Id = 1,
                    LastName = "ooo"
                });

                transaction.InsertOrReplace(new Sample()
                {
                    Age = 21,
                    FirstName = "aaa",
                    Id = 4,
                    LastName = "weq"
                });

                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "aaa",
                    LastName = "bbb",
                    RandomId = "1"
                });

                db.PeopleTable.GetAllSorted().Count.Should().Be(2);
                db.PeopleTable.Count.Should().Be(2);
                db.SampleTable.GetAllSorted().Count.Should().Be(3);
                db.SampleTable.Count.Should().Be(3);

                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "eee",
                    LastName = "fff",
                    RandomId = "1"
                });

                throw new Exception("error");
            }).Should().BeFalse();

            db.PeopleTable.Count.Should().Be(0);
            var all = db.PeopleTable.GetAllSorted();
            all.Count.Should().Be(0);
        }

        [Fact]
        public void TransactionOrder()
        {
            using var db = new Database();
            int i = 0;
            using var personObserver = db.Transaction.GetObserver<PersonModel>().OnCommit.Subscribe(x =>
            {
                var item = x.Item;
                switch (i++)
                {
                    case 0:
                        item.FirstName.Should().Be("ccc");
                        item.LastName.Should().Be("ddd");
                        item.RandomId.Should().Be("2");
                        break;
                    case 4:
                        item.FirstName.Should().Be("aaa");
                        item.LastName.Should().Be("bbb");
                        item.RandomId.Should().Be("1");
                        break;
                    case 5:
                        item.FirstName.Should().Be("eee");
                        item.LastName.Should().Be("fff");
                        item.RandomId.Should().Be("3");
                        break;
                    default:
                        return;
                }
            
                x.Type.Should().Be(OperationType.InsertOrReplace);
            });

            using var sampleObserver = db.Transaction.GetObserver<Sample>().OnCommit.Subscribe(x =>
            {
                var item = x.Item;
                switch (i++)
                {
                    case 1:
                        item.Age.Should().Be(21);
                        item.FirstName.Should().Be("aaa");
                        item.Id.Should().Be(10);
                        item.LastName.Should().Be("lll");
                        break;
                    case 2:
                        item.Age.Should().Be(26);
                        item.FirstName.Should().Be("fff");
                        item.Id.Should().Be(1);
                        item.LastName.Should().Be("ooo");
                        break;
                    case 3:
                        item.Age.Should().Be(21);
                        item.FirstName.Should().Be("aaa");
                        item.Id.Should().Be(4);
                        item.LastName.Should().Be("weq");
                        break;
                    default:
                        return;
                }

                x.Type.Should().Be(OperationType.InsertOrReplace);
            });

            db.Transaction(transaction =>
            {
                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "ccc",
                    LastName = "ddd",
                    RandomId = "2"
                });

                transaction.InsertOrReplace(new Sample()
                {
                    Age = 21,
                    FirstName = "aaa",
                    Id = 10,
                    LastName = "lll"
                });

                transaction.InsertOrReplace(new Sample()
                {
                    Age = 26,
                    FirstName = "fff",
                    Id = 1,
                    LastName = "ooo"
                });

                transaction.InsertOrReplace(new Sample()
                {
                    Age = 21,
                    FirstName = "aaa",
                    Id = 4,
                    LastName = "weq"
                });

                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "aaa",
                    LastName = "bbb",
                    RandomId = "1"
                });

                db.PeopleTable.GetAllSorted().Count.Should().Be(2);
                db.PeopleTable.Count.Should().Be(2);
                db.SampleTable.GetAllSorted().Count.Should().Be(3);
                db.SampleTable.Count.Should().Be(3);

                transaction.InsertOrReplace(new PersonModel
                {
                    FirstName = "eee",
                    LastName = "fff",
                    RandomId = "1"
                });
            });

            db.PeopleTable.Count.Should().Be(2);
            db.PeopleTable.GetAllSorted().Count.Should().Be(2);
        
            db.SampleTable.Count.Should().Be(3);
            db.SampleTable.GetAllSorted().Count.Should().Be(3);
        }
    }
}