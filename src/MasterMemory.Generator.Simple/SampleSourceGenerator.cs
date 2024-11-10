using System.Linq;
using MasterMemory.Generator.Core;
using MasterMemory.Generator.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MasterMemory.Generator.Simple;

[Generator]
public class SampleSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            return;

        var compilation = context.Compilation;
        var tableModels = receiver.RecordTypeDeclarations.Select(x =>
        {
            var namedTypeSymbol = compilation.GetSemanticModel(x.type.SyntaxTree).GetDeclaredSymbol(x.type);
            if (namedTypeSymbol == null)
            {
                return (TableModel?)null;
            }

            var attribute = namedTypeSymbol.GetAttributes().First(attributeData =>
                attributeData.AttributeClass!.ToDisplayString() == DatabaseSourceGenerator.TableAttribute.FullName);

            return DatabaseSourceGenerator.ParseTable(namedTypeSymbol, attribute, compilation);
        }).Where(x => x.HasValue).Select(x => x!.Value).ToArray();

        var validatorModels = receiver.ValidatorTypeDeclarations.Select(x =>
        {
            var namedTypeSymbol = compilation.GetSemanticModel(x.type.SyntaxTree).GetDeclaredSymbol(x.type);
            if (namedTypeSymbol != null && namedTypeSymbol.GetAttributes().Any(attributeData =>
                    attributeData.AttributeClass!.ToDisplayString() ==
                    DatabaseSourceGenerator.ValidatorAttribute.FullName))
            {
                return DatabaseSourceGenerator.ParseValidator(namedTypeSymbol, x.type);
            }

            return (ValidatorModel?)null;
        }).Where(x => x.HasValue).Select(x => x!.Value).ToArray();

        INamedTypeSymbol? databaseType = receiver.DatabaseTypeDeclaration != null ?
            compilation.GetSemanticModel(receiver.DatabaseTypeDeclaration.SyntaxTree)
                .GetDeclaredSymbol(receiver.DatabaseTypeDeclaration) :
            null;

        var databaseModel = databaseType is not null ?
            DatabaseSourceGenerator.ParseDatabase(databaseType,
                databaseType.GetAttributes().First(x =>
                    x.AttributeClass!.ToDisplayString() == DatabaseSourceGenerator.DatabaseAttribute.FullName),
                compilation) :
            DatabaseModel.CreateDefault(compilation.GetReferencedFlags());

        var parseOptions =
            new ParseOptionsModel(((CSharpParseOptions)compilation.SyntaxTrees.First().Options).LanguageVersion);

        DatabaseSourceGenerator.GenerateDatabase(context,
            tableModels,
            databaseModel,
            validatorModels,
            parseOptions,
            true);
    }
}