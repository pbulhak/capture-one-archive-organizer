namespace ArchiveOrganizer.Core.Models;

/// <summary>
/// Progress information for copy/move operations.
/// </summary>
public sealed class CopyProgress
{
    /// <summary>
    /// Name of the file currently being processed.
    /// </summary>
    public string CurrentFileName { get; init; } = "";

    /// <summary>
    /// Number of files already processed.
    /// </summary>
    public int ProcessedFiles { get; init; }

    /// <summary>
    /// Total number of files to process.
    /// </summary>
    public int TotalFiles { get; init; }

    /// <summary>
    /// Current transfer speed in bytes per second.
    /// </summary>
    public double SpeedBytesPerSecond { get; init; }
}
