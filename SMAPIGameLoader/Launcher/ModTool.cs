using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Launcher;

internal static class ModTool
{
    const string ModsDirName = "Mods";
    public static string ModsDir { get; } = Path.Combine(FileTool.ExternalFilesDir, ModsDirName);

    public static string ManifiestFileName = "manifest.json";
    public static void FindManifestFile(string rootDirPath, List<string> manifestFiles)
    {
        try
        {
            if (Directory.Exists(rootDirPath) is false)
                return;

            //search current only 1
            var manifestFilePath = Path.Combine(rootDirPath, ManifiestFileName);
            //assert manifest file invalid path
            if (rootDirPath != ModsDir)
            {
                if (Path.Exists(manifestFilePath))
                {
                    manifestFiles.Add(manifestFilePath);
                    return;
                }
            }

            //search with next folder
            var folders = Directory.GetDirectories(rootDirPath);
            foreach (var folderPath in folders)
            {
                FindManifestFile(folderPath, manifestFiles);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

    }
}
