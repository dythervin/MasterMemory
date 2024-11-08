using System.Collections.Generic;

namespace MasterMemory
{
    public interface IDbTransaction<T>
    {
        bool Insert(in T item);

        int Insert(IEnumerable<T> items);

        int Insert(IReadOnlyList<T> items);

        bool Replace(in T item);

        int Replace(IEnumerable<T> items);

        int Replace(IReadOnlyList<T> items);

        bool InsertOrReplace(in T item);

        int InsertOrReplace(IEnumerable<T> items);

        int InsertOrReplace(IReadOnlyList<T> items);

        bool Remove(in T item);

        int Remove(IEnumerable<T> items);

        int Remove(IReadOnlyList<T> items);

        bool Execute(in Operation<T> item);

        int Execute(IEnumerable<Operation<T>> items);

        int Execute(IReadOnlyList<Operation<T>> items);

        void Clear();
    }
}