namespace MasterMemory
{
    public delegate void OnOperationChange<TElement>(in OperationChange<TElement> operation);
    public delegate void OnOperation<TElement>(in Operation<TElement> operation);
}