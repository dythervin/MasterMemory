namespace MasterMemory
{
    public interface ITableTransaction
    {
        bool IsOpen { get; }
    }

    public interface ITableTransaction<TValue> : ITableTransaction
    {
        ITransactionObserver<TValue> Observer { get; }

        void InsertOrReplace(TValue item);

        void Remove(TValue key);
    }
}