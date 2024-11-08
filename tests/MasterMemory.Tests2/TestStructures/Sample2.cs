using MemoryPack;

namespace MasterMemory.Tests2.TestStructures
{
    [Table("s_a_m_p_l_e"), MemoryPackable]
    public partial record Sample2
    {
        [PrimaryKey]
        [SecondaryKey(1)]
        [SecondaryKey(2)]
        [SecondaryKey(3)]
        [MemoryPackOrder(0)]
        public int Id { get; set; }

        [SecondaryKey]
        [SecondaryKey(1)]
        [SecondaryKey(2)]
        [SecondaryKey(3)]
        [SecondaryKey(6, 1)]
        [MemoryPackOrder(3)]
        public int Age { get; set; }

        [SecondaryKey]
        [SecondaryKey(0), UniqueKey(0)]
        [SecondaryKey(1)]
        [SecondaryKey(3)]
        [SecondaryKey(6, 0)]
        [MemoryPackOrder(1)]
        public string FirstName { get; set; }

        [SecondaryKey]
        [SecondaryKey(0), UniqueKey(0)]
        [SecondaryKey(1)]
        [MemoryPackOrder(2)]
        public string LastName { get; set; }

        public override string ToString()
        {
            return $"{Id} {Age} {FirstName} {LastName}";
        }

        public Sample2(int Id, int Age, string FirstName, string LastName)
        {
            this.Id = Id;
            this.Age = Age;
            this.FirstName = FirstName;
            this.LastName = LastName;
        }
    }
}