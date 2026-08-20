using System;
using Avalonia; // ADD THIS LINE
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using RonCafeApp.Views;

namespace RonCafeApp.Services
{
    public class WindowSelectionService
    {
        private string _selectedWindow = "MainWindow";
        
        public event EventHandler<string>? WindowChanged;
        
        public string SelectedWindow
        {
            get => _selectedWindow;
            set
            {
                if (_selectedWindow != value)
                {
                    _selectedWindow = value;
                    WindowChanged?.Invoke(this, value);
                }
            }
        }

        public void ApplyWindowSelection(string windowType)
        {
            // Store the selection
            SelectedWindow = windowType;
            
            // Switch the window on the UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        var mainWindow = desktop.MainWindow;
                        
                        if (windowType == "MainWindow2")
                        {
                            // Switch to MainWindow2
                            var newWindow = new MainWindow2();
                            desktop.MainWindow = newWindow;
                            newWindow.Show();
                            
                            // Close old window if it exists and is different
                            if (mainWindow != null && mainWindow != newWindow)
                            {
                                mainWindow.Close();
                            }
                            
                            Console.WriteLine($"✅ Switched to MainWindow2");
                        }
                        else // MainWindow (default)
                        {
                            // Switch to MainWindow
                            var newWindow = new MainWindow();
                            desktop.MainWindow = newWindow;
                            newWindow.Show();
                            
                            if (mainWindow != null && mainWindow != newWindow)
                            {
                                mainWindow.Close();
                            }
                            
                            Console.WriteLine($"✅ Switched to MainWindow");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Could not get desktop application lifetime");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error switching window: {ex.Message}");
                }
            });
        }
    }
}