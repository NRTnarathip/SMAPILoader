using System.Reflection;

namespace SMAPILoaderDLL;
//[HarmonyPatch]
//static class MainActivityPatcher
//{
//    [HarmonyPrefix]
//    [HarmonyPatch(typeof(StardewValley.MainActivity), "OnCreatePartTwo")]
//    static void PrefixOnCreatePartTwo()
//    {
//        Android.Util.Log.Debug("SMAPI-Tag", "On PrefixOnCreatePartTwo");
//    }
//    [HarmonyPostfix]
//    [HarmonyPatch(typeof(StardewValley.MainActivity), "OnCreatePartTwo")]
//    static void PostFixOnCreatePartTwo()
//    {
//        Android.Util.Log.Debug("SMAPI-Tag", "On PostFixOnCreatePartTwo");
//    }
//}
public static class Program
{
    public static void Log(object msg)
    {
        Android.Util.Log.Debug("SMAPI-Tag", msg?.ToString());
    }
    public static void Start()
    {
        Android.Util.Log.Debug("SMAPI-Tag", "Starting SMAPI Loader DLL...");

        try
        {
            StartInternal();
        }
        catch (Exception e)
        {
            Log("Error try Start(); " + e);
        }


        Android.Util.Log.Debug("SMAPI-Tag", "Successfully Start SMAPI Loader");
    }

    static void StartInternal()
    {
        var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Log("current dir: " + currentDir);
        var coreLib = Assembly.LoadFrom(currentDir + "/System.Private.CoreLib.dll");
        coreLib.GetType("");
        AppDomain.CurrentDomain.TypeResolve += CurrentDomain_TypeResolve;
        AppDomain.CurrentDomain.AssemblyResolve += AppDomain_AssemblyResolve;
    }

    private static Assembly? CurrentDomain_TypeResolve(object? sender, ResolveEventArgs args)
    {
        Log("on type resolve");
        return null;
    }

    static Assembly? AppDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
    {
        Log("On AssemblyResolve");
        Log("asem: " + args.Name);

        return null;
    }
}