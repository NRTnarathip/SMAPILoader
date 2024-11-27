using Android.App;
using System;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class ErrorDialogTool
{
    // you will got error when you alert.Show() & Finish() it
    //example case
    // android.view.WindowLeaked: Activity crc644389b739a03c2b33.SMAPIActivity has leaked window DecorView@fd80140[Error Dialog] that was originally added here
    public static void Show(Exception exception, Activity activity)
    {
        if (exception is null)
            return;

        var dialog = new AlertDialog.Builder(activity);
        var alert = dialog.Create();
        alert.SetTitle("Error Dialog");
        alert.SetMessage(exception.ToString());
        alert.SetButton("OK", (c, ev) =>
        {
            // Ok button click task  
        });
        alert.Show();

        Clipboard.SetTextAsync(exception.ToString());
    }
}
