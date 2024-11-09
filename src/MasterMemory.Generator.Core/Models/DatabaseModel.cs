using MasterMemory.Annotations;
using Microsoft.CodeAnalysis;

namespace MasterMemory.Generator.Core.Models;

public record DatabaseModel
{
    public const string DefaultNamespace = "MasterMemory";
    public const string DefaultName = "Database";
    public const int DefaultThreadedComplexityThreshold = 1024;
    public readonly int ThreadedComplexityThreshold;

    private readonly INamedTypeSymbol? _typeSymbol;

    public DatabaseFlags Flags { get; init; }

    public string FullName { get; init; }

    public string GlobalName;

    public string Name { get; init; }

    public string Namespace { get; init; }

    public bool IsMultithreaded => ThreadedComplexityThreshold > 0;

    public string AccessibilityModifier => _typeSymbol?.GetAccessibilityModifier() ?? "internal";

    public DatabaseModel(INamedTypeSymbol? typeSymbol, DatabaseFlags flags, int threadedComplexityThreshold)
    {
        Flags = flags;
        _typeSymbol = typeSymbol;
        Name = _typeSymbol?.Name ?? DefaultName;
        Namespace = typeSymbol?.ContainingNamespace is { IsGlobalNamespace: false } ?
            typeSymbol.ContainingNamespace.ToDisplayString() :
            DefaultNamespace;

        FullName = string.IsNullOrEmpty(Namespace) ? Name : Namespace + "." + Name;

        GlobalName = "global::" + FullName;
        ThreadedComplexityThreshold = threadedComplexityThreshold;
    }

    public static DatabaseModel CreateDefault(DatabaseFlags flags)
    {
        return new(null, flags, DefaultThreadedComplexityThreshold);
    }

    public bool HasFlag(DatabaseFlags flag)
    {
        return Flags.HasFlagFast(flag);
    }

    public virtual bool Equals(DatabaseModel other)
    {
        return ThreadedComplexityThreshold == other.ThreadedComplexityThreshold &&
               SymbolEqualityComparer.Default.Equals(_typeSymbol, other._typeSymbol) && Flags == other.Flags &&
               FullName == other.FullName && Name == other.Name && Namespace == other.Namespace;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = ThreadedComplexityThreshold;
            hashCode = (hashCode * 397) ^ (_typeSymbol != null ? _typeSymbol.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (int)Flags;
            hashCode = (hashCode * 397) ^ FullName.GetHashCode();
            hashCode = (hashCode * 397) ^ Name.GetHashCode();
            hashCode = (hashCode * 397) ^ Namespace.GetHashCode();
            return hashCode;
        }
    }
}