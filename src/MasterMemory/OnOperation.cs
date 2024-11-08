namespace MasterMemory
{
    public delegate void OnOperation<TElement>(in OperationChange<TElement> operation);
}