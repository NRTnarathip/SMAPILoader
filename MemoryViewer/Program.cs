using System.Diagnostics;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        while (true)
        {
            const int refreshTime = 100;
            Thread.Sleep(refreshTime);


            //Update
            DateTime now = DateTime.Now;

            int hours = now.Hour;
            int minutes = now.Minute;
            int seconds = now.Second;
            int millisec = now.Millisecond;

            Log($"Current Time: {hours:D2}:{minutes:D2}:{seconds:D2}:{millisec:D2}");

            Process process = new Process();
            process.StartInfo.FileName = "adb";
            process.StartInfo.Arguments = "shell cat /proc/meminfo";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var MemTotal = lines[0];
            var MemFree = lines[1];
            var MemAvailable = lines[2];
            PrintLineDataFromMemInfo(MemTotal);
            PrintLineDataFromMemInfo(MemFree);
            PrintLineDataFromMemInfo(MemAvailable);


            //Render
            RenderLog();
        }
    }

    static void Log(string msg) => sbLog.AppendLine(msg);
    private static void RenderLog()
    {
        Console.Clear();
        Console.WriteLine(sbLog.ToString());

        sbLog.Clear();
    }

    static StringBuilder sbLog = new();
    static void PrintLineDataFromMemInfo(string lineData)
    {
        var data = lineData.Split(":", StringSplitOptions.TrimEntries);
        int kb = int.Parse(data[1].Replace("kB", "").Trim());
        var varName = data[0];
        sbLog.AppendLine($"{varName}: {kb / (1024f):F3} MB");
    }
}