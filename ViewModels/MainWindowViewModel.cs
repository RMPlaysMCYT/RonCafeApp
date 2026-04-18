using RonCafeApp.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace RonCafeApp.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly string _configurationPath = "cafeLauncher.json";

    // ─── Current overlay page ────────────────────────────────────────────────
    private object? _currentPage;
    public object? CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; Notify(nameof(CurrentPage)); }
    }

    // ─── Theme / Appearance ──────────────────────────────────────────────────
    private IBrush _launcherBackground = SolidColorBrush.Parse("#1E1E2E");
    public IBrush LauncherBackground
    {
        get => _launcherBackground;
        set { _launcherBackground = value; Notify(nameof(LauncherBackground)); }
    }

    private IBrush _sidebarBackground = SolidColorBrush.Parse("#181825");
    public IBrush SidebarBackground
    {
        get => _sidebarBackground;
        set { _sidebarBackground = value; Notify(nameof(SidebarBackground)); }
    }

    private IBrush _accentColor = SolidColorBrush.Parse("#89B4FA");
    public IBrush AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Notify(nameof(AccentColor)); }
    }

    /// <summary>Called from SettingsView preset buttons via CommandParameter.</summary>
    public void SetTheme(object? param)
    {
        switch (param as string)
        {
            case "Mocha": // default dark
                LauncherBackground = SolidColorBrush.Parse("#1E1E2E");
                SidebarBackground  = SolidColorBrush.Parse("#181825");
                AccentColor        = SolidColorBrush.Parse("#89B4FA");
                break;
            case "Ocean":
                LauncherBackground = SolidColorBrush.Parse("#0D1B2A");
                SidebarBackground  = SolidColorBrush.Parse("#0A1220");
                AccentColor        = SolidColorBrush.Parse("#64DFDF");
                break;
            case "Forest":
                LauncherBackground = SolidColorBrush.Parse("#1A2318");
                SidebarBackground  = SolidColorBrush.Parse("#141C12");
                AccentColor        = SolidColorBrush.Parse("#A6E3A1");
                break;
            case "Sunset":
                LauncherBackground = SolidColorBrush.Parse("#2E1A1A");
                SidebarBackground  = SolidColorBrush.Parse("#241414");
                AccentColor        = SolidColorBrush.Parse("#FAB387");
                break;
            case "Midnight":
                LauncherBackground = SolidColorBrush.Parse("#0A0A0F");
                SidebarBackground  = SolidColorBrush.Parse("#07070B");
                AccentColor        = SolidColorBrush.Parse("#CBA6F7");
                break;
        }
        SaveConfig();
    }

    // ─── New-app form fields ─────────────────────────────────────────────────
    private string _newAppName = string.Empty;
    public string NewAppName
    {
        get => _newAppName;
        set { if (_newAppName != value) { _newAppName = value; Notify(nameof(NewAppName)); } }
    }

    private string _newAppExecutionPath = string.Empty;
    public string NewAppExecutionPath
    {
        get => _newAppExecutionPath;
        set { if (_newAppExecutionPath != value) { _newAppExecutionPath = value; Notify(nameof(NewAppExecutionPath)); } }
    }

    private string _newIconPlaceHolder = string.Empty;
    public string NewIconPlaceHolder
    {
        get => _newIconPlaceHolder;
        set { if (_newIconPlaceHolder != value) { _newIconPlaceHolder = value; Notify(nameof(NewIconPlaceHolder)); } }
    }

    // ─── App list ────────────────────────────────────────────────────────────
    public bool IsAppListEmpty => DisplayedApps.Count == 0;

    private List<AppItem> _allApps = new();

    private ObservableCollection<AppItem> _displayedApps = new();
    public ObservableCollection<AppItem> DisplayedApps
    {
        get => _displayedApps;
        set { _displayedApps = value; Notify(nameof(DisplayedApps)); }
    }

    public List<string> Categories { get; } = new()
    {
        "Games", "Documents", "Programming", "Entertainment", "Utilities"
    };

    private string _selectedCategory = "Games";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory != value)
            {
                _selectedCategory = value;
                Notify(nameof(SelectedCategory));
                FilterApps();
            }
        }
    }

    // ─── Constructor ─────────────────────────────────────────────────────────
    public MainWindowViewModel()
    {
        LoadConfig();
        FilterApps();
    }

    // ─── App CRUD ────────────────────────────────────────────────────────────
    public void LaunchApp(object? param)
    {
        if (param is not string execPath || string.IsNullOrWhiteSpace(execPath)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = execPath,
                WorkingDirectory = Path.GetDirectoryName(execPath),
                UseShellExecute = true
            });
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Could not launch app: {ex.Message}");
        }
    }

    public void AddNewApp()
    {
        if (string.IsNullOrWhiteSpace(NewAppName) || string.IsNullOrWhiteSpace(NewAppExecutionPath))
            return;

        _allApps.Add(new AppItem
        {
            Name             = NewAppName,
            Category         = SelectedCategory,
            getExecutionPATH = NewAppExecutionPath,
            IconPlaceholder  = string.IsNullOrWhiteSpace(NewIconPlaceHolder)
                               ? "/Assets/placeholder.png"
                               : NewIconPlaceHolder
        });

        SaveConfig();
        FilterApps();
        NewAppName           = string.Empty;
        NewAppExecutionPath  = string.Empty;
        NewIconPlaceHolder   = string.Empty;
    }

    public void RemoveApp(object? param)
    {
        if (param is not AppItem app) return;
        _allApps.Remove(app);
        SaveConfig();
        FilterApps();
    }

    // ─── File pickers ────────────────────────────────────────────────────────
    public async void BrowseForExecutable()
    {
        var files = await OpenPickerAsync("Select Game Executable",
            new FilePickerFileType("Executables") { Patterns = new[] { "*.exe" } });
        if (files?.Count >= 1) NewAppExecutionPath = files[0].Path.LocalPath;
    }

    public async void BrowseForIcon()
    {
        var files = await OpenPickerAsync("Select App Icon",
            new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.ico", "*.webp" } },
            new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } });
        if (files?.Count >= 1) NewIconPlaceHolder = files[0].Path.LocalPath;
    }

    private static async System.Threading.Tasks.Task<IReadOnlyList<IStorageFile>?> OpenPickerAsync(
        string title, params FilePickerFileType[] filters)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;
        var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
        if (topLevel == null) return null;
        return await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title, AllowMultiple = false, FileTypeFilter = filters
        });
    }

    // ─── Navigation ──────────────────────────────────────────────────────────
    public void ShowSettings()  => CurrentPage = new RonCafeApp.Views.SettingsView { DataContext = this };
    public void CloseSettings() => CurrentPage = null;

    // ─── Persistence ─────────────────────────────────────────────────────────
    private void LoadConfig()
    {
        if (!File.Exists(_configurationPath))
        {
            _allApps = new List<AppItem>();
            SaveConfig();
            return;
        }

        string json = File.ReadAllText(_configurationPath);

        // Handle old format: plain array of apps
        if (json.TrimStart().StartsWith("["))
        {
            _allApps = JsonSerializer.Deserialize<List<AppItem>>(json) ?? new List<AppItem>();
            SaveConfig(); // re-save in new format immediately
            return;
        }

        // New format: LauncherConfig object
        var config = JsonSerializer.Deserialize<LauncherConfig>(json) ?? new LauncherConfig();
        _allApps = config.Apps;
        try
        {
            LauncherBackground = SolidColorBrush.Parse(config.BackgroundColor);
            SidebarBackground  = SolidColorBrush.Parse(config.SidebarColor);
            AccentColor        = SolidColorBrush.Parse(config.AccentColor);
        }
        catch { /* keep defaults if saved color string is invalid */ }
    }

    private void SaveConfig()
    {
        var config = new LauncherConfig
        {
            Apps            = _allApps,
            BackgroundColor = (LauncherBackground as SolidColorBrush)?.Color.ToString() ?? "#1E1E2E",
            SidebarColor    = (SidebarBackground  as SolidColorBrush)?.Color.ToString() ?? "#181825",
            AccentColor     = (AccentColor         as SolidColorBrush)?.Color.ToString() ?? "#89B4FA",
        };
        File.WriteAllText(_configurationPath,
            JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void FilterApps()
    {
        DisplayedApps = new ObservableCollection<AppItem>(
            _allApps.Where(a => a.Category == SelectedCategory));
        Notify(nameof(IsAppListEmpty));
    }

    private void Notify(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}