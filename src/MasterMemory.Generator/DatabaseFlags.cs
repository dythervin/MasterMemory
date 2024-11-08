using System;

namespace MasterMemory.Generator;

[Flags]
internal enum DatabaseFlags
{
    None = 0,

    UniRx = 1 << 1,
    R3 = 1 << 2,

    UniTask = 1 << 3,

    MemoryPack = 1 << 5,

    SystemTextJson = 1 << 6,
    NewtonsoftJson = 1 << 7,
}

[Flags]
internal enum DbTableFlags
{
    None = 0,
    MultithreadedInitialization = 1 << 0,
    MultithreadedModifications = 1 << 1,
    Multithreaded = MultithreadedInitialization | MultithreadedModifications,
}

internal static class DbTableFlagsExtensions
{
    public static bool HasFlagFast(this DbTableFlags value, DbTableFlags flag)
    {
        return (value & flag) != 0;
    }
}

internal static class DatabaseFlagsExtensions
{
    public static bool HasFlagFast(this DatabaseFlags value, DatabaseFlags flag)
    {
        return (value & flag) != 0;
    }
}