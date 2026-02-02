namespace ArchiveOrganizer.Tests;

using ArchiveOrganizer.Core.Models;
using ArchiveOrganizer.Core.Services;

public class FileOrganizerTests : IDisposable
{
    private const string TestPrefix = "img";
    private readonly string _sourceFolder;
    private readonly string _destFolder;

    public FileOrganizerTests()
    {
        var testId = Guid.NewGuid().ToString();
        _sourceFolder = Path.Combine(Path.GetTempPath(), "FileOrganizerTests_Source_" + testId);
        _destFolder = Path.Combine(Path.GetTempPath(), "FileOrganizerTests_Dest_" + testId);
        Directory.CreateDirectory(_sourceFolder);
        Directory.CreateDirectory(_destFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceFolder))
        {
            Directory.Delete(_sourceFolder, recursive: true);
        }
        if (Directory.Exists(_destFolder))
        {
            Directory.Delete(_destFolder, recursive: true);
        }
    }

    [Fact]
    public void Copy_SingleItemWithCos_CreatesCorrectStructure()
    {
        // Arrange
        var item = CreateTestItem("123x", withCos: true);

        // Act
        var results = FileOrganizer.Copy([item], _destFolder, TestPrefix);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].Success);

        // Check structure - folder name is prefix_inventoryId
        Assert.True(Directory.Exists(Path.Combine(_destFolder, "img_123x")));
        Assert.True(Directory.Exists(Path.Combine(_destFolder, "img_123x", "CaptureOne", "Settings153")));
        Assert.True(File.Exists(Path.Combine(_destFolder, "img_123x", "img_123x(1)_240115.CR3")));
        Assert.True(File.Exists(Path.Combine(_destFolder, "img_123x", "CaptureOne", "Settings153", "img_123x(1)_240115.cos")));

        // Source still exists (copy)
        Assert.True(File.Exists(item.MasterFilePath));
    }

    [Fact]
    public void Copy_ItemWithoutCos_CopiesOnlyMaster()
    {
        // Arrange
        var item = CreateTestItem("456", withCos: false);

        // Act
        var results = FileOrganizer.Copy([item], _destFolder, TestPrefix);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].Success);
        Assert.True(File.Exists(Path.Combine(_destFolder, "img_456", "img_456(1)_240115.CR3")));
        Assert.False(File.Exists(Path.Combine(_destFolder, "img_456", "CaptureOne", "Settings153", "img_456(1)_240115.cos")));
    }

    [Fact]
    public void Move_SingleItem_RemovesSource()
    {
        // Arrange
        var item = CreateTestItem("789", withCos: true);
        var sourceMasterPath = item.MasterFilePath;
        var sourceCosPath = item.CosFilePath;

        // Act
        var results = FileOrganizer.Move([item], _destFolder, TestPrefix);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].Success);

        // Destination exists
        Assert.True(File.Exists(Path.Combine(_destFolder, "img_789", "img_789(1)_240115.CR3")));

        // Source removed
        Assert.False(File.Exists(sourceMasterPath));
        Assert.False(File.Exists(sourceCosPath));
    }

    [Fact]
    public void Copy_UnselectedItem_IsSkipped()
    {
        // Arrange
        var item = CreateTestItem("skipped", withCos: true);
        item.IsSelected = false;

        // Act
        var results = FileOrganizer.Copy([item], _destFolder, TestPrefix);

        // Assert
        Assert.Empty(results);
        Assert.False(Directory.Exists(Path.Combine(_destFolder, "img_skipped")));
    }

    [Fact]
    public void Copy_MultipleItems_CreatesMultipleFolders()
    {
        // Arrange
        var item1 = CreateTestItem("aaa", withCos: true);
        var item2 = CreateTestItem("bbb", withCos: false);

        // Act
        var results = FileOrganizer.Copy([item1, item2], _destFolder, TestPrefix);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        Assert.True(Directory.Exists(Path.Combine(_destFolder, "img_aaa")));
        Assert.True(Directory.Exists(Path.Combine(_destFolder, "img_bbb")));
    }

    [Fact]
    public void Copy_SameInventoryId_BothFilesInSameFolder()
    {
        // Arrange
        var item1 = CreateTestItem("same_id", withCos: true, counter: 1);
        var item2 = CreateTestItem("same_id", withCos: true, counter: 2);

        // Act
        var results = FileOrganizer.Copy([item1, item2], _destFolder, TestPrefix);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));

        var folder = Path.Combine(_destFolder, "img_same_id");
        Assert.True(File.Exists(Path.Combine(folder, "img_same_id(1)_240115.CR3")));
        Assert.True(File.Exists(Path.Combine(folder, "img_same_id(2)_240115.CR3")));
    }

    [Fact]
    public void Copy_ResultContainsDestinationPaths()
    {
        // Arrange
        var item = CreateTestItem("test", withCos: true);

        // Act
        var results = FileOrganizer.Copy([item], _destFolder, TestPrefix);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.Equal(Path.Combine(_destFolder, "img_test", "img_test(1)_240115.CR3"), result.DestinationMasterPath);
        Assert.Equal(Path.Combine(_destFolder, "img_test", "CaptureOne", "Settings153", "img_test(1)_240115.cos"), result.DestinationCosPath);
    }

    [Fact]
    public void Copy_FileAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var item = CreateTestItem("exists", withCos: true);

        // Create destination file first
        var destFolder = Path.Combine(_destFolder, "img_exists");
        Directory.CreateDirectory(destFolder);
        File.WriteAllText(Path.Combine(destFolder, "img_exists(1)_240115.CR3"), "existing");

        // Act
        var results = FileOrganizer.Copy([item], _destFolder, TestPrefix);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].Success);
        Assert.NotNull(results[0].ErrorMessage);
    }

    private ArchiveItem CreateTestItem(string inventoryId, bool withCos, int counter = 1)
    {
        var fileName = $"img_{inventoryId}({counter})_240115.CR3";
        var masterPath = Path.Combine(_sourceFolder, fileName);
        File.WriteAllText(masterPath, "master content");

        string? cosPath = null;
        if (withCos)
        {
            var settingsDir = Path.Combine(_sourceFolder, "CaptureOne", "Settings");
            Directory.CreateDirectory(settingsDir);
            var cosFileName = $"img_{inventoryId}({counter})_240115.cos";
            cosPath = Path.Combine(settingsDir, cosFileName);
            File.WriteAllText(cosPath, "cos content");
        }

        return new ArchiveItem
        {
            InventoryId = inventoryId,
            MasterFilePath = masterPath,
            CosFilePath = cosPath,
            ParsedName = new ParsedFileName
            {
                InventoryId = inventoryId,
                Counter = counter,
                Date = "240115",
                Extension = "CR3",
                OriginalFileName = fileName
            }
        };
    }
}
