using System.Text;
using MasterMemory.Generator.Internal;
using MasterMemory.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MasterMemory.Generator;

internal static class TransactionObserverGenerator
{
    public const string Name = "TransactionValueObserver";

    internal static void Execute(SourceProductionContext context, DatabaseModel database, StringBuilder sb)
    {
        sb.AppendUsings(database);

        using (sb.NamespaceScope(database.Namespace))
        {
            string accessModifier = database.AccessibilityModifier;

            using (sb.Append(accessModifier).Append($" interface I{Name}<TValue> : I{Name}").BracketScope())
            {
                if (database.HasFlag(DatabaseFlags.R3))
                {
                    sb.Append("Observable<Operation<TValue>> OnCommit { get; }");
                }
                else if (database.HasFlag(DatabaseFlags.UniRx))
                {
                    sb.Append("IObservable<Operation<TValue>> OnCommit { get; }");
                }
                else
                {
                    sb.Append("event System.Action<Operation<TValue>> OnCommit;");
                }
            }
        }

        context.AddSource($"I{Name}.g.cs",
            SourceText.From(SourceTextFormatter.FormatCompilationUnit(sb.ToStringAndClear()).ToFullString(),
                Encoding.UTF8));

        sb.AppendUsings(database);

        using (sb.NamespaceScope(database.Namespace))
        {
            string accessModifier = database.AccessibilityModifier;

            using (sb.Append(accessModifier).Append($" class {Name}<TValue> : {Name}Base<TValue>, I{Name}<TValue>")
                       .BracketScope())
            {
                if (database.HasFlag(DatabaseFlags.R3))
                {
                    sb.Append("public Observable<Operation<TValue>> OnCommit => _onCommit;");
                    sb.Append("private readonly Subject<Operation<TValue>> _onCommit = new();");
                }
                else if (database.HasFlag(DatabaseFlags.UniRx))
                {
                    sb.Append("public IObservable<Operation<TValue>> OnCommit => _onCommit;");
                    sb.Append("private readonly Subject<Operation<TValue>> _onCommit = new();");
                }
                else
                {
                    sb.Append("public event System.Action<Operation<TValue>> OnCommit;");
                }

                using (sb.Append("protected override void PublishNext(in Operation<TValue> operation)").BracketScope())
                {
                    if (database.HasFlag(DatabaseFlags.R3) || database.HasFlag(DatabaseFlags.UniRx))
                    {
                        sb.AppendLine("_onCommit.OnNext(operation);");
                    }
                    else
                    {
                        sb.AppendLine("OnCommit?.Invoke(operation);");
                    }
                }

                using (sb.Append("protected override void OnDispose()").BracketScope())
                {
                    if (database.HasFlag(DatabaseFlags.R3) || database.HasFlag(DatabaseFlags.UniRx))
                        sb.AppendLine("_onCommit.Dispose();");
                    else
                        sb.AppendLine("OnCommit = null;");

                    sb.AppendLine("base.OnDispose();");
                }
            }
        }

        context.AddSource($"{Name}.g.cs",
            SourceText.From(SourceTextFormatter.FormatCompilationUnit(sb.ToStringAndClear()).ToFullString(),
                Encoding.UTF8));
    }
}