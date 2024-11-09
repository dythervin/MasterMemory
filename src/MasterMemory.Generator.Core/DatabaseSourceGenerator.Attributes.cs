namespace MasterMemory.Generator.Core;

public partial class DatabaseSourceGenerator
{
    public const string GroupIndexParameterName = "groupIndex";
    public const string KeyOrderParameterName = "keyOrder";

    public static class TableAttribute
    {
        public const string ShortName = "Table";
        public const string Name = ShortName + "Attribute";
        public const string FullName = $"{AnnotationsNamespace}.{Name}";
        public const string FlagsParameterName = "flags";
        public const string TableNameParameterName = "tableName";
        public const string BatchNameParameterName = "threadBatchSize";
        public const int BatchDefaultValue = 1;
    }

    public static class PrimaryKeyAttribute
    {
        public const string ShortName = "PrimaryKey";
        public const string Name = ShortName + "Attribute";
        public const string FullName = $"{AnnotationsNamespace}.{Name}";
    }

    public static class UniqueKeyAttribute
    {
        public const string ShortName = "UniqueKey";
        public const string Name = ShortName + "Attribute";
        public const string FullName = $"{AnnotationsNamespace}.{Name}";
    }

    public static class SecondaryKeyAttribute
    {
        public const string ShortName = "SecondaryKey";
        public const string Name = ShortName + "Attribute";
        public const string FullName = $"{AnnotationsNamespace}.{Name}";
    }

    public static class DatabaseAttribute
    {
        public const string ShortName = "Database";
        public const string Name = ShortName + "Attribute";
        public const string FlagsParameterName = "flags";
        public const string ThreadedComplexityThresholdParameterName = "threadedComplexityThreshold";

        public const string FullName = $"{AnnotationsNamespace}.{Name}";
    }

    public static class ValidatorAttribute
    {
        public const string ShortName = "Validator";
        public const string Name = ShortName + "Attribute";
        public const string FullName = $"{AnnotationsNamespace}.{Name}";
    }
}