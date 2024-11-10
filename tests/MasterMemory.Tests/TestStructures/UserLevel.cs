using MasterMemory.Annotations;
using MemoryPack;

namespace MasterMemory.Tests
{
    [Table("UserLevel"), MemoryPackable]
    public partial class UserLevel
    {
        [PrimaryKey]
        public int Level { get; init; }
        [SecondaryKey(0)]
        public int Exp { get; init; }
    }
}