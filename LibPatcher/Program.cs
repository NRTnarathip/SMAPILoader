using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using System.Security.Cryptography;
using System.Text;

internal class Program
{
    const string PackageDirPath = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Runtime.Mono.android-arm64";
    const string LibFileName = "libmonosgen-2.0.so";
    const string LibFileHashSha256Target = "3a2ae3237b0be6d5ed7c4bda0b2c5fa8b2836a0a6de20fc96b007fb7389571b4";
    static string LibPath = Path.Combine(PackageDirPath, @"8.0.10\runtimes\android-arm64\native", LibFileName);

    private static void Main(string[] args)
    {

        var fileInfo = new FileInfo(LibPath);
        Console.WriteLine($"lib file info: {fileInfo.Length} byte");
        var fileHash = ComputeSHA256(LibPath);
        Console.WriteLine($"hash: {fileHash}");

        if (fileHash != LibFileHashSha256Target)
        {
            Console.WriteLine("file hash not match to: " + LibFileHashSha256Target);
            Exit();
        }

        //ready
        try
        {
            StartPachAll();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        //close
        Exit();
    }
    static ISymbolTable dynamicSymbolTable;
    static IELF reader;
    static Dictionary<string, SymbolEntry<UInt64>> monoMethodMap = new();
    static void StartPachAll()
    {
        Console.WriteLine("Start Patch Lib...");

        var currentLibPath = LibFileName;
        File.Copy(LibPath, currentLibPath, true);
        Console.WriteLine("original hash: " + ComputeSHA256(currentLibPath));

        //setup elf reader
        reader = ELFReader.Load(currentLibPath);
        dynamicSymbolTable = reader.GetSection(".dynsym") as ISymbolTable;
        foreach (SymbolEntry<UInt64> item in dynamicSymbolTable.Entries)
        {
            if (item.Type == SymbolType.Function && item.Name.StartsWith("mono_"))
            {
                monoMethodMap[item.Name] = item;
            }
        }

        //ready patch all

        //1 patch Field Access Exception
        Patch_FieldAccessException();



        var newHash = ComputeSHA256(currentLibPath);
        Console.WriteLine("new lib hash: " + newHash);


        Console.WriteLine("Successfully Patch Lib");
    }
    static ulong GetFunctionOffset(SymbolEntry<UInt64> func)
    {
        return func.Value - func.PointedSection.Offset;
    }
    static byte[] ReadByteArrayFromFunction(SymbolEntry<UInt64> func, int readOffset, int readCount)
    {
        var section = func.PointedSection;
        var funcOffset = GetFunctionOffset(func);
        byte[] sectionData = section.GetContents();

        byte[] result = new byte[readCount];
        Array.Copy(sectionData, (int)funcOffset + readOffset, result, 0, readCount);
        return result;
    }
    static void Patch_FieldAccessException()
    {
        Console.WriteLine($"Start {nameof(Patch_FieldAccessException)}");
        var mono_method_can_access_method = monoMethodMap["mono_method_can_access_method"];
        var bytes = ReadByteArrayFromFunction(mono_method_can_access_method, 0, 16);
        DumpHex(bytes);
    }
    static void DumpHex(byte[] bytes, int start, int length)
    {
        byte[] crop = new byte[length];
        Array.Copy(bytes, start, crop, 0, length);
        DumpHex(crop);
    }
    static void DumpHex(byte[] bytes, int dumpRowLength = 4)
    {
        Console.WriteLine("===== Dump Memory =====");
        int x = 0;
        StringBuilder sb = new();
        for (int i = 0; i < bytes.Length; i++)
        {
            byte value = bytes[i];

            //new line
            if (x == dumpRowLength)
            {
                sb.Append("\n");
                x = 0;
            }

            sb.Append($"{value:X2} ");
            x++;
        }
        Console.WriteLine(sb.ToString());
        Console.WriteLine("===== End Dump Memory =====");
    }

    static void Exit()
    {
        Console.Read();
    }
    public static string ComputeSHA256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        // Compute hash
        byte[] hashBytes = sha256.ComputeHash(fileStream);

        // Convert hash to hex string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}