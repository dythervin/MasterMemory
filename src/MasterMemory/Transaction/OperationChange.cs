using System;

namespace MasterMemory
{
    public readonly struct OperationChange<TValue>
    {
        public readonly OperationType Type;

        public readonly TValue? Previous;
        public readonly bool HasPrevious;

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

        public OperationChange(OperationType type, in TValue previous, in TValue value)
        {
            Type = type;
            Previous = previous;
            _value = value;
            HasPrevious = true;
        }

        public OperationChange(OperationType type, in TValue? value)
        {
            Type = type;
            Previous = default;
            HasPrevious = false;
            _value = value;
        }

        public static implicit operator Operation<TValue>(OperationChange<TValue> operation)
        {
            return new Operation<TValue>(operation.Type, operation.ValueOrDefault);
        }
    }
}