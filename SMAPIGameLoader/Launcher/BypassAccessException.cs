using MonoGame.Framework.Utilities;
using Octokit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Android.Net.Wifi.WifiEnterpriseConfig;

namespace SMAPIGameLoader.Launcher;

internal static class BypassAccessException
{
    private const int RTLD_LAZY = 1;

    [DllImport("libdl.so")]
    static extern IntPtr dlopen(string filename, int flags);

    [DllImport("libdl.so")]
    public static extern nint dlsym(nint handle, string symbol);

    [DllImport("libdl.so")]
    public static extern nint dlerror();

    public static void Apply()
    {
        Console.WriteLine("Try Apply BypassAccessException");
        try
        {
            ApplyInternal();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        Console.WriteLine("Successfully BypassAccessException");
    }
    private const int PROT_READ = 0x1;
    private const int PROT_WRITE = 0x2;
    private const int PROT_EXEC = 0x4;

    [DllImport("libc.so", SetLastError = true)]
    private static extern int mprotect(IntPtr addr, UIntPtr len, int prot);


    static void ApplyInternal()
    {
        var libHandle = dlopen("libmonosgen-2.0.so", 0x1);

        IntPtr mono_method_can_access_field = dlsym(libHandle, "mono_method_can_access_field");

        Console.WriteLine("Start Patch mono_method_can_access_field");
        unsafe
        {
            //x64
            IntPtr targetAddress = mono_method_can_access_field + 0x132;
            byte[] patchBytes = [
                //bypass return true
                0xB8, 0x01, 0x00, 0x00, 0x00, // MOV EAX, 1
                0x48, 0x83, 0xC4, 0x58,       // ADD RSP, 0x58
                0x5B,                         // POP RBX
                0x41, 0x5C,                   // POP R12
                0x41, 0x5D,                   // POP R13
                0x41, 0x5E,                   // POP R14
                0x41, 0x5F,                   // POP R15
                0x5D,                         // POP RBP
                0xC3                          // RET

                //original
                //0x44, 0x89 ,0xf0,             // MOV EAX, R14D
                //0x48, 0x83, 0xC4, 0x58,       // ADD RSP, 0x58
                //0x5B,                         // POP RBX
                //0x41, 0x5C,                   // POP R12
                //0x41, 0x5D,                   // POP R13
                //0x41, 0x5E,                   // POP R14
                //0x41, 0x5F,                   // POP R15
                //0x5D,                         // POP RBP
                //0xC3                          // RET
            ];            //add offset into ret

            //patch bypass return true
            Patch(targetAddress, patchBytes);

            //test crash
            //Patch(mono_method_can_access_field, Enumerable.Repeat((byte)0xCC, 0x132).ToArray());
        }
        Console.WriteLine("After patch");
        DumpMemory(mono_method_can_access_field);
    }
    static void Patch(IntPtr targetAddress, byte[] patchBytes)
    {
        var pageAddress = AlignToPageSize(targetAddress);
        var pageSize = Environment.SystemPageSize;
        Console.WriteLine("trying set memory page protection");
        int protectResultError = mprotect(pageAddress, (uint)pageSize, PROT_EXEC | PROT_READ | PROT_WRITE);
        Console.WriteLine("done set memory page protect: " + protectResultError);
        if (protectResultError != 0)
        {
            Console.WriteLine("error can't set protect memory at address: " + pageAddress.ToString("X"));
            return;
        }
        Console.WriteLine("trying patch bytes at: " + targetAddress.ToString("X"));
        Marshal.Copy(patchBytes, 0, targetAddress, patchBytes.Length);
        Console.WriteLine("done patch bytes at address: " + targetAddress.ToString("X"));
    }
    static IntPtr AlignToPageSize(IntPtr address)
    {
        long pageSize = Environment.SystemPageSize;
        return new IntPtr(address.ToInt64() & ~(pageSize - 1));
    }

    static void DumpMemory(nint addressToDump, int dumpLength = 512)
    {
        Console.WriteLine("Dump address: " + addressToDump.ToString("X"));
        nint startAddress = addressToDump;
        byte[] buffer = new byte[dumpLength];
        Marshal.Copy(startAddress, buffer, 0, dumpLength);

        // Print the memory in hex
        for (int i = 0; i < dumpLength; i++)
        {
            if (i % 32 == 0)
            {
                Console.WriteLine();
            }
            Console.Write($"{buffer[i]:X2} ");
        }
        Console.WriteLine();
    }
}
