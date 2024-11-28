using HarmonyLib;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader;

[Harmony]
internal class Log
{
    public static void It(string message)
    {
        Console.WriteLine(message);
    }
    public static void Setup()
    {
        var harmony = new Harmony("SMAPIGameLoader");
        var DefaultLogger = typeof(MainActivity).Assembly.GetType("StardewValley.Logging.DefaultLogger");
        var LogImpl = AccessTools.Method(DefaultLogger, "LogImpl");
        harmony.Patch(LogImpl, prefix: AccessTools.Method(typeof(Log), nameof(PrefixLogImpl)));
    }
    static void PrefixLogImpl(string level, string message, Exception exception = null)
    {
        Console.WriteLine($"LogImpl(level: {level}, msg: {message})");
    }
}
