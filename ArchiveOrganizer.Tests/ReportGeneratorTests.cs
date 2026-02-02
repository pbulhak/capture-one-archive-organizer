namespace ArchiveOrganizer.Tests;

using ArchiveOrganizer.Core.Models;
using ArchiveOrganizer.Core.Services;

public class ReportGeneratorTests : IDisposable
{
    private readonly string _testFolder;

    public ReportGeneratorTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), "ReportGeneratorTests_" + Guid.NewGuid());
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
    public void GenerateCsv_EmptyResults_CreatesFileWithHeaderOnly()
    {
        // Arrange
        var outputPath = Path.Combine(_testFolder, "empty.csv");

        // Act
        ReportGenerator.GenerateCsv([], outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var content = File.ReadAllText(outputPath);
        Assert.Contains("InventoryId,SourceMaster,SourceCos,DestMaster,DestCos,Status,Error", content);
    }

    [Fact]
    public void GenerateCsv_SuccessfulResult_WritesOkStatus()
    {
        // Arrange
        var outputPath = Path.Combine(_testFolder, "success.csv");
        var item = CreateTestItem("123x");
        var result = OperationResult.Ok(item, "/dest/master.CR3", "/dest/cos.cos");

        // Act
        ReportGenerator.GenerateCsv([result], outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        Assert.Contains("123x", content);
        Assert.Contains("OK", content);
    }

    [Fact]
    public void GenerateCsv_FailedResult_WritesFailedStatusAndError()
    {
        // Arrange
        var outputPath = Path.Combine(_testFolder, "failed.csv");
        var item = CreateTestItem("456");
        var result = OperationResult.Fail(item, "File not found");

        // Act
        ReportGenerator.GenerateCsv([result], outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        Assert.Contains("FAILED", content);
        Assert.Contains("File not found", content);
    }

    [Fact]
    public void GenerateCsvContent_ReturnsStringWithHeader()
    {
        // Arrange
        var item = CreateTestItem("test");
        var result = OperationResult.Ok(item, "/dest/m.CR3", null);

        // Act
        var content = ReportGenerator.GenerateCsvContent([result]);

        // Assert
        Assert.StartsWith("InventoryId,", content);
        Assert.Contains("test", content);
    }

    [Fact]
    public void GenerateCsv_ValueWithComma_IsEscaped()
    {
        // Arrange
        var outputPath = Path.Combine(_testFolder, "comma.csv");
        var item = CreateTestItem("test");
        var result = OperationResult.Fail(item, "Error, with comma");

        // Act
        ReportGenerator.GenerateCsv([result], outputPath);

        // Assert
        var content = File.ReadAllText(outputPath);
        Assert.Contains("\"Error, with comma\"", content);
    }

    private static ArchiveItem CreateTestItem(string inventoryId)
    {
        return new ArchiveItem
        {
            InventoryId = inventoryId,
            MasterFilePath = $"/source/{inventoryId}.CR3",
            CosFilePath = $"/source/{inventoryId}.cos",
            ParsedName = new ParsedFileName
            {
                InventoryId = inventoryId,
                Counter = 1,
                Date = "240115",
                Extension = "CR3",
                OriginalFileName = $"img_{inventoryId}(1)_240115.CR3"
            }
        };
    }
}
