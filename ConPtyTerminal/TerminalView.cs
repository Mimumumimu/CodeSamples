using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

/// <summary>
/// xterm.js で、ターミナルの画面を描く部品。
/// シェルのことは知らない。文字を渡されたら描き、キーが打たれたら知らせるだけ。
/// </summary>
public sealed class TerminalView : UserControl
{
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };

    /// <summary>画面の準備ができた (桁数, 行数)</summary>
    public event Action<int, int>? Ready;

    /// <summary>画面の中で、キーが打たれた (打たれた文字)</summary>
    public event Action<string>? InputTyped;

    /// <summary>画面の大きさが変わった (桁数, 行数)</summary>
    public event Action<int, int>? Resized;

    public TerminalView()
    {
        Controls.Add(_webView);

        Load += async (_, _) =>
        {
            await _webView.EnsureCoreWebView2Async();
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            _webView.NavigateToString(Html);
        };
    }

    /// <summary>画面に文字を描く。画面を動かすスレッドから呼ぶ</summary>
    public void Write(string text)
    {
        var json = JsonSerializer.Serialize(new { type = "output", data = text });
        _webView.CoreWebView2?.PostWebMessageAsJson(json);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        using var message = JsonDocument.Parse(e.WebMessageAsJson);
        var root = message.RootElement;

        switch (root.GetProperty("type").GetString())
        {
            case "ready":
                Ready?.Invoke(root.GetProperty("cols").GetInt32(), root.GetProperty("rows").GetInt32());
                break;
            case "input":
                InputTyped?.Invoke(root.GetProperty("data").GetString() ?? "");
                break;
            case "resize":
                Resized?.Invoke(root.GetProperty("cols").GetInt32(), root.GetProperty("rows").GetInt32());
                break;
        }
    }

    /// <summary>画面の HTML。xterm.js を、CDN から読み込む</summary>
    private const string Html = """
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8">
          <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@xterm/xterm@5.5.0/css/xterm.css">
          <style>
            html, body { margin: 0; height: 100%; background: #000; overflow: hidden; }
            #terminal { height: 100%; }
          </style>
        </head>
        <body>
          <div id="terminal"></div>
          <script src="https://cdn.jsdelivr.net/npm/@xterm/xterm@5.5.0/lib/xterm.js"></script>
          <script src="https://cdn.jsdelivr.net/npm/@xterm/addon-fit@0.10.0/lib/addon-fit.js"></script>
          <script>
            const host = window.chrome.webview;
            const term = new Terminal({ fontSize: 14 });
            const fit = new FitAddon.FitAddon();
            term.loadAddon(fit);
            term.open(document.getElementById("terminal"));
            fit.fit();

            // 打ったキーを、C# へ送る
            term.onData(data => host.postMessage({ type: "input", data }));
            // 大きさが変わったら、C# へ伝える
            term.onResize(size => host.postMessage({ type: "resize", cols: size.cols, rows: size.rows }));
            window.addEventListener("resize", () => fit.fit());

            // C# から届いた文字を、画面に描く
            host.addEventListener("message", e => {
              if (e.data.type === "output") term.write(e.data.data);
            });

            // 準備ができたことを、C# へ伝える
            host.postMessage({ type: "ready", cols: term.cols, rows: term.rows });
          </script>
        </body>
        </html>
        """;
}
