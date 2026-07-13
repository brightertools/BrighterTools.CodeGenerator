using BrighterTools.CodeGenerator.CommandLine;
using Xunit;

namespace BrighterTools.CodeGenerator.Tests;

public class CommandLineParserTests
{
    [Fact]
    public void Parse_LegacyGenerateArguments_UsesLegacyMode()
    {
        var options = CommandLineParser.Parse(["--config", "CodeGeneration/codegen.json", "--dry-run"]);

        Assert.Equal(CommandKind.Generate, options.CommandKind);
        Assert.Equal("CodeGeneration/codegen.json", options.ConfigPath);
        Assert.True(options.DryRun);
        Assert.True(options.UsesLegacyGenerateMode);
    }

    [Fact]
    public void Parse_GenerateCommand_UsesExplicitMode()
    {
        var options = CommandLineParser.Parse(["generate", "--config=CodeGeneration/codegen.json"]);

        Assert.Equal(CommandKind.Generate, options.CommandKind);
        Assert.Equal("CodeGeneration/codegen.json", options.ConfigPath);
        Assert.False(options.UsesLegacyGenerateMode);
    }

    [Fact]
    public void Parse_InitCommand_ReadsOptions()
    {
        var options = CommandLineParser.Parse(["init", "--repo-root", ".", "--config-dir", "CodeGeneration", "--force"]);

        Assert.Equal(CommandKind.Init, options.CommandKind);
        Assert.Equal(".", options.RepoRootPath);
        Assert.Equal("CodeGeneration", options.ConfigDirectory);
        Assert.True(options.Force);
    }
}
