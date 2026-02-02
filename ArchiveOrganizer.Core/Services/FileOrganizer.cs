namespace ArchiveOrganizer.Core.Services;

using System.Diagnostics;
using ArchiveOrganizer.Core.Models;

/// <summary>
/// Organizes archive items by copying or moving them to destination folder.
/// </summary>
public static class FileOrganizer
{
    private const string CaptureOneSettingsPath = "CaptureOne/Settings153";
    private const int BufferSize = 81920; // 80 KB buffer for progress reporting

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

    /// <summary>
    /// Copies selected archive items to destination folder asynchronously with progress reporting.
    /// </summary>
    public static async Task<List<OperationResult>> CopyAsync(
        IEnumerable<ArchiveItem> items,
        string destinationFolder,
        string prefix,
        IProgress<CopyProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await ProcessItemsAsync(items, destinationFolder, prefix, moveFiles: false, progress, cancellationToken);
    }

    /// <summary>
    /// Moves selected archive items to destination folder asynchronously with progress reporting.
    /// </summary>
    public static async Task<List<OperationResult>> MoveAsync(
        IEnumerable<ArchiveItem> items,
        string destinationFolder,
        string prefix,
        IProgress<CopyProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await ProcessItemsAsync(items, destinationFolder, prefix, moveFiles: true, progress, cancellationToken);
    }

    private static async Task<List<OperationResult>> ProcessItemsAsync(
        IEnumerable<ArchiveItem> items,
        string destinationFolder,
        string prefix,
        bool moveFiles,
        IProgress<CopyProgress>? progress,
        CancellationToken cancellationToken)
    {
        var results = new List<OperationResult>();
        var selectedItems = items.Where(i => i.IsSelected).ToList();
        var totalFiles = selectedItems.Count;
        var processedFiles = 0;

        foreach (var item in selectedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(item.MasterFilePath);
            var (result, speed) = await ProcessItemAsync(item, destinationFolder, prefix, moveFiles, cancellationToken);
            results.Add(result);

            processedFiles++;
            progress?.Report(new CopyProgress
            {
                CurrentFileName = fileName,
                ProcessedFiles = processedFiles,
                TotalFiles = totalFiles,
                SpeedBytesPerSecond = speed
            });
        }

        return results;
    }

    private static async Task<(OperationResult Result, double Speed)> ProcessItemAsync(
        ArchiveItem item,
        string destinationFolder,
        string prefix,
        bool moveFiles,
        CancellationToken cancellationToken)
    {
        try
        {
            var folderName = $"{prefix}{item.InventoryId}";
            var itemFolder = Path.Combine(destinationFolder, folderName);
            var settingsFolder = Path.Combine(itemFolder, CaptureOneSettingsPath);

            Directory.CreateDirectory(itemFolder);
            Directory.CreateDirectory(settingsFolder);

            var masterFileName = Path.GetFileName(item.MasterFilePath);
            var destMasterPath = Path.Combine(itemFolder, masterFileName);

            double speed = 0;
            if (moveFiles)
            {
                File.Move(item.MasterFilePath, destMasterPath, overwrite: false);
            }
            else
            {
                speed = await CopyFileWithSpeedAsync(item.MasterFilePath, destMasterPath, cancellationToken);
            }

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
                    await CopyFileWithSpeedAsync(item.CosFilePath, destCosPath, cancellationToken);
                }
            }

            return (OperationResult.Ok(item, destMasterPath, destCosPath), speed);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (OperationResult.Fail(item, ex.Message), 0);
        }
    }

    private static async Task<double> CopyFileWithSpeedAsync(
        string sourcePath,
        string destPath,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        long totalBytes = 0;

        await using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        await using var destStream = new FileStream(destPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

        var buffer = new byte[BufferSize];
        int bytesRead;

        while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytes += bytesRead;
        }

        stopwatch.Stop();
        var seconds = stopwatch.Elapsed.TotalSeconds;
        return seconds > 0 ? totalBytes / seconds : 0;
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
            var folderName = $"{prefix}{item.InventoryId}";
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
