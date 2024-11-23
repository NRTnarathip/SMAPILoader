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
    static Toast LastToast;
    public static void Notify(string message, ToastLength duration = ToastLength.Long)
    {
        if (LastToast != null)
            LastToast.Cancel();

        var ctx = Application.Context;
        LastToast = Toast.MakeText(ctx, message, duration);
        LastToast.Show();
    }
}
