using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using MasterMemory.Annotations;
using MasterMemory.Generator.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MasterMemory.Generator.Core;

public partial class DatabaseSourceGenerator
{
    public static void GenerateValidator(SourceGeneratorContext context, ValidatorModel validatorModel,
        DatabaseModel database)
    {
        GenerateValidator(context, validatorModel with { DatabaseModel = database });
    }

    private static void GenerateValidator(SourceGeneratorContext context, ValidatorModel model)
    {
        try
        {
            ValidatorGenerator.Execute(context, model);
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(Diagnostic.Create("GEN001",
                "Generator",
                e.ToString(),
                DiagnosticSeverity.Error,
                DiagnosticSeverity.Error,
                true,
                0));
        }
    }

    public static ValidatorModel ParseValidator(INamedTypeSymbol contextTargetSymbol, SyntaxNode node)
    {
        return new ValidatorModel(contextTargetSymbol, (TypeDeclarationSyntax)node);
    }

    public static TableModel ParseTable(INamedTypeSymbol type, AttributeData attributeData, Compilation compilation)
    {
        var flags = DbTableFlags.Multithreaded;
        string tableName = type.Name;
        int threadBatchSize = TableAttribute.BatchDefaultValue;
        foreach (TypedConstant attributeDataConstructorArgument in attributeData.ConstructorArguments)
        {
            if (attributeDataConstructorArgument.Type?.SpecialType == SpecialType.System_String)
            {
                tableName = (string)attributeDataConstructorArgument.Value!;
            }

            if (attributeDataConstructorArgument.Type?.SpecialType == SpecialType.System_Int32)
            {
                threadBatchSize = (int)attributeDataConstructorArgument.Value!;
            }
            else if (attributeDataConstructorArgument.Type?.ToDisplayString() == AnnotationsNamespace + "." + nameof(DbTableFlags))
            {
                flags = (DbTableFlags)attributeDataConstructorArgument.Value!;
            }
        }

        foreach (var namedArgument in attributeData.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case TableAttribute.FlagsParameterName:
                    flags = (DbTableFlags)namedArgument.Value.Value!;
                    break;
                case TableAttribute.TableNameParameterName:
                    tableName = (string)namedArgument.Value.Value!;
                    break;
                case TableAttribute.BatchNameParameterName:
                    threadBatchSize = (int)namedArgument.Value.Value!;
                    break;
            }
        }

        if (threadBatchSize < 1)
        {
            threadBatchSize = TableAttribute.BatchDefaultValue;
        }

