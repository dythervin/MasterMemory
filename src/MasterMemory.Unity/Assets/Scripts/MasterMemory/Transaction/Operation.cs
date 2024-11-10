using System;

namespace MasterMemory
{
    public readonly struct Operation<TValue>
    {
        public readonly OperationType Type;

        public readonly TValue? _value;

        public TValue? Value
        {
            get
            {
                if (Type == OperationType.Clear)
                {
                    throw new InvalidOperationException("Value is not available when Type is Clear.");
                }

                return _value;
            }
        }

        public TValue? ValueOrDefault
        {
            get
            {
                if (Type == OperationType.Clear)
                {
                    return default;
                }

                return _value;
            }
        }

        public Operation(OperationType type, in TValue value)
        {
            Type = type;
            _value = value;
        }
    }
}