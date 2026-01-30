namespace ArchiveOrganizer.Tests;

using ArchiveOrganizer.Core.Services;

public class FileNameParserTests
{
    [Theory]
    [InlineData("img_123x(1)_240115.CR3", "123x")]
    [InlineData("img_123(1)_240115.NEF", "123")]
    [InlineData("img_124_1-5x(2)_240115.ARW", "124_1-5x")]
    [InlineData("img_124_1x(1)_240115.RAF", "124_1x")]
    [InlineData("img_124_5x(1)_240115.DNG", "124_5x")]
    [InlineData("img_a_123(1)_240115.TIF", "a_123")]
    [InlineData("img_123_c(3)_240115.TIFF", "123_c")]
    public void TryParse_ValidFileName_ReturnsCorrectInventoryId(string fileName, string expectedId)
    {
        var result = FileNameParser.TryParse(fileName);

        Assert.NotNull(result);
        Assert.Equal(expectedId, result.InventoryId);
    }

    [Theory]
    [InlineData("img_123x(1)_240115.CR3", 1)]
    [InlineData("img_124_1-5x(2)_240115.ARW", 2)]
    [InlineData("img_123_c(3)_240115.TIFF", 3)]
    public void TryParse_ValidFileName_ReturnsCorrectCounter(string fileName, int expectedCounter)
    {
        var result = FileNameParser.TryParse(fileName);

        Assert.NotNull(result);
        Assert.Equal(expectedCounter, result.Counter);
    }

    [Theory]
    [InlineData("img_123x(1)_240115.CR3", "240115")]
    [InlineData("img_a_123(1)_231201.TIF", "231201")]
    public void TryParse_ValidFileName_ReturnsCorrectDate(string fileName, string expectedDate)
    {
        var result = FileNameParser.TryParse(fileName);

        Assert.NotNull(result);
        Assert.Equal(expectedDate, result.Date);
    }

    [Theory]
    [InlineData("img_123x(1)_240115.CR3", "CR3")]
    [InlineData("img_123(1)_240115.NEF", "NEF")]
    [InlineData("img_a_123(1)_240115.TIF", "TIF")]
    [InlineData("img_123_c(3)_240115.TIFF", "TIFF")]
    public void TryParse_ValidFileName_ReturnsCorrectExtension(string fileName, string expectedExtension)
    {
        var result = FileNameParser.TryParse(fileName);

        Assert.NotNull(result);
        Assert.Equal(expectedExtension, result.Extension);
    }

    [Fact]
    public void TryParse_ValidFileName_ReturnsOriginalFileName()
    {
        const string fileName = "img_123x(1)_240115.CR3";

        var result = FileNameParser.TryParse(fileName);

        Assert.NotNull(result);
        Assert.Equal(fileName, result.OriginalFileName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParse_EmptyOrNull_ReturnsNull(string? fileName)
    {
        var result = FileNameParser.TryParse(fileName!);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("123x(1)_240115.CR3")] // Missing img_ prefix
    [InlineData("img_123x_240115.CR3")] // Missing counter
    [InlineData("img_123x(1)_24011.CR3")] // Date too short
    [InlineData("img_123x(1)_2401150.CR3")] // Date too long
    [InlineData("img_123x(10)_240115.CR3")] // Counter too long
    [InlineData("img_(1)_240115.CR3")] // Missing inventory ID
    [InlineData("random_file.CR3")] // Completely different format
    public void TryParse_InvalidFormat_ReturnsNull(string fileName)
    {
        var result = FileNameParser.TryParse(fileName);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("img_123x(1)_240115.JPG")] // Unsupported extension
    [InlineData("img_123x(1)_240115.PNG")]
    [InlineData("img_123x(1)_240115.PSD")]
    public void TryParse_UnsupportedExtension_ReturnsNull(string fileName)
    {
        var result = FileNameParser.TryParse(fileName);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("photo.NEF", true)]
    [InlineData("photo.CR3", true)]
    [InlineData("photo.ARW", true)]
    [InlineData("photo.RAF", true)]
    [InlineData("photo.DNG", true)]
    [InlineData("photo.TIF", true)]
    [InlineData("photo.TIFF", true)]
    [InlineData("photo.nef", true)] // lowercase
    [InlineData("photo.JPG", false)]
    [InlineData("photo.PNG", false)]
    [InlineData("photo", false)]
    public void IsSupportedMasterFile_ReturnsExpectedResult(string fileName, bool expected)
    {
        var result = FileNameParser.IsSupportedMasterFile(fileName);

        Assert.Equal(expected, result);
    }
}
