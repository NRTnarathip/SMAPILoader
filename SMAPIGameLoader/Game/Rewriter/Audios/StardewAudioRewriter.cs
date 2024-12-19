using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Game.Rewriter;

internal static class StardewAudioRewriter
{
    internal static void Rewrite(AssemblyDefinition stardewAssemblyDef)
    {
        try
        {
        }
        catch (Exception e)
        {
            ErrorDialogTool.Show(e, nameof(StardewAudioRewriter));
            throw;
        }
    }
}
