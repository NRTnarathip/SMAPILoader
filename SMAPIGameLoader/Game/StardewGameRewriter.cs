using System;
using System.Linq;
using Mono.Cecil;
using System.IO;

namespace SMAPIGameLoader;

internal static class StardewGameRewriter
{
    public static TypeReference FindType(ModuleDefinition moduleDefinition, Type type)
    {
        return moduleDefinition.Types.First(t => t.FullName == type.FullName);
    }
    public static ReaderParameters MonoCecilReaderConfig = new()
    {
        AssemblyResolver = StardewAssembliesResolver.Instance,
    };
    public static ModuleDefinition ReadModule(string filePath)
    {
        return ModuleDefinition.ReadModule(filePath, MonoCecilReaderConfig);
    }
    public static ModuleDefinition ReadModule(Stream stream)
    {
        return ModuleDefinition.ReadModule(stream, MonoCecilReaderConfig);
    }
    public static void AddInternalVisableTo(ModuleDefinition module, string visableTo)
    {
        var assembly = module.Assembly;
        var attributeConstructor = assembly.MainModule.ImportReference(
                typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute).GetConstructor(new[] { typeof(string) })
            );
        var customAttribute = new CustomAttribute(attributeConstructor);
        customAttribute.ConstructorArguments.Add(
            new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, visableTo)
        );

        assembly.CustomAttributes.Add(customAttribute);
        Console.WriteLine("Done added InternalsVisibleTo with: " + visableTo);

    }
    public static void Rewrite(string stardewDllFilePath)
    {
        ToastNotifyTool.Notify("Starting Game Rewriter...");

        try
        {
            using var stardewDllStream = File.Open(stardewDllFilePath, FileMode.Open, FileAccess.ReadWrite);
            var stardewModule = ReadModule(stardewDllStream);
            var mainActivityTypeDef = stardewModule.Types.First(t => t.Name == "MainActivity");
            var instance_FieldDef = mainActivityTypeDef.Fields.First(f => f.Name == "instance");
            //change FieldType MainActivity to SMAPIActivity;
            if (instance_FieldDef.FieldType.Name != typeof(SMAPIActivity).Name)
            {
                instance_FieldDef.FieldType = stardewModule.ImportReference(typeof(SMAPIActivity));
                Console.WriteLine("done change field type MainActivity to SMAPIActivity");
                stardewModule.Write();
                Console.WriteLine("Successfully Rewrite StardewValley.dll");
            }

            ToastNotifyTool.Notify("Done Game Rewriter");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ErrorDialogTool.Show(ex);
        }
    }
}
