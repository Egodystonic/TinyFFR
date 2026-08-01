using Avalonia.Controls;

using TinyFFR.Tests.Integrations.Avalonia.ViewModels;

namespace TinyFFR.Tests.Integrations.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        (DataContext as MainViewModel)?.Shutdown();
        base.OnClosing(e);
    }
}
