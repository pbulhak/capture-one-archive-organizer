namespace ArchiveOrganizer.UI.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchiveOrganizer.Core.Models;
using ArchiveOrganizer.Core.Services;

/// <summary>
/// Main view model for the application.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private string _sourcePath = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveCommand))]
    private string _destinationPath = "";

    [ObservableProperty]
    private string _prefix = FileNameParser.DefaultPrefix;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    private bool _isProcessing;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private int _progressMax = 100;

    [ObservableProperty]
    private string _currentFileName = "";

    [ObservableProperty]
    private string _transferSpeed = "";

    /// <summary>
    /// Collection of scanned archive items.
    /// </summary>
    public ObservableCollection<ArchiveItem> Items { get; } = [];

    /// <summary>
    /// Results of the last copy/move operation.
    /// </summary>
    public ObservableCollection<OperationResult> Results { get; } = [];

    /// <summary>
    /// Number of selected items.
    /// </summary>
    public int SelectedCount => Items.Count(i => i.IsSelected);

    /// <summary>
    /// Number of complete items (have both master and COS).
    /// </summary>
    public int CompleteCount => Items.Count(i => i.Status == ItemStatus.Complete);

    /// <summary>
    /// Number of items missing COS file.
    /// </summary>
    public int MissingCosCount => Items.Count(i => i.Status == ItemStatus.MissingCos);

    [RelayCommand(CanExecute = nameof(CanScan))]
    private void Scan()
    {
        Items.Clear();
        Results.Clear();
        StatusMessage = "Scanning...";

        try
        {
            var items = SessionScanner.Scan(SourcePath, Prefix);

            foreach (var item in items)
            {
                Items.Add(item);
            }

            StatusMessage = $"Found {Items.Count} files ({CompleteCount} complete, {MissingCosCount} missing COS)";
        }
        catch (DirectoryNotFoundException)
        {
            StatusMessage = "Error: Source folder not found";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "Error: Access denied to source folder";
        }
        catch (IOException ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CompleteCount));
        OnPropertyChanged(nameof(MissingCosCount));
        NotifySelectionChanged();
    }

    private bool CanScan() =>
        !string.IsNullOrWhiteSpace(SourcePath) &&
        Directory.Exists(SourcePath) &&
        !IsProcessing;

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private async Task CopyAsync()
    {
        await ProcessItemsAsync(moveFiles: false);
    }

    [RelayCommand(CanExecute = nameof(CanProcess))]
    private async Task MoveAsync()
    {
        await ProcessItemsAsync(moveFiles: true);
    }

    private bool CanProcess() =>
        !string.IsNullOrWhiteSpace(DestinationPath) &&
        Items.Any(i => i.IsSelected) &&
        !IsProcessing;

    private async Task ProcessItemsAsync(bool moveFiles)
    {
        IsProcessing = true;
        Results.Clear();
        CurrentFileName = "";
        TransferSpeed = "";

        var selectedItems = Items.Where(i => i.IsSelected).ToList();
        ProgressMax = selectedItems.Count;
        ProgressValue = 0;

        var operation = moveFiles ? "Moving" : "Copying";
        StatusMessage = $"{operation} {selectedItems.Count} files...";

        var progress = new Progress<CopyProgress>(p =>
        {
            CurrentFileName = p.CurrentFileName;
            ProgressValue = p.ProcessedFiles;
            TransferSpeed = FormatSpeed(p.SpeedBytesPerSecond);
        });

        try
        {
            var results = moveFiles
                ? await FileOrganizer.MoveAsync(selectedItems, DestinationPath, Prefix, progress)
                : await FileOrganizer.CopyAsync(selectedItems, DestinationPath, Prefix, progress);

            foreach (var result in results)
            {
                Results.Add(result);
            }

            var successCount = results.Count(r => r.Success);
            var failCount = results.Count(r => !r.Success);

            CurrentFileName = "";
            TransferSpeed = "";
            StatusMessage = $"Completed: {successCount} succeeded, {failCount} failed";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "Error: Access denied to destination folder";
        }
        catch (IOException ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }

        IsProcessing = false;
        ExportCsvCommand.NotifyCanExecuteChanged();
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond <= 0)
        {
            return "";
        }

        return bytesPerSecond switch
        {
            >= 1_073_741_824 => $"{bytesPerSecond / 1_073_741_824:F1} GB/s",
            >= 1_048_576 => $"{bytesPerSecond / 1_048_576:F1} MB/s",
            >= 1024 => $"{bytesPerSecond / 1024:F1} KB/s",
            _ => $"{bytesPerSecond:F0} B/s"
        };
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void ExportCsv(string filePath)
    {
        try
        {
            ReportGenerator.GenerateCsv(Results, filePath);
            StatusMessage = $"Report saved to {Path.GetFileName(filePath)}";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "Error: Cannot write to selected location";
        }
        catch (IOException ex)
        {
            StatusMessage = $"Error saving report: {ex.Message}";
        }
    }

    private bool CanExport() => Results.Count > 0 && !IsProcessing;

    /// <summary>
    /// Selects all items.
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in Items)
        {
            item.IsSelected = true;
        }
        OnPropertyChanged(nameof(SelectedCount));
    }

    /// <summary>
    /// Deselects all items.
    /// </summary>
    [RelayCommand]
    private void SelectNone()
    {
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }
        OnPropertyChanged(nameof(SelectedCount));
    }

    /// <summary>
    /// Selects only complete items (have both master and COS).
    /// </summary>
    [RelayCommand]
    private void SelectComplete()
    {
        foreach (var item in Items)
        {
            item.IsSelected = item.Status == ItemStatus.Complete;
        }
        OnPropertyChanged(nameof(SelectedCount));
    }

    /// <summary>
    /// Notifies that selection changed (call from UI when checkbox toggled).
    /// </summary>
    public void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        CopyCommand.NotifyCanExecuteChanged();
        MoveCommand.NotifyCanExecuteChanged();
    }
}
