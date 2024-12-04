extern alias MonoCecilAlias;

using Mono.Cecil;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SMAPIGameLoader;

public class StardewAssembliesResolver : DefaultAssemblyResolver
{
    public static StardewAssembliesResolver Instance { get; } = new();

    public StardewAssembliesResolver() : base()
    {
        this.AddSearchDirectory(GameAssemblyManager.AssembliesDirPath);
    }
}
