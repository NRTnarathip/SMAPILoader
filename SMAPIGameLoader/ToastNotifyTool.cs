using Android.App;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAPIGameLoader;

internal static class ToastNotifyTool
{
    public static void Notify(string message, ToastLength duration = ToastLength.Long)
    {
        var ctx = Application.Context;
        Toast.MakeText(ctx, message, duration).Show();
    }
}
