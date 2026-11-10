using System.Text.Json;

namespace MediaDater;

internal static class ManifestManager
{
    private const string ManifestFileName = ".rename_manifest.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Save(string directory, IEnumerable<FileRenameInfo> renames)
    {
        string manifestPath = Path.Combine(directory, ManifestFileName);
        string baseDir = GetFullBaseDirectory(directory);

        List<FileRenameInfo> manifestEntries =
        [
            .. renames.Select(r => new FileRenameInfo(
                ToRelativeManifestPath(baseDir, r.OriginalPath),
                ToRelativeManifestPath(baseDir, r.NewPath)))
        ];

        string json = JsonSerializer.Serialize(manifestEntries, JsonOptions);
        File.WriteAllText(manifestPath, json);
    }

    public static List<FileRenameInfo> Load(string directory)
    {
        string manifestPath = Path.Combine(directory, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            return [];
        }

        string json = File.ReadAllText(manifestPath);
        List<FileRenameInfo>? entries = JsonSerializer.Deserialize<List<FileRenameInfo>>(json);

        if (entries is null || entries.Count == 0)
        {
            return [];
        }

        string baseDir = GetFullBaseDirectory(directory);

        return
        [
            .. entries.Select(e => new FileRenameInfo(
                FromRelativeManifestPath(baseDir, e.OriginalPath),
                FromRelativeManifestPath(baseDir, e.NewPath)))
        ];
    }

    public static void Delete(string directory)
    {
        string manifestPath = Path.Combine(directory, ManifestFileName);

        if (File.Exists(manifestPath))
        {
            File.Delete(manifestPath);
        }
    }

    private static string GetFullBaseDirectory(string directory)
    {
        return string.IsNullOrEmpty(directory)
            ? Environment.CurrentDirectory
            : Path.GetFullPath(directory);
    }

    private static string ToRelativeManifestPath(string baseDir, string filePath)
    {
        return Path.GetRelativePath(baseDir, Path.GetFullPath(filePath))
            .Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string FromRelativeManifestPath(string baseDir, string relativePath)
    {
        string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(baseDir, normalized));
    }
}