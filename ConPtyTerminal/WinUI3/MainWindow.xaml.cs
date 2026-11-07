#nullable enable
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace ConPtyTerminalWinUI3;

/// <summary>
/// 部品をつなぐだけのウィンドウ。
/// 画面 (TerminalView)・出力の読み込み (ShellReader)・入力の送信 (ShellWriter) は、お互いを知らない。
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>起動するシェル。PowerShell にしたいときは "powershell.exe" にする</summary>
    private const string Shell = "cmd.exe";

    private readonly TerminalView _view = new();

    private PseudoConsole? _console;
    private ShellReader? _reader;
    private ShellWriter? _writer;

    public MainWindow()
    {
        InitializeComponent();
        Title = "ターミナル";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 640));

        // 名前空間のない部品は XAML から指定しにくいので、Grid にコードで入れる
        TerminalHost.Children.Add(_view);

        _view.Ready += OnViewReady;
        _view.InputTyped += text => _writer?.Send(text);          // 画面で打ったキーを、シェルへ
        _view.Resized += (columns, rows) => _console?.Resize(columns, rows);

        Closed += (_, _) => StopShell();
    }

    /// <summary>画面の準備ができたら、シェルを起動して、読み込みと送信の部品をつなぐ</summary>
    private void OnViewReady(int columns, int rows)
    {
        _console = PseudoConsole.Start(Shell, columns, rows);

        _reader = new ShellReader(_console.Output);
        // 読み込みは別のスレッドで動くので、画面に描くときは、画面のスレッドへ戻す
        _reader.OutputReceived += text => DispatcherQueue.TryEnqueue(() => _view.Write(text));
        _reader.Start();

        _writer = new ShellWriter(_console.Input);
    }

    private void OnSendClick(object sender, RoutedEventArgs e) => SendFromInputBox();

    /// <summary>入力欄で Enter を押しても、送信できる</summary>
    private void OnInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            SendFromInputBox();
            e.Handled = true;
        }
    }

    /// <summary>入力欄の文字を、シェルへ送る</summary>
    private void SendFromInputBox()
    {
        _writer?.SendLine(InputBox.Text);
        InputBox.Text = "";
    }

    /// <summary>シェルを終わらせて、後片付けをする</summary>
    private void StopShell()
    {
        _console?.Close();
        _reader?.Wait(TimeSpan.FromSeconds(1));
        _console?.Dispose();
        _console = null;
    }
}
