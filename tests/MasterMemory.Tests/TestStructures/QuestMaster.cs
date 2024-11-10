using MasterMemory.Annotations;
using MasterMemory.Validation;
using MemoryPack;

namespace MasterMemory.Tests
{
    [Table("quest_master"), MemoryPackable]
    public partial record QuestMaster
    {
        [PrimaryKey]
        public int QuestId { get; init; }

        [UniqueKey]
        public string Name { get; init; }

        [SecondaryKey]
        public int RewardItemId { get; init; }

        public int Cost { get; init; }
    }

    [Validator]
    internal partial class QuestMasterValidator
    {
        public void Validate(IDatabase db, IValidator validator)
        {
            
            validator.Exists(db.QuestmasterTable.GetAllSortedByRewardItemId(),
                db.ItemmasterTable.GetAllSortedByItemId());

            var helper = validator.GetTableValidator(db.QuestmasterTable);
            helper.Validate(x => x.Cost <= 100, x => "Cost <= 100, Cost = " + x.Cost);
            helper.Validate(x => x.Cost >= 0, x => ">= 0!!!");
            helper.ValidateAction(x => x.Cost <= 1000, x => "Cost <= 1000, Cost = " + x.Cost);
            helper.ValidateAction(x => x.Cost >= -90, x => ">= -90!!!");
            helper.Unique(x => x.Name);
        }
    }

    [Table("item_master"), MemoryPackable]
    public partial class ItemMaster
    {
        [PrimaryKey]
        public int ItemId { get; init; }
    }

    [Table("quest_master_empty"), MemoryPackable]
    public partial class QuestMasterEmptyValidate
    {
        [PrimaryKey]
        public int QuestId { get; init; }

        public string Name { get; init; }

        public int RewardItemId { get; init; }

        public int Cost { get; init; }
    }

    [Table("item_master_empty"), MemoryPackable]
    public partial class ItemMasterEmptyValidate
    {
        [PrimaryKey]
        public int ItemId { get; init; }
    }

    [Table("sequantial_master"), MemoryPackable]
    [Validator]
    public partial class SequentialCheckMaster
    {
        [PrimaryKey]
        public int Id { get; init; }

        [SecondaryKey]
        public int Cost { get; init; }

        public void Validate(IDatabase db, IValidator validator)
        {
            validator.Sequential(db.SequantialmasterTable.GetAllSortedById());
            validator.Sequential(db.SequantialmasterTable.GetAllSortedByCost());
        }
    }

    [Table("fail"), MemoryPackable]
    public partial class Fail
    {
        [PrimaryKey]
        public int Id { get; init; }
    }

    [Validator]
    internal partial class FailValidator
    {
        public void Validate(IDatabase db, IValidator validator)
        {
            validator.Validate(db.FailTable, x => false, x => "Id: " + x.Id);
        }
    }
}