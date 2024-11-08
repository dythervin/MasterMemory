using System;
using System.Collections.Generic;

namespace MasterMemory
{
    public abstract class DatabaseBuilderBase
    {
        public abstract void AppendDynamic(Type type, IEnumerable<object> items);
    }
}