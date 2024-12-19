using Android.App;
using Android.Views;
using Android.Widget;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class TaskTool
{
    public static Task LastTask { get; private set; }
    public static bool IsBusy => LastTask?.IsCompleted == false;
    public static Activity LastActivity { get; private set; }
    public static void Run(Activity activity, Func<Task> func)
    {
        if (IsBusy)
        {
            Console.WriteLine("Warn!!, current task it running, please wait");
            return;
        }

        //ready
        LastActivity = activity;
        LastTask = Task.Run(async () =>
        {
            ShowBusyDialog();
            await func();
            CloseBusyDialog();
        });
    }

    public static void CloseBusyDialog()
    {
        if (MainThread.IsMainThread == false)
        {
            RunMainThread(() =>
            {
                CloseBusyDialog();
            });
            return;
        }

        if (busyDialog == null)
            return;

        busyDialog.Dismiss();
        busyDialog = null;
    }
    static AlertDialog busyDialog;
    static string busyMessage;
    public static void RunMainThread(Action func)
    {
        bool doneMainThread = false;
        if (MainThread.IsMainThread == false)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                func();
                doneMainThread = true;
            });
        }
        else
        {
            func();
            doneMainThread = true;
        }

        while (!doneMainThread)
        {

        }
    }
    static void SetMessage(string msg)
    {
        if (MainThread.IsMainThread == false)
        {
            RunMainThread(() =>
            {
                SetMessage(msg);
            });
            return;
        }

        if (busyDialog is not null)
        {
            busyMessage = msg;
            busyDialog.SetMessage(msg);
        }
    }
    public static void NewLine(string msg)
    {
        RunMainThread(() =>
        {
            SetMessage(busyMessage + "\n" + msg);
        });
    }
    public static void SetTitle(string title)
    {

        if (MainThread.IsMainThread == false)
        {
            RunMainThread(() =>
            {
                SetTitle(title);
            });
            return;
        }

        if (busyDialog is null)
            return;
        busyDialog.SetTitle(title);
    }
    static void ShowBusyDialog()
    {
        if (MainThread.IsMainThread == false)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowBusyDialog();
            });
            return;
        }

        CloseBusyDialog();

        Console.WriteLine("try set busy dialog");
        var dialogBuilder = new AlertDialog.Builder(LastActivity);
        busyDialog = dialogBuilder.Create();
        busyDialog.SetTitle("Busy Task");

        SetMessage("please wait...");
        busyDialog.SetCancelable(false);
        busyDialog.Show();
        Console.WriteLine("done set busy dialog");
    }

    internal static void ShowCloseButton(string buttonName = "OK", Action callback = null)
    {
        if (MainThread.IsMainThread == false)
        {
            RunMainThread(() =>
            {
                ShowCloseButton(buttonName, callback);
            });
            return;
        }
    }
}
