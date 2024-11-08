namespace MasterMemory
{
    public readonly struct Operation<TValue>
    {
        public readonly OperationType Type;

        /// <summary>
        /// Null when <see cref="Type"/> is <see cref="OperationType.Clear"/>.
        /// </summary>
        public readonly TValue Item;

        public Operation(OperationType type, in TValue item)
        {
            Type = type;
            Item = item;
        }
    }
}