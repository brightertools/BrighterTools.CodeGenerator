using BrighterTools.CodeGenerator.Metadata;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace BrighterTools.CodeGenerator.Inspectors;

public sealed class ModelInspector(string projectPath, string modelNamespacePrefix)
{
    private readonly string _projectPath = projectPath;
    private readonly string _modelNamespacePrefix = modelNamespacePrefix;
    private readonly string _helpersNamespacePrefix = $"{modelNamespacePrefix}.Helpers";

    public async Task<IReadOnlyList<ClassMetadata>> InspectAsync(CancellationToken cancellationToken = default)
    {
        using var workspace = MSBuildWorkspace.Create();
        var solution = await OpenProjectAsSolutionAsync(workspace, _projectPath, cancellationToken);

        var joinTableTypes = await GetJoinTableTypesAsync(solution, cancellationToken);
        var result = new List<ClassMetadata>();

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                continue;
            }

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync(cancellationToken);

                foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol symbol)
                    {
                        continue;
                    }

                    var namespaceName = symbol.ContainingNamespace?.ToString() ?? string.Empty;
                    if (!IsModelNamespace(namespaceName))
                    {
                        continue;
                    }

                    result.Add(BuildClassMetadata(symbol, joinTableTypes));
                }
            }
        }

        return result.OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
    }

    private bool IsModelNamespace(string namespaceName)
    {
        return namespaceName.StartsWith(_modelNamespacePrefix, StringComparison.Ordinal)
            && !namespaceName.StartsWith(_helpersNamespacePrefix, StringComparison.Ordinal);
    }

    private static async Task<Solution> OpenProjectAsSolutionAsync(
        MSBuildWorkspace workspace,
        string projectFilePath,
        CancellationToken cancellationToken)
    {
        var project = await workspace.OpenProjectAsync(projectFilePath, cancellationToken: cancellationToken);
        return project.Solution;
    }

    private static async Task<HashSet<string>> GetJoinTableTypesAsync(Solution solution, CancellationToken cancellationToken)
    {
        var results = new HashSet<string>(StringComparer.Ordinal);

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                continue;
            }

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync(cancellationToken);

                foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol symbol)
                    {
                        continue;
                    }

                    if (HasAttribute(symbol, "IsJoinTableAttribute"))
                    {
                        results.Add(symbol.Name);
                    }
                }
            }
        }

        return results;
    }

    private ClassMetadata BuildClassMetadata(INamedTypeSymbol symbol, ISet<string> joinTableTypes)
    {
        var properties = GetPublicInstanceProperties(symbol).ToList();
        var propertyMetadata = properties.Select(x => BuildPropertyMetadata(symbol, x, properties, joinTableTypes)).ToList();
        var inheritsBaseModel = InheritsFrom(symbol, "BaseModel");

        var metadata = new ClassMetadata
        {
            Name = symbol.Name,
            PluralName = symbol.Name.Pluralize(),
            Namespace = symbol.ContainingNamespace?.ToString() ?? string.Empty,
            BaseClass = symbol.BaseType?.ToString() ?? string.Empty,
            InterfaceName = $"I{symbol.Name}",
            IsBaseModel = symbol.Name == "BaseModel",
            IsJsonModel = (symbol.ContainingNamespace?.ToString() ?? string.Empty).Contains("JsonModels", StringComparison.Ordinal),
            IsJoinTable = HasAttribute(symbol, "IsJoinTableAttribute"),
            InheritsBaseModel = inheritsBaseModel,
            ExcludeFromApi = HasAttribute(symbol, "ExcludeFromApiAttribute") || HasAttribute(symbol, "IsJoinTableAttribute"),
            ExcludeTenantIdInQueryFilters = HasAttribute(symbol, "ExcludeTenantIdInQueryFiltersAttribute"),
            HasId = propertyMetadata.Any(x => x.Name == "Id") || inheritsBaseModel,
            Properties = propertyMetadata,
            HasTenantId = propertyMetadata.Any(x => x.Name == "TenantId") || inheritsBaseModel,
            HasDeleted = propertyMetadata.Any(x => x.Name == "Deleted") || inheritsBaseModel,
            HasGuid = propertyMetadata.Any(x => x.Name == "Guid") || inheritsBaseModel,
            HasFileStorageId = propertyMetadata.Any(x => x.Name.Contains("FileStorageId", StringComparison.Ordinal)),
            HasOrder = propertyMetadata.Any(x => x.Name == "Order")
        };

        foreach (var foreignKeyProperty in metadata.Properties.Where(x => x.IsForeignKey))
        {
            var navigationProperty = metadata.Properties.FirstOrDefault(x => x.Name == foreignKeyProperty.ForeignKeyTarget);
            if (navigationProperty is null)
            {
                continue;
            }

            navigationProperty.ForeignKeyName = foreignKeyProperty.Name;
            navigationProperty.ForeignKeyTarget = foreignKeyProperty.ForeignKeyTarget;
            navigationProperty.IsNavigation = true;
        }

        return metadata;
    }

    private PropertyMetadata BuildPropertyMetadata(
        INamedTypeSymbol parentClass,
        IPropertySymbol property,
        IReadOnlyList<IPropertySymbol> allProperties,
        ISet<string> joinTableTypes)
    {
        var isForeignKey = IsForeignKeyIdType(property.Type)
                           && property.Name.EndsWith("Id", StringComparison.Ordinal)
                           && allProperties.Any(x => x.Name == property.Name[..^2]);

        var isJoinCollection = false;
        if (IsCollection(property) && TryGetCollectionElementTypeName(property.Type, out var elementTypeName))
        {
            isJoinCollection = joinTableTypes.Contains(elementTypeName);
        }

        var isFileReference = property.Name.EndsWith("FileStorageId", StringComparison.Ordinal);
        var fileReferenceName = isFileReference ? property.Name[..^"FileStorageId".Length] : string.Empty;
        var typeName = property.Type.ToString() ?? string.Empty;
        var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated;

        var metadata = new PropertyMetadata
        {
            Name = property.Name,
            PluralName = property.Name.Pluralize(),
            TypeName = typeName,
            DisplayTypeName = NormalizeDisplayType(typeName),
            ShortTypeName = property.Type.Name,
            DefaultValue = GetDefaultValue(property.Type, isNullable),
            ParentClassName = parentClass.Name,
            IsNullable = isNullable,
            IsEnum = IsEnumType(property.Type),
            IsStoredAsJson = typeName.Contains("JsonModels", StringComparison.Ordinal) || HasAttribute(property, "StoredAsJsonAttribute"),
            JsonIgnore = HasAttribute(property, "JsonIgnoreAttribute"),
            NotMapped = HasAttribute(property, "NotMappedAttribute"),
            IsExpressionBodied = property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is PropertyDeclarationSyntax syntax && syntax.ExpressionBody is not null,
            IsNavigation = IsNavigation(property),
            IsForeignKey = isForeignKey,
            ForeignKeyTarget = isForeignKey ? property.Name[..^2] : null,
            ForeignKeyName = GetForeignKeyNameIfAny(property, parentClass),
            IsCollection = IsCollection(property),
            IsRequired = IsRequired(property),
            IsFileReference = isFileReference,
            FileReferenceName = fileReferenceName,
            ExcludeFromApi = HasAttribute(property, "ExcludeFromApiAttribute"),
            IncludeInApi = HasAttribute(property, "IncludeInApiAttribute"),
            IsJoinPropertyCollection = isJoinCollection,
            IsInterface = IsInterfaceProperty(property),
            JsonSerializerOptionsName = GetJsonSerializerOptionsName(property),
            IsStoredAsJsonEncrypted = HasAttribute(property, "StoredAsJsonEncryptedAttribute"),
            JsonEncryptedModelName = GetJsonEncryptedModelName(property),
            RequestTypeName = ToDtoType(typeName, isNullable, true),
            ResponseTypeName = ToResponseDtoType(property.Type, isNullable, allowReferenceNullableSuffix: true),
            ResponseInitializer = GetResponseInitializer(typeName, isNullable),
            IncludeInCreateRequest = ShouldIncludeInRequest(property, removeId: true),
            IncludeInUpdateRequest = ShouldIncludeInRequest(property, removeId: false),
            IncludeInResponse = ShouldIncludeInResponse(property)
        };

        if (metadata.IsStoredAsJsonEncrypted)
        {
            metadata.IsStoredAsJson = true;
        }

        return metadata;
    }

    private IReadOnlyList<IPropertySymbol> GetPublicInstanceProperties(INamedTypeSymbol symbol)
    {
        var typeHierarchy = new Stack<INamedTypeSymbol>();
        for (var current = symbol; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            typeHierarchy.Push(current);
        }

        var properties = new List<IPropertySymbol>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        while (typeHierarchy.Count > 0)
        {
            var current = typeHierarchy.Pop();
            foreach (var property in current.GetMembers()
                         .OfType<IPropertySymbol>()
                         .Where(IsSupportedProperty)
                         .OrderBy(GetSourceOrder))
            {
                if (seen.Add(property.Name))
                {
                    properties.Add(property);
                }
            }
        }

        return properties;
    }

    private bool ShouldIncludeInRequest(IPropertySymbol property, bool removeId)
    {
        if (property.Name == "Id")
        {
            return false;
        }

        if (HasAttribute(property, "ExcludeFromApiAttribute") && !HasAttribute(property, "IncludeInApiAttribute"))
        {
            return false;
        }

        if (HasAttribute(property, "NotMappedAttribute") && !HasAttribute(property, "IncludeInApiAttribute"))
        {
            return false;
        }

        if (HasAttribute(property, "JsonIgnoreAttribute"))
        {
            return false;
        }

        if ((property.Type.ToString() ?? string.Empty).Contains("JsonModels", StringComparison.Ordinal))
        {
            return false;
        }

        if (property.Name is "Guid" or "TenantId" or "Tenant" or "Deleted" or "CreatedDate" or "LastUpdatedDate")
        {
            return false;
        }

        if (property.Name.EndsWith("Key", StringComparison.Ordinal)
            || IsCollection(property)
            || IsNavigation(property)
            || (HasAttribute(property, "NotMappedAttribute") && !HasAttribute(property, "IncludeInApiAttribute"))
            || property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is PropertyDeclarationSyntax syntax && syntax.ExpressionBody is not null)
        {
            return false;
        }

        return true;
    }

    private bool ShouldIncludeInResponse(IPropertySymbol property)
    {
        if (HasAttribute(property, "ExcludeFromApiAttribute") && !HasAttribute(property, "IncludeInApiAttribute"))
        {
            return false;
        }

        if (HasAttribute(property, "AdaptIgnoreAttribute"))
        {
            return false;
        }

        if (HasAttribute(property, "JsonIgnoreAttribute"))
        {
            return false;
        }

        if ((property.Type.ToString() ?? string.Empty).Contains("JsonModels", StringComparison.Ordinal))
        {
            return false;
        }

        if (HasAttribute(property, "NotMappedAttribute"))
        {
            if (HasAttribute(property, "IncludeInApiAttribute"))
            {
                return true;
            }

            return property.Type.SpecialType == SpecialType.System_String
                   && property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is PropertyDeclarationSyntax syntax
                   && syntax.ExpressionBody is not null;
        }

        return !IsJoinPropertyCollection(property, joinTableTypes: null);
    }

    private string ToResponseDtoType(ITypeSymbol typeSymbol, bool isNullable, bool allowReferenceNullableSuffix)
    {
        var underlyingType = UnwrapNullable(typeSymbol);

        if (underlyingType is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType.SpecialType == SpecialType.System_Byte
                ? "byte[]"
                : $"{ToResponseDtoType(arrayType.ElementType, isNullable: false, allowReferenceNullableSuffix: false)}[]";
        }

        if (underlyingType is INamedTypeSymbol namedType)
        {
            if (TryConvertResponseCollection(namedType, out var collectionType))
            {
                return collectionType + (allowReferenceNullableSuffix || isNullable ? "?" : string.Empty);
            }

            if (TryConvertResponseDictionary(namedType, out var dictionaryType))
            {
                return dictionaryType + (allowReferenceNullableSuffix || isNullable ? "?" : string.Empty);
            }
        }

        if (underlyingType.TypeKind == TypeKind.Enum)
        {
            var normalizedEnumType = NormalizeDisplayType(underlyingType.ToString());
            return normalizedEnumType + (isNullable ? "?" : string.Empty);
        }

        var namespaceName = underlyingType.ContainingNamespace?.ToString() ?? string.Empty;
        if (IsModelNamespace(namespaceName) && underlyingType is INamedTypeSymbol modelType)
        {
            var dtoTypeName = $"App.Dto.{modelType.Name.Pluralize()}.Responses.{modelType.Name}Response";
            return dtoTypeName + (allowReferenceNullableSuffix || isNullable ? "?" : string.Empty);
        }

        var normalized = NormalizeDisplayType(underlyingType.ToString());
        if (normalized.EndsWith("?", StringComparison.Ordinal))
        {
            normalized = normalized[..^1];
        }

        return normalized switch
        {
            "string" => isNullable ? "string?" : "string",
            "int" or "long" or "short" or "bool" or "double" or "decimal" or "float" or "Guid" or "DateTime" or "DateTimeOffset"
                => normalized + (isNullable ? "?" : string.Empty),
            _ => normalized + (isNullable ? "?" : string.Empty)
        };
    }

    private bool TryConvertResponseCollection(INamedTypeSymbol namedType, out string collectionType)
    {
        collectionType = string.Empty;
        var originalDefinition = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (originalDefinition is not (
                "global::System.Collections.Generic.List<T>"
                or "global::System.Collections.Generic.IList<T>"
                or "global::System.Collections.Generic.ICollection<T>"
                or "global::System.Collections.Generic.IEnumerable<T>"
                or "global::System.Collections.Generic.IReadOnlyList<T>"
                or "global::System.Collections.Generic.IReadOnlyCollection<T>"
                or "global::System.Collections.Generic.HashSet<T>"))
        {
            return false;
        }

        collectionType = $"List<{ToResponseDtoType(namedType.TypeArguments[0], isNullable: false, allowReferenceNullableSuffix: false)}>";
        return true;
    }

    private bool TryConvertResponseDictionary(INamedTypeSymbol namedType, out string dictionaryType)
    {
        dictionaryType = string.Empty;
        var originalDefinition = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (originalDefinition is not (
                "global::System.Collections.Generic.Dictionary<TKey, TValue>"
                or "global::System.Collections.Generic.IDictionary<TKey, TValue>"
                or "global::System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>"))
        {
            return false;
        }

        dictionaryType = $"Dictionary<string, {ToResponseDtoType(namedType.TypeArguments[1], isNullable: false, allowReferenceNullableSuffix: false)}>";
        return true;
    }

    private static bool IsJoinPropertyCollection(IPropertySymbol property, ISet<string>? joinTableTypes)
    {
        if (joinTableTypes is null || !IsCollection(property) || !TryGetCollectionElementTypeName(property.Type, out var elementTypeName))
        {
            return false;
        }

        return joinTableTypes.Contains(elementTypeName);
    }

    private static string ToDtoType(string rawType, bool isNullable, bool makeNullable)
    {
        var normalized = NormalizeDisplayType(rawType);
        if (normalized.EndsWith("?", StringComparison.Ordinal))
        {
            return normalized;
        }

        var needsNullableSuffix = makeNullable || isNullable;
        return normalized switch
        {
            "string" => needsNullableSuffix ? "string?" : "string",
            "int" or "long" or "short" or "bool" or "double" or "decimal" or "float" or "Guid" or "DateTime" or "DateTimeOffset"
                => normalized + (needsNullableSuffix ? "?" : string.Empty),
            _ => normalized + (needsNullableSuffix ? "?" : string.Empty)
        };
    }

    private static string GetResponseInitializer(string rawType, bool isNullable) =>
        NormalizeDisplayType(rawType) == "string" && !isNullable ? " = \"\";" : string.Empty;

    private static string NormalizeDisplayType(string rawType)
    {
        return rawType switch
        {
            "System.String" => "string",
            "System.Guid" => "Guid",
            "System.DateTime" => "DateTime",
            "System.DateTimeOffset" => "DateTimeOffset",
            _ => rawType
        };
    }

    private static string GetDefaultValue(ITypeSymbol typeSymbol, bool isNullable)
    {
        if (isNullable)
        {
            return "null";
        }

        return typeSymbol.SpecialType switch
        {
            SpecialType.System_Boolean => "false",
            SpecialType.System_Byte => "0",
            SpecialType.System_SByte => "0",
            SpecialType.System_Int16 => "0",
            SpecialType.System_UInt16 => "0",
            SpecialType.System_Int32 => "0",
            SpecialType.System_UInt32 => "0",
            SpecialType.System_Int64 => "0L",
            SpecialType.System_UInt64 => "0UL",
            SpecialType.System_Single => "0f",
            SpecialType.System_Double => "0d",
            SpecialType.System_Decimal => "0m",
            SpecialType.System_DateTime => "DateTime.MinValue",
            SpecialType.System_String => "\"\"",
            _ => typeSymbol.ToString() switch
            {
                "System.DateTimeOffset" => "DateTimeOffset.MinValue",
                "System.Guid" => "Guid.Empty",
                _ when typeSymbol.TypeKind == TypeKind.Enum => GetDefaultEnumValue(typeSymbol),
                _ => "null"
            }
        };
    }

    private static string GetDefaultEnumValue(ITypeSymbol enumType)
    {
        var member = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(x => x.HasConstantValue);

        return member?.ConstantValue?.ToString() ?? "0";
    }

    private static bool IsForeignKeyIdType(ITypeSymbol type) =>
        type.SpecialType == SpecialType.System_Int32 || IsNullableInt32(type);

    private static bool IsNullableInt32(ITypeSymbol type) =>
        type is INamedTypeSymbol named
        && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        && named.TypeArguments.Length == 1
        && named.TypeArguments[0].SpecialType == SpecialType.System_Int32;

    private static bool IsEnumType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0].TypeKind == TypeKind.Enum;
        }

        return type.TypeKind == TypeKind.Enum;
    }

    private bool IsNavigation(IPropertySymbol property)
    {
        if (property.Type.SpecialType == SpecialType.System_String
            || (property.Type.ToString() ?? string.Empty).Contains("JsonModels", StringComparison.Ordinal))
        {
            return false;
        }

        if (IsCollection(property))
        {
            var elementType = GetCollectionElementType(property.Type);
            return elementType?.TypeKind == TypeKind.Class
                   && IsModelNamespace(elementType.ContainingNamespace?.ToString() ?? string.Empty);
        }

        return property.Type.TypeKind == TypeKind.Class
               && IsModelNamespace(property.Type.ContainingNamespace?.ToString() ?? string.Empty);
    }

    private static bool IsCollection(IPropertySymbol property)
    {
        if (property.Type.SpecialType == SpecialType.System_String
            || property.Type is IArrayTypeSymbol
            || property.Type.ToString() is "byte[]" or "byte[]?")
        {
            return false;
        }

        return property.Type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToString() is "System.Collections.Generic.ICollection<T>"
                or "System.Collections.Generic.IEnumerable<T>"
                or "System.Collections.Generic.IList<T>");
    }

    private static ITypeSymbol? GetCollectionElementType(ITypeSymbol type) =>
        type is INamedTypeSymbol named && named.TypeArguments.Length == 1 ? named.TypeArguments[0] : null;

    private static bool TryGetCollectionElementTypeName(ITypeSymbol type, out string elementTypeName)
    {
        elementTypeName = string.Empty;
        if (type is not INamedTypeSymbol named || named.TypeArguments.Length != 1)
        {
            return false;
        }

        elementTypeName = named.TypeArguments[0].Name;
        return true;
    }

    private static bool IsRequired(IPropertySymbol property) =>
        (property.Type.IsValueType && !(property.Type.ToString() ?? string.Empty).EndsWith("?", StringComparison.Ordinal))
        || (property.Type.IsReferenceType && property.NullableAnnotation != NullableAnnotation.Annotated);

    private static string? GetForeignKeyNameIfAny(IPropertySymbol property, INamedTypeSymbol classSymbol)
    {
        if (!property.Name.EndsWith("Id", StringComparison.Ordinal) || !IsForeignKeyIdType(property.Type))
        {
            return null;
        }

        var relatedEntityName = property.Name[..^2];
        return classSymbol.GetMembers().Any(x => x.Name == relatedEntityName) ? property.Name : null;
    }

    private static bool IsInterfaceProperty(IPropertySymbol property)
    {
        if (property.Type.TypeKind == TypeKind.Interface)
        {
            return true;
        }

        return property.Type is INamedTypeSymbol named
               && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
               && named.TypeArguments.Length == 1
               && named.TypeArguments[0].TypeKind == TypeKind.Interface;
    }

    private static string GetJsonSerializerOptionsName(IPropertySymbol property)
    {
        var attribute = property.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "JsonSerializerOptionsNameAttribute");
        if (attribute?.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is string name)
        {
            return name;
        }

        return "Default";
    }

    private static string GetJsonEncryptedModelName(IPropertySymbol property)
    {
        var attribute = property.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "StoredAsJsonEncryptedAttribute");
        if (attribute?.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is string name)
        {
            return name;
        }

        return string.Empty;
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol namedType
               && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
               && namedType.TypeArguments.Length == 1
            ? namedType.TypeArguments[0]
            : typeSymbol;
    }

    private static bool IsSupportedProperty(IPropertySymbol property)
    {
        return !property.IsStatic
               && !property.IsIndexer
               && property.DeclaredAccessibility == Accessibility.Public
               && property.GetMethod?.DeclaredAccessibility == Accessibility.Public;
    }

    private static int GetSourceOrder(ISymbol symbol)
    {
        return symbol.DeclaringSyntaxReferences
            .Select(reference => reference.Span.Start)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
    }

    private static bool InheritsFrom(INamedTypeSymbol symbol, string baseTypeName)
    {
        for (var current = symbol.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == baseTypeName)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(x => x.AttributeClass?.Name == attributeName);
}
