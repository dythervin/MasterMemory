using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;
using MasterMemory.Annotations;

namespace MasterMemory.Generator.Core;

internal static class TransactionGenerator
{
    public const string Name = "DbTransaction";

    internal static void Execute(in SourceGeneratorContext context, in ImmutableArray<TableModel> tableArray,
        DatabaseModel database, StringBuilder sb)
    {
        sb.AppendUsings(tableArray[0].DatabaseModel);
        bool isMultithreaded = tableArray.IsMultithreadedTransaction();

        using (sb.NamespaceScope(database.Namespace))
        {
            sb.Append(database.AccessibilityModifier);
            using (sb.AppendLine($" class {Name} : DbTransactionBase, ").AppendJoin(", ",
                       tableArray,
                       (builder, model) =>
                       {
                           builder.Append("IDbTransaction<").Append(model.GlobalTypeName).Append(">");
                       }).BracketScope())
            {
                sb.AppendLine(
                    $"private readonly IReadOnlyDictionary<System.Type, I{DbItemObserverGenerator.Name}> _observers;");

                sb.AppendLine("private TableContainer _container;");
                if (isMultithreaded)
                {
                    sb.AppendLine("private int _state;");
                    sb.AppendLine("private readonly System.Action _keysSortTick;");
                    sb.AppendLine("private readonly System.Action _keysSortAction;");
                    sb.AppendLine("private int _threadCount;");
                    sb.AppendLine("private int _nextThreadIndex;");
                    sb.AppendLine("private int _maxDegreeOfParallelism;");
                    sb.AppendLine("private int[] _taskLocks = new int[")
                        .Append(tableArray.Sum(x => x.IsMultithreadedModifications ? x.ThreadCount : 0))
                        .AppendLine("];");

                    using (sb.AppendLine("public int MaxDegreeOfParallelism").BracketScope())
                    {
                        sb.AppendLine("get => _maxDegreeOfParallelism;");
                        using (sb.AppendLine("set").BracketScope())
                        {
                            sb.AppendLine(
                                "if (value < 1) throw new System.ArgumentOutOfRangeException(nameof(value));");

                            sb.AppendLine("_maxDegreeOfParallelism = value;");
                        }
                    }
                }

                foreach (TableModel model in tableArray)
                {
                    sb.Append($"private readonly {DbItemObserverGenerator.Name}<").Append(model.GlobalTypeName)
                        .Append("> _").AppendDecapitalized(model.TableName).AppendLine("Observer;");

                    sb.AppendOperations(model);
                    sb.AppendOperations(model, "Insert");
                    sb.AppendOperations(model, "InsertOrReplace");
                    sb.AppendOperations(model, "Replace");
                    sb.AppendOperations(model, "Remove");
                    sb.AppendRemoveByKey(model);
                    using (sb.Append("void IDbTransaction<").Append(model.GlobalTypeName).Append(">.Clear()")
                               .BracketScope())
                    {
                        sb.Append("Clear").Append(model.TableName).Append("();");
                    }

                    using (sb.Append("public void Clear").Append(model.TableName).Append("()").BracketScope())
                    {
                        sb.AppendLine("AssertCanExecuteOperations();");
                        sb.Append("if (this._container.").Append(model.TableName).AppendLine(".Count == 0) return;");

                        sb.Append("this._container.").Append(model.TableName).AppendLine(".Clear();");
                        if (isMultithreaded)
                            sb.Append("this.TryScheduleKeysSort();");
                    }
                }

                using (sb.Append("public void Clear()").BracketScope())
                {
                    sb.AppendLine("AssertCanExecuteOperations();");
                    foreach (TableModel model in tableArray)
                    {
                        sb.Append("this._container.").Append(model.TableName).AppendLine(".Clear();");
                    }

                    if (isMultithreaded)
                        sb.Append("this.TryScheduleKeysSort();");
                }

                if (isMultithreaded)
                {
                    using (sb.Append("public ").Append(Name).Append("(TableContainer container")
                               .Append("): this(container, ").AppendDefaultMaxDegreeOfParallelism(tableArray)
                               .Append(")").BracketScope())
                    {
                    }
                }

                using (sb.Append("public ").Append(Name).Append("(TableContainer container")
                           .Append(isMultithreaded, ", int maxDegreeOfParallelism").Append(")").BracketScope())
                {
                    sb.AppendLine("_container = container;");
                    if (isMultithreaded)
                    {
                        sb.AppendLine("_keysSortTick = KeysSortTick;");
                        sb.AppendLine("_keysSortAction = ExecuteKeysSort;");
                        sb.AppendLine("_maxDegreeOfParallelism = maxDegreeOfParallelism;");
                    }

                    sb.AppendLine($"_observers = new Dictionary<System.Type, I{DbItemObserverGenerator.Name}>(")
                        .Append(tableArray.Length).AppendLine(")");

                    using (sb.BracketScope())
                    {
                        sb.AppendJoin(", ",
                            tableArray,
                            (sb, model) =>
                            {
                                sb.Append("{ typeof(").Append(model.GlobalTypeName).Append("), ").Append("this._")
                                    .AppendDecapitalized(model.TableName)
                                    .Append($"Observer = new {DbItemObserverGenerator.Name}<")
                                    .Append(model.GlobalTypeName).AppendLine(">()}");
                            });
                    }

                    sb.AppendLine(";");

                    for (int i = 0; i < tableArray.Length; i++)
                    {
                        TableModel model = tableArray[i];
                        sb.Append("this._container.").Append(model.TableName)
                            .Append(".OnChange += (in OperationChange<").Append(model.GlobalTypeName)
                            .Append("> operation) => ");

                        using (sb.BracketScope())
                        {
                            sb.Append("this._").AppendDecapitalized(model.TableName)
                                .Append("Observer.Enqueue(operation);");

                            sb.AppendLine("this.OnTransaction(").Append(i).Append(");");
                        }

                        sb.Append(';');
                    }

                    if (isMultithreaded)
                    {
                        sb.AppendLine("this.TryScheduleKeysSort();");
                    }
                }

                sb.AppendLine($"public I{DbItemObserverGenerator.Name}<T> GetObserver<T>()");
                using (sb.BracketScope())
                {
                    sb.Append($"return (I{DbItemObserverGenerator.Name}<T>)this._observers[typeof(T)];");
                }

                if (isMultithreaded)
                {
                    using (sb.Append("private void RunOnThreadPool(System.Action action)").BracketScope())
                    {
                        sb.Append("this.RunOnThreadPool(action, CancellationToken.None);");
                    }

                    using (sb.Append(
                                   "private void RunOnThreadPool(System.Action action, CancellationToken cancellationToken)")
                               .BracketScope())
                    {
                        if (database.HasFlag(DatabaseFlags.UniTask))
                        {
                            sb.Append(
                                "Cysharp.Threading.Tasks.UniTask.RunOnThreadPool(action, cancellationToken: cancellationToken);");
                        }
                        else
                        {
                            sb.Append("System.Threading.Tasks.Task.Run(action, cancellationToken);");
                        }
                    }

                    using (sb.Append("private void TryScheduleKeysSort()").BracketScope())
                    {
                        using (sb.Append(
                                       "if (Interlocked.CompareExchange(ref this._state, (int)DbTransactionState.Scheduled, (int)DbTransactionState.None) == (int)DbTransactionState.None)")
                                   .BracketScope())
                        {
                            sb.AppendLine("this.RunOnThreadPool(this._keysSortTick);");
                        }
                    }

                    using (sb.AppendLine("private async void ExecuteKeysSort()").BracketScope())
                    {
                        sb.Append("int index = Interlocked.Increment(ref this._nextThreadIndex) - 1;");
                        using (sb.Append("do").BracketScope())
                        {
                            using (sb.Append("for (int i = 0; i < this._taskLocks.Length; i++)").BracketScope())
                            {
                                sb.Append("int taskIndex = (index + i) % this._taskLocks.Length;");

                                using (sb.Append(
                                               "if (Interlocked.CompareExchange(ref this._taskLocks[taskIndex], 1, 0) == 0)")
                                           .BracketScope())
                                {
                                    sb.AppendLine("this.ExecuteKeysSort(taskIndex);");
                                    sb.AppendLine("Interlocked.Exchange(ref this._taskLocks[taskIndex], 0);");
                                }
                            }

                            sb.AppendLine("await Task.Yield();");
                        }

                        sb.AppendLine("while (this._state == (int)DbTransactionState.Running);");
                        sb.AppendLine("Interlocked.Decrement(ref this._threadCount);");
                    }

                    using (sb.AppendLine("private void ExecuteKeysSort(int taskIndex)").BracketScope())
                    {
                        int index = 0;
                        using (sb.Append("switch (taskIndex)").BracketScope())
                        {
                            foreach (TableModel model in tableArray)
                            {
                                if (!model.IsMultithreadedModifications)
                                    continue;

                                for (int i = 0; i < model.ThreadCount; i++)
                                {
                                    sb.Append("case ").Append(index++).Append(": this._container.")
                                        .Append(model.TableName).Append(".ApplySortOperations(");

                                    if (model.ThreadCount > 1)
                                    {
                                        sb.Append(i);
                                    }

                                    sb.AppendLine("); break;");
                                }
                            }

                            sb.Append(
                                "default: throw new System.ArgumentOutOfRangeException(nameof(taskIndex), taskIndex, null);");
                        }
                    }

                    using (sb.AppendLine("private async void KeysSortTick()").BracketScope())
                    {
                        sb.Append(
                                "if (Interlocked.CompareExchange(ref this._state, (int)DbTransactionState.Running, (int)DbTransactionState.Scheduled) != (int)DbTransactionState.Scheduled)")
                            .AppendLine("throw new System.InvalidOperationException(\"Invalid state\");");

                        using (sb.Append("while (this._threadCount < this._maxDegreeOfParallelism)").BracketScope())
                        {
                            sb.AppendLine("int threadCount = Interlocked.Increment(ref this._threadCount);");
                            using (sb.AppendLine("if (threadCount > this._maxDegreeOfParallelism)").BracketScope())
                            {
                                sb.AppendLine("Interlocked.Decrement(ref this._threadCount);");
                                sb.AppendLine("break;");
                            }

                            sb.AppendLine("this.RunOnThreadPool(this._keysSortAction);");
                        }

                        using (sb.Append("do").BracketScope())
                        {
                            sb.AppendLine("await Task.Yield();");
                        }

                        sb.AppendLine("while (this.Depth > 0);");

                        sb.Append(
                                "if (Interlocked.CompareExchange(ref this._state, (int)DbTransactionState.None, (int)DbTransactionState.Running) != (int)DbTransactionState.Running)")
                            .AppendLine("throw new System.InvalidOperationException(\"Invalid state\");");
                    }
                }

                using (sb.AppendLine("protected override void ClearBuffers()").BracketScope())
                {
                    sb.AppendLine("base.ClearBuffers();");
                    foreach (TableModel model in tableArray)
                    {
                        sb.Append("this._container.").Append(model.TableName).AppendLine(".ClearRollback();");
                        sb.Append("this._").AppendDecapitalized(model.TableName).Append("Observer.Clear();");
                    }
                }

                using (sb.AppendLine("protected override void CommitOperation(int tableIndex)").BracketScope())
                {
                    using (sb.AppendLine("switch (tableIndex)").BracketScope())
                    {
                        for (int i = 0; i < tableArray.Length; i++)
                        {
                            TableModel model = tableArray[i];
                            sb.Append("case ").Append(i).Append(": this._").AppendDecapitalized(model.TableName)
                                .Append("Observer.PublishNext(); break;");
                        }

                        sb.Append(
                            "default: throw new System.ArgumentOutOfRangeException(nameof(tableIndex), tableIndex, null);");
                    }
                }

                using (sb.AppendLine("protected override void RollbackInternal()").BracketScope())
                {
                    foreach (TableModel model in tableArray)
                    {
                        sb.Append("this._container.").Append(model.TableName).AppendLine(".Rollback();");
                    }
                }

                using (sb.AppendLine("protected override void OnDispose()").BracketScope())
                {
                    foreach (TableModel model in tableArray)
                    {
                        sb.Append("this._").AppendDecapitalized(model.TableName).Append("Observer.Dispose();");
                    }

                    sb.AppendLine("base.OnDispose();");
                }
            }
        }

        context.AddSource($"Db.Transaction.g.cs", sb.ToStringAndClear());
    }

