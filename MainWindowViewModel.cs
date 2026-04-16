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
using Avalonia.Platform.Storage;

namespace RonCafeApp.ViewModels;


public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly string _configuationPath = "cafeLauncher.json";

    private string _newAppExecutionPath = string.Empty;

    public bool IsAppListEmpty => DisplayedApps.Count == 0;

    private string _newAppName = string.Empty;
    public string NewAppName
    {
        get => _newAppName;
        set
        {
            if (_newAppName != value)
            {
                _newAppName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewAppName)));
            }
        }
    }


    private string _newIconPlaceHolder = string.Empty;
    public string NewIconPlaceHolder
    {
        get => _newIconPlaceHolder;
        set
        {
            if (_newIconPlaceHolder != value)
            {
                _newIconPlaceHolder = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewIconPlaceHolder)));
            }
        }
    }

    public void LaunchApp(string execPath)
    {
        if (string.IsNullOrWhiteSpace(execPath)) return;

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = execPath,
                WorkingDirectory = System.IO.Path.GetDirectoryName(execPath),
                UseShellExecute = true
            };

            System.Diagnostics.Process.Start(startInfo);
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Could not launch app: {ex.Message}");
        }
    }

    public void AddNewApp()
    {
        if (string.IsNullOrWhiteSpace(NewAppName) || string.IsNullOrWhiteSpace(NewAppExecutionPath))
        {
            return;
        }
        var newApp = new AppItem
        {
            Name = NewAppName,
            Category = SelectedCategory,
            getExecutionPATH = NewAppExecutionPath,
            IconPlaceholder = string.IsNullOrWhiteSpace(NewIconPlaceHolder) ? "/assets/placeholder.png" : NewIconPlaceHolder
        };
        _allApps.Add(newApp);
        SaveApps();
        FilterApps();
        NewAppName = string.Empty;
        NewAppExecutionPath = string.Empty;
        NewIconPlaceHolder = string.Empty;
    }

    public string NewAppExecutionPath
    {
        get => _newAppExecutionPath;
        set
        {
            if (_newAppExecutionPath != value)
            {
                _newAppExecutionPath = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(NewAppExecutionPath)));
            }
        }
    }

    public async void BrowseForExecutable()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow == null) return;

            var topLevel = TopLevel.GetTopLevel(mainWindow);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Game Executable",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Executables") { Patterns = new[] { "*.exe" } } }
            });

            if (files.Count >= 1)
            {
                NewAppExecutionPath = files[0].Path.LocalPath;
            }
        }
    }



    public async void BrowseForIcon()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow == null) return;

            var topLevel = TopLevel.GetTopLevel(mainWindow);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select App Icon",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                new FilePickerFileType("Images") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.ico", "*.webp" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
            });

            if (files.Count >= 1)
            {
                NewIconPlaceHolder = files[0].Path.LocalPath;
            }
        }
    }


    public List<string> Categories { get; } = new()
    {
        "Games",
        "Documents",
        "Programming",
        "Entertainment",
        "Utilities"
    };

    private List<AppItem> _allApps = new();

    private ObservableCollection<AppItem> _displayedApps = new();
    public ObservableCollection<AppItem> DisplayedApps
    {
        get => _displayedApps;
        set
        {
            _displayedApps = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayedApps)));
        }
    }

    private string _selectedCategory = "Games";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory != value)
            {
                _selectedCategory = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCategory)));
                FilterApps();
            }
        }
    }

    public MainWindowViewModel()
    {
        LoadApps();
        FilterApps();
    }

    private void LoadApps()
    {
        if (File.Exists(_configuationPath))
        {
            string json = File.ReadAllText(_configuationPath);
            _allApps = JsonSerializer.Deserialize<List<AppItem>>(json) ?? new List<AppItem>();
        }
        else {
            _allApps = new List<AppItem>();
            SaveApps();
        } 
    }

    private void SaveApps()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(_allApps, options);
        File.WriteAllText(_configuationPath, json);
    }

    private void FilterApps()
    {
        var filtered = _allApps.Where(app => app.Category == SelectedCategory);
        DisplayedApps = new ObservableCollection<AppItem>(filtered);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAppListEmpty)));
    }
}