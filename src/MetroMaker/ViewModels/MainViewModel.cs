using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using MetroMaker.Services;

namespace MetroMaker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IconSearchService _searchService;
    private readonly IconExportService _exportService;
    private readonly DispatcherTimer _debounceTimer;
    private readonly string _outputDir;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private MaterialIconKind? _selectedIcon;

    [ObservableProperty]
    private string _exportFilename = string.Empty;

    [ObservableProperty]
    private ImageSource? _previewImage;

    [ObservableProperty]
    private ImageSource? _actualSizePreview;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _resultCount;

    public ObservableCollection<MaterialIconKind> FilteredIcons { get; } = [];

    public MainViewModel()
    {
        _searchService = new IconSearchService();
        _exportService = new IconExportService();
        _outputDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "Output"));

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            PerformSearch();
        };

        PerformSearch();
    }

    partial void OnSearchQueryChanged(string value)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    partial void OnSelectedIconChanged(MaterialIconKind? value)
    {
        if (value.HasValue)
        {
            PreviewImage = _exportService.RenderPreview(value.Value, 128);
            ActualSizePreview = _exportService.RenderPreview(value.Value, 24);
            if (string.IsNullOrEmpty(ExportFilename))
                ExportFilename = value.Value.ToString();
        }
        else
        {
            PreviewImage = null;
            ActualSizePreview = null;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        _debounceTimer.Stop();
        PerformSearch();
    }

    [RelayCommand]
    private void Export()
    {
        if (!SelectedIcon.HasValue)
        {
            StatusMessage = "Bitte zuerst ein Icon auswählen.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ExportFilename))
        {
            StatusMessage = "Bitte einen Dateinamen eingeben.";
            return;
        }

        try
        {
            var path = _exportService.Export(SelectedIcon.Value, ExportFilename, _outputDir);
            StatusMessage = $"Exportiert: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
        }
    }

    private void PerformSearch()
    {
        var results = _searchService.Search(SearchQuery);
        FilteredIcons.Clear();
        var maxDisplay = string.IsNullOrWhiteSpace(SearchQuery) ? 500 : results.Count;
        foreach (var icon in results.Take(maxDisplay))
            FilteredIcons.Add(icon);
        ResultCount = results.Count;
    }
}
