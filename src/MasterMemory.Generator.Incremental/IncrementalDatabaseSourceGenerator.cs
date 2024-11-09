using System.Linq;
using MasterMemory.Generator.Core;
using MasterMemory.Generator.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MasterMemory.Generator;

[Generator]
internal class IncrementalDatabaseSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tablePipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            DatabaseSourceGenerator.TableAttribute.FullName,
            static (syntaxNode, _) =>
                syntaxNode.Kind() is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration
                    or SyntaxKind.RecordStructDeclaration or SyntaxKind.RecordDeclaration,
            (context1, _) => DatabaseSourceGenerator.ParseTable((INamedTypeSymbol)context1.TargetSymbol,
                context1.Attributes[0],
                context1.SemanticModel.Compilation));

        var validatorPipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            DatabaseSourceGenerator.ValidatorAttribute.FullName,
            static (syntaxNode, _) => syntaxNode is TypeDeclarationSyntax,
            (context1, _) =>
                DatabaseSourceGenerator.ParseValidator((INamedTypeSymbol)context1.TargetSymbol, context1.TargetNode));

        var databasePipeline = context.SyntaxProvider
            .ForAttributeWithMetadataName(DatabaseSourceGenerator.DatabaseAttribute.FullName,
                static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                (context1, _) =>
                    DatabaseSourceGenerator.ParseDatabase((INamedTypeSymbol)context1.TargetSymbol,
                        context1.Attributes[0],
                        context1.SemanticModel.Compilation)).Collect().Combine(context.CompilationProvider)
            .Select((tuple, _) => tuple.Left.Length > 0 ?
                tuple.Left[0] :
                DatabaseModel.CreateDefault(tuple.Right.GetReferencedFlags()));

        var parseOptions =
            context.ParseOptionsProvider.Select((x, _) =>
                new ParseOptionsModel(((CSharpParseOptions)x).LanguageVersion));

        context.RegisterImplementationSourceOutput(tablePipeline.Combine(databasePipeline),
            (context1, tuple) =>
            {
                TableModel tableModel = tuple.Left with { DatabaseModel = tuple.Right };
                DatabaseSourceGenerator.GenerateTable(context1, tableModel);
            });

        context.RegisterImplementationSourceOutput(validatorPipeline.Combine(databasePipeline),
            (context1, tuple) =>
            {
                DatabaseSourceGenerator.GenerateValidator(context1, tuple.Left, tuple.Right);
            });

        context.RegisterImplementationSourceOutput(tablePipeline.Collect().Combine(databasePipeline)
                .Combine(validatorPipeline.Collect()).Combine(parseOptions),
            (productionContext, tuple) =>
            {
                var (((tableArray, database), validator), parseOptionsModel) = tuple;
                DatabaseSourceGenerator.GenerateDatabase(productionContext,
                    tableArray.ToArray(),
                    database,
                    validator.ToArray(),
                    parseOptionsModel,
                    false);
            });
    }
}