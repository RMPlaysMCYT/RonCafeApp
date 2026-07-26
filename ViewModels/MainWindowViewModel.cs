using System;
using RonCafeApp.Models;
using RonCafeApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace RonCafeApp.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly DatabaseService _dbService;
    private readonly ClockDisplay _clockDisplay;

    private string _wallpaperPath = string.Empty;
    public string WallpaperPath
    {
        get => _wallpaperPath;
        set 
        { 
            if (_wallpaperPath != value) 
            { 
                _wallpaperPath = value; 
                Notify(nameof(WallpaperPath));
                // REMOVED: SaveConfig() - Client is read-only
            }
        }
    }

    private IBrush? _wallpaperBrush;
    public IBrush? WallpaperBrush
    {
        get => _wallpaperBrush;
        set 
        { 
            _wallpaperBrush = value; 
            Notify(nameof(WallpaperBrush));
        }
    }

    private AppItem? _pendingLockApp;

    private string _currentTime = DateTime.Now.ToString("HH:mm:ss");
    public string CurrentTime
    {
        get => _currentTime;
        set { _currentTime = value; Notify(nameof(CurrentTime)); }
    }

    private object? _currentPage;
    public object? CurrentPage
    {
        get => _currentPage;
        set { _currentPage = value; Notify(nameof(CurrentPage)); }
    }

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

    // Theme colors dictionary for cleaner theme switching
    private static readonly Dictionary<string, (string bg, string sidebar, string accent)> ThemeColors = new()
    {
        ["Mocha"] = ("#1E1E2E", "#181825", "#89B4FA"),
        ["Ocean"] = ("#0D1B2A", "#0A1220", "#64DFDF"),
        ["Forest"] = ("#1A2318", "#141C12", "#A6E3A1"),
        ["Sunset"] = ("#2E1A1A", "#241414", "#FAB387"),
        ["Midnight"] = ("#0A0A0F", "#07070B", "#CBA6F7"),
    };

    // REMOVED: SetTheme - Client is read-only, themes managed by admin
    // public void SetTheme(object? param) { ... }

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
        "Games", "Documents", "Programming", "Creative", "Entertainment", "Utilities"
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

    // ─── Admin Password & Settings ───────────────────────────────────────────
    private string _adminPassword = string.Empty;
    public string AdminPasswordInput
    {
        get => _adminPassword;
        set { if (_adminPassword != value) { _adminPassword = value; Notify(nameof(AdminPasswordInput)); } }
    }

    private bool _useCoverArtView;
    public bool UseCoverArtView
    {
        get => _useCoverArtView;
        set 
        { 
            _useCoverArtView = value; 
            Notify(nameof(UseCoverArtView));
            // REMOVED: SaveConfig() - Client is read-only
        }
    }

    // ─── Constructor ─────────────────────────────────────────────────────────
    public MainWindowViewModel()
    {
        _dbService = new DatabaseService();
        // Database is initialized by the admin - client only reads

        _clockDisplay = new ClockDisplay();

        // Setup clock event handlers
        _clockDisplay.OnMinuteChanged += (now) =>
        {
            CurrentTime = now.ToString("HH:mm:ss");
        };

        _clockDisplay.OnWarningTriggered += (mins) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                new RonCafeApp.Views.CurfewWarningWindow().Show();
            });
        };

        _clockDisplay.OnCurfewReached += () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var app in _allApps.Where(a => a.Category == "Games"))
                {
                    app.CategoryLocked = true;
                }
                KillGamesOnly();
            });
        };

        _clockDisplay.OnCurfewLifted += () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var app in _allApps.Where(a => a.Category == "Games"))
                {
                    app.CategoryLocked = false;
                }
            });
        };

        // Load config (read-only)
        LoadConfig();

        if (_clockDisplay.IsCurrentlyInCurfewState())
        {
            foreach (var app in _allApps.Where(a => a.Category == "Games"))
            {
                app.CategoryLocked = true;
            }
        }

        FilterApps();
    }

    // ─── App Launch (Read-Only - No CRUD) ────────────────────────────────────
    public void LaunchApp(object? param)
    {
        if (param is not AppItem app || string.IsNullOrWhiteSpace(app.getExecutionPATH))
            return;

        if (app.CategoryLocked)
        {
            _pendingLockApp = app;
            return;
        }

        ExecuteGameLaunch(app);
    }

    private void ExecuteGameLaunch(AppItem app)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = app.getExecutionPATH,
                WorkingDirectory = Path.GetDirectoryName(app.getExecutionPATH),
                UseShellExecute = true
            });

            if (process != null && app.Category == "Games")
            {
                _dbService.LogProcessStart(app.Id, process.Id, app.Category);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not launch app: {ex.Message}");
        }
    }

    private void KillGamesOnly()
    {
        var gameProcesses = _dbService.GetRunningGameProcesses();
        foreach (var (processId, _) in gameProcesses)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                proc.Kill(true);
                _dbService.LogProcessEnd(processId);
            }
            catch { /* Process might already be closed */ }
        }
    }

    // ─── REMOVED: All CRUD Operations ────────────────────────────────────────
    // REMOVED: AddNewApp()
    // REMOVED: RemoveApp()
    // REMOVED: EditApp()
    // REMOVED: CancelEditApp()
    // REMOVED: ClearFormFields()
    // REMOVED: BrowseForExecutable()
    // REMOVED: BrowseForIcon()
    // REMOVED: BrowseForCoverArt()

    // ─── Navigation ──────────────────────────────────────────────────────────
    public void CloseSettings() => CurrentPage = null;


    // ─── Persistence (Read-Only) ────────────────────────────────────────────
    public void LoadConfig()
    {
        try
        {
            var config = _dbService.LoadConfig();
            _allApps = config.Apps;
            UseCoverArtView = config.UseCoverArtReview;

            LauncherBackground = SolidColorBrush.Parse(config.BackgroundColor);
            SidebarBackground = SolidColorBrush.Parse(config.SidebarColor);
            AccentColor = SolidColorBrush.Parse(config.AccentColor);
            
            WallpaperPath = config.WallpaperPath ?? string.Empty;
            if (!string.IsNullOrEmpty(WallpaperPath) && File.Exists(WallpaperPath))
            {
                LoadWallpaper();
            }
            else
            {
                WallpaperBrush = null;   
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config: {ex.Message}");
            _allApps = new List<AppItem>();
        }
    }

    // REMOVED: SaveConfig() - Client is read-only

    private void LoadWallpaper()
    {
        if (!string.IsNullOrEmpty(WallpaperPath) && File.Exists(WallpaperPath))
        {
            try
            {
                Console.WriteLine($"Loading wallpaper from: {WallpaperPath}");
            
                var bitmap = new Bitmap(WallpaperPath);
                WallpaperBrush = new ImageBrush
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill
                };
            
                Console.WriteLine("Wallpaper loaded successfully");
            
                Notify(nameof(WallpaperBrush));
                Notify(nameof(WallpaperPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load wallpaper: {ex.Message}");
                WallpaperBrush = null;
                Notify(nameof(WallpaperBrush));
            }
        }
        else
        {
            Console.WriteLine($"Wallpaper path is null or file doesn't exist: {WallpaperPath}");
            WallpaperBrush = null;
            Notify(nameof(WallpaperBrush));
        }
    }

    public void ForceRefresh()
    {
        Notify(nameof(WallpaperBrush));
        Notify(nameof(WallpaperPath));
        Notify(nameof(LauncherBackground));
        Notify(nameof(SidebarBackground));
        Notify(nameof(AccentColor));
    }

    private void FilterApps()
    {
        DisplayedApps = new ObservableCollection<AppItem>(
            _allApps.Where(a => a.Category == SelectedCategory));
        Notify(nameof(IsAppListEmpty));
    }

    private void Notify(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void ExitApplicationLauncher()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is MainWindow mainWindow)
            {
                mainWindow.IsAdminExit = false;
                mainWindow.Close();
            }
        }
    }
}