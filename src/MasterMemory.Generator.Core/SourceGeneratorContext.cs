using System;
using System.Text;
using MasterMemory.Generator.Core.Internal;
using Microsoft.CodeAnalysis;

namespace MasterMemory.Generator.Core;

public readonly ref struct SourceGeneratorContext
{
    private readonly SourceProductionContext? _incrementalProductionContext;
    private readonly GeneratorExecutionContext? _simpleProductionContext;

    public SourceGeneratorContext(SourceProductionContext incrementalProductionContext)
    {
        _incrementalProductionContext = incrementalProductionContext;
    }

    public SourceGeneratorContext(GeneratorExecutionContext simpleProductionContext)
    {
        _simpleProductionContext = simpleProductionContext;
    }

    public void AddSource(string fileName, string source)
    {
        if (_incrementalProductionContext != null)
        {
            _incrementalProductionContext.Value.AddSource(fileName, source);
        }
        else if (_simpleProductionContext != null)
        {
            _simpleProductionContext.Value.AddSource(fileName, source);
        }
        else
        {
            throw new InvalidOperationException("Invalid context.");
        }
    }

    public void AddSource(string fileName, StringBuilder sb)
    {
        AddSource(fileName, SourceTextFormatter.FormatCompilationUnit(sb.ToStringAndClear()).ToFullString());
    }

    public static implicit operator SourceGeneratorContext(SourceProductionContext generatorContext) =>
        new(generatorContext);

    public static implicit operator SourceGeneratorContext(GeneratorExecutionContext generatorContext) =>
        new(generatorContext);

    public void ReportDiagnostic(Diagnostic create)
    {
        if (_incrementalProductionContext != null)
        {
            _incrementalProductionContext.Value.ReportDiagnostic(create);
        }
        else if (_simpleProductionContext != null)
        {
            _simpleProductionContext.Value.ReportDiagnostic(create);
        }
        else
        {
            throw new InvalidOperationException("Invalid context.");
        }
    }
}