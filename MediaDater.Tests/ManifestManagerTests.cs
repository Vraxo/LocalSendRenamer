using AwesomeAssertions;
using Xunit;

namespace MediaDater.Tests;

public class ManifestManagerTests : IDisposable
{
    private readonly string _tempDir;

    public ManifestManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Save_ShouldCreateManifestFileWithRelativePaths()
    {
        string oldRoot = Path.Combine(_tempDir, "old1.jpg");
        string newRoot = Path.Combine(_tempDir, "new1.jpg");
        string oldNested = Path.Combine(_tempDir, "Sub", "old2.mp4");
        string newNested = Path.Combine(_tempDir, "Sub", "new2.mp4");

        List<FileRenameInfo> renames =
        [
            new(oldRoot, newRoot),
            new(oldNested, newNested)
        ];

        ManifestManager.Save(_tempDir, renames);

        string manifestPath = Path.Combine(_tempDir, ".rename_manifest.json");
        File.Exists(manifestPath).Should().BeTrue();

        string content = File.ReadAllText(manifestPath);
        content.Should().NotContain(Path.GetFileName(_tempDir));
        content.Should().Contain("\"OriginalPath\": \"old1.jpg\"");
        content.Should().Contain("\"NewPath\": \"new1.jpg\"");
        content.Should().Contain("\"OriginalPath\": \"Sub/old2.mp4\"");
        content.Should().Contain("\"NewPath\": \"Sub/new2.mp4\"");
    }

    [Fact]
    public void Load_WhenManifestExists_ResolvesRelativeEntries()
    {
        List<FileRenameInfo> relativeEntries =
        [
            new("old1.jpg", "new1.jpg"),
            new("Sub/old2.mp4", "Sub/new2.mp4")
        ];
        string manifestPath = Path.Combine(_tempDir, ".rename_manifest.json");
        string json = System.Text.Json.JsonSerializer.Serialize(relativeEntries);
        File.WriteAllText(manifestPath, json);

        List<FileRenameInfo> loaded = ManifestManager.Load(_tempDir);

        loaded.Should().HaveCount(2);
        loaded[0].OriginalPath.Should().Be(Path.Combine(_tempDir, "old1.jpg"));
        loaded[0].NewPath.Should().Be(Path.Combine(_tempDir, "new1.jpg"));
        loaded[1].OriginalPath.Should().Be(Path.Combine(_tempDir, "Sub", "old2.mp4"));
        loaded[1].NewPath.Should().Be(Path.Combine(_tempDir, "Sub", "new2.mp4"));
    }

    [Fact]
    public void Load_WhenNoManifest_ReturnsEmptyList()
    {
        List<FileRenameInfo> loaded = ManifestManager.Load(_tempDir);
        loaded.Should().BeEmpty();
    }

    [Fact]
    public void Delete_WhenManifestExists_RemovesManifestFile()
    {
        List<FileRenameInfo> renames =
        [
            new(Path.Combine(_tempDir, "old.jpg"), Path.Combine(_tempDir, "new.jpg"))
        ];
        ManifestManager.Save(_tempDir, renames);
        string manifestPath = Path.Combine(_tempDir, ".rename_manifest.json");
        File.Exists(manifestPath).Should().BeTrue();

        ManifestManager.Delete(_tempDir);
        File.Exists(manifestPath).Should().BeFalse();
    }

    [Fact]
    public void Save_WhenDirectoryHasTrailingSeparator_StoresRelativePaths()
    {
        string dirWithSeparator = _tempDir + Path.DirectorySeparatorChar;
        string oldPath = Path.Combine(_tempDir, "Sub", "old.jpg");
        string newPath = Path.Combine(_tempDir, "Sub", "new.jpg");

        ManifestManager.Save(dirWithSeparator, [new(oldPath, newPath)]);

        string content = File.ReadAllText(Path.Combine(_tempDir, ".rename_manifest.json"));
        content.Should().Contain("\"OriginalPath\": \"Sub/old.jpg\"");
        content.Should().Contain("\"NewPath\": \"Sub/new.jpg\"");
    }

    [Fact]
    public void Load_WhenDirectoryMoved_ResolvesRelativePathsAgainstNewLocation()
    {
        string originalDir = Path.Combine(_tempDir, "OriginalFolder");
        string movedDir = Path.Combine(_tempDir, "MovedFolder");
        Directory.CreateDirectory(Path.Combine(originalDir, "Trip"));

        string oldPath = Path.Combine(originalDir, "Trip", "IMG_001.jpg");
        string newPath = Path.Combine(originalDir, "Trip", "1403-02-23_14-30-45.jpg");
        ManifestManager.Save(originalDir, [new(oldPath, newPath)]);

        Directory.Move(originalDir, movedDir);

        List<FileRenameInfo> loaded = ManifestManager.Load(movedDir);

        loaded.Should().HaveCount(1);
        loaded[0].OriginalPath.Should().Be(Path.Combine(movedDir, "Trip", "IMG_001.jpg"));
        loaded[0].NewPath.Should().Be(Path.Combine(movedDir, "Trip", "1403-02-23_14-30-45.jpg"));
    }

    [Fact]
    public void Delete_WhenNoManifest_DoesNothing()
    {
        Action act = () => ManifestManager.Delete(_tempDir);
        act.Should().NotThrow();
    }
}