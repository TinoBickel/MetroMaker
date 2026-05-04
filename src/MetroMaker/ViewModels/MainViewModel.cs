using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MetroMaker.Models;
using MetroMaker.Services;

namespace MetroMaker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IconSearchService _searchService;
    private readonly IconExportService _exportService;
    private readonly DispatcherTimer _debounceTimer;
    private readonly string _settingsPath;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private IconEntry? _selectedIcon;

    [ObservableProperty]
    private ImageSource? _previewImage;

    [ObservableProperty]
    private ImageSource? _actualSizePreview;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _resultCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private double _strokeWeight = 0.4;

    public ObservableCollection<IconEntry> FilteredIcons { get; } = [];

    public MainViewModel()
    {
        _searchService = new IconSearchService();
        _exportService = new IconExportService();
        _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            PerformSearch();
        };

        TotalCount = _searchService.TotalCount;
        PerformSearch();
    }

    partial void OnSearchQueryChanged(string value)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    partial void OnSelectedIconChanged(IconEntry? value)
    {
        UpdatePreview();
    }

    partial void OnStrokeWeightChanged(double value)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (SelectedIcon != null)
        {
            PreviewImage = _exportService.RenderPreview(SelectedIcon, 128, StrokeWeight);
            ActualSizePreview = _exportService.RenderPreview(SelectedIcon, 24, StrokeWeight);
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
        if (SelectedIcon == null)
        {
            StatusMessage = "Bitte zuerst ein Icon auswählen.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PNG Dateien (*.png)|*.png",
            FileName = SelectedIcon.Name,
            DefaultExt = ".png"
        };

        var lastPath = LoadLastExportPath();
        if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
            dialog.InitialDirectory = lastPath;

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _exportService.Export(SelectedIcon, dialog.FileName, StrokeWeight);
            SaveLastExportPath(Path.GetDirectoryName(dialog.FileName)!);
            StatusMessage = $"Exportiert: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
        }
    }

    private string? LoadLastExportPath()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings?.LastExportPath;
            }
        }
        catch { }
        return null;
    }

    private void SaveLastExportPath(string path)
    {
        try
        {
            var settings = new AppSettings { LastExportPath = path };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch { }
    }

    private void PerformSearch()
    {
        var results = _searchService.Search(SearchQuery);
        FilteredIcons.Clear();
        var maxDisplay = string.IsNullOrWhiteSpace(SearchQuery) ? 500 : Math.Min(results.Count, 1000);
        foreach (var icon in results.Take(maxDisplay))
            FilteredIcons.Add(icon);
        ResultCount = results.Count;
    }

    private class AppSettings
    {
        public string? LastExportPath { get; set; }
    }
}
