using Android.App;
using Java.Lang;
using MonoGame.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Android.AssemblyStore;

namespace SMAPIGameLoader;

internal class GameAssemblyManager
{
    public const string AssembliesDirName = "Stardew Assemblies";
    public static string AssembliesDirPath => Path.Combine(FileTool.ExternalFilesDir, AssembliesDirName);
    public const string StardewDllName = "StardewValley.dll";
    public const string MonoGameFrameworkDllFileName = "MonoGame.Framework.dll";
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

        SetupLibs();
    }
    const string liblwjgl_lz4Name = "liblwjgl_lz4.so";
    static void SetupLibs()
    {
        Console.WriteLine("try setup libs");
        //clone libs
        //liblwjgl_lz4.so
        using var configApkZip = ZipFile.OpenRead(StardewApkTool.ConfigApkPath);
        var liblwjgl_lz4SO = configApkZip.Entries.Single(lib => lib.Name == liblwjgl_lz4Name);
        Console.WriteLine("lib lwjgl: " + liblwjgl_lz4SO);
        string personalPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        Console.WriteLine("personalPath: " + personalPath);
        string destLib_LWJGL_FilePath = Path.Combine(Path.GetDirectoryName(personalPath), "lib", liblwjgl_lz4Name);
        //create dir path for copy file
        Directory.CreateDirectory(Path.GetDirectoryName(destLib_LWJGL_FilePath));
        Console.WriteLine("try extract to: " + destLib_LWJGL_FilePath);
        liblwjgl_lz4SO.ExtractToFile(destLib_LWJGL_FilePath, true);
        Console.WriteLine("done added lib: " + liblwjgl_lz4SO);


        Console.WriteLine("done setup libs");
    }
    public static Assembly LoadAssembly(string dllFileName)
    {
        return Assembly.LoadFrom(Path.Combine(AssembliesDirPath, dllFileName));
    }
}
