using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Java.Util.Concurrent;
using SMAPIGameLoader.Tool;

namespace SMAPIGameLoader.Launcher;

internal static class mono_class_from_mono_type_internal_CrashFix
{
    [DllImport("libdl.so")]
    static extern IntPtr dlopen(string filename, int flags);

    [DllImport("libdl.so")]
    static extern nint dlsym(nint handle, string symbol);

    [DllImport("libdl.so")]
    static extern nint dlerror();

    //[DllImport(BypassAccessException.libName, CallingConvention = CallingConvention.Cdecl)]
    //static extern void PatchBytes(IntPtr targetAddress, byte[] bytes, IntPtr bytesLength);

    public static void Apply()
    {
        try
        {
            if (ArchitectureTool.IsX86Based())
            {
                throw new NotImplementedException("can't apply mono_class_from_mono_type_internal_CrashFix on x86_x64");
            }
            else
            {
                Apply_Arm64();
            }
            Console.WriteLine("successfully patch mono_class_from_mono_type_internal_CrashFix");
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }

    static void Apply_Arm64()
    {
        Console.WriteLine("Init mono_class_from_mono_type_internal Crash Fix");

        var libHandle = dlopen("libmonosgen-2.0.so", 0x1);
        unsafe
        {
            Console.WriteLine("try patch disable assert crash mono_class_from_mono_type_internal");
            IntPtr methodAddress = dlsym(libHandle, "mono_class_from_mono_type_internal");
            IntPtr targetAddress = methodAddress + 0x23c;
            Console.WriteLine("try patch target: " + targetAddress);

            //patch code 'g_assert_not_reached ();'

            // to

            //if (currentMonoType != 0)
            //    returnValueKlassType = currentMonoType;

            //return returnValueKlassType;

            // TODO
            //always 'return null';
            //because we not set any variable
            byte[] patchBytes = {
                0x1f ,0x01, 0x00, 0xf1,
                0x20, 0x01, 0x88, 0x9a,
                0xfd, 0x7b, 0xc1, 0xa8,
                0xc0, 0x03, 0x5f, 0xd6,
            };
            Console.WriteLine("try patch bytes...");
            //PatchBytes(targetAddress, patchBytes, patchBytes.Length);
        }
    }
}

