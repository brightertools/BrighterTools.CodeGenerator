using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;

public class ApiModelInspectorTests
{
    [Fact]
    public void ApiModelInspector_IsSupportedProperty_Excludes_JsonIgnore()
    {
        var property = CreatePropertySymbol("""
using System.Text.Json.Serialization;

namespace TestApp.Dto;

public class SampleDto
{
    [JsonIgnore]
    public string Secret { get; set; } = string.Empty;
}
""", "TestApp.Dto.SampleDto", "Secret");

        var supported = InvokeIsSupportedProperty(property);

        Assert.False(supported);
    }

    [Fact]
    public void ApiModelInspector_IsSupportedProperty_Excludes_ExcludeFromApi_Without_IncludeInApi()
    {
        var property = CreatePropertySymbol("""
namespace TestApp.Data.Models.Helpers
{
    public sealed class ExcludeFromApiAttribute : System.Attribute { }
}

namespace TestApp.Dto;

public class SampleDto
{
    [TestApp.Data.Models.Helpers.ExcludeFromApi]
    public string InternalOnly { get; set; } = string.Empty;
}
""", "TestApp.Dto.SampleDto", "InternalOnly");

        var supported = InvokeIsSupportedProperty(property);

        Assert.False(supported);
    }

    [Fact]
    public void ApiModelInspector_IsSupportedProperty_Allows_ExcludeFromApi_When_IncludeInApi_Present()
    {
        var property = CreatePropertySymbol("""
namespace TestApp.Data.Models.Helpers
{
    public sealed class ExcludeFromApiAttribute : System.Attribute { }
    public sealed class IncludeInApiAttribute : System.Attribute { }
}

namespace TestApp.Dto;

public class SampleDto
{
    [TestApp.Data.Models.Helpers.ExcludeFromApi]
    [TestApp.Data.Models.Helpers.IncludeInApi]
    public string ExternalValue { get; set; } = string.Empty;
}
""", "TestApp.Dto.SampleDto", "ExternalValue");

        var supported = InvokeIsSupportedProperty(property);

        Assert.True(supported);
    }

    [Fact]
    public void ApiModelInspector_IsSupportedProperty_Excludes_NotMapped_Without_IncludeInApi()
    {
        var property = CreatePropertySymbol("""
using System.ComponentModel.DataAnnotations.Schema;

namespace TestApp.Dto;

public class SampleDto
{
    [NotMapped]
    public string Calculated { get; set; } = string.Empty;
}
""", "TestApp.Dto.SampleDto", "Calculated");

        var supported = InvokeIsSupportedProperty(property);

        Assert.False(supported);
    }

    [Fact]
    public void ApiModelInspector_IsSupportedProperty_Allows_NotMapped_When_IncludeInApi_Present()
    {
        var property = CreatePropertySymbol("""
using System.ComponentModel.DataAnnotations.Schema;

namespace TestApp.Data.Models.Helpers
{
    public sealed class IncludeInApiAttribute : System.Attribute { }
}

namespace TestApp.Dto;

public class SampleDto
{
    [NotMapped]
    [TestApp.Data.Models.Helpers.IncludeInApi]
    public string Calculated { get; set; } = string.Empty;
}
""", "TestApp.Dto.SampleDto", "Calculated");

        var supported = InvokeIsSupportedProperty(property);

        Assert.True(supported);
    }

    private static bool InvokeIsSupportedProperty(IPropertySymbol property)
    {
        var method = typeof(BrighterTools.CodeGenerator.Inspectors.ApiModelInspector)
            .GetMethod("IsSupportedProperty", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Could not find IsSupportedProperty");

        return (bool)(method.Invoke(null, [property])
            ?? throw new InvalidOperationException("IsSupportedProperty returned null"));
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
            assemblyName: "ApiModelInspectorTestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var type = compilation.GetTypeByMetadataName(typeMetadataName)
                   ?? throw new InvalidOperationException($"Could not find type: {typeMetadataName}");

        return type.GetMembers().OfType<IPropertySymbol>().Single(p => p.Name == propertyName);
    }
}
