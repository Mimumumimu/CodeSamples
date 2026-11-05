# CredentialManagerSample

既存の C# のアプリで、データベースのパスワードを、設定ファイルに書かずに、Windows の資格情報マネージャーに保存するためのサンプルです。  
サーバー名などの、秘密ではない接続先は、JSON のファイルに保存します。  
パスワードだけを、資格情報マネージャーに保存します。  

## 構成

```
CredentialManagerSample/
  Secrets/
    CredentialStore.cs            資格情報マネージャーへの保存・読み込み・削除
  Settings/
    ConnectionSettings.cs         接続先の型。パスワードは持たない
    ConnectionSettingsFile.cs     接続先を JSON に保存して、読み込む
  MainForm.cs                     部品のつなぎ方の見本。保存・読み込み・削除のボタンがある
  Program.cs
```

`Secrets` と `Settings` の 3 ファイルが、部品です。  
2 つのフォルダは、お互いのことを知りません。使いたい部品だけを、コピーして使えます。  

| ファイル | 役割 | 知っていること |
|---|---|---|
| `CredentialStore.cs` | 名前ごとに、文字列を 1 件ずつ、資格情報マネージャーに保存する | Windows の API だけ |
| `ConnectionSettings.cs` | サーバー名・データベース名・ユーザー名を持つ | 何も知らない |
| `ConnectionSettingsFile.cs` | `ConnectionSettings` を JSON のファイルに、壊れない形で保存する | JSON のファイルだけ |

NuGet のパッケージは、1 つも要りません。  

## 仕組み

Windows の API (`advapi32.dll` の `CredWrite`・`CredRead`・`CredDelete`・`CredFree`)を、P/Invoke で呼んでいます。  
構造体の項目の意味と、つまずきやすいところは、記事に書いています。  
`MainForm.cs` に、保存と読み込みを呼ぶ形の見本があります。  

## 見本を動かす

Visual Studio で `CredentialManagerSample.csproj` を開いて、F5 を押します。  
コマンドなら、`CredentialManagerSample` フォルダで、次を実行します。  

```
dotnet run
```

ウィンドウが開きます。  
パスワードを入れて「保存」を押すと、サーバー名などが JSON に、パスワードが資格情報マネージャーに、保存されます。  
「資格情報マネージャーを開く」を押すと、保存された項目を、目で確かめられます。  
「汎用資格情報」の中の `CredentialManagerSample.Database` が、このサンプルの項目です。  

サーバー名などの JSON は、`%LocalAppData%\CredentialManagerSample\AppSettings.json` に作られます。  
試し終わったら、「パスワードを削除」を押して、JSON のフォルダを消してください。  

## 用意するもの

- Windows 11
- .NET 8 以降。動かして確かめたのは、.NET 10 です。

## うまく動かないとき

### 保存すると「スタブは正しくないデータを受信しました。」と出る

パスワードが、長すぎます。  
値は UTF-16 で保存されるので、上限は 1280 文字です。1281 文字以上を保存すると、エラー 1783 になります。  
メッセージからは、長すぎることが分かりません。  
接続文字列を丸ごと保存するのではなく、パスワードだけを保存すると、この上限には当たりにくくなります。  

### 別の Windows アカウントで開くと、保存した値が無い

資格情報マネージャーは、Windows アカウントごとに、別々です。  
別の人が、自分のアカウントで開くと、保存した値は見えません。  
同じ PC の別のアカウントでも、同じです。  

### 保存した値を、同じアカウントの別のプログラムから読める

資格情報マネージャーが防ぐのは、設定ファイルや Git に、平文が残ることです。  
同じ Windows アカウントで動く別のプログラムからは、読めてしまいます。  
マルウェアから守る仕組みではありません。  

## ライセンス

MIT ライセンスです。  
