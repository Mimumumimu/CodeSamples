#nullable enable
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// シェルの出力を読み続けて、文字列にして知らせる。
/// 画面のことも、入力のことも知らない。出力のストリームだけを受け取る。
/// </summary>
public sealed class ShellReader
{
    private readonly Stream _output;
    private Task? _task;

    /// <summary>シェルの出力を受け取った。画面を動かすスレッドとは別のスレッドから呼ばれる</summary>
    public event Action<string>? OutputReceived;

    /// <param name="output">シェルの出力のストリーム (<see cref="PseudoConsole.Output"/>)</param>
    public ShellReader(Stream output) => _output = output;

    /// <summary>読み取りを始める</summary>
    public void Start() => _task = Task.Run(ReadLoop);

    /// <summary>読み取りが終わるのを、最大で指定した時間だけ待つ</summary>
    public void Wait(TimeSpan timeout) => _task?.Wait(timeout);

    private void ReadLoop()
    {
        var buffer = new byte[16 * 1024];
        var chars = new char[Encoding.UTF8.GetMaxCharCount(buffer.Length)];
        // 日本語などが、読み取りの切れ目で分かれても壊れないよう、デコーダーを使い回す
        var decoder = Encoding.UTF8.GetDecoder();

        try
        {
            int read;
            while ((read = _output.Read(buffer)) > 0)
            {
                var count = decoder.GetChars(buffer, 0, read, chars, 0);
                if (count > 0)
                {
                    OutputReceived?.Invoke(new string(chars, 0, count));
                }
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // シェルが終わって、パイプが閉じられた
        }
    }
}
