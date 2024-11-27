using System;
using System.Threading.Tasks;

namespace SMAPIGameLoader;

internal static class TaskTool
{
    public static Task LastTask { get; private set; }
    public static void Run(Action func)
    {
        if (LastTask?.IsCompleted == false)
        {
            Console.WriteLine("Warn!!, current task it running, please wait");
            return;
        }

        LastTask = Task.Run(() =>
        {
            func();
        });
    }
}
