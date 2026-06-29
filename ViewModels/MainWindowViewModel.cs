using System;
using RonCafeApp.Models;
using RonCafeApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text.Json;
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

    // ─── Legacy JSON path (for migration) ────────────────────────────────────
    private readonly string _legacyConfigurationPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RonCafeApp", "RonCafeLauncherSettings.json");

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
                SaveConfig();
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

    public void SetTheme(object? param)
    {
        if (param is string themeName && ThemeColors.TryGetValue(themeName, out var colors))
        {
            LauncherBackground = SolidColorBrush.Parse(colors.bg);
            SidebarBackground = SolidColorBrush.Parse(colors.sidebar);
            AccentColor = SolidColorBrush.Parse(colors.accent);
            SaveConfig();
        }
    }

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

    private string _newCoverArtPlaceHolder = string.Empty;
    public string NewCoverArtPlaceHolder
    {
        get => _newCoverArtPlaceHolder;
        set
        {
            if (_newCoverArtPlaceHolder != value)
            {
                _newCoverArtPlaceHolder = value;
                Notify(nameof(NewCoverArtPlaceHolder));
            }
        }
    }

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

    private bool _isTryToOpenSettings = false;
    private string _savedAdminPassword = "abcadmin123";

    private bool _useCoverArtView;
    public bool UseCoverArtView
    {
        get => _useCoverArtView;
        set { _useCoverArtView = value; Notify(nameof(UseCoverArtView)); SaveConfig(); }
    }

    private AppItem? _editingApp;
    public string AddOrSaveButn => _editingApp == null ? "Add" : "Edit";
    public bool isEditing => _editingApp != null;


    // ─── Constructor ─────────────────────────────────────────────────────────
    public MainWindowViewModel()
    {
        _dbService = new DatabaseService();
        _dbService.InitializeDatabase();

        _clockDisplay = new ClockDisplay();

        // Setup clock event handlers (FIXED: no duplicates)
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

        // Load config (with migration support)
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

    // ─── App CRUD ────────────────────────────────────────────────────────────
    public void LaunchApp(object? param)
    {
        if (param is not AppItem app || string.IsNullOrWhiteSpace(app.getExecutionPATH))
            return;

        if (app.CategoryLocked)
        {
            _pendingLockApp = app;
            PasswordScreen();
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

    public void AddNewApp()
    {
        if (string.IsNullOrWhiteSpace(NewAppName) || string.IsNullOrWhiteSpace(NewAppExecutionPath))
            return;

        string finalIconPath = CopyImageToLocal(NewIconPlaceHolder, "/Assets/placeholder.png");
        string finalCoverPath = CopyImageToLocal(NewCoverArtPlaceHolder, "/Assets/placeholder.png");

        if (_editingApp != null)
        {
            // Update existing app
            if (_editingApp.IconPlaceholder != finalIconPath)
                DeleteLocalImage(_editingApp.IconPlaceholder);

            if (_editingApp.CoverArtPlaceholder != finalCoverPath)
                DeleteLocalImage(_editingApp.CoverArtPlaceholder);

            _editingApp.Name = NewAppName;
            _editingApp.Category = SelectedCategory;
            _editingApp.getExecutionPATH = NewAppExecutionPath;
            _editingApp.IconPlaceholder = finalIconPath;
            _editingApp.CoverArtPlaceholder = finalCoverPath;

            _dbService.UpdateApp(_editingApp);
        }
        else
        {
            // Add new app
            var newApp = new AppItem
            {
                Name = NewAppName,
                Category = SelectedCategory,
                getExecutionPATH = NewAppExecutionPath,
                IconPlaceholder = finalIconPath,
                CoverArtPlaceholder = finalCoverPath
            };

            newApp.Id = _dbService.AddApp(newApp);
            _allApps.Add(newApp);
        }

        FilterApps();
        CancelEditApp();
        ClearFormFields();
    }

    public void RemoveApp(object? param)
    {
        if (param is not AppItem app) return;

        DeleteLocalImage(app.IconPlaceholder);
        DeleteLocalImage(app.CoverArtPlaceholder);

        _dbService.DeleteApp(app.Id);
        _allApps.Remove(app);
        FilterApps();
    }

    public void EditApp(object? param)
    {
        if (param is not AppItem appItem) return;
        _editingApp = appItem;

        NewAppName = appItem.Name;
        NewAppExecutionPath = appItem.getExecutionPATH;
        NewIconPlaceHolder = appItem.IconPlaceholder;
        NewCoverArtPlaceHolder = appItem.CoverArtPlaceholder;

        SelectedCategory = appItem.Category;

        Notify(nameof(AddOrSaveButn));
        Notify(nameof(isEditing));
    }

    public void CancelEditApp()
    {
        _editingApp = null;
        ClearFormFields();
        Notify(nameof(AddOrSaveButn));
        Notify(nameof(isEditing));
    }

    private void ClearFormFields()
    {
        NewAppName = string.Empty;
        NewAppExecutionPath = string.Empty;
        NewIconPlaceHolder = string.Empty;
        NewCoverArtPlaceHolder = string.Empty;
    }

    // ─── File Pickers ───────────────────────────────────────────────────────
    public async void BrowseForExecutable()
    {
        var files = await OpenPickerAsync("Select Game Executable",
            new FilePickerFileType("Executables") { Patterns = new[] { "*.exe" } });
        if (files?.Count >= 1)
            NewAppExecutionPath = files[0].Path.LocalPath;
    }

    public async void BrowseForIcon()
    {
        var files = await OpenPickerAsync("Select App Icon",
            new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.ico", "*.webp" } },
            new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } });
        if (files?.Count >= 1)
            NewIconPlaceHolder = files[0].Path.LocalPath;
    }

    public async void BrowseForCoverArt()
    {
        var files = await OpenPickerAsync("Select Cover Art (Tall)",
            new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpeg", "*.jpg", "*.webp", "*.svg" } });
        if (files?.Count >= 1)
            NewCoverArtPlaceHolder = files[0].Path.LocalPath;
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
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters
        });
    }

    // ─── Navigation ──────────────────────────────────────────────────────────
    public void ShowSettings() => CurrentPage = new RonCafeApp.Views.SettingsView { DataContext = this };
    public void PasswordScreen() => CurrentPage = new RonCafeApp.Views.PasswordScreen { DataContext = this };
    public void CloseSettings() => CurrentPage = null;

    // ─── Password Management ─────────────────────────────────────────────────
    public void PromptSettingsPassword()
    {
        _isTryToOpenSettings = true;
        AdminPasswordInput = string.Empty;
        CurrentPage = new RonCafeApp.Views.PasswordScreen { DataContext = this };
    }

    public void CancelPasswordCommand()
    {
        _isTryToOpenSettings = false;
        _pendingLockApp = null;
        AdminPasswordInput = string.Empty;
        CloseSettings();
    }

    public void SubmitPasswordCommand()
    {
        if (AdminPasswordInput == _savedAdminPassword)
        {
            if (_isTryToOpenSettings)
            {
                _isTryToOpenSettings = false;
                ShowSettings();
            }
            else if (_pendingLockApp != null)
            {
                ExecuteGameLaunch(_pendingLockApp);
                _pendingLockApp = null;
                CloseSettings();
            }
        }
        else
        {
            AdminPasswordInput = string.Empty;
        }
    }

    
    
    
    
    public async void BrowseForWallpaper()
    {
        var files = await OpenPickerAsync("Select Wallpaper Image",
            new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp" } });
        if (files?.Count >= 1)
        {
            string localPath = CopyImageToLocal(files[0].Path.LocalPath, "");
            if (!string.IsNullOrEmpty(localPath))
            {
                WallpaperPath = localPath;
                LoadWallpaper();
                // Force UI update
                ForceRefresh();
                Notify(nameof(WallpaperBrush));
                SaveConfig();
            }
        }
    }
    private void LoadWallpaper()
    {
        if (!string.IsNullOrEmpty(WallpaperPath) && File.Exists(WallpaperPath))
        {
            try
            {
                Console.WriteLine($"Loading wallpaper from: {WallpaperPath}");
            
                // Load the image
                var bitmap = new Bitmap(WallpaperPath);
                WallpaperBrush = new ImageBrush
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill
                };
            
                Console.WriteLine("Wallpaper loaded successfully");
            
                // FORCE UI UPDATE - Important!
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
    
    public void ClearWallpaper()
    {
        if (!string.IsNullOrEmpty(WallpaperPath))
        {
            DeleteLocalImage(WallpaperPath);
        }
        WallpaperPath = string.Empty;
        WallpaperBrush = null;
        Notify(nameof(WallpaperBrush));
        Notify(nameof(WallpaperPath));
        SaveConfig();
    }
    
    
    public void ForceRefresh()
    {
        Notify(nameof(WallpaperBrush));
        Notify(nameof(WallpaperPath));
        Notify(nameof(LauncherBackground));
        Notify(nameof(SidebarBackground));
        Notify(nameof(AccentColor));
    }
    
    // public void ExitLauncher()
    // {
    //     Environment.Exit(0);
    // }

    // ─── Persistence ────────────────────────────────────────────────────────
    private void LoadConfig()
    {
        // First check if we need to migrate from JSON
        if (File.Exists(_legacyConfigurationPath) && !_dbService.HasExistingData())
        {
            Console.WriteLine("Migrating from JSON to SQLite...");
            MigrateFromJson();
        }

        // Load from database
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

    private void SaveConfig()
    {
        try
        {
            var config = new LauncherConfig
            {
                Apps = _allApps,
                BackgroundColor = (LauncherBackground as SolidColorBrush)?.Color.ToString() ?? "#1E1E2E",
                SidebarColor = (SidebarBackground as SolidColorBrush)?.Color.ToString() ?? "#181825",
                AccentColor = (AccentColor as SolidColorBrush)?.Color.ToString() ?? "#89B4FA",
                UseCoverArtReview = UseCoverArtView,
                WallpaperPath = WallpaperPath
            };
            _dbService.SaveConfig(config);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config: {ex.Message}");
        }
    }

    // ─── JSON Migration ─────────────────────────────────────────────────────
    private void MigrateFromJson()
    {
        try
        {
            string json = File.ReadAllText(_legacyConfigurationPath);

            LauncherConfig? config = null;

            // Handle old plain-array format
            if (json.TrimStart().StartsWith("["))
            {
                var apps = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListAppItem) ?? new List<AppItem>();
                config = new LauncherConfig { Apps = apps };
            }
            else
            {
                config = JsonSerializer.Deserialize(json, AppJsonContext.Default.LauncherConfig) ?? new LauncherConfig();
            }

            if (config != null)
            {
                // Save theme colors
                _dbService.SaveConfig(config);

                // Save all apps
                foreach (var app in config.Apps)
                {
                    app.Id = _dbService.AddApp(app);
                }

                Console.WriteLine($"✓ Successfully migrated {config.Apps.Count} apps from JSON to SQLite");

                // Optional: backup the old JSON file
                string backupPath = _legacyConfigurationPath + ".backup";
                if (!File.Exists(backupPath))
                {
                    File.Copy(_legacyConfigurationPath, backupPath);
                    Console.WriteLine($"✓ Backed up old JSON to {backupPath}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Migration error: {ex.Message}");
        }
    }

    // ─── Image Management ───────────────────────────────────────────────────
    private string CopyImageToLocal(string sourcePath, string fallbackPlaceholder)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            return fallbackPlaceholder;

        try
        {
            string imagesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "RonCafeApp", "Images");
            Directory.CreateDirectory(imagesDir);

            string originalName = Path.GetFileNameWithoutExtension(sourcePath);
            string extension = Path.GetExtension(sourcePath);
            string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            string destinationPath = Path.Combine(imagesDir, $"{originalName}_{uniqueId}{extension}");

            File.Copy(sourcePath, destinationPath, true);
            return destinationPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to copy image: {ex.Message}");
            return sourcePath;
        }
    }

    private void DeleteLocalImage(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return;

        try
        {
            string imagesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "RonCafeApp", "Images");

            if (imagePath.StartsWith(imagesDir) && File.Exists(imagePath))
            {
                File.Delete(imagePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not delete image {imagePath}: {ex.Message}");
        }
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
