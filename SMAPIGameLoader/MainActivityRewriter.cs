﻿using System;
using System.Linq;
using Mono.Cecil;
using System.IO;

namespace SMAPIGameLoader;

internal static class MainActivityRewriter
{
    public static TypeReference FindType(ModuleDefinition moduleDefinition, Type type)
    {
        return moduleDefinition.Types.First(t => t.FullName == type.FullName);
    }
    public static ReaderParameters MonoCecilReaderConfig = new()
    {
        AssemblyResolver = new CustomResolver(),
    };
    public static ModuleDefinition ReadModule(string filePath)
    {
        return ModuleDefinition.ReadModule(filePath, MonoCecilReaderConfig);
    }
    public static ModuleDefinition ReadModule(Stream stream)
    {
        return ModuleDefinition.ReadModule(stream, MonoCecilReaderConfig);
    }
    public static string ExternalFilesDir => SMAPIActivity.ExternalFilesDir;
    public static void Rewrite(string stardewDllFilePath, out bool isRewrite)
    {
        isRewrite = false;
        using var stardewDllStream = File.Open(stardewDllFilePath, FileMode.Open, FileAccess.ReadWrite);
        var stardewModule = ReadModule(stardewDllStream);
        try
        {
            var mainActivityTypeDef = stardewModule.Types.First(t => t.Name == "MainActivity");
            var instance_FieldDef = mainActivityTypeDef.Fields.First(f => f.Name == "instance");
            //change FieldType MainActivity to SMAPIActivity;
            if (instance_FieldDef.FieldType.Name != typeof(SMAPIActivity).Name)
            {
                instance_FieldDef.FieldType = stardewModule.ImportReference(typeof(SMAPIActivity));
                Console.WriteLine("done change field type MainActivity to SMAPIActivity");
                stardewModule.Write();
                isRewrite = true;
                Console.WriteLine("Successfully Rewrite StardewValley.dll");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}