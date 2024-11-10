using MasterMemory.Annotations;
using MemoryPack;

namespace MasterMemory.Tests
{
    [Table("skillmaster"), MemoryPackable]
    public partial class SkillMaster
    {
        [PrimaryKey]
        public int SkillId { get; init; }
        [PrimaryKey]
        public int SkillLevel { get; init; }
        public int AttackPower { get; init; }
        public string SkillName { get; init; }
        public string Description { get; init; }

        public SkillMaster(int SkillId, int SkillLevel, int AttackPower, string SkillName, string Description)
        {
            this.SkillId = SkillId;
            this.SkillLevel = SkillLevel;
            this.AttackPower = AttackPower;
            this.SkillName = SkillName;
            this.Description = Description;
        }

    }
}