using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Launcher;

internal static class ZipFileTool
{
    //fix crash File denied
    public static void Extract(ZipArchiveEntry entry, string filePath)
    {
        FileTool.MakeSureFilePath(filePath);
        if (File.Exists(filePath))
            File.Delete(filePath);
        entry.ExtractToFile(filePath);
    }
}
