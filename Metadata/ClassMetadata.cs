using Humanizer;

namespace BrighterTools.CodeGenerator.Metadata;

public sealed class ClassMetadata
{
    public static readonly string[] BaseModelProperties =
    [
        "Id",
        "TenantId",
        "Tenant",
        "Guid",
        "Deleted",
        "CreatedDate",
        "LastUpdatedDate"
    ];

    private static readonly HashSet<string> ReservedEntityTypeNames = new(StringComparer.Ordinal)
    {
        "Action",
        "Directory",
        "Environment",
        "File",
        "Index",
        "Path",
        "Range",
        "Task"
    };

    public string Name { get; init; } = string.Empty;
    public string PluralName { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public string BaseClass { get; init; } = string.Empty;
    public string InterfaceName { get; init; } = string.Empty;
    public string QualifiedName => $"{Namespace}.{Name}";
    public bool RequiresEntityTypeAlias => ReservedEntityTypeNames.Contains(Name);
    public string EntityTypeAlias => $"Data{Name}";
    public string EntityTypeReference => RequiresEntityTypeAlias ? EntityTypeAlias : Name;
    public string EntityTypeUsingDirective => RequiresEntityTypeAlias ? $"using {EntityTypeAlias} = {QualifiedName};" : $"using {Namespace};";
    public string RepositoryName => $"{Name}Repository";
    public string ServiceName => $"{Name}Service";
    public string RequestNamespace => $"App.Dto.{PluralName}.Requests";
    public string ResponseNamespace => $"App.Dto.{PluralName}.Responses";
    public List<PropertyMetadata> Properties { get; init; } = [];
    public bool IsBaseModel { get; init; }
    public bool IsJsonModel { get; init; }
    public bool IsJoinTable { get; init; }
    public bool InheritsBaseModel { get; init; }
    public bool ExcludeFromApi { get; init; }
    public bool ExcludeTenantIdInQueryFilters { get; init; }
    public bool HasTenantId { get; init; }
    public bool HasDeleted { get; init; }
    public bool HasGuid { get; init; }
    public bool HasId { get; init; }
    public bool HasFileStorageId { get; init; }
    public bool HasOrder { get; init; }

    public bool HasProperty(string name)
    {
        return Properties.Any(p => p.Name == name)
               || (BaseClass.Contains("BaseModel", StringComparison.Ordinal) && BaseModelProperties.Contains(name));
    }

    public IReadOnlyList<PropertyMetadata> StringProperties =>
        Properties.Where(p =>
                p.DisplayTypeName == "string"
                && !p.Name.EndsWith("Key", StringComparison.Ordinal)
                && !p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)
                && !p.Name.Contains("FileStorage", StringComparison.Ordinal))
            .ToList();

    public IReadOnlyList<PropertyMetadata> FileStorageProperties =>
        Properties.Where(p => p.Name.EndsWith("FileStorageId", StringComparison.Ordinal)).ToList();

    public PropertyMetadata? NameLookupProperty => GetLookupProperty("Name");
    public PropertyMetadata? EmailLookupProperty => GetLookupProperty("Email");
    public PropertyMetadata? HandleLookupProperty => GetLookupProperty("Handle");
    public PropertyMetadata? SlugLookupProperty => GetLookupProperty("Slug");

    public IReadOnlyList<PropertyMetadata> KeyLookupProperties =>
        Properties
            .Where(p => p.Name.EndsWith("Key", StringComparison.Ordinal) && IsLookupProperty(p))
            .ToList();

    public IReadOnlyList<PropertyMetadata> SettableStateProperties =>
        Properties
            .Where(p =>
                IsLookupProperty(p)
                && (p.DisplayTypeName == "bool" || p.IsEnum)
                && p.Name is not "Deleted" and not "IsNew")
            .ToList();

    public string DefaultSortProperty
    {
        get
        {
            foreach (var property in new[] { "Name", "Title", "Order", "LastUpdatedDate", "CreatedDate" })
            {
                if (HasProperty(property))
                {
                    return property;
                }
            }

            return "Id";
        }
    }

    public IReadOnlyList<PropertyMetadata> GetApiProperties(bool removeId, bool isUpdateRequest)
    {
        return Properties.Where(prop =>
            (!removeId || prop.Name != "Id")
            && !(prop.NotMapped && !prop.IncludeInApi)
            && !(prop.ExcludeFromApi && !prop.IncludeInApi)
            && !prop.IsStoredAsJson
            && !prop.JsonIgnore
            && !(isUpdateRequest && prop.Name is "Guid" or "TenantId" or "Tenant" or "Deleted" or "CreatedDate" or "LastUpdatedDate")
            && !(isUpdateRequest && prop.Name.EndsWith("Key", StringComparison.Ordinal))
            && !(isUpdateRequest && prop.IsExpressionBodied)
            && !(isUpdateRequest && prop.IsNavigation)
            && !(isUpdateRequest && prop.IsCollection)
            && !(isUpdateRequest && prop.NotMapped))
            .ToList();
    }

    public string CollectionDtoFolderName => PluralName;

    private PropertyMetadata? GetLookupProperty(string name) =>
        Properties.FirstOrDefault(p => p.Name == name && IsLookupProperty(p));

    private static bool IsLookupProperty(PropertyMetadata property) =>
        !property.IsNavigation
        && !property.IsCollection
        && !property.NotMapped
        && !property.IsExpressionBodied;
}