    private static StringBuilder AppendOperations(this StringBuilder sb, TableModel model, string? operation = null)
    {
        bool isMultithreaded = model.IsMultithreadedModifications;
        bool isExecute = string.IsNullOrEmpty(operation);
        if (isExecute)
            operation = "Execute";

        using (sb.Append("public bool ").Append(operation).Append("(in ").Append(isExecute, "Operation<")
                   .Append(model.GlobalTypeName).Append(isExecute, ">").Append(" item)").BracketScope())
        {
            sb.AppendLine("AssertCanExecuteOperations();");

            sb.Append("if(!this._container.").Append(model.TableName).AppendLine(".Execute(");
            if (!isExecute)
            {
                sb.Append("new Operation<").Append(model.GlobalTypeName).Append(">(OperationType.").Append(operation)
                    .Append(", item)");
            }
            else
            {
                sb.Append("item");
            }

            sb.Append(")) return false;");

            if (isMultithreaded)
                sb.Append("this.TryScheduleKeysSort();");

            sb.AppendLine("return true;");
        }

        using (sb.Append("public int ").Append(operation).Append("(IReadOnlyList<").Append(isExecute, "Operation<")
                   .Append(model.GlobalTypeName).Append(isExecute, ">").Append("> items)").BracketScope())
        {
            sb.AppendLine("if(items.Count == 0) return 0;");
            sb.AppendLine("AssertCanExecuteOperations();");

            sb.Append("int changed = 0;");

            using (sb.Append("for (int i = 0; i < items.Count; i++)").BracketScope())
            {
                sb.Append("if(!this._container.").Append(model.TableName).AppendLine(".Execute(");
                if (!isExecute)
                {
                    sb.Append("new Operation<").Append(model.GlobalTypeName).Append(">(OperationType.")
                        .Append(operation).Append(", items[i])");
                }
                else
                {
                    sb.Append("items[i]");
                }

                sb.Append(")) continue;");

                sb.Append("changed++;");
            }

            sb.Append("if (changed == 0) return 0;");

            if (isMultithreaded)
                sb.Append("this.TryScheduleKeysSort();");

            sb.AppendLine("return changed;");
        }

        using (sb.Append("public int ").Append(operation).Append("(IEnumerable<").Append(isExecute, "Operation<")
                   .Append(model.GlobalTypeName).Append(isExecute, ">").Append("> items)").BracketScope())
        {
            sb.AppendLine("AssertCanExecuteOperations();");
            sb.Append("if (items is IReadOnlyList<").Append(isExecute, "Operation<").Append(model.GlobalTypeName)
                .Append(isExecute, ">").Append("> list) return ").Append(operation).Append("(list);");

            sb.Append("int changed = 0;");

            using (sb.Append("foreach (var item in items)").BracketScope())
            {
                sb.Append("if(!this._container.").Append(model.TableName).AppendLine(".Execute(");

                if (!isExecute)
                {
                    sb.Append("new Operation<").Append(model.GlobalTypeName).Append(">(OperationType.")
                        .Append(operation).Append(", item)");
                }
                else
                {
                    sb.Append("item");
                }

                sb.Append(")) continue;");

                sb.Append("changed++;");
            }

            sb.Append("if (changed == 0) return 0;");

            if (isMultithreaded)
                sb.Append("this.TryScheduleKeysSort();");

            sb.AppendLine("return changed;");
        }

        return sb;
    }

