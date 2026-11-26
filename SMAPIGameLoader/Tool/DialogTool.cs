using Google.Android.Material.Dialog;
using System;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Tool;

internal class DialogTool
{
    internal static void Show(string title, string msg,
        string buttonOKName = "OK",
        string buttonCancelName = "Cancel",
        Action onClickYes = null, Action onClickCancel = null)
    {
        TaskTool.RunMainThread(() =>
        {
            // MaterialAlertDialogBuilder เพื่อให้ dialog ใช้สีพื้นตามธีม launcher
            var builder = new MaterialAlertDialogBuilder(ActivityTool.CurrentActivity);
            builder.SetPositiveButton(buttonOKName, (sender, e) =>
            {
                onClickYes?.Invoke();
            });

            if (onClickCancel != null)
            {
                builder.SetNegativeButton(buttonCancelName, (sender, e) =>
                {
                    onClickCancel();
                });
            }

            builder.SetMessage(msg);
            builder.SetTitle(title);

            var dialog = builder.Create();
            dialog.Show();
        });
    }
}
