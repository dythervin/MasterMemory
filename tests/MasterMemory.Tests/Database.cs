namespace MasterMemory.Tests
{
    [Database(DatabaseFlags.R3 | DatabaseFlags.UniRx | DatabaseFlags.MemoryPack | DatabaseFlags.SystemTextJson |
              DatabaseFlags.NewtonsoftJson)]
    public partial class Database
    {
    }
}