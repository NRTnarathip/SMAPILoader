﻿using MonoGame.Framework.Utilities;
using Octokit;
using SMAPIGameLoader.Tool;
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
    [DllImport("libdl.so")]
    static extern IntPtr dlopen(string filename, int flags);

    [DllImport("libdl.so")]
    public static extern nint dlsym(nint handle, string symbol);

    [DllImport("libdl.so")]
    public static extern nint dlerror();

    public static void Apply()
    {
        try
        {
            Log("Try Apply BypassAccessException");

            if (ArchitectureTool.IsIntel())
                ApplyInternal_Intel_x64();
            else
                ApplyInternal_Arm64();

            Log("Successfully BypassAccessException");
        }
        catch (Exception e)
        {
            Log(e);
        }

#if false
        //debug only
        DialogTool.Show("Bypass Field & Method AccessException", logLines.ToString());
#endif
    }


    private const int PROT_READ = 0x1;
    private const int PROT_WRITE = 0x2;
    private const int PROT_EXEC = 0x4;
    static StringBuilder logLines = new();
    static void Log(string msg)
    {
        logLines.AppendLine(msg);
    }
    static void Log(System.Exception ex) => Log(ex.ToString());

    [DllImport("libc.so", SetLastError = true)]
    private static extern int mprotect(IntPtr addr, UIntPtr len, int prot);

    [DllImport("libBypassAccessExceptionLib.so")]
    private static extern void ApplyBypass();

    static void ApplyInternal_Arm64()
    {
        Log("Start ApplyInternal_Arm64()");
        ApplyBypass();

        Log("Done ApplyInternal_Arm64()");
        return;

        var libHandle = dlopen("libmonosgen-2.0.so", 0x1);
        unsafe
        {
            IntPtr mono_method_can_access_field = dlsym(libHandle, "mono_method_can_access_field");
            IntPtr targetAddress = mono_method_can_access_field + 0x120;
            Log("try patch mono_method_can_access_field at: " + targetAddress);
            byte[] patchBytes =
            [
                0x20, 0x00, 0x80, 0x52// move w0, 1
            ];
            PatchBytes(targetAddress, patchBytes);
            Console.WriteLine("done patch mono_method_can_access_field");
        }
        unsafe
        {
            IntPtr mono_method_can_access_method_full = dlsym(libHandle, "mono_method_can_access_method") + 0x24;
            Log("start patch mono_method_can_access_method_full: " + mono_method_can_access_method_full);

            var targetAddress = mono_method_can_access_method_full + 0x1C;
            Console.WriteLine("targetAddress: " + targetAddress);
            byte[] patchBytes = [
                //0x1F, 0x11, 0x1E, 0x2E
                0x1F, 0x20, 0x03, 0xD5,
                0x1F, 0x20, 0x03, 0xD5,
                0x1F, 0x20, 0x03, 0xD5,
                0x1F, 0x20, 0x03, 0xD5,
                0x1F, 0x20, 0x03, 0xD5,
            ];

            PatchBytes(targetAddress, patchBytes);
            Console.WriteLine("done patch mono_method_can_access_method_full");
        }
    }

    static void ApplyInternal_Intel_x64()
    {
        var libHandle = dlopen("libmonosgen-2.0.so", 0x1);

        unsafe
        {
            IntPtr mono_method_can_access_field = dlsym(libHandle, "mono_method_can_access_field");
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
            ];

            PatchBytes(targetAddress, patchBytes);
        }

        unsafe
        {
            IntPtr mono_method_can_access_method = dlsym(libHandle, "mono_method_can_access_method");
            IntPtr mono_method_can_access_method_full = mono_method_can_access_method + 0x30;
            Log("start patch mono_method_can_access_method_full: " + mono_method_can_access_method_full);

            var targetAddress = mono_method_can_access_method_full + 0x15;
            byte[] patchBytes = [
                0xEB, 0x09, //jump to return;
            ];
            PatchBytes(targetAddress, patchBytes);
        }
    }
    static void PatchBytes(IntPtr targetAddress, byte[] patchBytes)
    {
        var pageAddress = AlignToPageSize(targetAddress);
        var pageSize = Environment.SystemPageSize;
        int protectResultError = mprotect(pageAddress, (uint)pageSize, PROT_EXEC | PROT_READ | PROT_WRITE);
        if (protectResultError != 0)
        {
            Log("error can't set protect memory at address: " + pageAddress.ToString("X"));
            return;
        }

        Log("try copy bytes hex to address");
        Marshal.Copy(patchBytes, 0, targetAddress, patchBytes.Length);

        Log("done patch bytes at address: " + targetAddress.ToString("X"));
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
