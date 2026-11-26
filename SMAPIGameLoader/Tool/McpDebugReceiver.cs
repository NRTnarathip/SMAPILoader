#if DEBUG
using Android.Content;
using SMAPIGameLoader.Launcher;
using System;
using System.Collections.Generic;

namespace SMAPIGameLoader.Tool;

/// <summary>
/// Debug-only bridge ให้ agent/AI สั่งงาน launcher ผ่าน adb:
/// adb shell am broadcast -a app.mcp.CMD --es cmd MCP_GetState
/// ผลลัพธ์คืนทาง broadcast result data
/// </summary>
public class McpDebugReceiver : BroadcastReceiver
{
    public const string Action = "app.mcp.CMD";

    // เพิ่ม tool ใหม่ = เพิ่ม 1 บรรทัดตรงนี้
    static readonly Dictionary<string, Func<string, string>> s_commands = new()
    {
        ["MCP_GoHomePage"] = _ => Navigate(LauncherActivity.HomeId),
        ["MCP_GoModManagerPage"] = _ => Navigate(LauncherActivity.ModManagerId),
        ["MCP_GetState"] = _ => LauncherActivity.Instance.CurrentPageName,
    };

    public override void OnReceive(Context context, Intent intent)
    {
        string cmd = intent.GetStringExtra("cmd");
        string arg = intent.GetStringExtra("arg");

        string result;
        try
        {
            // dynamic receiver นี้ deliver บน main thread จึงเรียก navigation ได้ตรง ๆ
            // (ถ้า tool ไหนกลายเป็น async ให้เปลี่ยนไปใช้ GoAsync + timeout แทน)
            result = cmd is not null && s_commands.TryGetValue(cmd, out var handler)
                ? handler(arg)
                : "error:unknown_cmd";
        }
        catch (Exception ex)
        {
            result = "error:" + ex.Message;
        }

        ResultData = result;
    }

    static string Navigate(int itemId)
    {
        var activity = LauncherActivity.Instance;
        activity.NavigateTo(itemId);
        return activity.CurrentPageName;
    }
}
#endif
