# ConPtyTerminal

既存の C# のアプリに、DOS 窓のようなターミナルを足すためのサンプルです。  
Windows の ConPTY (擬似コンソール) でシェルを動かし、出力を xterm.js で描きます。  

![ターミナルのウィンドウ。上に黒い画面、下に入力欄と送信ボタンがある](screenshot.png)

## 構成

```
ConPtyTerminal/
  Terminal/            自分のアプリにコピーする部品 (4 ファイル)
    PseudoConsole.cs
    ShellReader.cs
    ShellWriter.cs
    TerminalView.cs
  MainForm.cs          部品のつなぎ方の見本。入力欄と送信ボタンがある
  Program.cs
```

`Terminal` フォルダの 4 ファイルが、部品です。  
部品は、お互いのことを知りません。使いたい部品だけを、コピーして使えます。  

| ファイル | 役割 | 知っていること |
|---|---|---|
| `PseudoConsole.cs` | ConPTY でシェルを起動して、入力と出力のストリームを渡す | Windows の API だけ |
| `ShellReader.cs` | シェルの出力を読み続けて、文字列にして知らせる | 出力のストリームだけ |
| `ShellWriter.cs` | シェルへ、文字を送る | 入力のストリームだけ |
| `TerminalView.cs` | xterm.js で、ターミナルの画面を描く | 画面だけ。シェルのことは知らない |

たとえば、画面を持たないアプリから、シェルへコマンドを送りたいだけなら、`PseudoConsole.cs` と `ShellWriter.cs` の 2 つで足ります。  

## 自分のアプリに足す

1. WebView2 のパッケージを入れます。`Microsoft.Web.WebView2` です。
2. `Terminal` フォルダを、自分のプロジェクトのフォルダにコピーします。
3. 自分のフォームに、`MainForm.cs` を見ながら、数行を足します。

詳しくは、記事に書いています。  
https://qiita.com/Mimumumimu/items/faf040f7aacfbe57c966  

## 見本を動かす

Visual Studio で `ConPtyTerminal.csproj` を開いて、F5 を押します。  
コマンドなら、`ConPtyTerminal` フォルダで、次を実行します。  

```
dotnet run
```

ウィンドウが開いて、`cmd.exe` が動きます。  
下の入力欄に `dir` などを打って「送信」を押すと、結果が画面に出ます。  
画面の中を直接クリックして、キーボードで打つこともできます。  

## 用意するもの

- Windows 11
- .NET 8 以降。動かして確かめたのは、.NET 10 です。
- インターネットへの接続。xterm.js を、CDN から読み込みます。
- WebView2 ランタイム。Windows 11 には、最初から入っています。

## うまく動かないとき

### 「スレッド モードを設定してから変更することはできません」と出る

アプリの `Main` に、`[STAThread]` が付いていません。  
WebView2 は、画面を動かすスレッドが STA である必要があります。  
「Windows フォーム アプリ」のテンプレートには、最初から付いています。  

### 画面が真っ黒のまま、何も出ない

xterm.js を、CDN から読み込めていません。  
インターネットに接続できているかを、確かめてください。  

### シェルの出力が、画面に出ない

`PseudoConsole.cs` の `StartfUseStdHandles` の指定が、外れていないかを、確かめてください。  
この指定がないと、このアプリの標準出力がリダイレクトされているときに、シェルの出力が、擬似コンソールに届かないことがあります。  

## ライセンス

MIT ライセンスです。xterm.js も MIT ライセンスです。  
