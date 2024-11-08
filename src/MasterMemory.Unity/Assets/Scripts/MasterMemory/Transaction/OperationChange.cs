namespace MasterMemory
{
    public readonly struct OperationChange<TValue>
    {
        public readonly OperationType Type;

        public readonly TValue? Previous;
        public readonly bool HasPrevious;

        public readonly TValue? Item;

        public OperationChange(OperationType type, in TValue previous, in TValue item)
        {
            Type = type;
            Previous = previous;
            Item = item;
            HasPrevious = true;
        }

        public OperationChange(OperationType type, in TValue? item)
        {
            Type = type;
            Previous = default;
            HasPrevious = false;
            Item = item;
        }
        
        public static implicit operator Operation<TValue>(OperationChange<TValue> operation)
        {
            return new Operation<TValue>(operation.Type, operation.Item);
        }
    }
}