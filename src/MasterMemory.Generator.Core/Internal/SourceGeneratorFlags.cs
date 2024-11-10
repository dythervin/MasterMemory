using System;

namespace MasterMemory.Generator.Core.Internal;

[Flags]
public enum SourceGeneratorFlags
{
    None = 0,
    SortMembers = 1 << 0,
    NormalizeWhitespace = 1 << 1
}

public static class SourceGeneratorFlagsExtensions
{
    public static bool HasFlagFast(this SourceGeneratorFlags value, SourceGeneratorFlags flag)
    {
        return (value & flag) != 0;
    }
}