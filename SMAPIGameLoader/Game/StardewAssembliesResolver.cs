using System;
using Mono.Cecil;

namespace SMAPIGameLoader;


internal class StardewAssembliesResolver : BaseAssemblyResolver
{
    private DefaultAssemblyResolver _defaultResolver;

    public StardewAssembliesResolver()
    {
        _defaultResolver = new DefaultAssemblyResolver();
        _defaultResolver.AddSearchDirectory(GameAssemblyManager.AssembliesDirPath);
        _defaultResolver.AddSearchDirectory(FileTool.ExternalFilesDir);
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name)
    {
        AssemblyDefinition assembly = null;
        try
        {
            assembly = _defaultResolver.Resolve(name);
        }
        catch (AssemblyResolutionException ex)
        {
            Console.WriteLine(ex);
        }
        return assembly;
    }
}
