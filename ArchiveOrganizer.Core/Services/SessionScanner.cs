namespace ArchiveOrganizer.Core.Services;

using ArchiveOrganizer.Core.Models;

/// <summary>
/// Scans Capture One session folders for master files and their COS sidecars.
/// </summary>
public static class SessionScanner
{
    private const string CaptureOneSettingsPath = "CaptureOne/Settings";
    private const string CosExtension = ".cos";

    /// <summary>
    /// Scans a folder recursively for master files and pairs them with COS sidecars.
    /// </summary>
    /// <param name="folderPath">Root folder to scan.</param>
    /// <param name="prefix">Expected file name prefix.</param>
    /// <returns>List of archive items found.</returns>
    public static List<ArchiveItem> Scan(string folderPath, string prefix = FileNameParser.DefaultPrefix)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return [];
        }

        if (!Directory.Exists(folderPath))
        {
            return [];
        }

        var items = new List<ArchiveItem>();
        ScanDirectory(folderPath, items, prefix);
        return items;
    }

    private static void ScanDirectory(string directory, List<ArchiveItem> items, string prefix)
    {
        // Get all files in current directory
        string[] files;
        try
        {
            files = Directory.GetFiles(directory);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        foreach (var filePath in files)
        {
            var fileName = Path.GetFileName(filePath);
            var parsed = FileNameParser.TryParse(fileName, prefix);

            if (parsed is null)
            {
                continue;
            }

            var cosPath = FindCosFile(directory, fileName);

            items.Add(new ArchiveItem
            {
                InventoryId = parsed.InventoryId,
                MasterFilePath = filePath,
                CosFilePath = cosPath,
                ParsedName = parsed
            });
        }

        // Recurse into subdirectories (skip CaptureOne folder itself)
        string[] subdirectories;
        try
        {
            subdirectories = Directory.GetDirectories(directory);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        foreach (var subdir in subdirectories)
        {
            var dirName = Path.GetFileName(subdir);

            // Skip CaptureOne settings directories - they contain sidecars, not masters
            if (dirName.Equals("CaptureOne", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ScanDirectory(subdir, items, prefix);
        }
    }

    /// <summary>
    /// Finds COS sidecar file for a given master file.
    /// Looks in CaptureOne/Settings/ subdirectory relative to master file location.
    /// </summary>
    private static string? FindCosFile(string masterDirectory, string masterFileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(masterFileName);
        var cosFileName = baseName + CosExtension;

        // Look in CaptureOne/Settings/ relative to master file
        var settingsPath = Path.Combine(masterDirectory, CaptureOneSettingsPath, cosFileName);

        if (File.Exists(settingsPath))
        {
            return settingsPath;
        }

        return null;
    }
}
