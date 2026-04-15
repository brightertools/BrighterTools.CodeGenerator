namespace BrighterTools.CodeGenerator.Metadata;

public sealed class PropertyMetadata
{
    public string Name { get; init; } = string.Empty;
    public string PluralName { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public string DisplayTypeName { get; init; } = string.Empty;
    public string ShortTypeName { get; init; } = string.Empty;
    public string DefaultValue { get; init; } = "null";
    public string ParentClassName { get; init; } = string.Empty;
    public string? ForeignKeyTarget { get; set; }
    public string? ForeignKeyName { get; set; }
    public string FileReferenceName { get; init; } = string.Empty;
    public string JsonSerializerOptionsName { get; init; } = "Default";
    public string JsonEncryptedModelName { get; init; } = string.Empty;
    public string RequestTypeName { get; init; } = string.Empty;
    public string ResponseTypeName { get; init; } = string.Empty;
    public string ResponseInitializer { get; init; } = string.Empty;
    public bool IsNullable { get; init; }
    public bool IsEnum { get; init; }
    public bool IsStoredAsJson { get; set; }
    public bool JsonIgnore { get; init; }
    public bool NotMapped { get; init; }
    public bool IsExpressionBodied { get; init; }
    public bool IsNavigation { get; set; }
    public bool IsForeignKey { get; init; }
    public bool IsCollection { get; init; }
    public bool IsRequired { get; init; }
    public bool IsFileReference { get; init; }
    public bool ExcludeFromApi { get; init; }
    public bool IncludeInApi { get; init; }
    public bool IsJoinPropertyCollection { get; init; }
    public bool IsInterface { get; init; }
    public bool IsStoredAsJsonEncrypted { get; init; }
    public bool IncludeInCreateRequest { get; init; }
    public bool IncludeInUpdateRequest { get; init; }
    public bool IncludeInResponse { get; init; }

    public string NonNullableDisplayType =>
        DisplayTypeName.EndsWith("?", StringComparison.Ordinal)
            ? DisplayTypeName[..^1]
            : DisplayTypeName;
}
