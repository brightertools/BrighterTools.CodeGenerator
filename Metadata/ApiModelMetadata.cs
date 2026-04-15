namespace BrighterTools.CodeGenerator.Metadata;

public sealed class ApiModelMetadata
{
    public string Name { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public List<ApiModelPropertyMetadata> Properties { get; init; } = [];
}

public sealed class ApiModelPropertyMetadata
{
    public string Name { get; init; } = string.Empty;
    public string CamelCaseName { get; init; } = string.Empty;
    public string TypeScriptType { get; init; } = "any";
    public bool IsOptional { get; init; }
}
