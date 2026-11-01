#nullable enable
using System;
using System.IO;
using System.Text;

/// <summary>
/// シェルへ、文字を送る。シェルには、キー入力として届く。
/// 画面のことも、出力のことも知らない。入力のストリームだけを受け取る。
/// </summary>
public sealed class ShellWriter
{
    private readonly Stream _input;

    /// <param name="input">シェルの入力のストリーム (<see cref="PseudoConsole.Input"/>)</param>
    public ShellWriter(Stream input) => _input = input;

    /// <summary>文字をそのまま送る</summary>
    public void Send(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            _input.Write(Encoding.UTF8.GetBytes(text));
            _input.Flush();
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // シェルが終わったあとの入力は、捨てる
        }
    }

    /// <summary>文字を送って、Enter を押す</summary>
    public void SendLine(string text) => Send(text + "\r");
}
