using BrighterTools.CodeGenerator.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Globalization;

namespace BrighterTools.CodeGenerator.Inspectors;

public sealed class EnumInspector(string projectPath, IReadOnlyList<string>? namespacePrefixes = null)
{
    private readonly string _projectPath = projectPath;
    private readonly IReadOnlyList<string> _namespacePrefixes = NormalizePrefixes(namespacePrefixes);

    public async Task<IReadOnlyList<EnumMetadata>> InspectAsync(CancellationToken cancellationToken = default)
    {
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(_projectPath, cancellationToken: cancellationToken);
        var solution = project.Solution;
        var results = new List<EnumMetadata>();
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

                foreach (var enumDeclaration in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
                {
                    if (semanticModel.GetDeclaredSymbol(enumDeclaration, cancellationToken) is not INamedTypeSymbol symbol)
                    {
                        continue;
                    }

                    var @namespace = symbol.ContainingNamespace?.ToString() ?? string.Empty;
                    if (!IsEnumNamespace(@namespace) || !seen.Add(symbol.ToDisplayString()))
                    {
                        continue;
                    }

                    results.Add(new EnumMetadata
                    {
                        Name = symbol.Name,
                        Namespace = @namespace,
                        Members = symbol.GetMembers()
                            .OfType<IFieldSymbol>()
                            .Where(x => x.HasConstantValue)
                            .Select(x => new EnumMemberMetadata
                            {
                                Name = x.Name,
                                StringValue = GetEnumMemberValue(x),
                                Value = x.ConstantValue is int value ? value : null,
                                ValueLiteral = GetEnumValueLiteral(x.ConstantValue)
                            })
                            .ToList()
                    });
                }
            }
        }

        return results.OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
    }

    private bool IsEnumNamespace(string namespaceName)
    {
        return _namespacePrefixes.Any(prefix => namespaceName.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> NormalizePrefixes(IReadOnlyList<string>? namespacePrefixes)
    {
        if (namespacePrefixes is null || namespacePrefixes.Count == 0)
        {
            return ["App.Domain.Enums", "App.Data"];
        }

        return namespacePrefixes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string GetEnumMemberValue(IFieldSymbol fieldSymbol)
    {
        var enumMemberAttribute = fieldSymbol.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.Name == "EnumMemberAttribute");

        if (enumMemberAttribute is null)
        {
            return fieldSymbol.Name;
        }

        var valueArgument = enumMemberAttribute.NamedArguments.FirstOrDefault(x => x.Key == "Value");
        return valueArgument.Value.Value?.ToString() ?? fieldSymbol.Name;
    }

    private static string GetEnumValueLiteral(object? value)
    {
        return value switch
        {
            sbyte numeric => numeric.ToString(CultureInfo.InvariantCulture),
            byte numeric => numeric.ToString(CultureInfo.InvariantCulture),
            short numeric => numeric.ToString(CultureInfo.InvariantCulture),
            ushort numeric => numeric.ToString(CultureInfo.InvariantCulture),
            int numeric => numeric.ToString(CultureInfo.InvariantCulture),
            uint numeric => numeric.ToString(CultureInfo.InvariantCulture),
            long numeric => numeric.ToString(CultureInfo.InvariantCulture),
            ulong numeric => numeric.ToString(CultureInfo.InvariantCulture),
            _ => "0"
        };
    }
}
