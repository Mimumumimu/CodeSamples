#nullable enable
using System;
using System.IO;
using System.Text.Json;

/// <summary>
/// <see cref="ConnectionSettings"/> を、JSON のファイルに保存して、読み込む。
/// 保存は、一時ファイルに書いてから置き換えるので、途中で止まっても、ファイルは壊れない。
/// </summary>
public sealed class ConnectionSettingsFile
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _path;

    /// <param name="path">JSON のファイルの場所</param>
    public ConnectionSettingsFile(string path) => _path = path;

    /// <summary>JSON のファイルの場所</summary>
    public string Path => _path;

    /// <summary>読み込む。ファイルが無いときは、null を返す</summary>
    public ConnectionSettings? Load()
    {
        if (!File.Exists(_path)) return null;

        using var stream = File.OpenRead(_path);
        return JsonSerializer.Deserialize<ConnectionSettings>(stream);
    }

    /// <summary>保存する。フォルダが無ければ、作る</summary>
    public void Save(ConnectionSettings settings)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);

        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, Options));
        File.Move(temporary, _path, overwrite: true);
    }
}
