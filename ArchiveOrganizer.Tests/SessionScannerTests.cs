namespace ArchiveOrganizer.Tests;

using ArchiveOrganizer.Core.Models;
using ArchiveOrganizer.Core.Services;

public class SessionScannerTests : IDisposable
{
    private readonly string _testFolder;

    public SessionScannerTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), "SessionScannerTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
        {
            Directory.Delete(_testFolder, recursive: true);
        }
    }

    [Fact]
    public void Scan_EmptyFolder_ReturnsEmptyList()
    {
        var result = SessionScanner.Scan(_testFolder);

        Assert.Empty(result);
    }

    [Fact]
    public void Scan_NullPath_ReturnsEmptyList()
    {
        var result = SessionScanner.Scan(null!);

        Assert.Empty(result);
    }

    [Fact]
    public void Scan_NonExistentFolder_ReturnsEmptyList()
    {
        var result = SessionScanner.Scan("/non/existent/path");

        Assert.Empty(result);
    }

    [Fact]
    public void Scan_MasterFileWithCos_ReturnsCompleteItem()
    {
        // Arrange
        var masterFile = "img_123x(1)_240115.CR3";
        CreateMasterFile(masterFile);
        CreateCosFile(masterFile);

        // Act
        var result = SessionScanner.Scan(_testFolder);

        // Assert
        Assert.Single(result);
        var item = result[0];
        Assert.Equal("123x", item.InventoryId);
        Assert.Equal(ItemStatus.Complete, item.Status);
        Assert.NotNull(item.CosFilePath);
    }

    [Fact]
    public void Scan_MasterFileWithoutCos_ReturnsMissingCosItem()
    {
        // Arrange
        var masterFile = "img_456(2)_240115.NEF";
        CreateMasterFile(masterFile);

        // Act
        var result = SessionScanner.Scan(_testFolder);

        // Assert
        Assert.Single(result);
        var item = result[0];
        Assert.Equal("456", item.InventoryId);
        Assert.Equal(ItemStatus.MissingCos, item.Status);
        Assert.Null(item.CosFilePath);
    }

    [Fact]
    public void Scan_MultipleFilesWithSameInventoryId_ReturnsMultipleItems()
    {
        // Arrange
        CreateMasterFile("img_123x(1)_240115.CR3");
        CreateMasterFile("img_123x(2)_240115.CR3");
        CreateCosFile("img_123x(1)_240115.CR3");
        CreateCosFile("img_123x(2)_240115.CR3");

        // Act
        var result = SessionScanner.Scan(_testFolder);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal("123x", item.InventoryId));
    }

    [Fact]
    public void Scan_FilesInSubdirectory_FindsFiles()
    {
        // Arrange
        var subdir = Path.Combine(_testFolder, "Session1");
        Directory.CreateDirectory(subdir);

        var masterFile = "img_789(1)_240115.ARW";
        CreateMasterFile(masterFile, subdir);
        CreateCosFile(masterFile, subdir);

        // Act
        var result = SessionScanner.Scan(_testFolder);

        // Assert
        Assert.Single(result);
        Assert.Equal("789", result[0].InventoryId);
        Assert.Equal(ItemStatus.Complete, result[0].Status);
    }

    [Fact]
    public void Scan_SkipsNonMatchingFiles()
    {
        // Arrange
        CreateMasterFile("img_123x(1)_240115.CR3");
        File.WriteAllText(Path.Combine(_testFolder, "random_photo.jpg"), "");
        File.WriteAllText(Path.Combine(_testFolder, "document.pdf"), "");

        // Act
        var result = SessionScanner.Scan(_testFolder);

        // Assert
        Assert.Single(result);
        Assert.Equal("123x", result[0].InventoryId);
    }

    [Fact]
    public void Scan_AllSupportedExtensions_FindsAll()
    {
        // Arrange
        CreateMasterFile("img_a(1)_240115.NEF");
        CreateMasterFile("img_b(1)_240115.CR3");
        CreateMasterFile("img_c(1)_240115.ARW");
        CreateMasterFile("img_d(1)_240115.RAF");
        CreateMasterFile("img_e(1)_240115.DNG");
        CreateMasterFile("img_f(1)_240115.TIF");
        CreateMasterFile("img_g(1)_240115.TIFF");

        // Act
        var result = SessionScanner.Scan(_testFolder);

        // Assert
        Assert.Equal(7, result.Count);
    }

    [Fact]
    public void Scan_ReturnsCorrectPaths()
    {
        // Arrange
        var masterFile = "img_test(1)_240115.CR3";
        CreateMasterFile(masterFile);
        CreateCosFile(masterFile);

        // Act
        var result = SessionScanner.Scan(_testFolder);

        // Assert
        Assert.Single(result);
        var item = result[0];
        Assert.Equal(Path.Combine(_testFolder, masterFile), item.MasterFilePath);
        Assert.Equal(
            Path.Combine(_testFolder, "CaptureOne", "Settings", "img_test(1)_240115.cos"),
            item.CosFilePath);
    }

    [Fact]
    public void Scan_ParsedNameIsPopulated()
    {
        // Arrange
        CreateMasterFile("img_inventory_id(3)_231201.NEF");

        // Act
        var result = SessionScanner.Scan(_testFolder);

        // Assert
        Assert.Single(result);
        var parsed = result[0].ParsedName;
        Assert.Equal("inventory_id", parsed.InventoryId);
        Assert.Equal(3, parsed.Counter);
        Assert.Equal("231201", parsed.Date);
        Assert.Equal("NEF", parsed.Extension);
    }

    private void CreateMasterFile(string fileName, string? directory = null)
    {
        var dir = directory ?? _testFolder;
        File.WriteAllText(Path.Combine(dir, fileName), "");
    }

    private void CreateCosFile(string masterFileName, string? directory = null)
    {
        var dir = directory ?? _testFolder;
        var settingsDir = Path.Combine(dir, "CaptureOne", "Settings");
        Directory.CreateDirectory(settingsDir);

        var baseName = Path.GetFileNameWithoutExtension(masterFileName);
        var cosFileName = baseName + ".cos";
        File.WriteAllText(Path.Combine(settingsDir, cosFileName), "");
    }
}
