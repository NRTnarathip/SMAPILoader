using Android.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader.Tool;

internal static class ActivityTool
{
    public static Activity CurrentActivity { get; private set; }
    public static void Init(Activity activity)
    {
        CurrentActivity = activity;
    }
}
