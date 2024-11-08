using System;
using System.Collections.Generic;
using System.Threading;

namespace MasterMemory
{
    public interface ITransactionObserver<TValue> : IReadOnlyList<Operation<TValue>>
    {
        event Action<TransactionType, CancellationToken> OnTransaction;
    }
}