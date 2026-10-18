using BrighterTools.CodeGenerator.Generators;
using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.TemplateEngine;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;

public class KeyAndContextCompatibilityTests
{
    private static TemplateRenderer Renderer() => new(new TemplateLoader(Path.Combine(AppContext.BaseDirectory, "Templates")));
    private static GenerationContext Context(string key, string contextName = "ApplicationDbContext", bool registerContext = true)
    {
        var model = new ClassMetadata { Name = "User", PluralName = "Users", Namespace = "App.Data.Models", HasId = true,
            Properties = [new PropertyMetadata { Name = "Id", DisplayTypeName = key }] };
        return new GenerationContext { Options = new GeneratorOptions { DbContextName = contextName, RegisterDbContext = registerContext },
            Models = [model], AllModels = [model], ApiModels = [], Enums = [] };
    }

    [Theory]
    [InlineData("int", "int")]
    [InlineData("long", "long")]
    [InlineData("string", "string")]
    [InlineData("System.Guid", "Guid")]
    public void ScalarKeysArePreservedAcrossRepositoryServiceAndRequests(string input, string expected)
    {
        var context = Context(input);
        var repository = new RepositoryGenerator(Renderer()).Generate(context).Single().Content;
        var service = new ServiceGenerator(Renderer()).Generate(context).Single().Content;
        var request = new DtoRequestGenerator(Renderer()).Generate(context).Single(x => x.RelativePath.EndsWith("UpdateUserRequest.g.cs")).Content;
        Assert.Contains($"GetById({expected} id", repository);
        Assert.Contains($"GetById({expected} id", service);
        Assert.Contains($"public {expected}? Id", request);
        foreach (var code in new[] { repository, service, request })
            Assert.DoesNotContain(CSharpSyntaxTree.ParseText(code).GetDiagnostics(), x => x.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        if (expected == "string")
        {
            Assert.DoesNotContain("request.Id.Value", service);
            Assert.DoesNotContain("request.Id!.Value", service);
            Assert.DoesNotContain("request.Id.HasValue", service);
            Assert.Contains("string.IsNullOrWhiteSpace(request.Id)", service);
        }
        else if (expected == "Guid") Assert.Contains("request.Id.Value == Guid.Empty", service);
        else Assert.Contains("request.Id.Value > 0", service);
    }

    [Fact]
    public void HostCanOwnItsSingleContextRegistration()
    {
        var context = Context("string", "AppDbContext", false);
        var repository = new RepositoryGenerator(Renderer()).Generate(context).Single().Content;
        Assert.Contains("AppDbContext db", repository);
        Assert.DoesNotContain("ApplicationDbContext", repository);
        var registration = new DataServiceRegistrationGenerator(Renderer()).Generate(context).Single().Content;
        Assert.DoesNotContain("services.AddDbContext", registration);
        Assert.Contains("AddScoped<IUserRepository, UserRepository>", registration);
        Assert.Contains("AddScoped<IUserService, UserService>", registration);
    }

    [Fact]
    public void DefaultContextRegistrationRemainsCompatible()
    {
        var registration = new DataServiceRegistrationGenerator(Renderer()).Generate(Context("int")).Single().Content;
        Assert.Contains("AddDbContext<ApplicationDbContext>", registration);
    }

    [Fact]
    public void ContextFileNamesMatchConfiguredClass()
    {
        var files = new DbContextGenerator(Renderer()).Generate(Context("int", "HostDbContext")).ToArray();
        Assert.All(files, file => {
            Assert.Contains("HostDbContext", file.RelativePath);
            Assert.DoesNotContain("ApplicationDbContext", file.Content);
        });
    }

    [Fact]
    public void UnsupportedScalarKeyDoesNotSilentlyBecomeInt()
    {
        Assert.Throws<InvalidOperationException>(() => new RepositoryGenerator(Renderer()).Generate(Context("decimal")).ToArray());
    }
}

