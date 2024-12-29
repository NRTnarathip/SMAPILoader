using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Tool;

public static class ArchitectureTool
{
    public static bool IsX86Based()
    {
        var supportedAbis = Android.OS.Build.SupportedAbis;
        return supportedAbis.Any(abi => abi.StartsWith("x86"));
    }
    public static bool IsArm() => IsX86Based() is not true;
}
