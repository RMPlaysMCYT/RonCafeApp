using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.IO;

namespace RonCafeApp.Converters;

public class StringToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrWhiteSpace(path))
        {
            try
            {
                if (path.StartsWith("http"))
                {
                    return null;
                }
                if (path.StartsWith("/"))
                {
                    var uri = new Uri($"avares://RonCafeApp{path}");
                    return new Bitmap(AssetLoader.Open(uri));
                }
                if (File.Exists(path))
                {
                    return new Bitmap(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
            }
        }
        return new Bitmap(AssetLoader.Open(new Uri("avares://RonCafeApp/Assets/placeholder.png")));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}