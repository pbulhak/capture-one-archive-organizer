namespace ArchiveOrganizer.UI;

using System.Windows;
using Microsoft.Win32;
using ArchiveOrganizer.UI.ViewModels;

/// <summary>
/// Main application window.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseForFolder("Select Source Folder");
        if (!string.IsNullOrEmpty(path))
        {
            _viewModel.SourcePath = path;
        }
    }

    private void BrowseDestination_Click(object sender, RoutedEventArgs e)
    {
        var path = BrowseForFolder("Select Destination Folder");
        if (!string.IsNullOrEmpty(path))
        {
            _viewModel.DestinationPath = path;
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Results.Count == 0)
        {
            MessageBox.Show(
                "No results to export. Run Copy or Move first.",
                "Export",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "archive_summary.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.ExportCsvCommand.Execute(dialog.FileName);
        }
    }

    private static string? BrowseForFolder(string description)
    {
        // Using OpenFolderDialog (available in .NET 8)
        var dialog = new OpenFolderDialog
        {
            Title = description,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FolderName;
        }

        return null;
    }
}
