using AwesomeAssertions;
using Xunit;

namespace MediaDater.Tests;

public class RenameOrchestratorTests : IDisposable
{
    private readonly string _tempDir;

    public RenameOrchestratorTests()
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
    public void ExecuteRenames_WhenAllSucceed_RecordsAllRenames()
    {
        string orig1 = Path.Combine(_tempDir, "IMG_1.jpg");
        string new1 = Path.Combine(_tempDir, "1403-02-23_14-30-45.jpg");
        string orig2 = Path.Combine(_tempDir, "IMG_2.jpg");
        string new2 = Path.Combine(_tempDir, "1403-02-23_14-30-46.jpg");
        File.WriteAllText(orig1, "one");
        File.WriteAllText(orig2, "two");

        RenameBatch batch = new(_tempDir, [new(orig1, new1), new(orig2, new2)]);

        (int totalRenamed, List<FileRenameInfo> succeededRenames) = RenameOrchestrator.ExecuteRenames([batch], dryRun: false);

        totalRenamed.Should().Be(2);
        succeededRenames.Should().HaveCount(2);
        succeededRenames[0].Should().Be(new FileRenameInfo(orig1, new1));
        succeededRenames[1].Should().Be(new FileRenameInfo(orig2, new2));
        File.Exists(new1).Should().BeTrue();
        File.Exists(new2).Should().BeTrue();
    }

    [Fact]
    public void ExecuteRenames_WhenOneFails_ExcludesFailedRename()
    {
        string orig1 = Path.Combine(_tempDir, "IMG_1.jpg");
        string new1 = Path.Combine(_tempDir, "1403-02-23_14-30-45.jpg");
        File.WriteAllText(orig1, "one");

        string missingOrig = Path.Combine(_tempDir, "missing.jpg");
        string missingNew = Path.Combine(_tempDir, "1403-02-23_14-30-46.jpg");

        RenameBatch batch = new(_tempDir, [new(orig1, new1), new(missingOrig, missingNew)]);

        (int totalRenamed, List<FileRenameInfo> succeededRenames) = RenameOrchestrator.ExecuteRenames([batch], dryRun: false);

        totalRenamed.Should().Be(1);
        succeededRenames.Should().HaveCount(1);
        succeededRenames[0].Should().Be(new FileRenameInfo(orig1, new1));
        File.Exists(new1).Should().BeTrue();
    }

    [Fact]
    public void ExecuteRenames_WhenDryRun_DoesNotMoveFilesOrRecordRenames()
    {
        string orig1 = Path.Combine(_tempDir, "IMG_1.jpg");
        string new1 = Path.Combine(_tempDir, "1403-02-23_14-30-45.jpg");
        File.WriteAllText(orig1, "one");

        RenameBatch batch = new(_tempDir, [new(orig1, new1)]);

        (int totalRenamed, List<FileRenameInfo> succeededRenames) = RenameOrchestrator.ExecuteRenames([batch], dryRun: true);

        totalRenamed.Should().Be(1);
        succeededRenames.Should().BeEmpty();
        File.Exists(orig1).Should().BeTrue();
        File.Exists(new1).Should().BeFalse();
    }
}