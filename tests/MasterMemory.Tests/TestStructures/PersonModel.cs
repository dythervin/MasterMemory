using MasterMemory.Annotations;
using MemoryPack;

namespace MasterMemory.Tests
{
    [Table("people"), MemoryPackable]
    public readonly partial struct PersonModel
    {
        [SecondaryKey()]
        [SecondaryKey(1)]
        public string FirstName { get; init; }

        [SecondaryKey()]
        [SecondaryKey(1)]
        public string LastName { get; init; }

        [PrimaryKey] public string RandomId { get; init; }
    }
}