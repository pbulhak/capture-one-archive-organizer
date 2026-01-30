namespace ArchiveOrganizer.Core.Services;

using System.Text.RegularExpressions;
using ArchiveOrganizer.Core.Models;

/// <summary>
/// Parses Capture One file names to extract inventory ID and other components.
/// </summary>
public static partial class FileNameParser
{
    /// <summary>
    /// Pattern: mwp_[InventoryId]([Counter])_[YYMMDD].[Extension]
    /// </summary>
    [GeneratedRegex(@"^mwp_(.+?)\((\d)\)_(\d{6})\.([a-zA-Z0-9]+)$", RegexOptions.Compiled)]
    private static partial Regex FileNamePattern();

    /// <summary>
    /// Supported master file extensions (RAW + TIFF).
    /// </summary>
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "NEF", "CR3", "ARW", "RAF", "DNG", "TIF", "TIFF"
    };

    /// <summary>
    /// Attempts to parse a file name and extract its components.
    /// </summary>
    /// <param name="fileName">File name (not full path) to parse.</param>
    /// <returns>Parsed result or null if file name doesn't match expected pattern.</returns>
    public static ParsedFileName? TryParse(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var match = FileNamePattern().Match(fileName);
        if (!match.Success)
        {
            return null;
        }

        var extension = match.Groups[4].Value;
        if (!SupportedExtensions.Contains(extension))
        {
            return null;
        }

        return new ParsedFileName
        {
            InventoryId = match.Groups[1].Value,
            Counter = int.Parse(match.Groups[2].Value),
            Date = match.Groups[3].Value,
            Extension = extension,
            OriginalFileName = fileName
        };
    }

    /// <summary>
    /// Checks if file has a supported master extension.
    /// </summary>
    public static bool IsSupportedMasterFile(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        return SupportedExtensions.Contains(extension.TrimStart('.'));
    }
}
