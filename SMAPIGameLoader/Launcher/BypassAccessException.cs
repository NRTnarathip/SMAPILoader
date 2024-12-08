using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Launcher;

internal static class BypassAccessException
{
    public static void Apply()
    {
        Console.WriteLine("Try Apply BypassAccessException");
        try
        {
            ApplyInternal();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        Console.WriteLine("Successfully BypassAccessException");
    }

    static void ApplyInternal()
    {

    }
}
