using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Mod;

internal class AssetsModsManager
{
    static AssetsModsManager Instance;
    public AssetsModsManager()
    {
        Instance = this;
        GameAssetManager.OnOpenStream = GameAssetManager_OnOpenStream;
        Directory.CreateDirectory(AssetsModsDir);
    }
    internal static void Setup()
    {
        new AssetsModsManager();
    }

    public const string AssetsModsFolderName = "Assets Mods";
    static string _assetsModsDir;
    public static string AssetsModsDir
    {
        get
        {
            if (_assetsModsDir == null)
                _assetsModsDir = Path.Combine(FileTool.ExternalFilesDir, AssetsModsFolderName);
            return _assetsModsDir;
        }
    }
    private System.IO.Stream GameAssetManager_OnOpenStream(string assetName)
    {
        try
        {
            string assetFullPath = Path.Combine(AssetsModsDir, assetName);
            Console.WriteLine("try open stream: " + assetName);
            if (File.Exists(assetFullPath) == false)
            {
                return null;
            }

            var stream = File.OpenRead(assetFullPath);
            Console.WriteLine("loaded asset mod: " + assetFullPath);
            return stream;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return null;
    }

}
