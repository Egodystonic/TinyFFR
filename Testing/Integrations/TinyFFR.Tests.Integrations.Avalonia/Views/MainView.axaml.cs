using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using TinyFFR.Tests.Integrations.Avalonia.ViewModels;

namespace TinyFFR.Tests.Integrations.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        // The scene view is the element whose input events feed TinyFFR's ILatestInputRetriever
        DataContextChanged += (_, _) => (DataContext as MainViewModel)?.SetInputSource(SceneView);
    }
}
