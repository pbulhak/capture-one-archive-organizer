namespace ArchiveOrganizer.Core.Models;

/// <summary>
/// Result of a file copy/move operation.
/// </summary>
public sealed class OperationResult
{
    /// <summary>
    /// The archive item that was processed.
    /// </summary>
    public required ArchiveItem Item { get; init; }

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Destination path for the master file.
    /// </summary>
    public string? DestinationMasterPath { get; init; }

    /// <summary>
    /// Destination path for the COS file.
    /// </summary>
    public string? DestinationCosPath { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static OperationResult Ok(
        ArchiveItem item,
        string destinationMasterPath,
        string? destinationCosPath) => new()
        {
            Item = item,
            Success = true,
            DestinationMasterPath = destinationMasterPath,
            DestinationCosPath = destinationCosPath
        };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static OperationResult Fail(ArchiveItem item, string errorMessage) => new()
    {
        Item = item,
        Success = false,
        ErrorMessage = errorMessage
    };
}
