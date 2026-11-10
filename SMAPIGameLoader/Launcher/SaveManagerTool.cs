using Android.App;
using Android.Provider;
using SMAPIGameLoader.Tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Launcher;

internal static class SaveManager
{
    const string SavesDirName = "Saves";
    public static string GetSavesDir() => Path.Combine(FileTool.ExternalFilesDir, SavesDirName);
    public static void OnClickImportSaveZip(object sender, EventArgs args)
    {
        DialogTool.Show("Save Importer",
            "Importing overwrites farms with the same name. Export your current saves first if you want to keep them.",
            buttonOKName: "Import",
            onClickYes: async () =>
            {
                try
                {
                    await ImportSaveZipInternal();
                }
                catch (Exception ex)
                {
                    ErrorDialogTool.Show(ex);
                }
            },
            onClickCancel: () => { });
    }

    const string SaveGameInfo_FileName = "SaveGameInfo";

    static async Task ImportSaveZipInternal()
    {
        var pick = await FilePickerTool.PickZipFile();
        if (pick is null)
            return;

        if (!pick.FileName.StartsWith(SavesDirName, StringComparison.OrdinalIgnoreCase)
            || !pick.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            DialogTool.Show("Save Importer Error", "Please pick a Saves*.zip file, for example Saves.zip or an exported backup.");
            return;
        }

        //extract it
        HashSet<string> savesImportHashset = new();

        using (var zip = ZipFile.OpenRead(pick.FullPath))
        {
            var savesDirPath = GetSavesDir();
            var savesDirRoot = Path.GetFullPath(savesDirPath) + Path.DirectorySeparatorChar;
            const string SkipEntrySavesName = SavesDirName + "/";
            var entires = zip.Entries.ToArray();
            foreach (var entry in entires)
            {
                //skip entry name folder 'Saves/'
                if (entry.FullName == SkipEntrySavesName)
                    continue;

                if (FileTool.IsEntryDirectory(entry))
                    continue;

                var firstDirName = FileTool.GetFirstDirNameFromFilePath(entry.FullName);
                var currentEntryFilePath = entry.FullName;
                if (firstDirName != null && firstDirName == SavesDirName)
                    currentEntryFilePath = currentEntryFilePath.Remove(0, SkipEntrySavesName.Length);

                var extractFilePath = Path.GetFullPath(Path.Combine(savesDirPath, currentEntryFilePath));
                // a crafted zip ("../Mods/x.dll") must not write outside Saves
                if (!extractFilePath.StartsWith(savesDirRoot, StringComparison.Ordinal))
                    continue;

                ZipFileTool.Extract(entry, extractFilePath);
                var fileInfo = new FileInfo(extractFilePath);
                if (fileInfo.Name == SaveGameInfo_FileName)
                {
                    var parentDir = fileInfo.Directory;
                    savesImportHashset.Add(parentDir.Name);
                }
            }

            //options safety first
            RemoveEmergencySaveFile();
        }

        StringBuilder sbSaveImported = new();
        sbSaveImported.AppendLine($"Successfully import saves {savesImportHashset.Count}");

        {
            int saveIndexCounter = 0;
            foreach (var saveDirName in savesImportHashset)
            {
                saveIndexCounter++;
                sbSaveImported.AppendLine($"[{saveIndexCounter}] {saveDirName}");
            }
        }

        DialogTool.Show("Save Importer", sbSaveImported.ToString());
    }
    public static async void OnClickExportSaves(object sender, EventArgs args)
    {
        try
        {
            var savesDir = GetSavesDir();
            if (!Directory.Exists(savesDir) || !Directory.EnumerateFileSystemEntries(savesDir).Any())
            {
                DialogTool.Show("Save Exporter", "No saves found.");
                return;
            }

            var uri = await FilePickerTool.PickSaveLocation($"{SavesDirName}-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip", "application/zip");
            if (uri is null)
                return;

            await Task.Run(() => WriteSavesZip(savesDir, uri));
            DialogTool.Show("Save Exporter", "Saves exported.");
        }
        catch (Exception ex)
        {
            ErrorDialogTool.Show(ex);
        }
    }

    static void WriteSavesZip(string savesDir, Android.Net.Uri uri)
    {
        var resolver = Application.Context.ContentResolver;
        try
        {
            using var stream = resolver.OpenOutputStream(uri);
            ZipFile.CreateFromDirectory(savesDir, stream, CompressionLevel.Optimal, includeBaseDirectory: true);
        }
        catch
        {
            // a half-written zip must not look like a valid backup; some providers refuse deletes, keep the original error
            try { DocumentsContract.DeleteDocument(resolver, uri); } catch (Exception) { }
            throw;
        }
    }

    const string EMERGENCY_SAVE_FileName = "EMERGENCY_SAVE";
    public static void RemoveEmergencySaveFile()
    {
        string filePath = Path.Combine(GetSavesDir(), EMERGENCY_SAVE_FileName);
        FileTool.Delete(filePath);
    }
}
