using BrighterTools.CodeGenerator.Metadata;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace BrighterTools.CodeGenerator.Inspectors;

public sealed class ApiModelInspector(
    string projectPath,
    IReadOnlyList<string> namespacePrefixes,
    IReadOnlyList<string> enumNamespacePrefixes,
    bool generatedOnly)
{
    private readonly string _projectPath = projectPath;
    private readonly IReadOnlyList<string> _namespacePrefixes = NormalizePrefixes(namespacePrefixes);
    private readonly IReadOnlyList<string> _enumNamespacePrefixes = NormalizePrefixes(enumNamespacePrefixes);
    private readonly bool _generatedOnly = generatedOnly;

    public async Task<IReadOnlyList<ApiModelMetadata>> InspectAsync(CancellationToken cancellationToken = default)
    {
        if (_namespacePrefixes.Count == 0)
        {
            return [];
        }

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(_projectPath, cancellationToken: cancellationToken);
        var solution = project.Solution;
        var results = new List<ApiModelMetadata>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidateProject in solution.Projects)
        {
            var compilation = await candidateProject.GetCompilationAsync(cancellationToken);
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
                    if (!IsApiModelNamespace(namespaceName) || symbol.IsAbstract || !seen.Add(symbol.ToDisplayString()))
                    {
                        continue;
                    }

                    if (_generatedOnly && !IsGenerated(symbol, syntaxTree.FilePath))
                    {
                        continue;
                    }

                    results.Add(BuildMetadata(symbol));
                }
            }
        }

        return results.OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
    }

    private ApiModelMetadata BuildMetadata(INamedTypeSymbol symbol)
    {
        var properties = GetPublicInstanceProperties(symbol)
            .Select(property => new ApiModelPropertyMetadata
            {
                Name = property.Name,
                CamelCaseName = property.Name.Camelize(),
                TypeScriptType = ConvertTypeToTypeScript(property.Type),
                IsOptional = IsOptional(property)
            })
            .ToList();

        return new ApiModelMetadata
        {
            Name = symbol.Name,
            Namespace = symbol.ContainingNamespace?.ToString() ?? string.Empty,
            Properties = properties
        };
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

    private string ConvertTypeToTypeScript(ITypeSymbol typeSymbol)
    {
        var underlyingType = UnwrapNullable(typeSymbol);

        if (underlyingType is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType.SpecialType == SpecialType.System_Byte
                ? "number[]"
                : $"{ConvertTypeToTypeScript(arrayType.ElementType)}[]";
        }

        if (underlyingType.TypeKind == TypeKind.Enum)
        {
            var enumNamespace = underlyingType.ContainingNamespace?.ToString() ?? string.Empty;
            return IsEnumNamespace(enumNamespace)
                ? $"Enums.{underlyingType.Name}"
                : underlyingType.Name;
        }

        if (underlyingType is INamedTypeSymbol namedType)
        {
            if (TryConvertCollection(namedType, out var collectionType))
            {
                return collectionType;
            }

            if (TryConvertDictionary(namedType, out var dictionaryType))
            {
                return dictionaryType;
            }
        }

        var namespaceName = underlyingType.ContainingNamespace?.ToString() ?? string.Empty;
        if (IsApiModelNamespace(namespaceName))
        {
            return underlyingType.Name;
        }

        return underlyingType.SpecialType switch
        {
            SpecialType.System_String => "string",
            SpecialType.System_Boolean => "boolean",
            SpecialType.System_Byte => "number",
            SpecialType.System_SByte => "number",
            SpecialType.System_Int16 => "number",
            SpecialType.System_UInt16 => "number",
            SpecialType.System_Int32 => "number",
            SpecialType.System_UInt32 => "number",
            SpecialType.System_Int64 => "number",
            SpecialType.System_UInt64 => "number",
            SpecialType.System_Single => "number",
            SpecialType.System_Double => "number",
            SpecialType.System_Decimal => "number",
            SpecialType.System_Object => "any",
            _ => ConvertNamedTypeToTypeScript(underlyingType)
        };
    }

    private string ConvertNamedTypeToTypeScript(ITypeSymbol typeSymbol)
    {
        var fullyQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return fullyQualifiedName switch
        {
            "global::System.Guid" => "string",
            "global::System.DateTime" => "string",
            "global::System.DateTimeOffset" => "string",
            "global::System.DateOnly" => "string",
            "global::System.TimeOnly" => "string",
            "global::System.TimeSpan" => "string",
            "global::System.Text.Json.JsonDocument" => "any",
            "global::System.Text.Json.JsonElement" => "any",
            "global::Newtonsoft.Json.Linq.JObject" => "any",
            "global::Newtonsoft.Json.Linq.JToken" => "any",
            _ => typeSymbol.Name
        };
    }

    private bool TryConvertCollection(INamedTypeSymbol namedType, out string collectionType)
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

        collectionType = $"{ConvertTypeToTypeScript(namedType.TypeArguments[0])}[]";
        return true;
    }

    private bool TryConvertDictionary(INamedTypeSymbol namedType, out string dictionaryType)
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

        dictionaryType = $"{{ [key: string]: {ConvertTypeToTypeScript(namedType.TypeArguments[1])} }}";
        return true;
    }

    private bool IsApiModelNamespace(string namespaceName)
    {
        return _namespacePrefixes.Any(prefix => namespaceName.StartsWith(prefix, StringComparison.Ordinal));
    }

    private bool IsEnumNamespace(string namespaceName)
    {
        return _enumNamespacePrefixes.Any(prefix => namespaceName.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static bool IsGenerated(ISymbol symbol, string filePath)
    {
        return filePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
               || symbol.GetAttributes().Any(x => x.AttributeClass?.Name == "GeneratedCodeAttribute");
    }

    private static bool IsOptional(IPropertySymbol property)
    {
        return property.NullableAnnotation == NullableAnnotation.Annotated
               || IsNullableValueType(property.Type);
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol namedType
               && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
               && namedType.TypeArguments.Length == 1
            ? namedType.TypeArguments[0]
            : typeSymbol;
    }

    private static bool IsNullableValueType(ITypeSymbol typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol namedType
               && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
               && namedType.TypeArguments.Length == 1;
    }

    private static bool IsSupportedProperty(IPropertySymbol property)
    {
        var excludedFromApi = HasAttribute(property, "ExcludeFromApiAttribute");
        var includedInApi = HasAttribute(property, "IncludeInApiAttribute");

        return !property.IsStatic
               && !property.IsIndexer
               && property.DeclaredAccessibility == Accessibility.Public
               && property.GetMethod?.DeclaredAccessibility == Accessibility.Public
               && property.GetMethod is not null
               && !HasJsonIgnoreAttribute(property)
               && !HasAttribute(property, "AdaptIgnoreAttribute")
               && !(excludedFromApi && !includedInApi)
               && !(HasAttribute(property, "NotMappedAttribute") && !includedInApi);
    }

    private static bool HasJsonIgnoreAttribute(IPropertySymbol property)
    {
        return property.GetAttributes().Any(attribute =>
            string.Equals(attribute.AttributeClass?.Name, "JsonIgnoreAttribute", StringComparison.Ordinal)
            || string.Equals(attribute.AttributeClass?.ToDisplayString(), "System.Text.Json.Serialization.JsonIgnoreAttribute", StringComparison.Ordinal)
            || string.Equals(attribute.AttributeClass?.ToDisplayString(), "Newtonsoft.Json.JsonIgnoreAttribute", StringComparison.Ordinal));
    }

    private static bool HasAttribute(IPropertySymbol property, string attributeName)
    {
        return property.GetAttributes().Any(attribute =>
            string.Equals(attribute.AttributeClass?.Name, attributeName, StringComparison.Ordinal));
    }

    private static int GetSourceOrder(ISymbol symbol)
    {
        return symbol.DeclaringSyntaxReferences
            .Select(reference => reference.Span.Start)
            .DefaultIfEmpty(int.MaxValue)
            .Min();
    }

    private static IReadOnlyList<string> NormalizePrefixes(IReadOnlyList<string> prefixes)
    {
        return prefixes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
