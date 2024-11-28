using Android.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Tool;

internal class DialogTool
{
    internal static void Show(string title, string msg)
    {
        TaskTool.RunMainThread(() =>
        {
            var builder = new AlertDialog.Builder(ActivityTool.CurrentActivity);
            builder.SetPositiveButton("OK", (sender, e) =>
            {

            });
            builder.SetMessage(msg);
            builder.SetTitle(title);

            var dialog = builder.Create();
            dialog.Show();
        });
    }
}
