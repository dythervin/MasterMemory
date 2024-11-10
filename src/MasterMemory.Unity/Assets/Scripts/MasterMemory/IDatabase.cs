using System;
using System.Diagnostics.CodeAnalysis;

namespace MasterMemory
{
    public interface IDatabaseBase : IDisposable
    {
        bool TryGetTable<TKey, T>([NotNullWhen(true)] out ITable<TKey, T>? table);
        
        ITable<TKey, T> GetTable<TKey, T>();
    }
}