# ADB Command Bridge (Debug Only)

`McpDebugReceiver` lets an external tool or AI agent drive the launcher over `adb` without
touching the screen. It listens for a custom broadcast action (`app.mcp.CMD`) and returns the
current page name as the broadcast result data.

This bridge exists **only in `Dev` builds**. The whole receiver and the registration code are
wrapped in `#if DEBUG`, and the `Dev` configuration defines `DEBUG`
(`<DefineConstants>$(DefineConstants);DEBUG</DefineConstants>`). `Release` builds do not contain it.

## Requirements

1. Build and install a `Dev` APK (one command; auto-detects SDK/JDK/ADB):
   ```powershell
   .\tools.cmd dev
   ```
   Equivalently:
   ```bash
   .\build-dev.cmd
   adb install -r SMAPIGameLoader/bin/Dev/net9.0-android/abc.smapi.gameloader-Signed.apk
   ```
2. Launch the launcher and leave it in the foreground:
   ```bash
   adb shell monkey -p abc.smapi.gameloader -c android.intent.category.LAUNCHER 1
   ```
   or simply open the app on the device.
3. The receiver is registered **dynamically** in `LauncherActivity.OnCreate`, so the activity must
   be alive when the broadcast is sent. Do not broadcast while the app is force-stopped or the
   launcher activity has been finished.

Check that the launcher is the foreground activity:

```bash
adb shell dumpsys activity activities | grep -i ResumedActivity
# ResumedActivity: ActivityRecord{... abc.smapi.gameloader/crc64e91f1276c636690c.LauncherActivity}
```

The real package name is `abc.smapi.gameloader` (from `AndroidManifest.xml`), not the stale
`ApplicationId` in the `.csproj`.

## Commands

| Goal | Command | Expected `data` |
|---|---|---|
| Go to Home | `adb shell am broadcast -a app.mcp.CMD --es cmd MCP_GoHomePage` | `"home"` |
| Go to Mod Manager | `adb shell am broadcast -a app.mcp.CMD --es cmd MCP_GoModManagerPage` | `"mods"` |
| Go to Log | `adb shell am broadcast -a app.mcp.CMD --es cmd MCP_GoLogPage` | `"log"` |
| Go to Contact | `adb shell am broadcast -a app.mcp.CMD --es cmd MCP_GoToContact` | `"contact"` |
| Get current page | `adb shell am broadcast -a app.mcp.CMD --es cmd MCP_GetState` | `"home"`, `"mods"`, `"log"`, `"contact"`, `"settings"` or `"mod-config"` |
| Open a JSON file in the config editor by absolute path (`--es arg <path>`) | `adb shell am broadcast -a app.mcp.CMD --es cmd MCP_OpenConfigFile --es arg /storage/emulated/0/Android/data/abc.smapi.gameloader/files/Mods/<Mod>/config.json` | `"mod-config"` |
| Start the game (same as Play Game) | `adb shell am broadcast -a app.mcp.CMD --es cmd MCP_PlayGame` | `"starting_game"` |
| Unknown command | `adb shell am broadcast -a app.mcp.CMD --es cmd MCP_Whatever` | `"error:unknown_cmd"` |

Example output:

```text
Broadcasting: Intent { act=app.mcp.CMD flg=0x400000 (has extras) }
Broadcast completed: result=0, data="mods"
```

`result=0` means the broadcast completed without a receiver error; the useful value is `data`.
`data` is always the page name **after** navigation, so the response doubles as confirmation.

## Result semantics

- Success: the real current page name — `"home"`, `"mods"`, `"log"`, `"contact"`, `"settings"` or
  `"mod-config"`.
- `MCP_PlayGame`: `"starting_game"` means the launch was kicked off; it returns immediately because
  the game start itself runs in the background (watch logcat or the resumed activity to see it land).
- `MCP_OpenConfigFile`: `"mod-config"` means the editor page opened for the given file;
  `"error:missing_path"` means `--es arg` was empty, `"error:file_not_found"` means the path does not
  exist, `"error:invalid_json"` means the file was read but is not valid JSON (an error dialog is
  shown on the device).
- Unknown or missing `cmd`: `"error:unknown_cmd"`.
- Handler threw an exception: `"error:<exception message>"`.

### `MCP_OpenConfigFile` examples

