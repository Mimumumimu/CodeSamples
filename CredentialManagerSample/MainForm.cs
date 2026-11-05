using System.ComponentModel;
using System.Diagnostics;

/// <summary>
/// 部品をつなぐだけのウィンドウ。
/// 接続先 (ConnectionSettingsFile)とパスワード (CredentialStore)は、お互いを知らない。
/// 接続先は JSON のファイルに、パスワードは資格情報マネージャーに、別々に保存する。
/// </summary>
public sealed class MainForm : Form
{
    /// <summary>資格情報マネージャーに、パスワードを入れる名前。ターゲット名と呼ばれる。決めたら変えない。変えると、保存済みの値を読めなくなる</summary>
    private const string PasswordName = "CredentialManagerSample.Database";

    private readonly ConnectionSettingsFile _file = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CredentialManagerSample", "AppSettings.json"));

    private readonly TextBox _server = new() { Dock = DockStyle.Fill, Text = "localhost" };
    private readonly TextBox _database = new() { Dock = DockStyle.Fill, Text = "SampleDb" };
    private readonly TextBox _userName = new() { Dock = DockStyle.Fill, Text = "sample_user" };
    private readonly TextBox _password = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly Label _status = new() { Dock = DockStyle.Fill, AutoSize = false };

    public MainForm()
    {
        Text = "資格情報マネージャーのサンプル";
        Width = 560;
        Height = 330;

        var fields = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 4, Height = 130 };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(fields, 0, "サーバー", _server);
        AddRow(fields, 1, "データベース", _database);
        AddRow(fields, 2, "ユーザー名", _userName);
        AddRow(fields, 3, "パスワード", _password);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 6, 8, 0) };
        buttons.Controls.Add(MakeButton("保存", SaveSettings));
        buttons.Controls.Add(MakeButton("読み込み", LoadSettings));
        buttons.Controls.Add(MakeButton("パスワードを削除", DeletePassword));
        buttons.Controls.Add(MakeButton("資格情報マネージャーを開く", OpenCredentialManager, 190));

        var path = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(8, 0, 8, 0),
            Text = $"サーバー名などの保存先: {_file.Path}\r\nパスワードの保存先: 資格情報マネージャー ({PasswordName})",
        };

        _status.Padding = new Padding(8, 4, 8, 0);

        // 下から順に足す。Dock が Fill のものを、最初に足す
        Controls.Add(_status);
        Controls.Add(path);
        Controls.Add(buttons);
        Controls.Add(fields);
    }

    /// <summary>サーバー名などを JSON に、パスワードを資格情報マネージャーに、保存する</summary>
    private void SaveSettings()
    {
        if (string.IsNullOrEmpty(_password.Text))
        {
            _status.Text = "パスワードが空です。";
            return;
        }

        _file.Save(new ConnectionSettings(_server.Text, _database.Text, _userName.Text));
        CredentialStore.Set(PasswordName, _password.Text);
        _status.Text = "保存しました。";
    }

    /// <summary>JSON と資格情報マネージャーから、読み込む</summary>
    private void LoadSettings()
    {
        var settings = _file.Load();
        if (settings is not null)
        {
            _server.Text = settings.Server;
            _database.Text = settings.Database;
            _userName.Text = settings.UserName;
        }

        var password = CredentialStore.Get(PasswordName);
        _password.Text = password ?? "";
        _status.Text = password is null ? "パスワードは、保存されていません。" : "読み込みました。";
    }

    /// <summary>資格情報マネージャーから、パスワードだけを消す</summary>
    private void DeletePassword()
    {
        _status.Text = CredentialStore.Remove(PasswordName) ? "パスワードを削除しました。" : "パスワードは、保存されていません。";
        _password.Clear();
    }

    /// <summary>Windows の資格情報マネージャーの画面を開く。保存した値を、目で確かめられる</summary>
    private static void OpenCredentialManager()
        => Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.CredentialManager") { UseShellExecute = true });

    /// <summary>ボタンを作る。押したときの処理で、保存の失敗が起きても、画面に出して続ける</summary>
    private Button MakeButton(string text, Action action, int width = 110)
    {
        var button = new Button { Text = text, Width = width, Height = 30 };
        button.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex) when (ex is Win32Exception or IOException)
            {
                _status.Text = "失敗しました。" + ex.Message;
            }
        };
        return button;
    }

    private static void AddRow(TableLayoutPanel table, int row, string caption, Control input)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        table.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) }, 0, row);
        table.Controls.Add(input, 1, row);
    }
}
