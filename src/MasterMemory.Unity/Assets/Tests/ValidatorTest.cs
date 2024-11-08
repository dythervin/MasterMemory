using FluentAssertions;
using Xunit;
using System.Linq;

namespace MasterMemory.Tests
{
    public class ValidatorTest
    {
        readonly Xunit.Abstractions.ITestOutputHelper output;

#if UNITY_2018_3_OR_NEWER
        public ValidatorTest()
        {
            this.output = new Xunit.Abstractions.DebugLogTestOutputHelper();
        }
#else
        public ValidatorTest(Xunit.Abstractions.ITestOutputHelper output)
        {
            this.output = output;
            //MessagePackSerializer.DefaultOptions = MessagePackSerializer.DefaultOptions.WithResolver(MessagePackResolver.Instance);
        }
#endif

        Database CreateDatabase(Fail[] data1)
        {
            return new Database(failTable: data1);
        }

        Database CreateDatabase(SequentialCheckMaster[] data1)
        {
            return new Database(sequantialmasterTable: data1);
        }

        Database CreateDatabase(QuestMaster[] data1, ItemMaster[] data2)
        {

            return new DatabaseBuilder()
                .Append(data1)
                .Append(data2)
                .Build();
        }

        Database CreateDatabase(QuestMasterEmptyValidate[] data1, ItemMasterEmptyValidate[] data2)
        {

            return new Database(questmasteremptyTable: data1, itemmasteremptyTable: data2);
        }

        [Fact]
        public void Empty()
        {
            var validateResult = CreateDatabase(new QuestMaster[]
            {
            }, new ItemMaster[]
            {
            }).Validate();

            validateResult.IsValidationFailed.Should().BeFalse();
            validateResult.FailedResults.Count.Should().Be(0);
        }

        // test IValidator

        /*
        public interface IValidator<T>
        {
            ValidatableSet<T> GetTableSet();
            ReferenceSet<T, TRef> GetReferenceSet<TRef>();
            void Validate(Expression<Func<T, bool>> predicate);
            void Validate(Func<T, bool> predicate, string message);
            void ValidateAction(Expression<Func<bool>> predicate);
            void ValidateAction(Func<bool> predicate, string message);
            void Fail(string message);
            bool CallOnce();
        }

        ReferenceSet.Exists
        ValidatableSet.Unique
        ValidatableSet.Sequential
    */

        [Fact]
        public void Exists()
        {
            var validateResult = CreateDatabase(new QuestMaster[]
            {
                new QuestMaster { QuestId = 1, RewardItemId = 1, Name = "foo" },
                new QuestMaster { QuestId = 2, RewardItemId = 3, Name = "bar" },
                new QuestMaster { QuestId = 3, RewardItemId = 2, Name = "baz" },
                new QuestMaster { QuestId = 4, RewardItemId = 5, Name = "tako"},
                new QuestMaster { QuestId = 5, RewardItemId = 4, Name = "nano"},
            }, new ItemMaster[]
            {
                new ItemMaster { ItemId = 1 },
                new ItemMaster { ItemId = 2 },
                new ItemMaster { ItemId = 3 },
            }).Validate();
            output.WriteLine(validateResult.FormatFailedResults());
            validateResult.IsValidationFailed.Should().BeTrue();

            validateResult.FailedResults[0].Message.Should().Be("Exists failed: QuestMaster.RewardItemId -> ItemMaster.ItemId, value = 4, PK(QuestId) = 5");
            validateResult.FailedResults[1].Message.Should().Be("Exists failed: QuestMaster.RewardItemId -> ItemMaster.ItemId, value = 5, PK(QuestId) = 4");
        }

        [Fact]
        public void Unique()
        {
            using var db = new Database();
            db.Transaction(transaction =>
            {
                transaction.InsertOrReplace(
                    new QuestMaster[]
                    {
                        new QuestMaster { QuestId = 1, Name = "foo" },
                        new QuestMaster { QuestId = 2, Name = "bar" },
                        new QuestMaster { QuestId = 3, Name = "bar" },
                        new QuestMaster { QuestId = 4, Name = "tako" },
                        new QuestMaster { QuestId = 5, Name = "foo" },
                    });
            });

            var all = db.QuestmasterTable.GetAllSorted();
            all.Count.Should().Be(3);
            all[0].QuestId.Should().Be(3);
            all[1].QuestId.Should().Be(4);
            all[2].QuestId.Should().Be(5);
        }

