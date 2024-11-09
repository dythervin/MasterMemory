using MasterMemory.Annotations;

namespace MasterMemory.Generator.Core;

public static class DbTableFlagsExtensions
{
    public static bool HasFlagFast(this DbTableFlags value, DbTableFlags flag)
    {
        return (value & flag) != 0;
    }

    public static bool HasFlagFast(this DatabaseFlags value, DatabaseFlags flag)
    {
        return (value & flag) != 0;
    }
}