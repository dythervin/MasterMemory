using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using MasterMemory.Generator.Internal;
using MasterMemory.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MasterMemory.Generator;

/// <summary>
///     A sample source generator that creates a custom report based on class properties. The target class should be
///     annotated with the "Generators.ReportAttribute" attribute.
///     When using the source code as a baseline, an incremental source generator is preferable because it reduces the
///     performance overhead.
/// </summary>
[Generator]
public partial class DatabaseSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attribute to the compilation.
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("Attributes.g.cs",
                SourceText.From(SourceTextFormatter.FormatCompilationUnit($"namespace {Namespace}" + "{" +
                                                                          TableAttribute.Source +
                                                                          PrimaryKeyAttribute.Source +
                                                                          SecondaryKeyAttribute.Source +
                                                                          UniqueKeyAttribute.Source +
                                                                          DatabaseAttribute.Source +
                                                                          ValidatorAttribute.Source + "}",
                        false).ToFullString(),
                    Encoding.UTF8));
        });

        var tablePipeline = context.SyntaxProvider.ForAttributeWithMetadataName(TableAttribute.FullName,
            static (syntaxNode, cancellationToken) => syntaxNode.Kind() is SyntaxKind.ClassDeclaration
                or SyntaxKind.StructDeclaration or SyntaxKind.RecordStructDeclaration or SyntaxKind.RecordDeclaration,
            TransformTable);

        var validatorPipeline = context.SyntaxProvider.ForAttributeWithMetadataName(ValidatorAttribute.FullName,
            static (syntaxNode, cancellationToken) => syntaxNode is TypeDeclarationSyntax,
            TransformValidator);

        var databasePipeline = context.SyntaxProvider
            .ForAttributeWithMetadataName(DatabaseAttribute.FullName,
                static (syntaxNode, cancellationToken) => syntaxNode is ClassDeclarationSyntax,
                TransformDatabase).Collect().Combine(context.CompilationProvider).Select((tuple, _) =>
                tuple.Left.Length > 0 ? tuple.Left[0] : DatabaseModel.CreateDefault(tuple.Right.GetReferencedFlags()));

        var parseOptions =
            context.ParseOptionsProvider.Select((x, _) =>
                new ParseOptionsModel(((CSharpParseOptions)x).LanguageVersion));

        context.RegisterImplementationSourceOutput(tablePipeline.Combine(databasePipeline), GenerateTable);
        context.RegisterImplementationSourceOutput(validatorPipeline.Combine(databasePipeline), GenerateValidator);

        context.RegisterImplementationSourceOutput(tablePipeline.Collect().Combine(databasePipeline)
                .Combine(validatorPipeline.Collect()).Combine(parseOptions),
            GenerateDatabase);
    }

    private static void GenerateValidator(SourceProductionContext context,
        (ValidatorModel Left, DatabaseModel Right) tuple)
    {
        try
        {
            ValidatorGenerator.Execute(context, tuple.Left with { DatabaseModel = tuple.Right });
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

    private static TableModel TransformTable(GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var type = (INamedTypeSymbol)context.TargetSymbol;

        AttributeData attributeData = context.Attributes[0];
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
            else if (attributeDataConstructorArgument.Type?.ToDisplayString() == Namespace + "." + nameof(DbTableFlags))
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
            .Select(m => SelectKey(m, PrimaryKeyAttribute.FullName).FirstOrDefault()).ToArray();

        var uniqueKeys = type.GetMembers()
            .Where(m => m.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == UniqueKeyAttribute.FullName))
            .SelectMany(m => SelectKey(m, UniqueKeyAttribute.FullName)).ToGroups();

        List<KeyGroupModel> keyGroups;
        {
            var secondaryKeys = type.GetMembers()
                .Where(m => m.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() == SecondaryKeyAttribute.FullName))
                .SelectMany(m => SelectKey(m, SecondaryKeyAttribute.FullName)).ToArray();

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
            threadBatchSize)
        {
            DatabaseModel = DatabaseModel.CreateDefault(context.SemanticModel.Compilation.GetReferencedFlags())
        };
    }

    private static ValidatorModel TransformValidator(GeneratorAttributeSyntaxContext context, CancellationToken arg2)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        return new ValidatorModel(namedTypeSymbol, (TypeDeclarationSyntax)context.TargetNode);
    }

    private static DatabaseModel TransformDatabase(GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.TargetSymbol;
        AttributeData attributeData = context.Attributes[0];
        int threadedComplexityThreshold = 0;
        DatabaseFlags? flags = null;
        foreach (TypedConstant attributeDataConstructorArgument in attributeData.ConstructorArguments)
        {
            if (attributeDataConstructorArgument.Type?.SpecialType == SpecialType.System_Int32)
            {
                threadedComplexityThreshold = (int)attributeDataConstructorArgument.Value!;
            }
            else if (attributeDataConstructorArgument.Type?.ToDisplayString() ==
                     Namespace + "." + nameof(DatabaseFlags))
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

        return new(namedTypeSymbol.ToDisplayString(),
            namedTypeSymbol,
            flags ?? context.SemanticModel.Compilation.GetReferencedFlags(),
            threadedComplexityThreshold);
    }

    private static void GenerateTable(SourceProductionContext context, (TableModel Left, DatabaseModel Right) tuple)
    {
        try
        {
            TableGenerator.Execute(context, tuple.Left with { DatabaseModel = tuple.Right });
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

    private static IEnumerable<KeyModel> SelectKey(ISymbol m, string attributeFullName)
    {
        foreach (AttributeData attributeData in m.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() != attributeFullName)
            {
                continue;
            }

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

            yield return new(m, groupIndex, false, keyOrder);
        }
    }

    private static void GenerateDatabase(SourceProductionContext context,
        (((ImmutableArray<TableModel> tableArray, DatabaseModel database) Left, ImmutableArray<ValidatorModel>
            validatorArray) Left, ParseOptionsModel parseOptionsModel) tuple)
    {
        try
        {
            var tableArray = tuple.Left.Left.tableArray;
            if (tableArray.Length == 0)
            {
                return;
            }

            DatabaseModel databaseModel = tuple.Left.Left.database;
            var newArray = new TableModel[tableArray.Length];
            for (int i = 0; i < newArray.Length; i++)
            {
                newArray[i] = tableArray[i] with { DatabaseModel = databaseModel };
            }

            tableArray = newArray.ToImmutableArray();

            var sb = new StringBuilder();
            DatabaseGenerator.Execute(context, tableArray, tuple.Left.validatorArray, databaseModel, sb);
            DatabaseSerializationGenerator.Execute(context, tableArray, databaseModel, sb);
            DatabaseExtensionsGenerator.Execute(context, tableArray, databaseModel, sb);

            TableContainerGenerator.Execute(context, tableArray, databaseModel, sb);
            TransactionGenerator.Execute(context, tableArray, databaseModel, sb);
            DatabaseJsonConvertorGenerator.Execute(context, tableArray, databaseModel, sb);
            TransactionObserverGenerator.Execute(context, databaseModel, sb);
            DatabaseBuilderGenerator.Execute(context, tableArray, databaseModel, sb);
            ValidatorGenerator.Execute(context, databaseModel, sb);
            MemoryPackFormatterGenerator.Execute(context, databaseModel, tuple.parseOptionsModel, sb);
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