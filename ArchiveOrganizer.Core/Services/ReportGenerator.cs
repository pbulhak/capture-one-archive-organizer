namespace ArchiveOrganizer.Core.Services;

using System.Text;
using ArchiveOrganizer.Core.Models;

/// <summary>
/// Generates CSV reports from operation results.
/// </summary>
public static class ReportGenerator
{
    /// <summary>
    /// Generates a CSV summary report.
    /// </summary>
    /// <param name="results">Operation results to include in report.</param>
    /// <param name="outputPath">Path to output CSV file.</param>
    public static void GenerateCsv(IEnumerable<OperationResult> results, string outputPath)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("InventoryId,SourceMaster,SourceCos,DestMaster,DestCos,Status,Error");

        foreach (var result in results)
        {
            var status = result.Success ? "OK" : "FAILED";
            var line = string.Join(",",
                EscapeCsv(result.Item.InventoryId),
                EscapeCsv(result.Item.MasterFilePath),
                EscapeCsv(result.Item.CosFilePath ?? ""),
                EscapeCsv(result.DestinationMasterPath ?? ""),
                EscapeCsv(result.DestinationCosPath ?? ""),
                status,
                EscapeCsv(result.ErrorMessage ?? ""));

            sb.AppendLine(line);
        }

        File.WriteAllText(outputPath, sb.ToString());
    }

    /// <summary>
    /// Generates CSV content as string (for preview).
    /// </summary>
    public static string GenerateCsvContent(IEnumerable<OperationResult> results)
    {
        var sb = new StringBuilder();

        sb.AppendLine("InventoryId,SourceMaster,SourceCos,DestMaster,DestCos,Status,Error");

        foreach (var result in results)
        {
            var status = result.Success ? "OK" : "FAILED";
            var line = string.Join(",",
                EscapeCsv(result.Item.InventoryId),
                EscapeCsv(result.Item.MasterFilePath),
                EscapeCsv(result.Item.CosFilePath ?? ""),
                EscapeCsv(result.DestinationMasterPath ?? ""),
                EscapeCsv(result.DestinationCosPath ?? ""),
                status,
                EscapeCsv(result.ErrorMessage ?? ""));

            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        // If contains comma, quote, or newline - wrap in quotes and escape quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
