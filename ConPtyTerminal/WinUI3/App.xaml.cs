#nullable enable
using Microsoft.UI.Xaml;

namespace ConPtyTerminalWinUI3;

/// <summary>アプリの入口。メインウィンドウを 1 つ開く</summary>
public partial class App : Application
{
    private Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
