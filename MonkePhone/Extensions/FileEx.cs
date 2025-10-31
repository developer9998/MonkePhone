using System;
using System.Runtime.InteropServices;

namespace MonkePhone.Extensions;

public static class FileEx
{
    private const int FO_DELETE          = 3;
    private const int FOF_ALLOWUNDO      = 0x0040;
    private const int FOF_NOCONFIRMATION = 0x0010;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT fileOp);

    public static bool RecycleFile(this string path)
    {
        SHFILEOPSTRUCT shf = new()
        {
                wFunc  = FO_DELETE,
                pFrom  = path + '\0' + '\0',
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION,
        };

        int result = SHFileOperation(ref shf);

        return result == 0;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint   wFunc;
        public string pFrom;
        public string pTo;
        public ushort fFlags;
        public bool   fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }
}