namespace BrighterTools.CodeGenerator.Metadata;

public sealed class GenerationContext
{
    public required GeneratorOptions Options { get; init; }
    public required IReadOnlyList<ClassMetadata> Models { get; init; }
    public required IReadOnlyList<ClassMetadata> AllModels { get; init; }
    public required IReadOnlyList<ApiModelMetadata> ApiModels { get; init; }
    public required IReadOnlyList<EnumMetadata> Enums { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
}
