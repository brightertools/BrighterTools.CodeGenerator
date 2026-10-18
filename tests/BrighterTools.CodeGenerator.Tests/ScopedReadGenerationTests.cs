using BrighterTools.CodeGenerator.Generators;
using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.Runtime;
using BrighterTools.CodeGenerator.TemplateEngine;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;
public class ScopedReadGenerationTests
{
    private static GenerationContext Context(string[] keys) => new() {
        Options = new GeneratorOptions { PersistenceMode = "scoped-read", PersistenceKeys = new() { ["Membership"] = keys } },
        Models = [new ClassMetadata { Name = "Membership", PluralName = "Memberships", Namespace = "App.Data.Models", Properties = [
            new PropertyMetadata { Name = "TenantId", DisplayTypeName = "string" }, new PropertyMetadata { Name = "UserId", DisplayTypeName = "string" } ] }],
        AllModels = [], ApiModels = [], Enums = []
    };
    [Fact]
    public void CompositeKeysAreExplicitAndCommandsAreNotGenerated()
    {
        var context = Context(["TenantId", "UserId"]);
        PersistenceModelValidator.Validate(context.Options, context.Models);
        var renderer = new TemplateRenderer(new TemplateLoader(Path.Combine(AppContext.BaseDirectory, "Templates")));
        var repository = new RepositoryGenerator(renderer).Generate(context).Single().Content;
        var service = new ServiceGenerator(renderer).Generate(context).Single().Content;
        Assert.Contains("MembershipKey(string TenantId, string UserId)", repository);
        Assert.Contains("entity.TenantId == key.TenantId && entity.UserId == key.UserId", repository);
        Assert.Contains("IReadScope<Membership>", repository);
        Assert.DoesNotContain("Save(", repository);
        Assert.DoesNotContain("Create(", service);
        Assert.Contains("GetByKeyAsync", service);
    }
    [Theory]
    [InlineData("Missing")]
    [InlineData("TenantId", "TenantId")]
    public void InvalidKeysFailBeforeGeneration(params string[] keys)
    {
        var context = Context(keys);
        Assert.Throws<InvalidOperationException>(() => PersistenceModelValidator.Validate(context.Options, context.Models));
    }
}
