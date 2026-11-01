using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

/// <summary>ConPTY (擬似コンソール) でプロセスを動かす。</summary>
public sealed class PseudoConsole : IDisposable
{
    private readonly IntPtr _console;
    private readonly IntPtr _process;
    private bool _closed;

    /// <summary>シェルへ書き込むストリーム (キー入力)</summary>
    public Stream Input { get; }

    /// <summary>シェルから読み取るストリーム (画面に出る内容)</summary>
    public Stream Output { get; }

    private PseudoConsole(IntPtr console, IntPtr process, Stream input, Stream output)
    {
        _console = console;
        _process = process;
        Input = input;
        Output = output;
    }

    /// <summary>擬似コンソールを作って、コマンドを動かす</summary>
    public static PseudoConsole Start(string commandLine, int columns, int rows)
    {
        // 1. 入力用と出力用のパイプを 1 本ずつ作る
        if (!CreatePipe(out var inputRead, out var inputWrite, IntPtr.Zero, 0)) throw new Win32Exception();
        if (!CreatePipe(out var outputRead, out var outputWrite, IntPtr.Zero, 0)) throw new Win32Exception();

        // 2. 擬似コンソールを作る。シェルは、入力を inputRead から読み、出力を outputWrite へ書く
        var size = new COORD { X = (short)columns, Y = (short)rows };
        Marshal.ThrowExceptionForHR(CreatePseudoConsole(size, inputRead, outputWrite, 0, out var console));

        // 3. パイプの片側は、擬似コンソールが持つので、こちらでは閉じる
        inputRead.Dispose();
        outputWrite.Dispose();

        // 4. 「このプロセスを、擬似コンソールにつないで起動する」という指定を作る
        var listSize = IntPtr.Zero;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref listSize);
        var attributeList = Marshal.AllocHGlobal(listSize);
        if (!InitializeProcThreadAttributeList(attributeList, 1, 0, ref listSize)) throw new Win32Exception();
        if (!UpdateProcThreadAttribute(attributeList, 0, (IntPtr)ProcThreadAttributePseudoConsole,
                console, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero)) throw new Win32Exception();

        var startupInfo = new STARTUPINFOEX { lpAttributeList = attributeList };
        startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
        // このアプリの標準入出力を、シェルに引き継がせない (引き継ぐと、画面に出る内容が擬似コンソールに届かないことがある)
        startupInfo.StartupInfo.dwFlags = StartfUseStdHandles;

        // 5. プロセスを起動する。コマンドラインは、書き換えられることがあるので、StringBuilder で渡す
        var commandLineBuffer = new StringBuilder(commandLine);
        if (!CreateProcessW(null, commandLineBuffer, IntPtr.Zero, IntPtr.Zero, false,
                ExtendedStartupInfoPresent, IntPtr.Zero, null, ref startupInfo, out var processInfo))
        {
            throw new Win32Exception();
        }

        DeleteProcThreadAttributeList(attributeList);
        Marshal.FreeHGlobal(attributeList);
        CloseHandle(processInfo.hThread);

        return new PseudoConsole(
            console,
            processInfo.hProcess,
            new FileStream(inputWrite, FileAccess.Write, 0),
            new FileStream(outputRead, FileAccess.Read, 0));
    }

    /// <summary>シェルが終わるまで待つ</summary>
    public void WaitForExit() => WaitForSingleObject(_process, 0xFFFFFFFF);

    /// <summary>画面の大きさ (桁数と行数) を変える</summary>
    public void Resize(int columns, int rows)
    {
        if (!_closed)
        {
            ResizePseudoConsole(_console, new COORD { X = (short)columns, Y = (short)rows });
        }
    }

    /// <summary>擬似コンソールを閉じる。出力を読み続けたまま呼ぶこと</summary>
    public void Close()
    {
        if (_closed) return;
        _closed = true;

        Input.Dispose();
        // 出力を読み続けていないと、閉じる処理が戻らないことがあるので、別のスレッドで閉じて、3 秒だけ待つ
        var console = _console;
        Task.Run(() => ClosePseudoConsole(console)).Wait(TimeSpan.FromSeconds(3));
    }

    public void Dispose()
    {
        Close();
        Output.Dispose();
        CloseHandle(_process);
    }

    private const uint ExtendedStartupInfoPresent = 0x00080000;
    private const int StartfUseStdHandles = 0x00000100;
    private const int ProcThreadAttributePseudoConsole = 0x00020016;

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, IntPtr attributes, uint size);

    [DllImport("kernel32.dll")]
    private static extern int CreatePseudoConsole(COORD size, SafeFileHandle input, SafeFileHandle output, uint flags, out IntPtr console);

    [DllImport("kernel32.dll")]
    private static extern int ResizePseudoConsole(IntPtr console, COORD size);

    [DllImport("kernel32.dll")]
    private static extern void ClosePseudoConsole(IntPtr console);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref IntPtr size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags, IntPtr attribute, IntPtr value, IntPtr size, IntPtr previous, IntPtr returned);

    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(IntPtr list);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessW(string? applicationName, StringBuilder commandLine, IntPtr processAttributes, IntPtr threadAttributes,
        bool inheritHandles, uint creationFlags, IntPtr environment, string? currentDirectory, ref STARTUPINFOEX startupInfo, out PROCESS_INFORMATION processInfo);

    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);
}
