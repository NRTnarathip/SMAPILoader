using SMAPIGameLoader.Launcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace SMAPIGameLoader.Tool;

/// <summary>
/// อ่านและแชร์ SMAPI log (อัปโหลดไป smapi.io/log แล้ว copy ลิงก์ไว้ให้แปะใน Discord)
/// เดิมชื่อ LogParser ซึ่งไม่ตรงหน้าที่ (ไม่ได้ parse) จึงเปลี่ยนชื่อเป็น tool ตาม pattern ของโปรเจกต์
/// </summary>
internal static class SmapiLogTool
{
    const string SmapiLogUrl = "https://smapi.io/log";
    const string SmapiLogFileName = "SMAPI-latest.txt";

    // cache ลิงก์ที่แชร์ล่าสุด กันกด Share ซ้ำ ๆ ตอน log ไม่เปลี่ยน (กันสแปม smapi.io)
    const string SharedHashPrefKey = "smapi_log_share_hash";
    const string SharedUrlPrefKey = "smapi_log_share_url";

    public static string GetLogFilePath()
    {
        return Path.Combine(FileTool.ExternalFilesDir, "ErrorLogs", SmapiLogFileName);
    }

    /// <summary>อ่าน log ทั้งไฟล์ คืน false พร้อมเหตุผลถ้าอ่านไม่ได้</summary>
    public static bool TryReadLog(out string content, out string error)
    {
        content = null;
        error = null;

        string logFilePath = GetLogFilePath();
        if (File.Exists(logFilePath) is false)
        {
            error = $"Not found file {logFilePath}";
            return false;
        }

        try
        {
            content = File.ReadAllText(logFilePath);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static void ShareLog()
    {
        TaskTool.Run(ActivityTool.CurrentActivity, async () =>
        {
            try
            {
                TaskTool.SetTitle("SMAPI Log Uploading...");
                await UploadLogAsync();
            }
            catch (Exception ex)
            {
                ErrorDialogTool.Show(ex);
            }
        });
    }

    static async Task<HttpResponseMessage> PostHTTPRequestAsync(this HttpClient client,
      string url, Dictionary<string, string> data)
    {
        using HttpContent formContent = new FormUrlEncodedContent(data);
        return await client.PostAsync(url, formContent).ConfigureAwait(false);
    }

    static async Task UploadLogAsync()
    {
        TaskTool.NewLine("starting task upload log");

        string logFilePath = GetLogFilePath();
        if (File.Exists(logFilePath) is false)
        {
            ErrorDialogTool.Show(new Exception($"Not found file {logFilePath}"), "SMAPI Log Error");
            return;
        }

        TaskTool.NewLine("read log from path: " + logFilePath);
        var logStringContent = File.ReadAllText(logFilePath);
        string logHash = ComputeHash(logStringContent);
        TaskTool.NewLine($"log hash: {logHash}");

        // ไฟล์ไม่เปลี่ยนตั้งแต่แชร์ครั้งก่อน -> ใช้ลิงก์เดิม ไม่ต้องอัปโหลดซ้ำ
        string cachedUrl = Preferences.Get(SharedUrlPrefKey, string.Empty);
        if (cachedUrl.Length > 0 && logHash == Preferences.Get(SharedHashPrefKey, string.Empty))
        {
            TaskTool.NewLine("log unchanged, reuse cached link: " + cachedUrl);
            await PresentLogLinkAsync(cachedUrl, reused: true);
            return;
        }

        using HttpClient client = new();
        client.BaseAddress = new Uri(SmapiLogUrl);
        var fileSize = new FileInfo(logFilePath).Length / 1024f;
        TaskTool.NewLine($"file size: {fileSize:F2}kb");

        TaskTool.NewLine("please wait for uploading..");
        var response = await client.PostHTTPRequestAsync(SmapiLogUrl, new()
        {
            { "input", logStringContent }
        });
        TaskTool.NewLine($"response status code: ${response.StatusCode}");

        if (response.IsSuccessStatusCode is false)
        {
            DialogTool.Show("SMAPI Log Parser", $"respond error code: {response.StatusCode}");
            return;
        }

        var logUrl = response.RequestMessage.RequestUri.ToString();
        Preferences.Set(SharedHashPrefKey, logHash);
        Preferences.Set(SharedUrlPrefKey, logUrl);

        await PresentLogLinkAsync(logUrl, reused: false);
    }

    /// <summary>เขียน clipboard ให้พร้อมแปะใน Discord แล้วบอกผลทาง dialog</summary>
    static async Task PresentLogLinkAsync(string logUrl, bool reused)
    {
        await Clipboard.SetTextAsync("");

        var clipboardString = new StringBuilder();
        clipboardString.AppendLine($"### SMAPI Log Latest");
        clipboardString.AppendLine($"> ## [Click Link Log Here]({logUrl})");
        clipboardString.AppendLine($"### Current App Info");
        clipboardString.AppendLine($"> Game {GameApkTool.CurrentGameVersion}");
        var appBuildCode = ApkTool.LauncherBuildCode;
        clipboardString.AppendLine($"> Launcher {ApkTool.AppVersion} - {appBuildCode}");
        clipboardString.AppendLine($"> SMAPI {SMAPIInstaller.GetCurrentVersion()} - {SMAPIInstaller.GetBuildCode()}");

        await Clipboard.SetTextAsync(clipboardString.ToString());
        DialogTool.Show("SMAPI Log Parser",
            reused
                ? $"log unchanged since last share, reused link {logUrl}"
                : $"uploaded link {logUrl}" +
                  $"\nyou can share link click 'paste' on discord");
    }

    static string ComputeHash(string content)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash);
    }
}
