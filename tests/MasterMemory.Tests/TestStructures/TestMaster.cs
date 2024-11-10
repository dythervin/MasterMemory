using MasterMemory.Annotations;
using MemoryPack;

namespace MasterMemory.Tests
{
    [MemoryPackable]
    [Table(nameof(TestMaster))]
    public partial class TestMaster
    {
        [PrimaryKey]
        public int TestID { get; init; }

        public int Value { get; init; }

        public TestMaster(int TestID, int Value)
        {
            this.TestID = TestID;
            this.Value = Value;
        }
    }
}