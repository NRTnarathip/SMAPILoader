using System.IO.Compression;

namespace SMAPIGameLoader.Launcher;

internal static class ZipFileTool
{
    //fix crash File denied
    public static void Extract(ZipArchiveEntry entry, string filePath)
    {
        FileTool.MakeSureFilePath(filePath);
        entry.ExtractToFile(filePath, true);
    }
}
