
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StardewValley;
using System;
using System.IO;
using System.IO.Compression;
using HarmonyLib;

namespace SMAPIGameLoader;

[HarmonyPatch]
static class GameAssetManager
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TitleContainer), nameof(TitleContainer.OpenStream))]
    static bool PrefixOpenStream(ref Stream __result, string name)
    {
        __result = FixOpenStream(name);
        return false;
    }
    public const string StardewAssetFolderName = "Stardew Assets";
    static string _gameAssetDir = null;
    static string GetGameAssetsDir
    {
        get
        {
            if (_gameAssetDir == null)
                _gameAssetDir = Path.Combine(FileTool.ExternalFilesDir, StardewAssetFolderName);
            return _gameAssetDir;
        }
    }
    public delegate Stream OnOpenStreamDelegate(string assetName);
    public static OnOpenStreamDelegate OnOpenStream;
    static Stream FixOpenStream(string assetName)
    {
        //example: Cotent\BigCraftables
        assetName = assetName.Replace("//", "/"); //safePath
        assetName = assetName.Replace("\\", "/"); //safePath
        try
        {

            //load form other stream
            var hookOpenStream = OnOpenStream?.Invoke(assetName);
            if (hookOpenStream != null)
                return hookOpenStream;

            //load vanila
            var rootDirectory = GetGameAssetsDir;
            string assetAbsolutePath = Path.Combine(rootDirectory, assetName);
            return File.OpenRead(assetAbsolutePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }


    public static void VerifyAssets()
    {
        //check & update game content
        var baseContentApk = ApkTool.ContentApkPath;
        using (FileStream apkFileStream = new FileStream(baseContentApk, FileMode.Open, FileAccess.Read))
        using (ZipArchive apkArchive = new ZipArchive(apkFileStream, ZipArchiveMode.Read))
        {
            //Console.WriteLine("Contents of APK:");
            var externalAssetsDir = Path.Combine(FileTool.ExternalFilesDir, StardewAssetFolderName);
            foreach (ZipArchiveEntry entry in apkArchive.Entries)
            {
                if (entry.FullName.StartsWith("assets/Content") == false)
                    continue;

                //Console.WriteLine($"- {entry.FullName} ({entry.Length} bytes), date: {entry.LastWriteTime}");

                var destFilePath = Path.Combine(externalAssetsDir, entry.FullName.Replace("assets/", ""));
                var destFolderFullPath = Path.GetDirectoryName(destFilePath);
                if (Directory.Exists(destFolderFullPath) == false)
                {
                    Directory.CreateDirectory(destFolderFullPath);
                }
                using var entryStream = entry.Open();
                //if (File.Exists(destFilePath))
                //{
                //check if same file don't clone just skip
                //using var destFileStreamCheck = File.OpenRead(destFilePath);
                //if (FileTool.IsSameFile(entryStream, destFileStreamCheck))
                //{
                //    Console.WriteLine("skip file: " + destFilePath);
                //    continue;
                //}
                //}

                using var destFileStream = new FileStream(destFilePath, FileMode.Create, FileAccess.ReadWrite);
                entryStream.CopyTo(destFileStream);
                //Console.WriteLine("done clone file to: " + destFilePath);
            }
        }

        Console.WriteLine("successfully verify & clone assets");
    }
}

