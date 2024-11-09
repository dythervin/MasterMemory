// ReSharper disable once CheckNamespace

namespace MasterMemory.Annotations
{
    [System.AttributeUsage(System.AttributeTargets.Struct | System.AttributeTargets.Class)]
    public class TableAttribute : System.Attribute
    {
        public string TableName { get; }

        public DbTableFlags Flags { get; }

        public TableAttribute(string tableName,
            DbTableFlags flags = DbTableFlags.MultithreadedInitialization | DbTableFlags.MultithreadedModifications,
            int threadBatchSize = 1)
        {
            TableName = tableName;
            Flags = flags;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public class PrimaryKeyAttribute : System.Attribute
    {
        public int KeyOrder { get; }

        public PrimaryKeyAttribute(int keyOrder = 0)
        {
            this.KeyOrder = keyOrder;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class UniqueKeyAttribute : System.Attribute
    {
        public uint? GroupIndex { get; }

        public int KeyOrder { get; }

        public UniqueKeyAttribute(uint groupIndex, int keyOrder = 0)
        {
            GroupIndex = groupIndex;
            KeyOrder = keyOrder;
        }

        public UniqueKeyAttribute()
        {
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class SecondaryKeyAttribute : System.Attribute
    {
        public uint? GroupIndex { get; }

        public int KeyOrder { get; }

        public SecondaryKeyAttribute(uint groupIndex, int keyOrder = 0)
        {
            GroupIndex = groupIndex;
            KeyOrder = keyOrder;
        }

        public SecondaryKeyAttribute()
        {
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class DatabaseAttribute : System.Attribute
    {
        public DatabaseFlags? Flags { get; }

        public int ThreadedComplexityThreshold { get; }

        public DatabaseAttribute(int threadedComplexityThreshold = 1024)
        {
            Flags = null;
            ThreadedComplexityThreshold = threadedComplexityThreshold;
        }

        public DatabaseAttribute(DatabaseFlags flags, int threadedComplexityThreshold = 1024)
        {
            Flags = flags;
            ThreadedComplexityThreshold = threadedComplexityThreshold;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class ValidatorAttribute : System.Attribute
    {
    }
}