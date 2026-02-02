namespace ArchiveOrganizer.Core.Models;

/// <summary>
/// Represents parsed components of a Capture One file name.
/// Format: [prefix][InventoryId]([Counter])_[YYMMDD].[Extension]
/// </summary>
public sealed class ParsedFileName
{
    /// <summary>
    /// Inventory ID (Job Name) - used as target folder name.
    /// </summary>
    public required string InventoryId { get; init; }

    /// <summary>
    /// File counter (1-9).
    /// </summary>
    public required int Counter { get; init; }

    /// <summary>
    /// Date string in YYMMDD format.
    /// </summary>
    public required string Date { get; init; }

    /// <summary>
    /// File extension without dot (e.g., "CR3", "NEF").
    /// </summary>
    public required string Extension { get; init; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public required string OriginalFileName { get; init; }
}
