using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json.Serialization;
using BrighterTools.CodeGenerator.Inspectors;
using BrighterTools.CodeGenerator.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;

public class ModelInspectorTests
{
    [Fact]
    public void ClassMetadata_GetApiProperties_Excludes_JsonIgnore()
    {
        var model = new ClassMetadata
        {
            Properties =
            [
                new PropertyMetadata { Name = "Id", JsonIgnore = false },
                new PropertyMetadata { Name = "PublicValue", JsonIgnore = false },
                new PropertyMetadata { Name = "Secret", JsonIgnore = true }
            ]
        };

        var props = model.GetApiProperties(removeId: false, isUpdateRequest: false);

        Assert.Contains(props, p => p.Name == "PublicValue");
        Assert.DoesNotContain(props, p => p.Name == "Secret");
    }

    [Fact]
    public void ClassMetadata_GetApiProperties_Respects_IncludeInApi_For_NotMapped_On_Create()
    {
        var model = new ClassMetadata
        {
            Properties =
            [
                new PropertyMetadata { Name = "ExternalA", NotMapped = true, IncludeInApi = true },
                new PropertyMetadata { Name = "ExternalB", NotMapped = true, IncludeInApi = false }
            ]
        };

        var props = model.GetApiProperties(removeId: false, isUpdateRequest: false);

        Assert.Contains(props, p => p.Name == "ExternalA");
        Assert.DoesNotContain(props, p => p.Name == "ExternalB");
    }

    [Fact]
    public void ModelInspector_ShouldIncludeInRequest_Allows_NotMapped_When_IncludeInApi_Present()
    {
        var property = CreatePropertySymbol("""
using System.ComponentModel.DataAnnotations.Schema;

namespace TestApp.Data.Models.Helpers { public sealed class IncludeInApiAttribute : System.Attribute {} }
namespace TestApp.Data.Models;

public class SampleModel
{
    [NotMapped]
    [TestApp.Data.Models.Helpers.IncludeInApi]
    public string ExternalValue { get; set; } = string.Empty;
}
""", "TestApp.Data.Models.SampleModel", "ExternalValue");

        var include = InvokeShouldIncludeInRequest(property);

        Assert.True(include);
    }



    [Fact]
    public void ModelInspector_BuildPropertyMetadata_Sets_IsStoredAsJson_From_StoredAsJsonAttribute()
    {
        var property = CreatePropertySymbol("""
namespace TestApp.Data.Models.Helpers
{
    public sealed class StoredAsJsonAttribute : System.Attribute { }
}

namespace TestApp.Data.Models;

public class SampleModel
{
    [TestApp.Data.Models.Helpers.StoredAsJson]
    public string Metadata { get; set; } = string.Empty;
}
""", "TestApp.Data.Models.SampleModel", "Metadata");

        var inspector = new ModelInspector("dummy.csproj", "TestApp.Data.Models");
        var method = typeof(ModelInspector).GetMethod("BuildPropertyMetadata", BindingFlags.Instance | BindingFlags.NonPublic)
                     ?? throw new InvalidOperationException("Could not find BuildPropertyMetadata");

        var classSymbol = CreateTypeSymbol("""
namespace TestApp.Data.Models;
public class SampleModel { public string Metadata { get; set; } = string.Empty; }
""", "TestApp.Data.Models.SampleModel");

        var metadata = (PropertyMetadata)(method.Invoke(inspector, [classSymbol, property, new List<IPropertySymbol> { property }, new HashSet<string>(StringComparer.Ordinal)])
                       ?? throw new InvalidOperationException("BuildPropertyMetadata returned null"));

        Assert.True(metadata.IsStoredAsJson);
    }
    [Fact]
    public void ModelInspector_BuildClassMetadata_Sets_ExcludeFromApi_For_JoinTable()
    {
        var symbol = CreateTypeSymbol("""
namespace TestApp.Data.Models.Helpers
{
    public sealed class IsJoinTableAttribute : System.Attribute { }
}

namespace TestApp.Data.Models;

[TestApp.Data.Models.Helpers.IsJoinTable]
public class SampleJoin
{
    public int LeftId { get; set; }
    public int RightId { get; set; }
}
""", "TestApp.Data.Models.SampleJoin");

        var inspector = new ModelInspector("dummy.csproj", "TestApp.Data.Models");
        var method = typeof(ModelInspector).GetMethod("BuildClassMetadata", BindingFlags.Instance | BindingFlags.NonPublic)
                     ?? throw new InvalidOperationException("Could not find BuildClassMetadata");

        var metadata = (ClassMetadata)(method.Invoke(inspector, [symbol, new HashSet<string>(StringComparer.Ordinal)])
                       ?? throw new InvalidOperationException("BuildClassMetadata returned null"));

        Assert.True(metadata.IsJoinTable);
        Assert.True(metadata.ExcludeFromApi);
    }
    [Fact]
    public void ModelInspector_ShouldIncludeInRequest_Excludes_JsonIgnore()
    {
        var property = CreatePropertySymbol("""
using System.Text.Json.Serialization;

namespace TestApp.Data.Models;

public class SampleModel
{
    [JsonIgnore]
    public string Secret { get; set; } = string.Empty;
}
""", "TestApp.Data.Models.SampleModel", "Secret");

        var include = InvokeShouldIncludeInRequest(property);

        Assert.False(include);
    }

    private static bool InvokeShouldIncludeInRequest(IPropertySymbol property)
    {
        var inspector = new ModelInspector("dummy.csproj", "TestApp.Data.Models");
        var method = typeof(ModelInspector).GetMethod("ShouldIncludeInRequest", BindingFlags.Instance | BindingFlags.NonPublic)
                     ?? throw new InvalidOperationException("Could not find ShouldIncludeInRequest");

        return (bool)(method.Invoke(inspector, [property, false])
                      ?? throw new InvalidOperationException("ShouldIncludeInRequest returned null"));
    }


    private static INamedTypeSymbol CreateTypeSymbol(string source, string typeMetadataName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "TypeTestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetTypeByMetadataName(typeMetadataName)
               ?? throw new InvalidOperationException($"Could not find type: {typeMetadataName}");
    }
    private static IPropertySymbol CreatePropertySymbol(string source, string typeMetadataName, string propertyName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(NotMappedAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JsonIgnoreAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var type = compilation.GetTypeByMetadataName(typeMetadataName)
                   ?? throw new InvalidOperationException($"Could not find type: {typeMetadataName}");

        return type.GetMembers().OfType<IPropertySymbol>().Single(p => p.Name == propertyName);
    }
}