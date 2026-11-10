using System.CommandLine;

namespace MediaDater;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = BuildRootCommand();
        ParseResult parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }

    internal static RootCommand BuildRootCommand()
    {
        RootCommand rootCommand = new("MediaDater – rename media files using Persian calendar capture dates");

        Option<bool> dryRunOption = new("--dry-run")
        {
            Description = "Perform a trial run with no changes made"
        };

        rootCommand.Options.Add(dryRunOption);

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            bool dryRun = parseResult.GetValue(dryRunOption);
            await RenameOrchestrator.Run(Environment.CurrentDirectory, dryRun);
        });

        Command renameCommand = new("rename", "Rename files in a directory");

        Argument<DirectoryInfo?> directoryArg = new("directory")
        {
            Description = "Directory to process (defaults to current directory)",
            Arity = ArgumentArity.ZeroOrOne
        };

        renameCommand.Options.Add(dryRunOption);
        renameCommand.Arguments.Add(directoryArg);

        renameCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            DirectoryInfo? dirInfo = parseResult.GetValue(directoryArg);
            string baseDirectory = dirInfo?.FullName ?? Environment.CurrentDirectory;
            bool dryRun = parseResult.GetValue(dryRunOption);
            await RenameOrchestrator.Run(baseDirectory, dryRun);
        });

        rootCommand.Subcommands.Add(renameCommand);

        Command undoCommand = new("undo", "Revert the last rename operation in the current directory");
        undoCommand.SetAction(parseResult => UndoCommand.Run(Environment.CurrentDirectory));
        rootCommand.Subcommands.Add(undoCommand);

        return rootCommand;
    }
}