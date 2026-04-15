namespace BrighterTools.CodeGenerator.Metadata;

public sealed class EnumMetadata
{
    public string Name { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public List<EnumMemberMetadata> Members { get; init; } = [];
}

public sealed class EnumMemberMetadata
{
    public string Name { get; init; } = string.Empty;
    public string? StringValue { get; init; }
    public int? Value { get; init; }
    public string ValueLiteral { get; init; } = "0";
}
