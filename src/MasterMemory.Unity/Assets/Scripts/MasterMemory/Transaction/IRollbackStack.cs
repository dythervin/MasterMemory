namespace MasterMemory
{
    public interface IRollbackStack<T>
    {
        void Push(in OperationChange<T> change);
    }
}