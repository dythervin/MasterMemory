using MasterMemory.Annotations;

namespace MasterMemory.Tests2
{
    [Database(DatabaseFlags.UniRx | DatabaseFlags.MemoryPack | DatabaseFlags.NewtonsoftJson)]
    public partial class Database2
    {
    }
}