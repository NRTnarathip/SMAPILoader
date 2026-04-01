using K4os.Compression.LZ4;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Xamarin.Android.AssemblyStore
{
    public class AssemblyStoreAssembly
    {
        public uint DataOffset { get; }
        public uint DataSize { get; }
        public uint DebugDataOffset { get; }
        public uint DebugDataSize { get; }
        public uint ConfigDataOffset { get; }
        public uint ConfigDataSize { get; }

        public uint Hash32 { get; set; }
        public ulong Hash64 { get; set; }
        public string Name { get; set; } = String.Empty;
        public uint RuntimeIndex { get; set; }

        public AssemblyStoreReader Store { get; }
        public string DllName => MakeFileName("dll");
        public string PdbName => MakeFileName("pdb");
        public string ConfigName => MakeFileName("dll.config");

        internal AssemblyStoreAssembly(BinaryReader reader, AssemblyStoreReader store)
        {
            Store = store;

            if (store.Version <= 1)
            {
                // V1: 24 bytes per entry
                DataOffset = reader.ReadUInt32();
                DataSize = reader.ReadUInt32();
                DebugDataOffset = reader.ReadUInt32();
                DebugDataSize = reader.ReadUInt32();
                ConfigDataOffset = reader.ReadUInt32();
                ConfigDataSize = reader.ReadUInt32();
            }
            else
            {
                // V2: 28 bytes per entry — index(4) + offset(4) + size(4) + reserved(16)
                uint index = reader.ReadUInt32();
                DataOffset = reader.ReadUInt32();
                DataSize = reader.ReadUInt32();
                DebugDataOffset = reader.ReadUInt32();
                DebugDataSize = reader.ReadUInt32();
                ConfigDataOffset = reader.ReadUInt32();
                ConfigDataSize = reader.ReadUInt32();
            }
        }

        public void ExtractImage(string outputDirPath, string? fileName = null, bool decompress = true)
        {
            var outputFilePath = MakeOutputFilePath(outputDirPath, "dll", fileName);
            Store.ExtractAssemblyImage(this, outputFilePath);
            if (decompress)
                DecompressDll(outputFilePath);

            // If name was not resolved (no manifest, e.g. v2 format), derive it from the DLL
            if (String.IsNullOrEmpty(Name) && File.Exists(outputFilePath))
            {
                string? derivedName = TryGetAssemblyNameFromDll(outputFilePath);
                if (!String.IsNullOrEmpty(derivedName))
                {
                    Name = derivedName;
                    string newPath = Path.Combine(outputDirPath, $"{derivedName}.dll");
                    if (outputFilePath != newPath)
                    {
                        if (File.Exists(newPath))
                            File.Delete(newPath);
                        File.Move(outputFilePath, newPath);
                    }
                }
            }
        }

        /// <summary>Try to extract the assembly/module name from a .NET DLL's metadata.</summary>
        static string? TryGetAssemblyNameFromDll(string dllPath)
        {
            try
            {
                byte[] data = File.ReadAllBytes(dllPath);
                // Quick check for MZ header
                if (data.Length < 4 || data[0] != 'M' || data[1] != 'Z')
                    return null;

                // Search for .dll name patterns in the metadata string heap
                string text = Encoding.Latin1.GetString(data);
                var match = Regex.Match(text, @"([A-Za-z][A-Za-z0-9_.]+)\.dll");
                return match.Success ? match.Groups[1].Value : null;
            }
            catch
            {
                return null;
            }
        }

        public static void DecompressDll(string path)
        {
            var compressedData = File.ReadAllBytes(path);
            var header = Encoding.ASCII.GetString(compressedData[0..4]);
            if (header != "XALZ")
                return;

            var unpackLength = BitConverter.ToInt32(compressedData[8..12]);
            var payload = compressedData[12..];
            byte[] decompressedData = new byte[unpackLength];
            LZ4Codec.Decode(payload, decompressedData);
            File.WriteAllBytes(path, decompressedData);
            //Console.WriteLine($"Decompressed LZ4: {path}");
        }

        public void ExtractImage(Stream output)
        {
            Store.ExtractAssemblyImage(this, output);
        }

        public void ExtractDebugData(string outputDirPath, string? fileName = null)
        {
            Store.ExtractAssemblyDebugData(this, MakeOutputFilePath(outputDirPath, "pdb", fileName));
        }

        public void ExtractDebugData(Stream output)
        {
            Store.ExtractAssemblyDebugData(this, output);
        }

        public void ExtractConfig(string outputDirPath, string? fileName = null)
        {
            Store.ExtractAssemblyConfig(this, MakeOutputFilePath(outputDirPath, "dll.config", fileName));
        }

        public void ExtractConfig(Stream output)
        {
            Store.ExtractAssemblyConfig(this, output);
        }

        string MakeOutputFilePath(string outputDirPath, string extension, string? fileName)
        {
            return Path.Combine(outputDirPath, MakeFileName(extension, fileName));
        }

        string MakeFileName(string extension, string? fileName = null)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                fileName = Name;

                if (String.IsNullOrEmpty(fileName))
                {
                    fileName = $"{Hash32:x}_{Hash64:x}";
                }

                fileName = $"{fileName}.{extension}";
            }

            return fileName!;
        }
    }
}
