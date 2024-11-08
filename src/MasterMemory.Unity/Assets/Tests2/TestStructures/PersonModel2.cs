using MemoryPack;

namespace MasterMemory.Tests2.TestStructures
{
    [Table("people"), MemoryPackable]
    public partial struct PersonModel2
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