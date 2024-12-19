using Mono.Cecil;
using MonoMod.Utils;
using System;
using System.Linq;

namespace SMAPIGameLoader.Game.Rewriter;

internal static class StardewRewriterTool
{
    static AssemblyDefinition assemblyDefinition;
    static ModuleDefinition mainModule;
    internal static void Init(AssemblyDefinition stardewAsmDef)
    {
        StardewRewriterTool.assemblyDefinition = stardewAsmDef;
        mainModule = stardewAsmDef.MainModule;
    }
    internal static TypeDefinition FindType(string fname) => mainModule.Types.Single(t => t.FullName == fname);
}
