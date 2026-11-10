using System.Diagnostics;

namespace MediaDater;

internal static class RenameOrchestrator
{
    private static readonly string[] AllowedExtensions =
    [
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp",
        ".gif",
        ".mp4",
        ".jfif",
        ".webp",
        ".heic"
    ];

    public static async Task Run(string baseDirectory, bool dryRun = false)
    {
        if (dryRun)
        {
            ConsoleLogger.Info("DRY RUN MODE – no files will be changed.\n");
        }

        Console.WriteLine($"Processing directory: {baseDirectory}\n");
        Stopwatch stopwatch = Stopwatch.StartNew();

        (List<FileEntry> entries, int skippedCount) = FileScanner.Scan(baseDirectory, AllowedExtensions);
        int totalScanned = skippedCount + entries.Count;

        if (!DateValidator.AreAllPlausible(entries))
        {
            ConsoleLogger.Error("\nAborting: no files were renamed.");
            Console.ReadLine();
            return;
        }

        IEnumerable<RenameBatch> batches = new RenamePlanner(baseDirectory, AllowedExtensions).Plan(entries);
        (int totalRenamed, List<FileRenameInfo> succeededRenames) = ExecuteRenames(batches, dryRun);

        if (!dryRun && succeededRenames.Count > 0)
        {
            ManifestManager.Save(baseDirectory, succeededRenames);
        }

        stopwatch.Stop();
        PrintSummary(totalScanned, skippedCount, totalRenamed, stopwatch.Elapsed, dryRun);
        Console.ReadLine();
    }

    internal static (int TotalRenamed, List<FileRenameInfo> SucceededRenames) ExecuteRenames(IEnumerable<RenameBatch> batches, bool dryRun)
    {
        List<FileRenameInfo> succeededRenames = [];
        int totalRenamed = 0;

        foreach (RenameBatch batch in batches)
        {
            foreach (FileRenameInfo rename in batch.Renames)
            {
                if (dryRun)
                {
                    ConsoleLogger.Info($"[DRY RUN] Would rename: {Path.GetFileName(rename.OriginalPath)} -> {Path.GetFileName(rename.NewPath)}");
                    totalRenamed++;
                    continue;
                }

                if (!RenameExecutor.Execute(rename))
                {
                    continue;
                }

                succeededRenames.Add(rename);
                totalRenamed++;
            }
        }

        return (totalRenamed, succeededRenames);
    }

    private static void PrintSummary(int totalScanned, int skippedCount, int totalRenamed, TimeSpan elapsed, bool dryRun)
    {
        if (totalScanned > 0)
        {
            Console.WriteLine();
        }

        ConsoleLogger.LogAligned("Total", totalScanned.ToString());
        ConsoleLogger.LogAligned("Skipped", skippedCount.ToString());
        ConsoleLogger.LogAligned("Renamed", totalRenamed.ToString());
        ConsoleLogger.LogAligned("Time", elapsed.ToString());

        if (!dryRun)
        {
            return;
        }

        ConsoleLogger.Info("\nDry run complete – no files were actually changed.");
    }
}