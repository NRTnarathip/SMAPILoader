using System;
using System.IO;
using Xamarin.Android.AssemblyStore;

var blobPath = args[0];
var outputDir = Path.Combine(Path.GetDirectoryName(blobPath)!, "extracted");
Directory.CreateDirectory(outputDir);

using var stream = File.OpenRead(blobPath);
var reader = new AssemblyStoreReader(stream, "arm64-v8a", keepStoreInMemory: true);

Console.WriteLine($"Found {reader.Assemblies.Count} assemblies");
foreach (var asm in reader.Assemblies)
{
    Console.WriteLine($"Extracting: {asm.DllName}");
    asm.ExtractImage(outputDir);
}
Console.WriteLine($"Done! Files in: {outputDir}");
