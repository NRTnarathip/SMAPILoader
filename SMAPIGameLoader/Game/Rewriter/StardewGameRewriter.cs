using System;
using System.Linq;
using Mono.Cecil;
using System.IO;
using Android.App;

namespace SMAPIGameLoader.Game.Rewriter;

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
    public static ModuleDefinition ReadModule(Stream stream)
    {
        return ModuleDefinition.ReadModule(stream, MonoCecilReaderConfig);
    }
    public static AssemblyDefinition ReadAssembly(Stream stream)
    {
        return AssemblyDefinition.ReadAssembly(stream, MonoCecilReaderConfig);
    }
    public static void Rewrite(AssemblyDefinition assemblyDefinition)
    {
        try
        {
            StardewRewriterTool.Init(assemblyDefinition);
            var stardewModule = assemblyDefinition.MainModule;
            {
                var mainActivityTypeDef = stardewModule.Types.First(t => t.Name == "MainActivity");
                var instance_FieldDef = mainActivityTypeDef.Fields.First(f => f.Name == "instance");
                //change FieldType MainActivity to SMAPIActivity;
                instance_FieldDef.FieldType = stardewModule.ImportReference(typeof(SMAPIActivity));
                TaskTool.NewLine("changed field type MainActivity to SMAPIActivity");
            }
        
            Console.WriteLine("done Rewrite assembly: " + assemblyDefinition.FullName);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ErrorDialogTool.Show(ex);
        }
    }
}
