using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace RonCafeApp.Models;

public class AppItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // Database ID
    public int Id { get; set; }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                Notify(nameof(Name));
            }
        }
    }

    private string _category = string.Empty;
    public string Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                Notify(nameof(Category));
            }
        }
    }

    private string _executionPath = string.Empty;
    public string getExecutionPATH // Keep for backward compatibility
    {
        get => _executionPath;
        set
        {
            if (_executionPath != value)
            {
                _executionPath = value;
                Notify(nameof(getExecutionPATH));
            }
        }
    }

    private string _iconPlaceholder = string.Empty;
    public string IconPlaceholder
    {
        get => _iconPlaceholder;
        set
        {
            if (_iconPlaceholder != value)
            {
                _iconPlaceholder = value;
                Notify(nameof(IconPlaceholder));
            }
        }
    }

    private string _coverArtPlaceholder = string.Empty;
    public string CoverArtPlaceholder
    {
        get => _coverArtPlaceholder;
        set
        {
            if (_coverArtPlaceholder != value)
            {
                _coverArtPlaceholder = value;
                Notify(nameof(CoverArtPlaceholder));
            }
        }
    }
    
    // Add these missing properties
    public DateTime? LastModified { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    private bool _categoryLocked;

    [JsonIgnore]
    public bool CategoryLocked
    {
        get => _categoryLocked;
        set
        {
            if (_categoryLocked != value)
            {
                _categoryLocked = value;
                Notify(nameof(CategoryLocked));
            }
        }
    }

    private void Notify(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public override string ToString() => Name;
}