        var primaryKeyMembers = type.GetMembers()
            .Where(m => m.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == PrimaryKeyAttribute.FullName))
            .Select(m => ParseKeys(m, PrimaryKeyAttribute.FullName).FirstOrDefault()).ToArray();

        var uniqueKeys = type.GetMembers()
            .Where(m => m.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == UniqueKeyAttribute.FullName))
            .SelectMany(m => ParseKeys(m, UniqueKeyAttribute.FullName)).ToGroups();

        List<KeyGroupModel> keyGroups;
        {
            var secondaryKeys = type.GetMembers()
                .Where(m => m.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() == SecondaryKeyAttribute.FullName))
                .SelectMany(m => ParseKeys(m, SecondaryKeyAttribute.FullName)).ToArray();

            foreach (var uniqueKey in uniqueKeys)
            {
                if (!uniqueKey.IsSingle)
                    continue;

                for (int i = 0; i < primaryKeyMembers.Length; i++)
                {
                    KeyModel member = primaryKeyMembers[i];
                    if (uniqueKey.Name == member.Symbol.Name)
                        primaryKeyMembers[i] = member with { IsUnique = true };
                }

                for (int i = 0; i < secondaryKeys.Length; i++)
                {
                    var member = secondaryKeys[i];
                    if (uniqueKey.Name == member.Symbol.Name)
                        secondaryKeys[i] = member with { IsUnique = true };
                }
            }

            if (primaryKeyMembers.Length == 1)
            {
                KeyModel primaryKey = primaryKeyMembers[0] with { IsUnique = true };
                primaryKeyMembers[0] = primaryKey;
                for (int i = 0; i < secondaryKeys.Length; i++)
                {
                    var member = secondaryKeys[i];
                    if (member.Name == primaryKey.Name)
                        secondaryKeys[i] = member with { IsUnique = true };
                }
            }

            keyGroups = secondaryKeys.ToGroups();

            foreach (var uniqueKey in uniqueKeys)
            {
                if (uniqueKey.IsSingle)
                    continue;

                for (int i = 0; i < keyGroups.Count; i++)
                {
                    var member = keyGroups[i];
                    if (uniqueKey.Name == member.Name)
                        keyGroups[i] = member with { IsGroupUnique = true };
                }
            }

            keyGroups.Insert(0, new(primaryKeyMembers) { IsGroupUnique = true });
        }

        return new(type,
            keyGroups.ToImmutableArray(),
            uniqueKeys.ToImmutableArray(),
            tableName ?? type.Name,
            flags,
            threadBatchSize) { DatabaseModel = DatabaseModel.CreateDefault(compilation.GetReferencedFlags()) };
    }

    public static DatabaseModel ParseDatabase(INamedTypeSymbol type, AttributeData attributeData,
        Compilation compilation)
    {
        int threadedComplexityThreshold = 0;
        DatabaseFlags? flags = null;
        foreach (TypedConstant attributeDataConstructorArgument in attributeData.ConstructorArguments)
        {
            if (attributeDataConstructorArgument.Type?.SpecialType == SpecialType.System_Int32)
            {
                threadedComplexityThreshold = (int)attributeDataConstructorArgument.Value!;
            }
            else if (attributeDataConstructorArgument.Type?.ToDisplayString() ==
                     AnnotationsNamespace + "." + nameof(DatabaseFlags))
            {
                flags = (DatabaseFlags)attributeDataConstructorArgument.Value!;
            }
        }

        foreach (var namedArgument in attributeData.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case DatabaseAttribute.FlagsParameterName:
                    flags = (DatabaseFlags)namedArgument.Value.Value!;
                    break;
                case DatabaseAttribute.ThreadedComplexityThresholdParameterName:
                    threadedComplexityThreshold = (int)namedArgument.Value.Value!;
                    break;
            }
        }

        return new(type, flags ?? compilation.GetReferencedFlags(), threadedComplexityThreshold);
    }

    public static void GenerateTable(SourceGeneratorContext context, TableModel tableModel)
    {
        try
        {
            TableGenerator.Execute(context, tableModel);
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(Diagnostic.Create("GEN001",
                "Generator",
                e.ToString(),
                DiagnosticSeverity.Error,
                DiagnosticSeverity.Error,
                true,
                0));
        }
    }

    public static IEnumerable<KeyModel> ParseKeys(ISymbol m, string attributeFullName)
    {
        foreach (AttributeData attributeData in m.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() == attributeFullName)
            {
                yield return ParseKey(m, attributeData);
            }
        }
    }

    public static KeyModel ParseKey(ISymbol symbol, AttributeData attributeData)
    {
        uint? groupIndex = null;
        int keyOrder = 0;
        foreach (TypedConstant argument in attributeData.ConstructorArguments)
        {
            switch (argument.Type?.SpecialType)
            {
                case SpecialType.System_UInt32:
                    groupIndex = (uint)argument.Value!;
                    break;
                case SpecialType.System_Int32:
                    keyOrder = (int)argument.Value!;
                    break;
            }
        }

        foreach (var argument in attributeData.NamedArguments)
        {
            switch (argument.Key)
            {
                case GroupIndexParameterName:
                    groupIndex = (uint)argument.Value.Value!;
                    break;
                case KeyOrderParameterName:
                    keyOrder = (int)argument.Value.Value!;
                    break;
            }
        }

        return new(symbol, groupIndex, false, keyOrder);
    }

    public static void GenerateDatabase(SourceGeneratorContext context, TableModel[] tables, DatabaseModel database,
        ValidatorModel[] validators, ParseOptionsModel parseOptionsModel, bool generateTablesAndValidators)
    {
        try
        {
            if (tables.Length == 0)
            {
                return;
            }

            {
                for (int i = 0; i < tables.Length; i++)
                {
                    tables[i] = tables[i] with { DatabaseModel = database };
                }
            }

            var tableArray = tables.ToImmutableArray();

            {
                for (int i = 0; i < validators.Length; i++)
                {
                    validators[i] = validators[i] with { DatabaseModel = database };
                }
            }

            var validatorArray = validators.ToImmutableArray();

            if (generateTablesAndValidators)
            {
                foreach (TableModel model in tables)
                {
                    GenerateTable(context, model);
                }

                foreach (ValidatorModel model in validatorArray)
                {
                    GenerateValidator(context, model);
                }
            }

            var sb = new StringBuilder();
            DatabaseGenerator.Execute(context, tableArray, validatorArray, database, sb);
            DatabaseSerializationGenerator.Execute(context, tableArray, database, sb);
            DatabaseExtensionsGenerator.Execute(context, tableArray, database, sb);

            TableContainerGenerator.Execute(context, tableArray, database, sb);
            TransactionGenerator.Execute(context, tableArray, database, sb);
            DatabaseJsonConvertorGenerator.Execute(context, tableArray, database, sb);
            TransactionObserverGenerator.Execute(context, database, sb);
            DatabaseBuilderGenerator.Execute(context, tableArray, database, sb);
            ValidatorGenerator.Execute(context, database, sb);
            MemoryPackFormatterGenerator.Execute(context, database, parseOptionsModel, sb);
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(Diagnostic.Create("GEN001",
                "Generator",
                e.ToString(),
                DiagnosticSeverity.Error,
                DiagnosticSeverity.Error,
                true,
                0));
        }
    }
}