    private static StringBuilder AppendRemoveByKey(this StringBuilder sb, TableModel model)
    {
        bool isMultithreaded = model.IsMultithreadedModifications;

        KeyGroupModel primaryKey = model.PrimaryKey;
        using (sb.Append("public bool Remove").Append(model.TypeName).Append("By").Append(primaryKey.Name).Append("(")
                   .Append(primaryKey.Modifier).Append(" ").Append(primaryKey.Type).Append(" key)").BracketScope())
        {
            sb.AppendLine("AssertCanExecuteOperations();");

            sb.Append("if(!this._container.").Append(model.TableName).AppendLine(".Remove(").Append(primaryKey.Modifier)
                .Append(" key)) return false;");

            if (isMultithreaded)
                sb.Append("this.TryScheduleKeysSort();");

            sb.AppendLine("return true;");
        }

        using (sb.Append("public int Remove").Append(model.TypeName).Append("By").Append(primaryKey.Name).Append("(")
                   .Append(" IReadOnlyList<").Append(primaryKey.Type).Append("> keys)").BracketScope())
        {
            sb.AppendLine("AssertCanExecuteOperations();");
            sb.AppendLine("if(keys.Count == 0) return 0;");

            sb.AppendLine("int changed = 0;");
            using (sb.Append("for (int i = 0; i < keys.Count; i++)").BracketScope())
            {
                sb.Append("if(!this._container.").Append(model.TableName).AppendLine(".Remove(")
                    .Append(primaryKey.Modifier).Append(" keys[i])) continue;");

                sb.AppendLine("changed++;");
            }

            sb.AppendLine("if (changed == 0) return 0;");

            if (isMultithreaded)
                sb.Append("this.TryScheduleKeysSort();");

            sb.AppendLine("return changed;");
        }

        using (sb.Append("public int Remove").Append(model.TypeName).Append("By").Append(primaryKey.Name).Append("(")
                   .Append(" IEnumerable<").Append(primaryKey.Type).Append("> keys)").BracketScope())
        {
            sb.AppendLine("AssertCanExecuteOperations();");
            sb.AppendLine("if (keys is IReadOnlyList<").Append(primaryKey.Type).Append("> list) return ")
                .Append("Remove").Append(model.TypeName).Append("By").Append(primaryKey.Name).Append("(list);");

            sb.AppendLine("int changed = 0;");
            using (sb.Append("foreach (var key in keys)").BracketScope())
            {
                sb.Append("if(!this._container.").Append(model.TableName).AppendLine(".Remove(")
                    .Append(primaryKey.Modifier).Append(" key)) continue;");

                sb.AppendLine("changed++;");
            }

            sb.AppendLine("if (changed == 0) return 0;");

            if (isMultithreaded)
                sb.Append("this.TryScheduleKeysSort();");

            sb.AppendLine("return changed;");
        }

        return sb;
    }
}