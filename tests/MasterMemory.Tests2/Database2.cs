using MasterMemory.Annotations;
using UniRx;

namespace MasterMemory.Tests2
{
    [Database(DatabaseFlags.UniRx | DatabaseFlags.MemoryPack | DatabaseFlags.NewtonsoftJson)]
    public partial class Database2
    {
    }
}