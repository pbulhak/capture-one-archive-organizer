namespace ArchiveOrganizer.Core.Services;

using ArchiveOrganizer.Core.Models;

/// <summary>
/// Scans Capture One session folders for master files and their COS sidecars.
/// </summary>
public static class SessionScanner
{
    private static readonly string[] CaptureOneSettingsPaths =
    [
        "CaptureOne/Settings153",  // Capture One 15.x
        "CaptureOne/Settings",     // Older versions
    ];
    private const string CosExtension = ".cos";
    private static readonly string[] AdditionalSidecarExtensions = [".icm", ".lcc"];

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
            var additionalSidecars = FindAdditionalSidecars(directory, fileName);

            items.Add(new ArchiveItem
            {
                InventoryId = parsed.InventoryId,
                MasterFilePath = filePath,
                CosFilePath = cosPath,
                AdditionalSidecarPaths = additionalSidecars.Count > 0 ? additionalSidecars : null,
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
    /// Looks in CaptureOne/Settings*/ subdirectories relative to master file location.
    /// </summary>
    private static string? FindCosFile(string masterDirectory, string masterFileName)
    {
        // Capture One names COS files as originalFileName.cos (preserving the original extension)
        var cosFileName = masterFileName + CosExtension;

        // Try each known settings path location
        foreach (var settingsSubPath in CaptureOneSettingsPaths)
        {
            var cosPath = Path.Combine(masterDirectory, settingsSubPath, cosFileName);

            if (File.Exists(cosPath))
            {
                return cosPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds additional sidecar files (ICM/LCC profiles) for a given master file.
    /// Pattern: masterFileName.profileName.icm or .lcc
    /// </summary>
    private static List<string> FindAdditionalSidecars(string masterDirectory, string masterFileName)
    {
        var sidecars = new List<string>();

        foreach (var settingsSubPath in CaptureOneSettingsPaths)
        {
            var settingsDir = Path.Combine(masterDirectory, settingsSubPath);

            if (!Directory.Exists(settingsDir))
            {
                continue;
            }

            try
            {
                // Pattern: masterFileName.*.icm or masterFileName.*.lcc
                var prefix = masterFileName + ".";
                var files = Directory.GetFiles(settingsDir);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);

                    if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(file);
                    if (AdditionalSidecarExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    {
                        sidecars.Add(file);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip inaccessible directories
            }
        }

        return sidecars;
    }
}
