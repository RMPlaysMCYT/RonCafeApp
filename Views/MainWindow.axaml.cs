using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RonCafeApp;

public partial class MainWindow : Window
{
    public bool IsAdminExit { get; set; } = false;
    public MainWindow()
    {
        InitializeComponent();
        this.Closing += MainWindow_Closing;
    }
    
    private void MinimizeButtonClick(object? sender,RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (!IsAdminExit)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;    
        }
    }
    
}