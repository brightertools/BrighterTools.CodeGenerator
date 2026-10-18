using BrighterTools.CodeGenerator.Metadata;
using BrighterTools.CodeGenerator.Runtime;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;

public class PersistenceModelValidatorTests
{
    [Fact]
    public void CompositeMembershipCannotSilentlyBecomeIntegerKey()
    {
        var error = Assert.Throws<InvalidOperationException>(() => PersistenceModelValidator.Validate(new GeneratorOptions(),
            [new ClassMetadata { Name = "TenantUser", Properties = [new PropertyMetadata { Name = "TenantId" }, new PropertyMetadata { Name = "UserId" }] }]));
        Assert.Contains("TenantUser", error.Message);
    }
    [Fact]
    public void ScalarStringIdIsAccepted()
    {
        PersistenceModelValidator.Validate(new GeneratorOptions(), [new ClassMetadata { Name = "Tenant", Properties = [new PropertyMetadata { Name = "Id", DisplayTypeName = "string" }] }]);
    }
    [Fact]
    public void NonPersistenceGenerationCanDescribeCompositeModels()
    {
        PersistenceModelValidator.Validate(new GeneratorOptions { EnabledGenerators = ["typescript-models"] }, [new ClassMetadata { Name = "TenantUser" }]);
    }
}
