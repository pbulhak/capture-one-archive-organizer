namespace ArchiveOrganizer.Core.Models;

using System.ComponentModel;

/// <summary>
/// Represents a single file pair (master image + optional COS sidecar).
/// </summary>
public sealed class ArchiveItem : INotifyPropertyChanged
{
    private bool _isSelected = true;

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
    /// Relative path to the Settings folder (e.g., "CaptureOne/Settings120").
    /// Used to preserve original folder structure when copying.
    /// </summary>
    public string? SettingsSubPath { get; init; }

    /// <summary>
    /// Paths to additional sidecar files (ICC/LCC profiles).
    /// </summary>
    public List<string>? AdditionalSidecarPaths { get; init; }

    /// <summary>
    /// Whether this item has ICC or LCC profile files.
    /// </summary>
    public bool HasIccLcc => AdditionalSidecarPaths is { Count: > 0 };

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
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
}
