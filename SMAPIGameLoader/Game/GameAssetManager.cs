
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
        try
        {
            //example: Cotent\\BigCraftables
            assetName = FileTool.SafePath(assetName);

            //load form other stream
            var hookOpenStream = OnOpenStream?.Invoke(assetName);
            if (hookOpenStream != null)
                return hookOpenStream;

            //load vanilla
            string assetFullPath = Path.Combine(GetGameAssetsDir, assetName);
            //Console.WriteLine("on OpenStream: " + assetName);
            return File.OpenRead(assetFullPath);
        }
        catch (Exception ex)
        {
            //Console.WriteLine(ex);
            throw;
        }
    }


    public static void VerifyAssets()
    {
        //check & update game content
        var baseContentApk = StardewApkTool.ContentApkPath;
        using (FileStream apkFileStream = new FileStream(baseContentApk, FileMode.Open, FileAccess.Read))
        using (ZipArchive apkArchive = new ZipArchive(apkFileStream, ZipArchiveMode.Read))
        {
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
                using var destFileStream = new FileStream(destFilePath, FileMode.Create, FileAccess.ReadWrite);
                entryStream.CopyTo(destFileStream);
            }
        }

        Console.WriteLine("successfully verify & clone assets");
        ToastNotifyTool.Notify("Done Setup Assets Content");
    }
}

