using System.Text;
using MasterMemory.Generator.Core.Internal;
using MasterMemory.Generator.Core.Models;
using MasterMemory.Annotations;

namespace MasterMemory.Generator.Core;

internal static class ObservableDbValueGenerator
{
    public const string Name = "ObservableDbItem";

    public static string GetAbstractName(DatabaseModel database)
    {
        return (database.HasFlag(DatabaseFlags.R3) ? $"{Name}Base" : $"I{Name}");
    }

    internal static void Execute(SourceGeneratorContext context, DatabaseModel database, StringBuilder sb)
    {
        sb.AppendUsings(database);

        using (sb.NamespaceScope(database.Namespace))
        {
            string accessModifier = database.AccessibilityModifier;
            sb.Append(accessModifier);

            if (database.HasFlag(DatabaseFlags.R3))
            {
                sb.Append($" abstract class {Name}Base<T> : Observable<T?>, System.IDisposable");
                using (sb.BracketScope())
                {
                    sb.AppendLine("public abstract T Value { get; }");
                    sb.AppendLine("public abstract bool HasValue { get; }");
                    sb.AppendLine("public abstract void Dispose();");
                }
            }
            else
            {
                sb.Append(" interface ").Append($"I{Name}<T> ").Append(database.HasFlag(DatabaseFlags.UniRx),
                    ": IObservable<T?>, System.IDisposable");

                using (sb.BracketScope())
                {
                    sb.Append(!database.HasFlag(DatabaseFlags.UniRx), "event System.Action<T?> OnChange;");

                    sb.AppendLine("bool HasValue { get; }");
                    sb.AppendLine("T Value { get; }");
                }
            }

            using (sb.Append(accessModifier).Append($" class {Name}<TPrimaryKey, T> : ")
                       .Append(!database.HasFlag(DatabaseFlags.R3), "I").Append(Name)
                       .Append(database.HasFlag(DatabaseFlags.R3), "Base").Append("<T>").BracketScope())
            {
                string @override = database.HasFlag(DatabaseFlags.R3) ? " override " : "";

                sb.AppendLine("private bool _isDisposed;");
                if (database.HasFlag(DatabaseFlags.R3) || database.HasFlag(DatabaseFlags.UniRx))
                    sb.AppendLine("private System.IDisposable _subscription;");

                if (database.HasFlag(DatabaseFlags.R3) || database.HasFlag(DatabaseFlags.UniRx))
                {
                    sb.AppendLine("private ReactiveProperty<T> _reactiveProperty = new ReactiveProperty<T>();");
                }
                else
                {
                    sb.AppendLine("private T? _value;");
                    sb.AppendLine("public event System.Action<T> OnChange;");
                }

                sb.AppendLine("private bool _hasValue;");
                sb.AppendLine("public ").Append(@override).Append("bool HasValue => _hasValue;");
                sb.AppendLine("private readonly TPrimaryKey _key;");
                sb.AppendLine("private readonly ITable<TPrimaryKey, T> _table;");
                sb.AppendLine($"private I{DbItemObserverGenerator.Name}<T>? _observer;");

                sb.AppendLine("public ").Append(@override).Append("T? Value => ").Append(
                    database.HasFlag(DatabaseFlags.R3) || database.HasFlag(DatabaseFlags.UniRx) ?
                        "_reactiveProperty.Value;" :
                        "this._value;");

                using (sb.Append("public ").Append(Name).Append("(I").Append(database.Name)
                           .Append(" database, TPrimaryKey key)").BracketScope())
                {
                    sb.AppendLine("this._table = database.GetTable<TPrimaryKey, T>();");
                    sb.AppendLine("this._key = key;");
                    sb.AppendLine("this._hasValue = _table.TryGetValue(key, out var value);");
                    sb.AppendLine("this._observer = database.GetObserver<T>();");
                    if (database.HasFlag(DatabaseFlags.R3) || database.HasFlag(DatabaseFlags.UniRx))
                    {
                        sb.Append("if (_hasValue) _reactiveProperty.Value = value;");
                        sb.AppendLine("_subscription = this._observer.OnCommit.Subscribe(OnCommit);");
                    }
                    else
                    {
                        sb.AppendLine("this._observer.OnCommit += OnCommit;");
                    }
                }

                using (sb.Append("public ").Append(Name).Append("(I").Append(database.Name)
                           .Append(
                               " database, T value) : this(database, database.GetTable<TPrimaryKey, T>().KeySelector(value))")
                           .BracketScope())
                {
                }

                using (sb.Append("private void OnCommit(")
                           .Append(!database.HasFlag(DatabaseFlags.R3) && !database.HasFlag(DatabaseFlags.UniRx), "in ")
                           .Append("Operation<T> operation)").BracketScope())
                {
                    sb.AppendLine("if(!_hasValue && operation.Type == OperationType.Clear ||" +
                                  " !EqualityComparer<TPrimaryKey>.Default.Equals(this._table.KeySelector(operation.Value), this._key)) return;");

                    sb.AppendLine(
                        "this._hasValue = operation.Type is not (OperationType.Clear or OperationType.Remove);");

                    if (database.HasFlag(DatabaseFlags.R3) || database.HasFlag(DatabaseFlags.UniRx))
                    {
                        sb.AppendLine("_reactiveProperty.Value = operation.Value;");
                    }
                    else
                    {
                        sb.AppendLine("this._value = operation.Value;");
                        sb.AppendLine("this.OnChange?.Invoke(_value);");
                    }
                }

                if (database.HasFlag(DatabaseFlags.R3))
                {
                    using (sb.AppendLine("protected override System.IDisposable SubscribeCore(Observer<T?> observer)")
                               .BracketScope())
                    {
                        sb.AppendLine("AssertNotDisposed();");
                        sb.AppendLine("return _reactiveProperty.Subscribe(observer);");
                    }
                }
                else if (database.HasFlag(DatabaseFlags.UniRx))
                {
                    using (sb.AppendLine("public System.IDisposable Subscribe(IObserver<T?> observer)").BracketScope())
                    {
                        sb.AppendLine("AssertNotDisposed();");
                        sb.AppendLine("return _reactiveProperty.Subscribe(observer);");
                    }
                }

                using (sb.Append("public").Append(@override).Append(" void Dispose()").BracketScope())
                {
                    sb.Append("if (_isDisposed) return;");

                    sb.AppendLine("_isDisposed = true;");

                    if (database.HasFlag(DatabaseFlags.R3) || database.HasFlag(DatabaseFlags.UniRx))
                    {
                        sb.AppendLine("_subscription.Dispose();");
                        sb.AppendLine("_reactiveProperty.Dispose();");
                    }
                    else
                    {
                        sb.AppendLine("this._observer.OnCommit -= OnCommit;");
                    }
                }

                using (sb.AppendLine("private void AssertNotDisposed()").BracketScope())
                {
                    sb.AppendLine(
                        $"if (_isDisposed) throw new System.ObjectDisposedException(nameof({Name}<TPrimaryKey, T>));");
                }
            }
        }

        context.AddSource($"Db.{Name}.g.cs", sb.ToStringAndClear());
    }
}