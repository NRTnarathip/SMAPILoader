using System.Diagnostics;

internal class Program
{
    private static void Main(string[] args)
    {
        while (true)
        {
            Thread.Sleep(500);


            Process process = new Process();
            process.StartInfo.FileName = "adb";
            process.StartInfo.Arguments = "shell cat /proc/meminfo";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            Clear();
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var MemTotal = lines[0];
            var MemFree = lines[1];
            var MemAvailable = lines[2];
            Log(MemTotal);
            Log(MemFree);
            Log(MemAvailable);
        }
    }
    static void Log(string lineData)
    {
        var data = lineData.Split(":", StringSplitOptions.TrimEntries);
        int kb = int.Parse(data[1].Replace("kB", "").Trim());
        var varName = data[0];
        Console.WriteLine($"{varName}: {kb / (1024f):F3} MB");
    }
    static void Clear()
    {
        //Console.Write(new string('\n', Console.WindowHeight));
        //Console.SetCursorPosition(0, 0);
        Console.Clear();
    }
}