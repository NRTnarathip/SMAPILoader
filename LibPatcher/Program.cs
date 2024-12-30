using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using ELFSharp.Utilities;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

internal class Program
{
    const string PackageDirPath = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Runtime.Mono.android-arm64";
    const string LibFileName = "libmonosgen-2.0.so";
    const string LibFileHashSha256Target = "3a2ae3237b0be6d5ed7c4bda0b2c5fa8b2836a0a6de20fc96b007fb7389571b4";
    static string LibPath = Path.Combine(PackageDirPath, @"8.0.10\runtimes\android-arm64\native", LibFileName);

    const string LibModifyOutput = "libmonosgen-2.0-arm64-v8a.so";

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
    static IELF LibReader;
    static Dictionary<string, SymbolEntry<UInt64>> monoMethodMap = new();
    static FileStream LibWriter = null;
    static void StartPachAll()
    {
        Console.WriteLine("Start Patch Lib...");

        var currentOriginalLib = LibFileName;

        //clone temp original
        File.Copy(LibPath, currentOriginalLib, true);
        //clone result output modify lib
        File.Copy(LibPath, LibModifyOutput, true);

        Console.WriteLine("original hash: " + ComputeSHA256(currentOriginalLib));

        //setup elf reader
        LibReader = ELFReader.Load(currentOriginalLib);
        dynamicSymbolTable = LibReader.GetSection(".dynsym") as ISymbolTable;
        foreach (SymbolEntry<UInt64> item in dynamicSymbolTable.Entries)
        {
            if (item.Type == SymbolType.Function && item.Name.StartsWith("mono_"))
            {
                monoMethodMap[item.Name] = item;
            }
        }

        //setup writer
        LibWriter = File.Open(LibModifyOutput, FileMode.Open, FileAccess.ReadWrite);

        //ready patch all

        //1 patch Field Access Exception
        Patch_FieldAccessException();



        //cleanup
        LibWriter.Close();

        var newHash = ComputeSHA256(currentOriginalLib);
        Console.WriteLine("new lib hash: " + newHash);


        Console.WriteLine("Successfully Patch Lib");
    }
    static ulong GetFunctionOffsetVASection(string name) => GetFunctionOffsetVASection(GetFunction(name));
    static ulong GetFunctionOffsetVASection(SymbolEntry<UInt64> func)
    {
        return func.Value - func.PointedSection.Offset;
    }
    static ulong GetFunctionOffsetVAFile(string name)
    {
        var func = GetFunction(name);
        var section = func.PointedSection;
        var offsetOnSection = GetFunctionOffsetVASection(func);

        var headerSize = section.Size;
        var headerOffset = section.Offset;

        return headerOffset + offsetOnSection;
    }
    static byte[] ReadByteArrayFromFunction(SymbolEntry<UInt64> func, int readOffset, int readCount)
    {
        var section = func.PointedSection;
        var funcOffset = GetFunctionOffsetVASection(func);
        byte[] sectionData = section.GetContents();

        byte[] result = new byte[readCount];
        Array.Copy(sectionData, (int)funcOffset + readOffset, result, 0, readCount);
        return result;
    }
    static SymbolEntry<UInt64> GetFunction(string name) => monoMethodMap[name];
    static void Patch_FieldAccessException()
    {
        Console.WriteLine($"Start {nameof(Patch_FieldAccessException)}");
        var mono_method_can_access_method = GetFunctionOffsetVAFile("mono_method_can_access_method");
        var mono_method_can_access_method_full = mono_method_can_access_method + 0x24;
        //try patch function mono_method_can_access_method_full

        //debug print bytes
        var patchTarget = mono_method_can_access_method_full;
        byte[] patchBytes =
        {
            0x1F, 0x20, 0x03, 0xD5,
            0x1F, 0x20, 0x03, 0xD5,
            0x1F, 0x20, 0x03, 0xD5,
            0x1F, 0x20, 0x03, 0xD5,
            0x1F, 0x20, 0x03, 0xD5,
        };

        Console.WriteLine("before patch");
        DumpHex((int)patchTarget, patchBytes.Length);
        //var section = mono_method_can_access_method.PointedSection;
        //test 
        Console.WriteLine();
    }
    static void WriteByteArray(byte[] bytes, long start)
    {
        LibWriter.Seek(start, SeekOrigin.Begin);
        LibWriter.Write(bytes);
    }
    static void Read(byte[] bytes, long start)
    {
        LibWriter.Seek(start, SeekOrigin.Begin);
        LibWriter.Read(bytes);
    }
    static void DumpHex(byte[] bytes, int start, int length)
    {
        byte[] crop = new byte[length];
        Array.Copy(bytes, start, crop, 0, length);
        DumpHex(crop);
    }
    static void DumpHex(int start, int length)
    {
        byte[] bytes = new byte[length];
        Read(bytes, start);
        DumpHex(bytes);
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