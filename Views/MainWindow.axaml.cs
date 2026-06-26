using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RonCafeApp;

public partial class MainWindow : Window
{
    public bool IsAdminExit { get; set; } = false;
    public MainWindow()
    {
        InitializeComponent();
        this.Closing += MainWindow_Closing
    }
}