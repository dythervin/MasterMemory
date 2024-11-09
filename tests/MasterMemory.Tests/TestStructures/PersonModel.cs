using MasterMemory.Annotations;
using MemoryPack;

namespace MasterMemory.Tests
{
    [Table("people"), MemoryPackable]
    public partial struct PersonModel
    {
        [SecondaryKey()]
        [SecondaryKey(1)]
        public string FirstName { get; set; }

        [SecondaryKey()]
        [SecondaryKey(1)]
        public string LastName { get; set; }

        [PrimaryKey] public string RandomId { get; set; }
    }
}