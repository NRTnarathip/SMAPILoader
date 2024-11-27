using Android.App;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class ToastNotifyTool
{
    static Toast LastToast;
    static void NotifyAtMainThread(string message, ToastLength duration = ToastLength.Long)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Notify(message, duration);
        });
    }
    public static void Notify(string message, ToastLength duration = ToastLength.Long)
    {
        if (MainThread.IsMainThread == false)
        {
            NotifyAtMainThread(message, duration);
            return;
        }

        if (LastToast is not null)
            LastToast?.Cancel();

        var ctx = Application.Context;
        LastToast = Toast.MakeText(ctx, message, duration);
        LastToast.Show();
    }
}
