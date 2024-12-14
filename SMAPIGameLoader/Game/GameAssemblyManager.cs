using Android.App;
using MonoGame.Framework.Utilities;
using SMAPIGameLoader.Game;
using SMAPIGameLoader.Launcher;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Xamarin.Android.AssemblyStore;

namespace SMAPIGameLoader;

internal class GameAssemblyManager
{
    public const string AssembliesDirName = "Stardew Assemblies";
    public static string AssembliesDirPath => Path.Combine(FileTool.ExternalFilesDir, AssembliesDirName);
    public const string StardewDllName = "StardewValley.dll";
    public const string MonoGameDLLFileName = "MonoGame.Framework.dll";
    public static string StardewValleyFilePath => Path.Combine(AssembliesDirPath, StardewDllName);
    public static void VerifyAssemblies()
    {
        Console.WriteLine("Verify Assemblies");
        var assembliesOutputDirPath = AssembliesDirPath;
        Directory.CreateDirectory(assembliesOutputDirPath);

        {
            Console.WriteLine("try clone stardew assemblies");
            //clone dlls Stardew Valley 
            var store = new AssemblyStoreExplorer(StardewApkTool.BaseApkPath, keepStoreInMemory: true);
            foreach (var asm in store.Assemblies)
            {
                asm.ExtractImage(assembliesOutputDirPath);
            }
            Console.WriteLine("done clone stardew assemblies");
        }

        {
            Console.WriteLine("try clone SMAPI Game Loader Assemblies");
            //clone dll & no trimming from this app 
            var appInfo = Application.Context.ApplicationInfo;
            var store = new AssemblyStoreExplorer(appInfo.PublicSourceDir, keepStoreInMemory: true);
            foreach (var asm in store.Assemblies)
            {
                asm.ExtractImage(assembliesOutputDirPath);
            }
            Console.WriteLine("done clone SMAPI Game Loader Assemblies");
        }

        //rewrite MonoGame.Framework
        MonoGameRewriter.Rewrite(Path.Combine(assembliesOutputDirPath, MonoGameDLLFileName));

    }
    public static Assembly LoadAssembly(string dllFileName)
    {
        return Assembly.LoadFrom(Path.Combine(AssembliesDirPath, dllFileName));
    }

    static string LibDirPath => Path.Combine(FileTool.ExternalFilesDir, "lib");
    internal static void VerifyLibs()
    {
        Console.WriteLine("try setup libs");
        //clean lib first
        Console.WriteLine("clean up lib dir");
        if (Directory.Exists(LibDirPath))
            Directory.Delete(LibDirPath, true);
        Console.WriteLine("done setup libs");
    }
}
