using System.CommandLine;
using AwesomeAssertions;
using Xunit;

namespace MediaDater.Tests;

public class ProgramTests
{
    [Fact]
    public void Parse_RootWithoutOptions_DefaultsDryRunToFalse()
    {
        ParseResult result = Program.BuildRootCommand().Parse([]);

        result.Errors.Should().BeEmpty();
        result.CommandResult.Command.Should().BeOfType<RootCommand>();
        result.GetValue<bool>("--dry-run").Should().BeFalse();
    }

    [Fact]
    public void Parse_RootWithDryRun_SetsDryRunToTrue()
    {
        ParseResult result = Program.BuildRootCommand().Parse(["--dry-run"]);

        result.Errors.Should().BeEmpty();
        result.CommandResult.Command.Should().BeOfType<RootCommand>();
        result.GetValue<bool>("--dry-run").Should().BeTrue();
    }

    [Fact]
    public void Parse_RenameWithDirectoryAndDryRun_ParsesBoth()
    {
        string dir = Path.GetTempPath();
        ParseResult result = Program.BuildRootCommand().Parse(["rename", dir, "--dry-run"]);

        result.Errors.Should().BeEmpty();
        result.CommandResult.Command.Name.Should().Be("rename");
        result.GetValue<bool>("--dry-run").Should().BeTrue();
        result.GetValue<DirectoryInfo?>("directory")?.FullName.Should().Be(new DirectoryInfo(dir).FullName);
    }

    [Fact]
    public void Parse_DryRunBeforeRenameSubcommand_SetsDryRunToTrue()
    {
        ParseResult result = Program.BuildRootCommand().Parse(["--dry-run", "rename"]);

        result.Errors.Should().BeEmpty();
        result.CommandResult.Command.Name.Should().Be("rename");
        result.GetValue<bool>("--dry-run").Should().BeTrue();
    }

    [Fact]
    public void Parse_UndoWithDryRun_ReportsParseError()
    {
        ParseResult result = Program.BuildRootCommand().Parse(["undo", "--dry-run"]);

        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_Undo_Succeeds()
    {
        ParseResult result = Program.BuildRootCommand().Parse(["undo"]);

        result.Errors.Should().BeEmpty();
        result.CommandResult.Command.Name.Should().Be("undo");
    }
}