```bash
# Git Bash on Windows mangles /storage/... paths — disable conversion first
MSYS_NO_PATHCONV=1 adb shell am broadcast -a app.mcp.CMD \
  --es cmd MCP_OpenConfigFile \
  --es arg "/storage/emulated/0/Android/data/abc.smapi.gameloader/files/Mods/Virtual Keyboard/config.json"
```

```text
Broadcasting: Intent { act=app.mcp.CMD flg=0x400000 (has extras) }
Broadcast completed: result=0, data="mod-config"
```

The path must be an absolute path on the device and readable by the app (mod folders under
`/storage/emulated/0/Android/data/abc.smapi.gameloader/files/Mods/` are). The editor opens directly
on the form view (Raw is still available in the header); Back/Discard returns to the Mods tab because
there is no file list to go back to. The UI is built on the main thread, so take a screenshot after
the broadcast to confirm the page rendered.

### `MCP_PlayGame`

`MCP_PlayGame` goes through the same path as the Play Game button (`OnClickStartGame` →
`LauncherActivity.LaunchGameActivityInternal`): it checks the game version and the SMAPI install, runs
`GameCloner.Setup()` and starts `SMAPIActivity`. A Dev build launches the game for real, so the command
is useful for smoke-testing gameplay; the launch runs in the background, hence the immediate
`"starting_game"` reply. Release builds work the same way but are compiled without `DEBUG`, so
`McpDebugReceiver` is not present there at all.

The optional `--es arg <value>` extra is used by `MCP_OpenConfigFile` (absolute JSON path); other
tools ignore it.

## Adding a new tool

Tools are one static dictionary in `SMAPIGameLoader/Tool/McpDebugReceiver.cs`:

```csharp
static readonly Dictionary<string, Func<string, string>> s_commands = new()
{
    ["MCP_GoHomePage"] = _ => Navigate(LauncherActivity.HomeId),
    ["MCP_GoModManagerPage"] = _ => Navigate(LauncherActivity.ModManagerId),
    ["MCP_GetState"] = _ => LauncherActivity.Instance.CurrentPageName,

    // new tool: one line, return value becomes the broadcast result data
    ["MCP_MyCommand"] = arg => MyHandler(arg),
};
```

Rules of thumb:

- `OnReceive` already runs on the main thread, so synchronous navigation is safe. If a handler
  must be asynchronous, switch to `GoAsync()`, finish the `PendingResult` with a 3-second timeout,
  and never use `async void`.
- Keep handlers side-effect-only or return a short, machine-readable string.
- New pages are added in `SMAPIGameLoader/Launcher/Pages/`; expose their id through
  `LauncherActivity` and call `NavigateTo(id)` like the existing tools do.
- Nothing else is required: no manifest entry, no new permission, no HTTP server.

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `Broadcast completed: result=0, data=""` (empty) | No receiver registered — the app is not running, is a `Release` build, or the launcher activity is not alive. Launch the `Dev` APK and keep the launcher open. |
| `data="error:unknown_cmd"` | Typo in the `cmd` value; it is case-sensitive. |
| `MCP_PlayGame` returns `"starting_game"` but the game never opens | The launch failed after the reply — check logcat and the on-device task log for a game-version or missing-SMAPI failure. |
| Broadcast delivered but UI does not change | Confirm the activity is actually in the foreground (see check above); background receivers may be registered but the UI is not visible. |
| Bridge missing after update | You installed a `Release` APK or a build without `DEBUG`; rebuild with `build-dev.cmd`. |
| `java.lang.SecurityException` on API 33+ | The receiver is registered with `ReceiverFlags.Exported`; reinstall the current `Dev` build if you see this. |

## Related files

- `SMAPIGameLoader/Tool/McpDebugReceiver.cs` — receiver + command dictionary.
- `SMAPIGameLoader/Launcher/LauncherActivity.cs` — `RegisterMcpReceiver()` / `UnregisterMcpReceiver()`,
  `NavigateTo(int)`, `ShowModConfig(ModInfo)`, `ShowConfigFile(string)` and `CurrentPageName`.
- `SMAPIGameLoader/Launcher/Pages/ModConfigPage.cs` — config editor page (`OpenFile(path)` opens the
  editor directly, bypassing the JSON file list).
- `docs/building.md` — full build prerequisites.
