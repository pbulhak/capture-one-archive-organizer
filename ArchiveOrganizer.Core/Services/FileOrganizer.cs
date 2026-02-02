namespace ArchiveOrganizer.Core.Services;

using ArchiveOrganizer.Core.Models;

/// <summary>
/// Organizes archive items by copying or moving them to destination folder.
/// </summary>
public static class FileOrganizer
{
    private const string CaptureOneSettingsPath = "CaptureOne/Settings153";

    /// <summary>
    /// Copies selected archive items to destination folder.
    /// </summary>
    /// <param name="items">Items to copy.</param>
    /// <param name="destinationFolder">Root destination folder.</param>
    /// <param name="prefix">Prefix for destination folder names.</param>
    /// <returns>List of operation results.</returns>
    public static List<OperationResult> Copy(
        IEnumerable<ArchiveItem> items,
        string destinationFolder,
        string prefix = FileNameParser.DefaultPrefix)
    {
        return ProcessItems(items, destinationFolder, prefix, moveFiles: false);
    }

    /// <summary>
    /// Moves selected archive items to destination folder.
    /// </summary>
    /// <param name="items">Items to move.</param>
    /// <param name="destinationFolder">Root destination folder.</param>
    /// <param name="prefix">Prefix for destination folder names.</param>
    /// <returns>List of operation results.</returns>
    public static List<OperationResult> Move(
        IEnumerable<ArchiveItem> items,
        string destinationFolder,
        string prefix = FileNameParser.DefaultPrefix)
    {
        return ProcessItems(items, destinationFolder, prefix, moveFiles: true);
    }

    private static List<OperationResult> ProcessItems(
        IEnumerable<ArchiveItem> items,
        string destinationFolder,
        string prefix,
        bool moveFiles)
    {
        var results = new List<OperationResult>();

        foreach (var item in items)
        {
            if (!item.IsSelected)
            {
                continue;
            }

            var result = ProcessItem(item, destinationFolder, prefix, moveFiles);
            results.Add(result);
        }

        return results;
    }

    private static OperationResult ProcessItem(
        ArchiveItem item,
        string destinationFolder,
        string prefix,
        bool moveFiles)
    {
        try
        {
            // Create destination folder structure with prefix
            var folderName = $"{prefix}_{item.InventoryId}";
            var itemFolder = Path.Combine(destinationFolder, folderName);
            var settingsFolder = Path.Combine(itemFolder, CaptureOneSettingsPath);

            Directory.CreateDirectory(itemFolder);
            Directory.CreateDirectory(settingsFolder);

            // Copy/move master file
            var masterFileName = Path.GetFileName(item.MasterFilePath);
            var destMasterPath = Path.Combine(itemFolder, masterFileName);

            if (moveFiles)
            {
                File.Move(item.MasterFilePath, destMasterPath, overwrite: false);
            }
            else
            {
                File.Copy(item.MasterFilePath, destMasterPath, overwrite: false);
            }

            // Copy/move COS file if exists
            string? destCosPath = null;
            if (item.CosFilePath is not null)
            {
                var cosFileName = Path.GetFileName(item.CosFilePath);
                destCosPath = Path.Combine(settingsFolder, cosFileName);

                if (moveFiles)
                {
                    File.Move(item.CosFilePath, destCosPath, overwrite: false);
                }
                else
                {
                    File.Copy(item.CosFilePath, destCosPath, overwrite: false);
                }
            }

            return OperationResult.Ok(item, destMasterPath, destCosPath);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail(item, ex.Message);
        }
    }
}
