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

            // actually don't need,
            // because we can use WatcherFactory.ForReferenceList instead ForObservableCollection
            const bool fix_IList_location = false;
            if (fix_IList_location)
            {
                var Game1_TypeDef = stardewModule.Types.First(t => t.Name == "Game1");
                var _locations_FieldDef = Game1_TypeDef.Fields.First(f => f.Name == "_locations");
                var gameLocationType = stardewModule.Types.First(t => t.Name == "GameLocation");
                Console.WriteLine("Game location type: " + gameLocationType.FullName);
                var gameLocationRef = stardewModule.ImportReference(gameLocationType);
                Console.WriteLine("Game location type Import: " + gameLocationRef.FullName);
                //var corlibPath = Path.Combine(GameAssemblyManager.AssembliesDirPath, "System.Private.CoreLib.dll");
                //var corlibAssemblyDef = AssemblyDefinition.ReadAssembly(corlibPath); // หรือ "System.Private.CoreLib.dll"
                //var coreModule = corlibAssemblyDef.MainModule;

                // 2. หา IList`1
                //var ilistType = coreModule.Types
                //    .First(t => t.Namespace == "System.Collections.Generic" && t.Name == "IList`1");
                //var iListRef = stardewModule.ImportReference(ilistType);

                var iListRef = stardewModule.ImportReference(typeof(System.Collections.Generic.IList<>));

                Console.WriteLine("loaded IList import ref : " + iListRef);

                {

                    var IListGameLocation_InstanceType = new GenericInstanceType(iListRef);
                    IListGameLocation_InstanceType.GenericArguments.Add(gameLocationRef);
                    _locations_FieldDef.FieldType = stardewModule.ImportReference(IListGameLocation_InstanceType);
                    TaskTool.NewLine("changed field type List Game1._locations to IList");
                }

                {
                    var locationsGetter = Game1_TypeDef.Properties.Single(p => p.Name == "locations");
                    Console.WriteLine("locations getter: " + locationsGetter);
                    var IListGameLocation_InstanceType = new GenericInstanceType(iListRef);
                    IListGameLocation_InstanceType.GenericArguments.Add(gameLocationRef);
                    locationsGetter.PropertyType = stardewModule.ImportReference(IListGameLocation_InstanceType);
                    locationsGetter.GetMethod.ReturnType = stardewModule.ImportReference(IListGameLocation_InstanceType);
                    Console.WriteLine("changed getter return type: " + locationsGetter);
                }
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
