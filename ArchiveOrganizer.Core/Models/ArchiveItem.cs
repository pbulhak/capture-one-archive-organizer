namespace ArchiveOrganizer.Core.Models;

/// <summary>
/// Represents a single file pair (master image + optional COS sidecar).
/// </summary>
public sealed class ArchiveItem
{
    /// <summary>
    /// Inventory ID extracted from file name - used as target folder name.
    /// </summary>
    public required string InventoryId { get; init; }

    /// <summary>
    /// Full path to the master file (RAW or TIFF).
    /// </summary>
    public required string MasterFilePath { get; init; }

    /// <summary>
    /// Full path to the COS sidecar file. Null if not found.
    /// </summary>
    public string? CosFilePath { get; init; }

    /// <summary>
    /// Parsed file name components.
    /// </summary>
    public required ParsedFileName ParsedName { get; init; }

    /// <summary>
    /// Status based on presence of master and COS files.
    /// </summary>
    public ItemStatus Status => CosFilePath is null ? ItemStatus.MissingCos : ItemStatus.Complete;

    /// <summary>
    /// Whether this item is selected for processing.
    /// </summary>
    public bool IsSelected { get; set; } = true;
}
