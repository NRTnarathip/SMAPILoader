internal class Program
{
    private static void Main(string[] args)
    {
        var folderPath = @"C:\Users\narat\Desktop\Stardew Valley Android\Mods Download\Stardew Valley - THAI\assets\Content";

        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        var fileTypes = new Dictionary<string, string>();
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            fileTypes[fileInfo.Extension.ToString()] = file;
        }
        foreach (var t in fileTypes)
        {
            Console.WriteLine("all fileType: " + t);
        }
    }
}