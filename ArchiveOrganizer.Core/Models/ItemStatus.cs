namespace ArchiveOrganizer.Core.Models;

/// <summary>
/// Status of an archive item (RAW + COS pair).
/// </summary>
public enum ItemStatus
{
    /// <summary>
    /// Both RAW/TIFF and COS files are present.
    /// </summary>
    Complete,

    /// <summary>
    /// RAW/TIFF file exists but COS sidecar is missing.
    /// </summary>
    MissingCos,

    /// <summary>
    /// COS sidecar exists but RAW/TIFF file is missing.
    /// </summary>
    MissingRaw
}
