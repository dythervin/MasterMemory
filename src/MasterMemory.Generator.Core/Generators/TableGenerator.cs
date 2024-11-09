using System.Diagnostics;
using System.Text;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;
using Microsoft.CodeAnalysis;
using MasterMemory.Annotations;

namespace MasterMemory.Generator.Core;

internal static class TableGenerator
{
    private const string PrimaryKey = "MainKey";
    private const string Element = "TElement";

    private const string UniqueMapName = "_uniqueIndexBy";
    private const string KeysCollectionName = "_keysBy";

    public static void Execute(in SourceGeneratorContext context, TableModel model)
    {
        var sb = new StringBuilder();
        {
            sb.AppendUsings(model.DatabaseModel);
            using (sb.NamespaceScope(model.Namespace))
            {
                using (sb.Append("public interface I").Append(model.TableName)
                           .AppendLine($" : ITable<{PrimaryKey}, {Element}>").BracketScope())
                {
                    for (int i = 0; i < model.KeyGroups.Length; i++)
                    {
                        sb.AppendMembers(model, i, true);
                    }

                    foreach (KeyGroupModel key in model.UniqueKeys)
                    {
                        sb.Append("public bool TryGetValueBy").AppendCapitalized(key.Name).Append('(').AppendKeyTypeNotNull(key)
                            .Append(" ").AppendDecapitalized(key.Name).Append($", out {Element} value);");
                    }
                }

                sb.ReplaceKeys(model);
            }

            context.AddSource($"TableInterfaces_{model.TableName}.g.cs", sb);
        }

        sb.AppendUsings(model.DatabaseModel);
        using (sb.NamespaceScope(model.Namespace))
        {
            using (sb.Append("public partial class ").Append(model.TableName)
                       .Append($" : Table<{PrimaryKey}, {Element}>, I").Append(model.TableName).BracketScope())
            {
                if (model.IsMultithreaded)
                {
                    if (model.ThreadCount > 1)
                    {
                        sb.Append("private readonly BatchData[] _batchData = new BatchData[").Append(model.ThreadCount)
                            .Append("]");

                        using (sb.BracketScope())
                        {
                            for (int i = 0; i < model.ThreadCount; i++)
                            {
                                sb.Append("new BatchData(new object())");
                                if (i < model.ThreadCount - 1)
                                {
                                    sb.Append(',');
                                }
                            }
                        }

                        sb.Append(';');
                    }
                    else
                    {
                        sb.Append("private BatchData _batchData = new BatchData(new object());");
                    }
                }

                sb.Append("public const int ThreadCount = ").Append(model.ThreadCount).Append(';');
                sb.AppendUniqueKeyMembers(model);
                sb.AppendMetadata(model);

                sb.Append("public override string KeyName => \"").Append(model.PrimaryKey.Name).Append("\";");
                sb.Append("public override string TableName => \"").Append(model.TableRawName).Append("\";");
                sb.Append("public override string ElementName => \"").Append(model.TypeName).Append("\";");

                for (int i = 0; i < model.KeyGroups.Length; i++)
                {
                    sb.AppendMembers(model, i, false);
                }

                foreach (KeyGroupModel keyGroup in model.UniqueKeys)
                {
                    sb.AppendKeySelectorDeclaration(keyGroup);
                }

                // Constructor
                sb.AppendConstructor(model);
                sb.Append("partial void OnAfterConstruct();");

                using (sb.Append($"protected override bool CanInsert(in {Element} item)").BracketScope())
                {
                    if (model.UniqueKeys.Length > 0)
                    {
                        foreach (KeyGroupModel key in model.UniqueKeys)
                        {
                            using (sb.BracketScope())
                            {
                                sb.Append("var key = ").AppendKeyAccessor("item", key).Append(";");

                                if (key.IsNullable)
                                {
                                    sb.Append("if (key != null)");
                                }

                                using (sb.BracketScope(key.IsNullable))
                                {
                                    sb.Append("if (").AppendUniqueKeyMap(key).Append(".ContainsKey(key")
                                        .AppendKeyValueAccessor(key).Append(")) return false;");
                                }
                            }
                        }
                    }

                    sb.AppendLine("return true;");
                }

                sb.AppendApplyToKeyCollections(model);

                if (model.IsMultithreaded)
                {
                    using (sb.Append("private void RunOnThreadPool(System.Action action)").BracketScope())
                    {
                        sb.Append("this.RunOnThreadPool(action, CancellationToken.None);");
                    }

                    using (sb.Append("private void RunOnThreadPool(System.Action action, CancellationToken cancellationToken)")
                               .BracketScope())
                    {
                        if (model.DatabaseModel.HasFlag(DatabaseFlags.UniTask))
                        {
                            sb.Append(
                                "Cysharp.Threading.Tasks.UniTask.RunOnThreadPool(action, cancellationToken: cancellationToken);");
                        }
                        else
                        {
                            sb.Append("System.Threading.Tasks.Task.Run(action, cancellationToken);");
                        }
                    }

                    if (model.IsMultithreadedModifications)
                    {
                        using (sb.Append("public void ApplySortOperations(")
                                   .Append(model.ThreadCount > 1, "int threadIndex").Append(')').BracketScope())
                        {
                            if (model.ThreadCount <= 1)
                            {
                                using (sb.AppendKeyGroupLock(model, 0))
                                {
                                    sb.AppendEnsureKeysReadyUnsafeCall(model, 0);
                                }
                            }
                            else
                            {
                                using (sb.Append("switch (threadIndex)").BracketScope())
                                {
                                    for (int i = 0; i < model.KeyGroups.Length; i += model.ThreadBatchSize)
                                    {
                                        using (sb.Append("case ").AppendKeyGroupIndex(model, i, true).Append(':')
                                                   .BracketScope())
                                        {
                                            using (sb.AppendKeyGroupLock(model, i))
                                            {
                                                sb.AppendEnsureKeysReadyUnsafeCall(model, i);
                                            }
                                        }

                                        sb.Append("break;");
                                    }

                                    sb.Append("default: throw new System.ArgumentOutOfRangeException(nameof(threadIndex));");
                                }
                            }
                        }
                    }

                    {
                        int keyGroupIndex = 0;
                        for (int t = 0; t < model.ThreadCount; t++)
                        {
                            using (sb.Append("private void EnsureKeysReadyUnsafe")
                                       .AppendKeyGroupIndex(model, keyGroupIndex, false).Append("()").BracketScope())
                            {
                                if (model.IsMultithreadedInitialization)
                                {
                                    sb.Append("var initAction = Interlocked.Exchange(ref this._batchData")
                                        .AppendKeyGroupIndexer(model, keyGroupIndex).Append(".Initialize, null);");

                                    sb.Append("initAction?.Invoke();");
                                }

                                if (model.IsMultithreadedModifications)
                                {
                                    sb.Append("var batchData = this._batchData")
                                        .AppendKeyGroupIndexer(model, keyGroupIndex).Append(';');

                                    using (sb.Append("while (batchData.OperationQueue.TryDequeue(out var operation))")
                                               .BracketScope())
                                    {
                                        for (int i = 0; i < model.ThreadBatchSize; i++)
                                        {
                                            if (keyGroupIndex >= model.KeyGroups.Length)
                                            {
                                                break;
                                            }

                                            KeyGroupModel key = model.KeyGroups[keyGroupIndex];
                                            sb.AppendKeyCollection(key).Append(".Execute(operation);");
                                            keyGroupIndex++;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    using (sb.Append("private struct BatchData").BracketScope())
                    {
                        sb.Append("public readonly object Lock;");
                        if (model.IsMultithreadedInitialization)
                        {
                            sb.Append("public System.Action? Initialize;");
                        }

                        sb.Append(model.IsMultithreadedModifications,
                            $"public readonly ConcurrentQueue<OperationChange<{Element}>> OperationQueue;");

                        using (sb.Append("public BatchData(object @lock)").BracketScope())
                        {
                            sb.Append("Lock = @lock;");
                            if (model.IsMultithreadedInitialization)
                            {
                                sb.Append("Initialize = null;");
                            }

                            sb.Append(model.IsMultithreadedModifications,
                                $"OperationQueue = new ConcurrentQueue<OperationChange<{Element}>>();");
                        }
                    }
                }
            }
        }

        sb.ReplaceKeys(model);
        context.AddSource($"Tables_{model.TableName}.g.cs", sb);
    }

    private static StringBuilder AppendApplyToKeyCollections(this StringBuilder sb, TableModel model)
    {
        using (sb.Append($"protected override void ApplyToKeyCollections(in OperationChange<{Element}> operation)")
                   .BracketScope())
        {
            if (model.UniqueKeys.Length > 0)
            {
                //using (sb.Append("if (this._syncUniqueIndexLock == 0)").BracketScope())
                {
                    //sb.Append("this._syncUniqueIndexLock++;");
                    using (sb.Append("switch (operation.Type)").BracketScope())
                    {
                        using (sb.Append("case OperationType.Insert:").BracketScope())
                        {
                            foreach (KeyGroupModel key in model.UniqueKeys)
                            {
                                using (sb.BracketScope())
                                {
                                    sb.Append("var key = ").AppendKeyAccessor("operation.Item", key).Append(";");

                                    if (key.IsNullable)
                                    {
                                        sb.Append("if (key != null)");
                                    }

                                    using (sb.BracketScope(key.IsNullable))
                                    {
                                        sb.AppendUniqueKeyMap(key).Append(".Add(key").AppendKeyValueAccessor(key)
                                            .Append(", Selector(operation.Item));");
                                    }
                                }
                            }

                            sb.Append("break;");
                        }

                        using (sb.Append("case OperationType.InsertOrReplace:").BracketScope())
                        {
                            using (sb.Append("if (operation.HasPrevious)").BracketScope())
                            {
                                sb.Append("goto case OperationType.Replace;");
                            }

                            sb.Append($"{PrimaryKey} primaryKey = Selector(operation.Item);");
                            foreach (KeyGroupModel key in model.UniqueKeys)
                            {
                                using (sb.BracketScope())
                                {
                                    sb.Append("var key = ").AppendKeyAccessor("operation.Item", key).Append(";");

                                    if (key.IsNullable)
                                    {
                                        sb.Append("if (key != null)");
                                    }

                                    using (sb.BracketScope(key.IsNullable))
                                    {
                                        using (sb.Append("if (").AppendUniqueKeyMap(key).Append(".TryGetValue(key")
                                                   .AppendKeyValueAccessor(key).Append(", out var previousPrimaryKey))")
                                                   .BracketScope())
                                        {
                                            sb.Append("if (!EqualityComparer<").Append(model.PrimaryKey.Type)
                                                .Append(">.Default.Equals(previousPrimaryKey, primaryKey))");

                                            using (sb.BracketScope())
                                            {
                                                sb.Append("this.Remove(previousPrimaryKey);");
                                                sb.AppendUniqueKeyMap(key).Append("[key").AppendKeyValueAccessor(key)
                                                    .Append("] = primaryKey;");
                                            }
                                        }

                                        using (sb.Append("else").BracketScope())
                                        {
                                            sb.AppendUniqueKeyMap(key).Append(".Add(key").AppendKeyValueAccessor(key)
                                                .Append(", primaryKey);");
                                        }
                                    }
                                }
                            }

                            sb.Append("break;");
                        }

                        using (sb.Append("case OperationType.Replace:").BracketScope())
                        {
                            sb.Append($"{PrimaryKey} primaryKey = Selector(operation.Item);");

                            foreach (KeyGroupModel key in model.UniqueKeys)
                            {
                                using (sb.BracketScope())
                                {
                                    sb.Append("var key = ").AppendKeyAccessor("operation.Item", key).Append(";");

                                    sb.Append("var previousKey = ").AppendKeyAccessor("operation.Previous", key)
                                        .Append(";");

                                    sb.Append("if( !EqualityComparer<").AppendKeyType(key)
                                        .Append(">.Default.Equals(key, previousKey))");

                                    using (sb.BracketScope())
                                    {
                                        if (key.IsNullable)
                                        {
                                            sb.Append("if (previousKey != null)");
                                        }

                                        sb.AppendUniqueKeyMap(key).Append(".Remove(previousKey")
                                            .AppendKeyValueAccessor(key).Append(");");

                                        sb.Append("if (");
                                        if (key.IsNullable)
                                        {
                                            sb.Append("key != null &&");
                                        }

                                        sb.AppendUniqueKeyMap(key).Append(".TryGetValue(key")
                                            .AppendKeyValueAccessor(key).Append(", out var previousPrimaryKey))");

                                        using (sb.BracketScope())
                                        {
                                            sb.Append("if (!EqualityComparer<").Append(model.PrimaryKey.Type)
                                                .Append(">.Default.Equals(previousPrimaryKey, primaryKey))");

                                            using (sb.BracketScope())
                                            {
                                                sb.Append("this.Remove(previousPrimaryKey);");
                                                sb.AppendUniqueKeyMap(key).Append("[key").AppendKeyValueAccessor(key)
                                                    .Append("] = primaryKey;");
                                            }
                                        }

                                        using (sb.Append("else").BracketScope())
                                        {
                                            sb.AppendUniqueKeyMap(key).Append(".Add(key").AppendKeyValueAccessor(key)
                                                .Append(", primaryKey);");
                                        }
                                    }
                                }
                            }

                            sb.Append("break;");
                        }

                        using (sb.Append("case OperationType.Clear:").BracketScope())
                        {
                            sb.Append("break;");
                        }

                        using (sb.Append("case OperationType.Remove:").BracketScope())
                        {
                            foreach (KeyGroupModel key in model.UniqueKeys)
                            {
                                using (sb.BracketScope())
                                {
                                    sb.Append("var key = ").AppendKeyAccessor("operation.Item", key).Append(";");

                                    if (key.IsNullable)
                                    {
                                        sb.Append("if (key != null)");
                                    }

                                    using (sb.BracketScope(key.IsNullable))
                                    {
                                        sb.AppendUniqueKeyMap(key).Append(".Remove(key").AppendKeyValueAccessor(key)
                                            .Append(");");
                                    }
                                }
                            }

                            sb.Append("break;");
                        }

                        sb.Append("default: throw new System.ArgumentOutOfRangeException();");
                    }

                    //sb.Append("this._syncUniqueIndexLock--;");
                }
            }

            if (model.IsMultithreadedModifications)
            {
                using (sb.Append("if (operation.Type == OperationType.Clear)").BracketScope())
                {
                    if (model.ThreadCount > 1)
                    {
                        using (sb.Append("for (int i = 0; i < this._batchData.Length; i++)").BracketScope())
                        {
                            sb.Append("var operationQueue = this._batchData[i].OperationQueue;");
                            sb.Append("operationQueue.Clear();");
                            sb.Append("operationQueue.Enqueue(operation);");
                        }
                    }
                    else
                    {
                        sb.Append("this._batchData.OperationQueue.Clear();");
                        sb.Append("this._batchData.OperationQueue.Enqueue(operation);");
                    }

                    sb.Append("return;");
                }
            }

            if (model.IsMultithreadedModifications)
            {
                if (model.ThreadCount > 1)
                {
                    using (sb.Append("for (int i = 0; i < this._batchData.Length; i++)").BracketScope())
                    {
                        sb.Append("this._batchData[i].OperationQueue.Enqueue(operation);");
                    }
                }
                else
                {
                    sb.Append("this._batchData.OperationQueue.Enqueue(operation);");
                }
            }
            else
            {
                foreach (KeyGroupModel key in model.KeyGroups)
                {
                    sb.AppendKeyCollection(key).Append(".Execute(operation);");
                }
            }
        }

        return sb;
    }

    private static void AppendMetadata(this StringBuilder sb, TableModel model)
    {
        sb.Append("public static MetaTable CreateMetaTable()");
        using (sb.BracketScope())
        {
            using (sb.Append("return new MetaTable").Scope('('))
            {
                sb.Append("typeof(").Append(model.GlobalTypeName).Append("), ");
                sb.Append("typeof(").Append(model.TableName).Append("), ");
                sb.Append("\"").Append(model.TableRawName).Append("\", ");
                using (sb.Append("new MetaProperty[]").BracketScope())
                {
                    for (int i = 0; i < model.Keys.Length; i++)
                    {
                        KeyModel key = model.Keys[i];
                        if (i > 0)
                        {
                            sb.AppendLine(",");
                        }

                        sb.Append("new MetaProperty(typeof(").Append(model.GlobalTypeName)
                            .Append(").GetProperty(nameof(").Append(model.GlobalTypeName).Append('.').Append(key.Name)
                            .Append(")))");
                    }
                }

                sb.Append(", ");
                using (sb.Append("new MetaIndex[]").BracketScope())
                {
                    for (int i = 0; i < model.KeyGroups.Length; i++)
                    {
                        KeyGroupModel keyGroup = model.KeyGroups[i];
                        if (i > 0)
                        {
                            sb.AppendLine(",");
                        }

                        using (sb.Append("new MetaIndex(new System.Reflection.PropertyInfo[]").BracketScope())
                        {
                            sb.AppendJoin(", ",
                                keyGroup,
                                key => sb.Append("typeof(").Append(model.GlobalTypeName).Append(").GetProperty(nameof(")
                                    .Append(model.GlobalTypeName).Append('.').Append(key.Name).Append("))"));
                        }

                        sb.Append(", ").Append(i == 0 ? "true" : "false").Append(',')
                            .Append(keyGroup.IsGroupUnique ? "true" : "false").Append(", Comparer<")
                            .AppendKeysTupleDeclaration(keyGroup).Append(">.Default)");
                    }
                }
            }

            sb.Append(";");
        }
    }

    private static void AppendUniqueKeyMembers(this StringBuilder sb, TableModel model)
    {
        if (model.UniqueKeys.Length > 0)
        {
            foreach (KeyGroupModel key in model.UniqueKeys)
            {
                sb.Append("private readonly Dictionary<").AppendKeyTypeNotNull(key)
                    .Append($", {PrimaryKey}> {UniqueMapName}").AppendCapitalized(key.Name).Append(';');

                sb.Append("public bool TryGetValueBy").AppendCapitalized(key.Name).Append('(').AppendKeyTypeNotNull(key)
                    .Append(" ").AppendDecapitalized(key.Name).Append($", out {Element} value)");

                using (sb.BracketScope())
                {
                    sb.Append("if (").AppendUniqueKeyMap(key).Append(".TryGetValue(").AppendDecapitalized(key.Name)
                        .Append(", out var primaryKey))");

                    using (sb.BracketScope())
                    {
                        sb.Append("value = this[primaryKey];");
                        sb.Append("return true;");
                    }

                    sb.Append("value = default;");
                    sb.Append("return false;");
                }
            }

            //sb.AppendLine("private int _syncUniqueIndexLock;");
        }
    }

    private static void AppendConstructor(this StringBuilder sb, TableModel model)
    {
        using (sb.Append("public ").Append(model.TableName).Scope('('))
        {
            sb.Append("IReadOnlyList<").Append(model.GlobalTypeName).Append("> sortedData");
        }

        sb.Append(" : base(sortedData, Selector)");

        using (sb.BracketScope())
        {
            foreach (KeyGroupModel keyGroup in model.KeyGroups)
            {
                sb.AppendConstructorStatements(keyGroup);
            }

            if (model.UniqueKeys.Length > 0)
            {
                foreach (KeyGroupModel key in model.UniqueKeys)
                {
                    sb.AppendUniqueKeyMap(key).Append(" = new Dictionary<").AppendKeyTypeNotNull(key)
                        .Append($", {PrimaryKey}>(sortedData.Count);");
                }

                using (sb.Append("for (int i = 0; i < sortedData.Count; i++)").BracketScope())
                {
                    sb.AppendLine("var item = sortedData[i];");
                    sb.AppendLine("var primaryKey = Selector(item);");
                    foreach (KeyGroupModel key in model.UniqueKeys)
                    {
                        using (sb.BracketScope())
                        {
                            sb.Append("var key = ").AppendKeyAccessor("item", key).Append(";");

                            if (key.IsNullable)
                            {
                                sb.Append("if (key != null)");
                            }

                            using (sb.BracketScope(key.IsNullable))
                            {
                                sb.AppendUniqueKeyMap(key).Append(".Add(key").AppendKeyValueAccessor(key)
                                    .Append(", primaryKey);");
                            }
                        }
                    }
                }
            }

            using (sb.Append("if (sortedData.Count > 1)").BracketScope())
            {
                if (model.IsMultithreadedInitialization)
                {
                    for (int i = 0; i < model.KeyGroups.Length;)
                    {
                        sb.Append("this._batchData").AppendKeyGroupIndexer(model, i).Append(".Initialize = () => ");

                        using (sb.BracketScope())
                        {
                            for (int b = 0; b < model.ThreadBatchSize; b++)
                            {
                                if (i >= model.KeyGroups.Length)
                                {
                                    break;
                                }

                                KeyGroupModel key = model.KeyGroups[i];
                                sb.AppendKeyCollection(key).Append(".Sort();");
                                i++;
                            }
                        }

                        sb.Append(";");
                    }
                }
                else
                {
                    foreach (KeyGroupModel keyGroup in model.KeyGroups)
                    {
                        sb.AppendKeyCollection(keyGroup).Append(".Sort();");
                    }
                }
            }

            sb.Append("this.OnAfterConstruct();");
        }
    }

    private static StringBuilder AppendEnsureKeysReadyUnsafeCall(this StringBuilder sb, TableModel model,
        int keyGroupIndex)
    {
        if (model.IsMultithreaded)
        {
            sb.Append("this.EnsureKeysReadyUnsafe").AppendKeyGroupIndex(model, keyGroupIndex, false).Append("();");
        }

        return sb;
    }

    private static int GetKeyGroupIndex(this TableModel model, int keyGroupIndex)
    {
        return keyGroupIndex / model.ThreadBatchSize;
    }

    private static StringBuilder AppendKeyGroupIndex(this StringBuilder sb, TableModel model, int keyGroupIndex,
        bool numberOnly)
    {
        if (model.ThreadCount <= 1)
        {
            return sb;
        }

        if (numberOnly || model.ThreadBatchSize > 1)
        {
            sb.Append(model.GetKeyGroupIndex(keyGroupIndex));
        }
        else
        {
            sb.Append("By").AppendCapitalized(model.KeyGroups[keyGroupIndex].Name);
        }

        return sb;
    }

    private static StringBuilderScope AppendKeyGroupLock(this StringBuilder sb, in TableModel model, int keyGroupIndex)
    {
        if (!model.IsMultithreaded)
        {
            return StringBuilderScope.Empty;
        }

        //sb.Append("// ").Append(keyGroupIndex).Append("/").Append(model.ThreadBatchSize).Append(" ").AppendLine();
        sb.Append("lock (this._batchData").AppendKeyGroupIndexer(model, keyGroupIndex).Append(".Lock)");

        return sb.BracketScope();
    }

    private static StringBuilder AppendKeyGroupIndexer(this StringBuilder sb, in TableModel model, int keyGroupIndex)
    {
        if (!model.IsMultithreaded)
        {
            return sb;
        }

        if (model.ThreadCount > 1)
        {
            sb.Append('[').AppendKeyGroupIndex(model, keyGroupIndex, true).Append(']');
        }

        return sb;
    }

    private static StringBuilder ReplaceKeys(this StringBuilder sb, in TableModel model)
    {
        sb.Replace(PrimaryKey, model.PrimaryKey.Type);
        sb.Replace(Element, model.GlobalTypeName);
        return sb;
    }

    private static void AppendConstructorStatements(this StringBuilder sb, in KeyGroupModel keyGroup)
    {
        sb.AppendKeyCollection(keyGroup).Append(" = new KeyCollection<").AppendKeyType(keyGroup)
            .Append(">(sortedData, this, ").AppendKeysSelector(keyGroup).Append(", ").AppendComparer(keyGroup)
            .Append(", ").AppendKeyOnlyComparer(keyGroup).Append(", \"").Append(keyGroup.Name).Append("\");");
    }

    private static void AppendMembers(this StringBuilder sb, in TableModel model, in int keyGroupIndex,
        bool isInterface)
    {
        bool isPrimaryKey = keyGroupIndex == 0;
        KeyGroupModel keyGroup = model.KeyGroups[keyGroupIndex];
        string keyName = keyGroup.Count == 1 ? keyGroup[0].Name : "Key";

        AppendGetAllSortedBy(sb, model, keyGroupIndex, isInterface, keyGroup, isPrimaryKey);

        AppendFindBy(sb, model, keyGroupIndex, isInterface, isPrimaryKey, keyGroup, keyName);

        AppendTryFind(sb, model, keyGroupIndex, isInterface, keyGroup, keyName, isPrimaryKey);

        AppendFindClosest(sb, model, keyGroupIndex, isInterface, keyGroup, keyName);

        AppendFindRangeBy(sb, model, keyGroupIndex, isInterface, keyGroup, keyName);

        if (isInterface)
        {
            return;
        }

        if (isPrimaryKey)
        {
            sb.Append($"public static readonly KeySelector<{Element}, {PrimaryKey}> Selector = (in {Element} x) => ")
                .AppendTupleSelector("x", model.PrimaryKey).Append(';');
        }

        sb.Append($"public static readonly KeySelector<{Element}, ").AppendKeysTupleDeclaration(keyGroup).Append("> ")
            .AppendKeysSelector(keyGroup);

        sb.Append($" = (in {Element} x) => ").AppendTupleSelector("x", keyGroup, model.PrimaryKey).Append(';');

        sb.Append("private static Comparer<").AppendKeysTupleDeclaration(keyGroup).Append("> ").AppendComparer(keyGroup)
            .Append(" = Comparer<").AppendKeysTupleDeclaration(keyGroup).Append(">.Default;");

        sb.Append("private static Comparer<").AppendKeysTupleDeclaration(keyGroup).Append("> ")
            .AppendKeyOnlyComparer(keyGroup).Append(" = Comparer<").AppendKeysTupleDeclaration(keyGroup)
            .Append(">.Create((x, y) => Comparer<").AppendKeyType(keyGroup).Append(">.Default.Compare(x.key, y.key));");

        sb.Append("private readonly KeyCollection<").AppendKeyType(keyGroup).Append($" > {KeysCollectionName}")
            .AppendCapitalized(keyGroup.Name).Append(';').AppendLine();

        sb.AppendLine();
    }

    private static void AppendGetAllSortedBy(StringBuilder sb, TableModel model, int keyGroupIndex, bool isInterface,
        KeyGroupModel keyGroup, bool isPrimaryKey)
    {
        sb.Append("public ").AppendRangeView(keyGroup).Append(" GetAllSortedBy").AppendCapitalized(keyGroup.Name)
            .Append("(bool ascendant = true)");

        if (isPrimaryKey)
        {
            using (sb.BracketScope())
            {
                sb.Append("return this.GetAllSorted(ascendant);");
            }

            sb.Append("public ").Append(isInterface, "new ").Append(!isInterface, "override ").AppendRangeView(keyGroup)
                .Append(" GetAllSorted(bool ascendant = true)");
        }

        if (isInterface)
        {
            sb.Append(';');
        }
        else
        {
            using (sb.BracketScope())
            {
                using (sb.AppendKeyGroupLock(model, keyGroupIndex))
                {
                    sb.AppendEnsureKeysReadyUnsafeCall(model, keyGroupIndex);
                    sb.Append(" return ").AppendKeyCollection(keyGroup).Append(".GetAll(ascendant);");
                }
            }
        }
    }

    private static void AppendFindBy(StringBuilder sb, TableModel model, int keyGroupIndex, bool isInterface,
        bool isPrimaryKey, KeyGroupModel keyGroup, string keyName)
    {
        sb.Append("public ");
        if (isPrimaryKey || !keyGroup.IsNonUnique)
        {
            sb.Append(Element);
            sb.Append(keyGroup.IsGroupUnique ? " GetBy" : " FindBy");
        }
        else
        {
            sb.AppendRangeView(keyGroup).Append(" FindBy");
        }

        using (sb.AppendCapitalized(keyGroup.Name).Scope('('))
        {
            sb.AppendKeyType(keyGroup).Append(' ').AppendDecapitalized(keyName);
            if (keyGroup.IsNonUnique)
            {
                sb.Append(", bool ascendant = true");
            }
        }

        if (isInterface)
        {
            sb.Append(';');
        }
        else
        {
            using (sb.BracketScope())
            {
                if (isPrimaryKey)
                {
                    sb.Append("return this[").AppendDecapitalized(keyName).Append("];");
                }
                else if (keyGroup.IsGroupUnique)
                {
                    sb.Append("return this[").AppendUniqueKeyMap(keyGroup).Append("[").AppendDecapitalized(keyName)
                        .Append("]];");
                }
                else
                {
                    using (sb.AppendKeyGroupLock(model, keyGroupIndex))
                    {
                        sb.AppendEnsureKeysReadyUnsafeCall(model, keyGroupIndex);

                        sb.Append("return ").AppendKeyCollection(keyGroup).Append(".Find");
                        sb.Append(keyGroup.IsAnyKeyUnique ? "Unique" : "Many");
                        sb.Append("(").AppendDecapitalized(keyName);
                        sb.Append(!keyGroup.IsAnyKeyUnique ? ", ascendant);" : ");");
                    }
                }
            }
        }
    }

    private static void AppendTryFind(StringBuilder sb, TableModel model, int keyGroupIndex, bool isInterface,
        KeyGroupModel keyGroup, string keyName, bool isPrimaryKey)
    {
        if (!keyGroup.IsNonUnique)
        {
            sb.Append("public bool Try");
            sb.Append(keyGroup.IsGroupUnique ? "Get" : "Find");
            using (sb.Append("By").AppendCapitalized(keyGroup.Name).Scope('('))
            {
                sb.AppendKeyType(keyGroup).Append(' ').AppendDecapitalized(keyName).Append($", out {Element} value");
            }

            if (isInterface)
            {
                sb.Append(';');
            }
            else
            {
                using (sb.BracketScope())
                {
                    using (sb.AppendKeyGroupLock(model, keyGroupIndex))
                    {
                        sb.AppendEnsureKeysReadyUnsafeCall(model, keyGroupIndex);

                        if (isPrimaryKey)
                        {
                            sb.Append("return this.TryGetValue(").AppendDecapitalized(keyName).Append(", out value);");
                        }
                        else if (keyGroup.IsGroupUnique)
                        {
                            using (sb.Append("if(").AppendUniqueKeyMap(keyGroup).Append(".TryGetValue(")
                                       .AppendDecapitalized(keyName).Append(", out var primaryKey))").BracketScope())
                            {
                                sb.Append("value = this[primaryKey];");
                                sb.Append("return true;");
                            }

                            sb.Append("value = default;");
                            sb.Append("return false;");
                        }
                        else
                        {
                            sb.Append("return ").AppendKeyCollection(keyGroup).Append(".TryFindUnique(").AppendDecapitalized(keyName)
                                .Append(", out value);");
                        }
                    }
                }
            }
        }
    }

    private static void AppendFindRangeBy(StringBuilder sb, TableModel model, int keyGroupIndex, bool isInterface,
        KeyGroupModel keyGroup, string keyName)
    {
        if (!keyGroup.IsAnyKeyUnique)
        {
            using (sb.Append("public ").AppendRangeView(keyGroup).Append(" FindRangeBy").AppendCapitalized(keyGroup.Name)
                       .Scope('('))
            {
                sb.AppendKeyType(keyGroup).Append(' ').AppendDecapitalized(keyName)
                    .Append(", bool selectLower = true, bool ascendant = true");
            }

            if (isInterface)
            {
                sb.Append(';');
            }
            else
            {
                using (sb.BracketScope())
                {
                    using (sb.AppendKeyGroupLock(model, keyGroupIndex))
                    {
                        sb.AppendEnsureKeysReadyUnsafeCall(model, keyGroupIndex);

                        sb.Append("return ").AppendKeyCollection(keyGroup).Append(".FindManyClosest(")
                            .AppendDecapitalized(keyName).Append(", selectLower, ascendant);");
                    }
                }
            }
        }

        {
            using (sb.Append("public ").AppendRangeView(keyGroup).Append(" FindRangeBy").AppendCapitalized(keyGroup.Name)
                       .Scope('('))
            {
                sb.AppendKeyType(keyGroup).Append(" min").AppendCapitalized(keyName).Append(", ")
                    .AppendKeyType(keyGroup).Append(" max").AppendCapitalized(keyName)
                    .Append(", bool ascendant = true");
            }

            if (isInterface)
            {
                sb.Append(';');
            }
            else
            {
                using (sb.BracketScope())
                {
                    using (sb.AppendKeyGroupLock(model, keyGroupIndex))
                    {
                        sb.AppendEnsureKeysReadyUnsafeCall(model, keyGroupIndex);

                        sb.Append("return ").AppendKeyCollection(keyGroup).Append(".FindUniqueRange(min")
                            .AppendCapitalized(keyName).Append(", max").AppendCapitalized(keyName)
                            .Append(", ascendant);");
                    }
                }
            }
        }
    }

    private static void AppendFindClosest(this StringBuilder sb, TableModel model, int keyGroupIndex, bool isInterface,
        KeyGroupModel keyGroup, string keyName)
    {
        sb.Append("public ");
        if (keyGroup.IsNonUnique)
        {
            sb.AppendRangeView(keyGroup);
        }
        else
        {
            sb.Append(Element);
        }

        using (sb.Append(" FindClosestBy").AppendCapitalized(keyGroup.Name).Scope('('))
        {
            sb.AppendKeyType(keyGroup).Append(' ').AppendDecapitalized(keyName).Append(", bool selectLower = true")
                .Append(keyGroup.IsNonUnique, ", bool ascendant = true");
        }

        if (isInterface)
        {
            sb.Append(';');
        }
        else
        {
            using (sb.BracketScope())
            {
                using (sb.AppendKeyGroupLock(model, keyGroupIndex))
                {
                    sb.AppendEnsureKeysReadyUnsafeCall(model, keyGroupIndex);

                    if (keyGroup.IsNonUnique)
                    {
                        sb.Append("return ").AppendKeyCollection(keyGroup).Append(".FindManyClosest(")
                            .AppendDecapitalized(keyName).Append(", selectLower, ascendant);");
                    }
                    else
                    {
                        sb.Append("return ").AppendKeyCollection(keyGroup).Append(".FindUniqueClosest(")
                            .AppendDecapitalized(keyName).Append(", selectLower);");
                    }
                }
            }
        }
    }

    private static StringBuilder AppendTupleSelector(this StringBuilder sb, string value, in KeyGroupModel model)
    {
        var keys = model.Keys;
        Debug.Assert(keys.Length > 0);

        int count = keys.Length;

        using (sb.Scope(count > 1, '('))
        {
            sb.AppendJoin(", ", keys, (sb, key) => sb.Append(value).Append('.').Append(key.Symbol.Name));
        }

        return sb;
    }

    private static StringBuilder AppendTupleSelector(this StringBuilder sb, string value, in KeyGroupModel key,
        in KeyGroupModel primaryKey)
    {
        using (sb.Scope('('))
        {
            sb.AppendTupleSelector(value, key);
            sb.Append(", ");
            sb.AppendTupleSelector(value, primaryKey);
        }

        return sb;
    }

    private static StringBuilder AppendKeySelectorDeclaration(this StringBuilder sb, in KeyGroupModel keyGroup)
    {
        sb.Append($"public static readonly KeySelector<{Element}, ").AppendKeyType(keyGroup).Append("> ")
            .AppendKeySelector(keyGroup);

        return sb.Append($" = (in {Element} x) => ").AppendTupleSelector("x", keyGroup).Append(';');
    }

    private static StringBuilder AppendKeysTupleDeclaration(this StringBuilder sb, in KeyGroupModel key)
    {
        using (sb.Scope('('))
        {
            sb.AppendKeyType(key).Append($" key, {PrimaryKey} primaryKey");
        }

        return sb;
    }

    private static StringBuilder AppendKeyType(this StringBuilder sb, in KeyGroupModel key)
    {
        return sb.Append(key.Type);
    }

    private static StringBuilder AppendKeySelector(this StringBuilder sb, in KeyGroupModel keyGroup)
    {
        return sb.Append("KeySelectorBy").AppendCapitalized(keyGroup.Name);
    }

    private static StringBuilder AppendKeysSelector(this StringBuilder sb, in KeyGroupModel keyGroup)
    {
        return sb.Append("KeysSelectorBy").AppendCapitalized(keyGroup.Name);
    }

    private static StringBuilder AppendKeyValueAccessor(this StringBuilder sb, in KeyGroupModel key)
    {
        return sb.Append(key.IsNullableStruct(out _), ".Value");
    }

    private static StringBuilder AppendKeyAccessor(this StringBuilder sb, string item, in KeyGroupModel key)
    {
        if (key.IsSingle)
        {
            return sb.Append(item).Append('.').Append(key.Name);
        }

        return sb.AppendKeySelector(key).Append('(').Append(item).Append(')');
    }

    private static StringBuilder AppendComparer(this StringBuilder sb, in KeyGroupModel key)
    {
        return sb.Append("ComparerBy").AppendCapitalized(key.Name);
    }

    private static StringBuilder AppendKeyOnlyComparer(this StringBuilder sb, in KeyGroupModel key)
    {
        return sb.Append("ComparerKeyOnlyBy").AppendCapitalized(key.Name);
    }

    private static StringBuilder AppendUniqueKeyMap(this StringBuilder sb, in KeyGroupModel key)
    {
        return sb.Append($"this.{UniqueMapName}").AppendCapitalized(key.Name);
    }

    private static StringBuilder AppendKeyCollection(this StringBuilder sb, in KeyGroupModel key)
    {
        return sb.Append($"this.{KeysCollectionName}").AppendCapitalized(key.Name);
    }

    private static StringBuilder AppendKeyTypeNotNull(this StringBuilder sb, in KeyGroupModel key)
    {
        return sb.Append(key.IsNullableStruct(out INamedTypeSymbol? type) ? type.ToDisplayString() : key.Type);
    }

    private static StringBuilder AppendRangeView(this StringBuilder sb, in KeyGroupModel key)
    {
        return sb.Append("RangeView<").AppendKeyType(key).Append($", {PrimaryKey}, {Element}>");
    }
}