using Android.App;
using SMAPIGameLoader.Launcher;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader;

internal static class TaskTool
{
    public static Task LastTask { get; private set; }
    public static bool IsBusy => LastTask?.IsCompleted == false;
    public static Activity LastActivity { get; private set; }

    static BusyDialog _busyDialog;
    static bool _failed;

    public static void Run(Activity activity, Func<Task> func)
    {
        if (IsBusy)
        {
            Console.WriteLine("Warn!!, current task is still running, please wait");
            return;
        }

        LastActivity = activity;
        _failed = false;
        LastTask = Task.Run(async () =>
        {
            ShowBusyDialog();
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                Console.WriteLine("task failed: " + ex);
                Fail(ex.Message);
            }

            // ถ้าล้มเหลว ปล่อย dialog ค้างไว้ให้ผู้ใช้ปิดเอง
            if (_failed is false)
                CloseBusyDialog();
        });
    }

    public static void CloseBusyDialog()
    {
        RunMainThread(() =>
        {
            _busyDialog?.Dismiss();
            _busyDialog = null;
        });
    }

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

    public static void NewLine(string msg)
    {
        Console.WriteLine("on TaskTool.NewLine: " + msg);
        RunMainThread(() => _busyDialog?.AppendLine(msg));
    }

    public static void SetTitle(string title)
    {
        RunMainThread(() => _busyDialog?.SetTitleText(title));
    }

    public static void Fail(string message)
    {
        // set ก่อนเพื่อให้ Run() เห็นค่าแล้วไม่ปิด dialog
        _failed = true;
        RunMainThread(() => _busyDialog?.ShowFailed(message));
    }

    static void ShowBusyDialog()
    {
        RunMainThread(() =>
        {
            _busyDialog?.Dismiss();
            _busyDialog = new BusyDialog(LastActivity, "Starting...");
            _busyDialog.Show();
        });
    }
}
