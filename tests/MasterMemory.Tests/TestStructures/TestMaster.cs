using MasterMemory.Annotations;
using MemoryPack;

namespace MasterMemory.Tests
{
    [MemoryPackable]
    [Table(nameof(TestMaster))]
    public partial class TestMaster
    {
        [PrimaryKey]
        public int TestID { get; set; }

        public int Value { get; set; }

        public TestMaster(int TestID, int Value)
        {
            this.TestID = TestID;
            this.Value = Value;
        }
    }
}