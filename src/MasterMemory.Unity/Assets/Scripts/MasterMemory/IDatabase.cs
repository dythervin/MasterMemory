using System;
using System.Diagnostics.CodeAnalysis;

namespace MasterMemory
{
    public interface IDatabase : IDisposable
    {
        bool TryGetTable<TKey, T>([NotNullWhen(true)] out ITable<TKey, T>? table);
    }
}