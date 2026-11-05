#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// 文字列を、Windows の資格情報マネージャー (汎用資格情報)に保存する。
/// 名前 (ターゲット名)ごとに、文字列を 1 件ずつ持つ。
/// 保存先は、今の Windows ユーザーの、この PC の中だけ。ほかの PC には、同期しない。
/// </summary>
/// <remarks>
/// 1 件の大きさには、OS の上限がある。値は UTF-16 で保存するので、上限は 1280 文字で、バイトでは 2560 になる。
/// 超えると、保存できずに <see cref="Win32Exception"/> になる。
/// </remarks>
public static class CredentialStore
{
    private const uint TypeGeneric = 1;          // CRED_TYPE_GENERIC
    private const uint PersistLocalMachine = 2;  // CRED_PERSIST_LOCAL_MACHINE
    private const int ErrorNotFound = 1168;      // ERROR_NOT_FOUND

    /// <summary>名前の値を読む。保存されていなければ null を返す</summary>
    /// <exception cref="Win32Exception">保存されていない以外の理由で、読めなかった</exception>
    public static string? Get(string name)
    {
        if (!CredRead(name, TypeGeneric, 0, out var pointer))
        {
            var error = Marshal.GetLastWin32Error();
            return error == ErrorNotFound ? null : throw new Win32Exception(error);
        }

        try
        {
            var credential = Marshal.PtrToStructure<Credential>(pointer);
            return Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
        }
        finally
        {
            CredFree(pointer);
        }
    }

    /// <summary>名前に値を保存する。同じ名前が、すでにあれば、上書きする</summary>
    /// <exception cref="Win32Exception">保存できなかった。値が大きすぎるときも、ここに来る</exception>
    public static void Set(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        var blob = Encoding.Unicode.GetBytes(value);
        var blobPointer = Marshal.AllocHGlobal(blob.Length);
        try
        {
            Marshal.Copy(blob, 0, blobPointer, blob.Length);
            var credential = new Credential
            {
                Type = TypeGeneric,
                TargetName = name,
                UserName = name,
                CredentialBlobSize = (uint)blob.Length,
                CredentialBlob = blobPointer,
                Persist = PersistLocalMachine,
            };
            if (!CredWrite(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            // 平文のコピーを、メモリに残さない
            Array.Clear(blob);
            Marshal.Copy(blob, 0, blobPointer, blob.Length);
            Marshal.FreeHGlobal(blobPointer);
        }
    }

    /// <summary>名前の値を消す。消したら true、もともと無ければ false を返す</summary>
    /// <exception cref="Win32Exception">保存されていない以外の理由で、消せなかった</exception>
    public static bool Remove(string name)
    {
        if (CredDelete(name, TypeGeneric, 0))
        {
            return true;
        }

        var error = Marshal.GetLastWin32Error();
        return error == ErrorNotFound ? false : throw new Win32Exception(error);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public uint Flags;
        public uint Type;
        public string? TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite(ref Credential credential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);
}
