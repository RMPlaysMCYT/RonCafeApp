using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace RonCafeApp.Views;

public partial class CurfewWarningWindow : Window
{
    public CurfewWarningWindow() => InitializeComponent();
    public void CloseClick(object sender, RoutedEventArgs e) => Close();
}