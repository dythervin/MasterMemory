using System;
using System.Linq;

namespace MasterMemory.Generator;

public partial class DatabaseSourceGenerator
{
    public const string GroupIndexParameterName = "groupIndex";
    public const string KeyOrderParameterName = "keyOrder";
    private static class TableAttribute
    {
        public const string Name = "TableAttribute";

        public const string FullName = $"{Namespace}.{Name}";
        public const string FlagsParameterName = "flags";
        public const string TableNameParameterName = "tableName";
        public const string BatchNameParameterName = "threadBatchSize";
        public const int BatchDefaultValue = 1;

        public static readonly string Source = $$"""
                                                 [System.Flags]
                                                 public enum {{nameof(DbTableFlags)}}
                                                 {
                                                      {{string.Join(", ", ((DbTableFlags[])Enum.GetValues(typeof(DbTableFlags))).Select(f => $"{f} = {(int)f}"))}}
                                                 }

                                                 [System.AttributeUsage(System.AttributeTargets.Struct | System.AttributeTargets.Class)]
                                                 public class {{Name}} : System.Attribute
                                                 {
                                                      public string TableName { get; }
                                                      
                                                      public {{nameof(DbTableFlags)}} Flags { get; }
                                                      
                                                      public {{Name}}(string {{TableNameParameterName}}, {{nameof(DbTableFlags)}} {{FlagsParameterName}} = {{nameof(DbTableFlags)}}.{{nameof(DbTableFlags.MultithreadedInitialization)}} | {{nameof(DbTableFlags)}}.{{nameof(DbTableFlags.MultithreadedModifications)}}, int {{BatchNameParameterName}} = {{BatchDefaultValue}})
                                                      {
                                                          TableName = {{TableNameParameterName}};
                                                          Flags = {{FlagsParameterName}};
                                                      }
                                                 }
                                                 """;
    }

    private static class PrimaryKeyAttribute
    {
        public const string Name = "PrimaryKeyAttribute";
        public const string FullName = $"{Namespace}.{Name}";

        public const string Source = $$"""
                                       [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
                                       public class {{Name}} : System.Attribute 
                                       {
                                           public int KeyOrder { get; }

                                           public PrimaryKeyAttribute(int {{KeyOrderParameterName}} = 0)
                                           {
                                               this.KeyOrder = {{KeyOrderParameterName}};
                                           }
                                       }
                                       """;
    }

    private static class UniqueKeyAttribute
    {
        public const string Name = "UniqueKeyAttribute";
        public const string FullName = $"{Namespace}.{Name}";

        public const string Source = $$"""
                                       [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
                                       public class {{Name}} : System.Attribute 
                                       {
                                            public uint? GroupIndex { get; }
                                            
                                            public int KeyOrder { get; }
                                           
                                            public {{Name}}(uint {{GroupIndexParameterName}}, int {{KeyOrderParameterName}} = 0)
                                            {
                                                GroupIndex = {{GroupIndexParameterName}};
                                                KeyOrder = {{KeyOrderParameterName}};
                                            }
                                           
                                            public {{Name}}()
                                            {
                                            }
                                       }
                                       """;
    }

    private static class SecondaryKeyAttribute
    {
        public const string Name = "SecondaryKeyAttribute";
        public const string FullName = $"{Namespace}.{Name}";

        public const string Source = $$"""
                                       [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
                                       public class {{Name}} : System.Attribute
                                       {
                                            public uint? GroupIndex { get; }
                                            
                                            public int KeyOrder { get; }
                                           
                                            public {{Name}}(uint {{GroupIndexParameterName}}, int {{KeyOrderParameterName}} = 0)
                                            {
                                                GroupIndex = {{GroupIndexParameterName}};
                                                KeyOrder = {{KeyOrderParameterName}};
                                            }
                                           
                                            public {{Name}}()
                                            {
                                            }
                                       }
                                       """;
    }

    private static class DatabaseAttribute
    {
        public const string Name = "DatabaseAttribute";
        public const string FlagsParameterName = "flags";
        public const string ThreadedComplexityThresholdParameterName = "threadedComplexityThreshold";

        public const string FullName = $"{Namespace}.{Name}";

        public static readonly string Source = $$"""
                                                 [System.Flags]
                                                 public enum {{nameof(DatabaseFlags)}}
                                                 {
                                                      {{string.Join(", ", ((DatabaseFlags[])Enum.GetValues(typeof(DatabaseFlags))).Select(f => $"{f} = {(int)f}"))}}
                                                 }

                                                 [System.AttributeUsage(System.AttributeTargets.Class)]
                                                 public class {{Name}} : System.Attribute 
                                                 {
                                                      public {{nameof(DatabaseFlags)}}? Flags { get; }
                                                 
                                                      public int ThreadedComplexityThreshold { get; }
                                                      
                                                      public {{Name}}(int {{ThreadedComplexityThresholdParameterName}} = 1024)
                                                      {
                                                          Flags = null;
                                                          ThreadedComplexityThreshold = {{ThreadedComplexityThresholdParameterName}};
                                                      }
                                                      
                                                      public {{Name}}({{nameof(DatabaseFlags)}} {{FlagsParameterName}}, int {{ThreadedComplexityThresholdParameterName}} = 1024)
                                                      {
                                                          Flags = {{FlagsParameterName}};
                                                          ThreadedComplexityThreshold = {{ThreadedComplexityThresholdParameterName}};
                                                      }
                                                      
                                                 }
                                                 """;
    }

    private static class ValidatorAttribute
    {
        public const string Name = "ValidatorAttribute";
        public const string FullName = $"{Namespace}.{Name}";

        public const string Source = $$"""
                                       [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
                                       public class {{Name}} : System.Attribute {}
                                       """;
    }
}