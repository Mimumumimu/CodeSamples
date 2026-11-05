#nullable enable

/// <summary>
/// データベースの接続先。パスワードは含まない。パスワードは、資格情報マネージャーに置く。
/// JSON のファイルに書いても、Git に上げても、困らないものだけを持つ。
/// </summary>
public sealed record ConnectionSettings(string Server, string Database, string UserName);
