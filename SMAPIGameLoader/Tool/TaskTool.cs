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

    public static void Run(Activity activity, Action func)
    {
        if (IsBusy)
        {
            Console.WriteLine("Warn!!, current task it running, please wait");
            return;
        }

        //ready
        LastActivity = activity;
        LastTask = Task.Run(() =>
        {
            ShowBusyDialog();
            func();
            CloseBusyDialog();
        });
    }

    static void CloseBusyDialog()
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
    static void RunMainThread(Action func)
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
        Console.WriteLine("try set msg: " + msg);
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
            Console.WriteLine("done set msg: " + msg);
        }
    }
    public static void AddNewLine(string msg)
    {
        RunMainThread(() =>
        {
            SetMessage(busyMessage + "\n" + msg);
        });
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

}
