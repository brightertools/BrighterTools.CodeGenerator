using BrighterTools.CodeGenerator.Metadata;

namespace BrighterTools.CodeGenerator.Generators;

public interface ICodeGenerator
{
    IEnumerable<GeneratedFile> Generate(GenerationContext context);
}
