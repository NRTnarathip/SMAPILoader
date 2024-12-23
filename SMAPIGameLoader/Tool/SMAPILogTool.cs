using SMAPIGameLoader.Launcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Tool;
internal static class SMAPILogTool
{
    static async Task<HttpResponseMessage> PostHTTPRequestAsync(this HttpClient client,
       string url, Dictionary<string, string> data)
    {
        using HttpContent formContent = new FormUrlEncodedContent(data);
        return await client.PostAsync(url, formContent).ConfigureAwait(false);
    }

    const string SMAPILogUrl = "https://smapi.io/log";
    const string SMAPILogFileName = "SMAPI-latest.txt";

    public static async void OnClickUploadLog()
    {
        TaskTool.Run(ActivityTool.CurrentActivity, async () =>
        {
            try
            {
                TaskTool.SetTitle("SMAPI Log Uploading...");
                await TaskUploadLog();
            }
            catch (Exception ex)
            {
                ErrorDialogTool.Show(ex);
            }
        });
    }
    static async Task TaskUploadLog()
    {
        TaskTool.NewLine("starting task upload log");

        string logFilePath = Path.Combine(FileTool.ExternalFilesDir, "ErrorLogs", SMAPILogFileName);
        if (File.Exists(logFilePath) is false)
        {
            ErrorDialogTool.Show(new Exception($"Not found file {logFilePath}"), "SMAPI Log Error");
            return;
        }

        TaskTool.NewLine("read log from path: " + logFilePath);
        using HttpClient client = new();
        client.BaseAddress = new Uri(SMAPILogUrl);
        var logStringContent = File.ReadAllText(logFilePath);
        var fileSize = new FileInfo(logFilePath).Length / 1024f;
        TaskTool.NewLine($"file size: {fileSize:F2}kb");

        TaskTool.NewLine("please wait for uploading..");
        var response = await client.PostHTTPRequestAsync(SMAPILogUrl, new()
        {
            { "input", logStringContent }
        });
        TaskTool.NewLine($"response status code: ${response.StatusCode}");
        await Clipboard.SetTextAsync("");
        if (response.IsSuccessStatusCode)
        {
            var clipboardString = new StringBuilder();

            var logUrl = response.RequestMessage.RequestUri.ToString();

            clipboardString.AppendLine($"### SMAPI Log Latest");
            clipboardString.AppendLine($"> [Click Link Log Here]({logUrl})");
            clipboardString.AppendLine($"> [Click Link Log Here]({logUrl})");
            clipboardString.AppendLine($"### Current App Info");
            clipboardString.AppendLine($"> Game {StardewApkTool.CurrentGameVersion}");
            clipboardString.AppendLine($"> Launcher {ApkTool.AppVersion}");
            clipboardString.AppendLine($"> SMAPI {SMAPIInstaller.GetCurrentVersion()} - {SMAPIInstaller.GetBuildCode()}");

            await Clipboard.SetTextAsync(clipboardString.ToString());
            DialogTool.Show("SMAPI Log Parser",
                $"uploaded link {logUrl}" +
                $"\nyou can share link click 'paste' on discord");
        }
        else
        {
            DialogTool.Show("SMAPI Log Parser", $"respond error code: {response.StatusCode}");
        }
    }
}