        [Fact]
        public void Sequential()
        {
            {
                var validateResult = CreateDatabase(new SequentialCheckMaster[]
                {
                    new SequentialCheckMaster { Id = 1, Cost = 10 },
                    new SequentialCheckMaster { Id = 2, Cost = 11 },
                    new SequentialCheckMaster { Id = 3, Cost = 11 },
                    new SequentialCheckMaster { Id = 4, Cost = 12 },
                }).Validate();
                output.WriteLine(validateResult.FormatFailedResults());
                validateResult.IsValidationFailed.Should().BeFalse();
            }
            {
                var validateResult = CreateDatabase(new SequentialCheckMaster[]
                {
                    new SequentialCheckMaster { Id = 1, Cost = 10 },
                    new SequentialCheckMaster { Id = 2, Cost = 11 },
                    new SequentialCheckMaster { Id = 3, Cost = 11 },
                    new SequentialCheckMaster { Id = 5, Cost = 13 },
                }).Validate();
                output.WriteLine(validateResult.FormatFailedResults());
                validateResult.IsValidationFailed.Should().BeTrue();

                validateResult.FailedResults[0].Message.Should().Be("Sequential failed: Id = (3, 5), PK(Id) = 5");
                validateResult.FailedResults[1].Message.Should().Be("Sequential failed: Cost = (11, 13), PK(Id) = 5");
            }
        }

        [Fact]
        public void Validate()
        {
            var validateResult = CreateDatabase(new QuestMaster[]
            {
                new QuestMaster { QuestId = 1, RewardItemId = 1, Name = "foo", Cost = -1 },
                new QuestMaster { QuestId = 2, RewardItemId = 3, Name = "bar", Cost = 99 },
                new QuestMaster { QuestId = 3, RewardItemId = 2, Name = "baz", Cost = 100 },
                new QuestMaster { QuestId = 4, RewardItemId = 3, Name = "tao", Cost = 101 },
                new QuestMaster { QuestId = 5, RewardItemId = 3, Name = "nao", Cost = 33 },
            }, new ItemMaster[]
            {
                new ItemMaster { ItemId = 1 },
                new ItemMaster { ItemId = 2 },
                new ItemMaster { ItemId = 3 },
            }).Validate();
            output.WriteLine(validateResult.FormatFailedResults());
            validateResult.IsValidationFailed.Should().BeTrue();
            validateResult.FailedResults.Count.Should().Be(2);

            validateResult.FailedResults[0].Message.Should().Be("Validate failed: Cost <= 100, Cost = 101, PK(QuestId) = 4");
            validateResult.FailedResults[1].Message.Should().Be("Validate failed: >= 0!!!, PK(QuestId) = 1");
        }

        [Fact]
        public void ValidateAction()
        {
            using var db = new Database();
            
            db.Transaction(transaction =>
            {
                transaction.InsertOrReplace(new QuestMaster[]
                {
                    new QuestMaster { QuestId = 1, RewardItemId = 1, Name = "foo", Cost = -100 },
                    new QuestMaster { QuestId = 2, RewardItemId = 3, Name = "bar", Cost = 99 },
                    new QuestMaster { QuestId = 3, RewardItemId = 2, Name = "baz", Cost = 100 },
                    new QuestMaster { QuestId = 4, RewardItemId = 3, Name = "tao", Cost = 1001 },
                    new QuestMaster { QuestId = 5, RewardItemId = 3, Name = "nao", Cost = 33 },
                });
                transaction.InsertOrReplace(new ItemMaster[]
                {
                    new ItemMaster { ItemId = 1 },
                    new ItemMaster { ItemId = 2 },
                    new ItemMaster { ItemId = 3 },
                });
            });

            var validateResult = db.Validate();
            output.WriteLine(validateResult.FormatFailedResults());
            validateResult.IsValidationFailed.Should().BeTrue();

            var results = validateResult.FailedResults.Select(x => x.Message).Where(x => x.Contains("ValidateAction faile")).ToArray();

            results[0].Should().Be("ValidateAction failed: Cost <= 1000, Cost = 1001, PK(QuestId) = 4");
            results[1].Should().Be("ValidateAction failed: >= -90!!!, PK(QuestId) = 1");
        }

        [Fact]
        public void Fail()
        {
            var validateResult = CreateDatabase(new Fail[]
            {
                new Fail { Id = 1},
                new Fail { Id = 2},
                new Fail { Id = 3},
            }).Validate();
            output.WriteLine(validateResult.FormatFailedResults());
            validateResult.IsValidationFailed.Should().BeTrue();

            var msg = validateResult.FailedResults.Select(x => x.Message).ToArray();
            msg[0].Should().Be("Validate failed: Id: 1, PK(Id) = 1");
            msg[1].Should().Be("Validate failed: Id: 2, PK(Id) = 2");
            msg[2].Should().Be("Validate failed: Id: 3, PK(Id) = 3");
        }
    }
}