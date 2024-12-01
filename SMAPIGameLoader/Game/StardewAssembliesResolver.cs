extern alias MonoCecilAlias;

using Mono.Cecil;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SMAPIGameLoader;

public class StardewAssembliesResolver : BaseAssemblyResolver
{
    //readonly Dictionary<string, AssemblyDefinition> cache = new(StringComparer.Ordinal);

    public static StardewAssembliesResolver Instance { get; } = new();

    public StardewAssembliesResolver() : base()
    {
        this.AddSearchDirectory(GameAssemblyManager.AssembliesDirPath);
    }

    //protected void RegisterAssembly(AssemblyDefinition assembly)
    //{
    //    if (assembly == null)
    //    {
    //        throw new ArgumentNullException("assembly");
    //    }

    //    string fullName = assembly.Name.FullName;
    //    if (!cache.ContainsKey(fullName))
    //    {
    //        cache[fullName] = assembly;
    //    }
    //}

    //protected override void Dispose(bool disposing)
    //{
    //    foreach (AssemblyDefinition value in cache.Values)
    //    {
    //        value.Dispose();
    //    }

    //    cache.Clear();
    //    base.Dispose(disposing);
    //}

    public override AssemblyDefinition Resolve(AssemblyNameReference name)
    {
        try
        {
            //if (cache.TryGetValue(name.FullName, out var value))
            //{
            //    return value;
            //}

            //value = base.Resolve(name);
            //cache[name.FullName] = value;
            Console.WriteLine("try resolve: " + name.Name);

            //var baseDir = GameAssemblyManager.AssembliesDirPath;
            //var value = AssemblyDefinition.ReadAssembly(Path.Combine(baseDir, name.Name + ".dll"));

            var value = base.Resolve(name);

            Console.WriteLine("resolved asm: " + value);
            return value;
        }
        catch (MonoCecilAlias::Mono.Cecil.AssemblyResolutionException ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }
}
