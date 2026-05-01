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
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace RonCafeApp.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly string _configurationPath = "cafeLauncher.json";

    private readonly Dictionary<int, string> _runningProcess = new();
    private readonly ClockDisplay _clockDisplay;

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
        _clockDisplay = new ClockDisplay();

        _clockDisplay.OnMinuteChanged += (now) =>
        {
            CurrentTime = now.ToString("HH:mm:ss");
        };

        _clockDisplay.OnCurfewReached += StopGamesActivity;
        
        LoadConfig();

        if (_clockDisplay.IsCurrentlyInCurfewState())
        {
            foreach (var app in _allApps.Where(a => a.Category == "Games"))
            {
                app.CategoryLocked = true;
            }
        }

// 1. Update Clock UI
        _clockDisplay.OnMinuteChanged += (now) => {
            CurrentTime = now.ToString("hh:mm:ss tt");
        };

        // 2. Show Warning Overlay
        _clockDisplay.OnWarningTriggered += (mins) => {
            Dispatcher.UIThread.Post(() => {
                new RonCafeApp.Views.CurfewWarningWindow().Show();
            });
        };

        // 3. Trigger Curfew (10:00 PM)
        _clockDisplay.OnCurfewReached += () => {
            Dispatcher.UIThread.Post(() => {
                foreach (var app in _allApps.Where(a => a.Category == "Games"))
                {
                    app.CategoryLocked = true;
                }
                KillGamesOnly();
            });
        };

        // 4. --- NEW: Lift Curfew (6:00 AM) ---
        _clockDisplay.OnCurfewLifted += () => {
            Dispatcher.UIThread.Post(() => {
                foreach (var app in _allApps.Where(a => a.Category == "Games"))
                {
                    app.CategoryLocked = false; // Unlock!
                }
            });
        };
        
        FilterApps();
    }

    // ─── App CRUD ────────────────────────────────────────────────────────────
    public void LaunchApp(object? param)
    {
        // Now expects an AppItem instead of a string
        if (param is not AppItem app || string.IsNullOrWhiteSpace(app.getExecutionPATH)) return;

        if (app.CategoryLocked)
        {
            _pendingLockApp = app;
            PasswordScreen(); // Opens your existing password screen
            return;
        }

        ExecuteGameLaunch(app.getExecutionPATH, app.Category);
    }

    private void ExecuteGameLaunch(string execPath, string category)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = execPath,
                WorkingDirectory = Path.GetDirectoryName(execPath),
                UseShellExecute = true
            });

            if (process != null && category == "Games")
            {
                _runningProcess.Add(process.Id, category);
            }
        }
        catch (System.Exception ex) { System.Console.WriteLine($"Could not launch app: {ex.Message}"); }
    }

    public void VerifyAdminPassword(string enteredPassword)
    {
        if (enteredPassword == "admin123" && _pendingLockApp != null)
        {
            ExecuteGameLaunch(_pendingLockApp.getExecutionPATH, _pendingLockApp.Category);
            _pendingLockApp = null;
            CloseSettings(); // Closes the password overlay
        }
    }

    private void KillGamesOnly()
    {
        foreach (var item in _runningProcess.ToList())
        {
            if (item.Value == "Games")
            {
                try
                {
                    var proc = Process.GetProcessById(item.Key);
                    proc.Kill();
                    _runningProcess.Remove(item.Key);
                }
                catch { /* Process might already be closed */ }
            }
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

    public void PasswordScreen() => CurrentPage = new RonCafeApp.Views.PasswordScreen { DataContext = this };
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

        // Handle old plain-array format
        if (json.TrimStart().StartsWith("["))
        {
            _allApps = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListAppItem)
                       ?? new List<AppItem>();
            SaveConfig();
            return;
        }

        var config = JsonSerializer.Deserialize(json, AppJsonContext.Default.LauncherConfig)
                     ?? new LauncherConfig();
        _allApps = config.Apps;
        try
        {
            LauncherBackground = SolidColorBrush.Parse(config.BackgroundColor);
            SidebarBackground  = SolidColorBrush.Parse(config.SidebarColor);
            AccentColor        = SolidColorBrush.Parse(config.AccentColor);
        }
        catch { /* keep defaults */ }
    }

    private void SaveConfig()
    {
        var config = new LauncherConfig
        {
            Apps            = _allApps,
            BackgroundColor = (LauncherBackground as SolidColorBrush)?.Color.ToString() ?? "#1E1E2E",
            SidebarColor    = (SidebarBackground  as SolidColorBrush)?.Color.ToString() ?? "#181825",
            AccentColor     = (AccentColor        as SolidColorBrush)?.Color.ToString() ?? "#89B4FA",
        };
        File.WriteAllText(_configurationPath,
            JsonSerializer.Serialize(config, AppJsonContext.Default.LauncherConfig));
    }


    private void FilterApps()
    {
        DisplayedApps = new ObservableCollection<AppItem>(
            _allApps.Where(a => a.Category == SelectedCategory));
        Notify(nameof(IsAppListEmpty));
    }

    private void Notify(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void StopGamesActivity()
    {
        foreach (var item in _runningProcess.ToList())
        {
            if (item.Value == "Games")
            {
                try
                {
                    var proc = Process.GetProcessById(item.Key);
                    proc.Kill();
                    _runningProcess.Remove(item.Key);
                }
                catch{}
            }
        }
    }
    
    private string _adminPassword = string.Empty;
    public string AdminPasswordInput
    {
        get => _adminPassword;
        set {if (_adminPassword == value) {_adminPassword = value; Notify(nameof(AdminPasswordInput)); }}
    }

    private bool _isTryToOpenSettings = false;
    private string _savedAdminPassword = "abcadmin123";

    public void PromptSettingsPassword()
    {
        _isTryToOpenSettings = true;
        AdminPasswordInput = string.Empty;
        CurrentPage = new RonCafeApp.Views.PasswordScreen{DataContext = this };
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
                _isTryToOpenSettings = true;
                ShowSettings();
            }
            else if (_pendingLockApp != null)
            {
                ExecuteGameLaunch(_pendingLockApp.getExecutionPATH, _pendingLockApp.Category);
                _pendingLockApp = null;
                CloseSettings(); 
            }
        }
        else
        {
            AdminPasswordInput = string.Empty;
        }
    }
}