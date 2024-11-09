using System;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable CheckNamespace

namespace MasterMemory.Annotations
{
    [Flags]
    public enum DatabaseFlags
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
    public enum DbTableFlags
    {
        None = 0,
        MultithreadedInitialization = 1 << 0,
        MultithreadedModifications = 1 << 1,
        Multithreaded = MultithreadedInitialization | MultithreadedModifications,
    }
}