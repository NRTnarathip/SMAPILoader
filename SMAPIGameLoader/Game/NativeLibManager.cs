using LWJGL;
using MonoGame.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Game;

internal static class NativeLibManager
{
    static nint Load_libLZ4()
    {
        nint num = FuncLoader.LoadLibrary("liblwjgl_lz4.so");
        if (num == IntPtr.Zero)
        {
            string folderPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string directoryName = Path.GetDirectoryName(folderPath);
            string libname = Path.Combine(directoryName, "lib", "liblwjgl_lz4.so");
            num = FuncLoader.LoadLibrary(libname);
        }
        return num;
    }
    public static void Loads()
    {
        try
        {
            int b = LZ4.CompressBound(10);
            Console.WriteLine("done setup native libs");
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }
}
