using System;
using System.Runtime.InteropServices;

namespace ImageOcclusionEditorWinUI3
{
    internal static partial class NativeHelper
    {

        // Win32 MessageBox constants
        public const uint MB_OK = 0x00000000;
        public const uint MB_ICONERROR = 0x00000010;

        [LibraryImport(
            "User32.dll",
            EntryPoint = "MessageBoxW",
            StringMarshalling = StringMarshalling.Utf16)]
        public static partial int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    }
}