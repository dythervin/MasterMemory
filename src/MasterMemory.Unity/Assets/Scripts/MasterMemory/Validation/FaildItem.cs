using System;

namespace MasterMemory.Validation
{
    public readonly struct FaildItem
    {
        public FaildItem(Type type, string message, object data)
        {
            Type = type;
            Message = message;
            Data = data;
        }

        public Type Type { get; }
        public string Message { get; }
        public object Data { get; }
    }
}