using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using ELFSharp.Utilities;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

internal class Program
{
    const string PackageDirPath = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Runtime.Mono.android-arm64";
    const string LibSrcFileName = "libmonosgen-2.0.so";
    const string LibFileHashSha256Target = "3a2ae3237b0be6d5ed7c4bda0b2c5fa8b2836a0a6de20fc96b007fb7389571b4";
    static string LibSrcPath = Path.Combine(PackageDirPath, @"8.0.10\runtimes\android-arm64\native", LibSrcFileName);

    const string LibOriginalCopyFileName = "libmonosgen-2.0-original.so";
    static string LibOrigialCopyFilePath = Path.Combine(PackageDirPath, @"8.0.10\runtimes\android-arm64\native",
        LibOriginalCopyFileName);

    const string LibModifyOutputFileName = "libmonosgen-2.0-modify.so";
    const string LibModifyOutputFilePath = LibModifyOutputFileName;

    static void Main(string[] args)
    {

        var fileInfo = new FileInfo(LibSrcPath);

        //ready
        try
        {
            Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        //close
        Exit();
    }
    static void Exit()
    {
        Console.WriteLine("Press Any Key To Exit..");
        Console.Read();
        Environment.Exit(0);
    }

    static ISymbolTable dynamicSymbolTable;
    static IELF LibReader;
    static Dictionary<string, SymbolEntry<UInt64>> monoMethodMap = new();
    static FileStream LibWriter = null;
    static void Run()
    {
        Console.WriteLine("Runing..");

        Console.WriteLine("Start Patch Lib...");

        //clone original first
        if (File.Exists(LibOrigialCopyFilePath) is false)
            File.Copy(LibSrcPath, LibOrigialCopyFilePath);

        //check verify original file hash
        var fileHash = ComputeSHA256(LibOrigialCopyFilePath);
        if (fileHash != LibFileHashSha256Target)
        {
            Console.WriteLine("file hash not match to: " + LibFileHashSha256Target);
            Exit();
        }

        //clone into local app
        //1 lib-modify.so
        File.Copy(LibSrcPath, LibModifyOutputFileName, true);

        Console.WriteLine("original hash: " + ComputeSHA256(LibOrigialCopyFilePath));

        //setup elf reader
        LibReader = ELFReader.Load(LibOrigialCopyFilePath);
        dynamicSymbolTable = LibReader.GetSection(".dynsym") as ISymbolTable;
        foreach (SymbolEntry<UInt64> item in dynamicSymbolTable.Entries)
        {
            if (item.Type == SymbolType.Function && item.Name.StartsWith("mono_"))
            {
                monoMethodMap[item.Name] = item;
            }
        }

        //setup writer
        LibWriter = File.Open(LibModifyOutputFileName, FileMode.Open, FileAccess.ReadWrite);


        //ready patch all
        Patch_FieldAccessException();
        Patch_MethodAccessException();
        Patch_mono_class_from_mono_type_internalCrashFix();


        //cleanup
        LibWriter.Close();
        LibReader.Dispose();
        //close lib original file before delete it
        var newHash = ComputeSHA256(LibModifyOutputFileName);
        Console.WriteLine($"{LibModifyOutputFileName} file hash: " + newHash);

        PostBuild();

        Console.WriteLine("Successfully Patch Lib");
    }

    private static void PostBuild()
    {
        //copy push lib modify into package dir lib
        //error can't access file path
        //need admin permission
        Console.WriteLine("Post Build...");
        Console.WriteLine($"Try copy {LibModifyOutputFilePath} to {LibSrcPath}");
        File.Copy(LibModifyOutputFilePath, LibSrcPath, true);
    }

    static void Patch_FieldAccessException()
    {
        Console.WriteLine($"Start {nameof(Patch_FieldAccessException)}");
        long mono_method_can_access_method = (long)GetFunctionOffsetVAFile("mono_method_can_access_method");
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

        WriteByteArray(patchTarget, patchBytes);

    }
    static void Patch_MethodAccessException()
    {
        Console.WriteLine($"Start {nameof(Patch_MethodAccessException)}");

        long funcAddr = (long)GetFunctionOffsetVAFile("mono_method_can_access_field");
        var patchTarget = funcAddr + 0x120;
        byte[] patchData = { 0x20, 0x00, 0x80, 0x52 };
        WriteByteArray(patchTarget, patchData);


    }
    static void Patch_mono_class_from_mono_type_internalCrashFix()
    {

        Console.WriteLine($"Start {nameof(Patch_mono_class_from_mono_type_internalCrashFix)}");

        long funcAddr = (long)GetFunctionOffsetVAFile("mono_method_can_access_field");
        var patchTarget = funcAddr + 0x23c;
        byte[] patchData =
        {
            0x1f ,0x01, 0x00, 0xf1,
            0x20, 0x01, 0x88, 0x9a,
            0xfd, 0x7b, 0xc1, 0xa8,
            0xc0, 0x03, 0x5f, 0xd6,
        };
        WriteByteArray(patchTarget, patchData);
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
    static void WriteByteArray(long start, byte[] bytes)
    {
        LibWriter.Seek(start, SeekOrigin.Begin);
        LibWriter.Write(bytes);
    }
    static void ReadByteArray(byte[] bytes, long start)
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
        ReadByteArray(bytes, start);
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
    static string ComputeSHA256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        // Compute hash
        byte[] hashBytes = sha256.ComputeHash(fileStream);

        // Convert hash to hex string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}