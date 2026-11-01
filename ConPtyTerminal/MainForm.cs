/// <summary>
/// 部品をつなぐだけのウィンドウ。
/// 画面 (TerminalView)・出力の読み込み (ShellReader)・入力の送信 (ShellWriter) は、お互いを知らない。
/// </summary>
public sealed class MainForm : Form
{
    /// <summary>起動するシェル。PowerShell にしたいときは "powershell.exe" にする</summary>
    private const string Shell = "cmd.exe";

    private readonly TerminalView _view = new() { Dock = DockStyle.Fill };
    private readonly TextBox _inputBox = new() { Dock = DockStyle.Fill };
    private readonly Button _sendButton = new() { Text = "送信", Dock = DockStyle.Right, Width = 80 };

    private PseudoConsole? _console;
    private ShellReader? _reader;
    private ShellWriter? _writer;

    public MainForm()
    {
        Text = "ターミナル";
        Width = 1000;
        Height = 600;

        // 画面の下に、入力欄と送信ボタンを置く
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = _inputBox.Height };
        bottom.Controls.Add(_inputBox);
        bottom.Controls.Add(_sendButton);
        Controls.Add(_view);
        Controls.Add(bottom);
        AcceptButton = _sendButton; // 入力欄で Enter を押しても、送信できる

        _view.Ready += OnViewReady;
        _view.InputTyped += text => _writer?.Send(text);          // 画面で打ったキーを、シェルへ
        _view.Resized += (columns, rows) => _console?.Resize(columns, rows);
        _sendButton.Click += (_, _) => SendFromInputBox();

        FormClosing += (_, _) => StopShell();
    }

    /// <summary>画面の準備ができたら、シェルを起動して、読み込みと送信の部品をつなぐ</summary>
    private void OnViewReady(int columns, int rows)
    {
        _console = PseudoConsole.Start(Shell, columns, rows);

        _reader = new ShellReader(_console.Output);
        // 読み込みは別のスレッドで動くので、画面に描くときは、BeginInvoke で画面のスレッドへ戻す
        _reader.OutputReceived += text => BeginInvoke(() => _view.Write(text));
        _reader.Start();

        _writer = new ShellWriter(_console.Input);
    }

    /// <summary>入力欄の文字を、シェルへ送る</summary>
    private void SendFromInputBox()
    {
        _writer?.SendLine(_inputBox.Text);
        _inputBox.Clear();
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
