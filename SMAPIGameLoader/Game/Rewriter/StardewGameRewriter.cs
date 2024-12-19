using System;
using System.Linq;
using Mono.Cecil;
using System.IO;

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
    public static void Rewrite(AssemblyDefinition assemblyDefinition)
    {

        try
        {
            StardewRewriterTool.Init(assemblyDefinition);
            var stardewModule = assemblyDefinition.MainModule;
            var mainActivityTypeDef = stardewModule.Types.First(t => t.Name == "MainActivity");
            var instance_FieldDef = mainActivityTypeDef.Fields.First(f => f.Name == "instance");
            //change FieldType MainActivity to SMAPIActivity;
            instance_FieldDef.FieldType = stardewModule.ImportReference(typeof(SMAPIActivity));
            TaskTool.NewLine("done change field type MainActivity to SMAPIActivity");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ErrorDialogTool.Show(ex);
        }
    }
}
