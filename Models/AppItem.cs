using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;

namespace RonCafeApp.Models;
public class AppItem
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string getExecutionPATH { get; set;  } = string.Empty; 
    public string IconPlaceholder { get; set; } = string.Empty;
    public string CoverArtPlaceholder { get; set; } = string.Empty;
    private bool _categoryLOCKED;

    [JsonIgnore]
    public bool CategoryLocked
    {
        get => _categoryLOCKED;
        set
        {
            if (_categoryLOCKED != value)
            {
                _categoryLOCKED = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CategoryLocked)));
            }
        }
    }
}
