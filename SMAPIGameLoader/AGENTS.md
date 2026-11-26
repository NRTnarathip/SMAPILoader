# AGENTS.md — SMAPIGameLoader (Android App)

ไฟล์นี้เป็นคำสั่งเฉพาะโปรเจกต์ `SMAPIGameLoader/` เสริมจาก `../AGENTS.md` ที่ root
เป้าหมาย: พัฒนา UI/ฟีเจอร์ของ launcher แล้วตรวจผลบนอุปกรณ์จริงผ่าน **mobile-mcp** เท่านั้น

## 1. กฎหลัก: ห้ามใช้ Visual Studio debug / hot reload

- ห้ามใช้ Visual Studio debugger, `devenv`, Hot Reload, `dotnet watch`
- ห้าม deploy แบบ fast deployment (`-t:Install`, `AndroidFastDeploymentType=Assemblies`)
- วงจรที่ใช้เท่านั้น: **build APK ที่ฝัง assemblies → install → launch → ตรวจด้วย mobile-mcp**
- อย่าเปิด/แก้ solution ใน Visual Studio ระหว่าง build เพราะ VS จะ lock ไฟล์ใน `obj/`
  ทำให้ build ล้มด้วย `XARDF7024: ... Java.Interop.dll ... being used by another process`

## 2. Environment (ตรวจก่อนทุกครั้ง)

- Device: เรียก `mobile_list_available_devices` ก่อน; ได้ `id` มาแล้วส่งเป็น `device`
  ให้ทุก tool call (ถ้ามีเครื่องเดียว mobile-mcp ก็ยังต้องส่ง `device` ทุกครั้ง)
- Package name จริงของแอป: **`abc.smapi.gameloader`**
  (มาจาก `SMAPIGameLoader/AndroidManifest.xml` — ไม่ใช่ `ApplicationId` ใน csproj)
- APK output: `SMAPIGameLoader/bin/<Config>/net9.0-android/abc.smapi.gameloader-Signed.apk`
- `mobile-mcp` ตั้งค่าไว้ที่ระดับ user (ไม่ต้องตั้งใน `.zcode/config.json` ของ repo)
  ไม่มี `start_session` แล้ว — tool เรียก ADB ให้เองทุกครั้ง
- คำสั่งที่ mobile-mcp ไม่มีให้ (เช่น `ui_dump` แบบดิบ, `adb pull`, `logcat` ที่ต้องการ format เฉพาะ)
  ให้เรียก `adb` CLI ตรง ๆ
- Build prerequisites: .NET 9 SDK + android workload, JDK 17+ (เครื่องนี้ใช้ JDK 21 ได้),
  Android SDK, game DLLs ที่ `../SMAPI-Android-1.6/src/DependenciesDll/`

## 3. Build (ไม่มี hot reload)

**ใช้ config `Dev` สำหรับงาน dev ทุกงาน** — เร็วที่สุดและ APK รันได้จริงบนเครื่อง
(arm64 เดียว, assembly store, ไม่ trim/AOT/analyzer, pdb ไม่ถูกยัดเข้า APK)
`Dev` คือ debug mode หลัก (define `DEBUG`) แทน `Debug` เดิมที่เลิกใช้แล้ว

```powershell
# ทางลัด (auto-detect SDK/JDK/ADB ให้)
.\tools.cmd dev              # build Dev แล้วติดตั้งลงเครื่อง
.\tools.cmd build dev        # build อย่างเดียว
.\tools.cmd install dev      # ติดตั้ง APK ที่ build ไว้แล้ว
.\tools.cmd release          # build Release แล้วติดตั้ง
.\tools.cmd help             # ดูคำสั่งทั้งหมด

.\build-dev.cmd              # เหมือน tools.cmd build dev (คงไว้เพื่อความเข้ากันได้)

# หรือเรียกตรง
dotnet build SMAPIGameLoader/SMAPIGameLoader.csproj -c "Dev" `
  -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" `
  -p:JavaSdkDirectory="C:\Program Files\Eclipse Adoptium\jdk-21.0.11.10-hotspot"
```

- ผลลัพธ์: `SMAPIGameLoader/bin/Dev/net9.0-android/abc.smapi.gameloader-Signed.apk` (~36 MB)
- เวลาที่วัดได้บนเครื่องนี้: full build ~58 s, build หลังแก้ไฟล์ 1 ไฟล์ ~23 s
- รองรับ **arm64 เท่านั้น** (ตรงกับข้อจำกัดของโปรเจกต์) — เครื่องทดสอบต้องเป็น arm64
- `Dev` define `DEBUG` และโค้ดบล็อกการเข้าเกมเมื่อ DEBUG → ทดสอบ gameplay ต้องใช้ `Release`
- `tools.cmd` auto-detect SDK/JDK/ADB; override ด้วย `ANDROID_HOME`/`ANDROID_SDK_ROOT`, `JAVA_HOME`, `ANDROID_SERIAL`
- `RestoreDotnetTools` (target ใน csproj) รัน `dotnet tool restore` เมื่อ config ไม่ใช่ Dev —
  ถ้าล้มเหลวและไม่เกี่ยวกับงาน ให้รายงาน ไม่ต้องแก้ถาวร

`Dev` แทนที่ `Debug` เดิมทั้งหมด (fast deploy ใช้กับ workflow ตรวจ UI บนเครื่องจริงไม่ได้) และ `Release` ใช้เฉพาะตอนปล่อย build
โดยไม่ต้องแก้อะไรใน csproj

Release (ค่า default ฝัง assemblies อยู่แล้ว):

```powershell
dotnet build SMAPIGameLoader/SMAPIGameLoader.csproj -c Release `
  -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" `
  -p:JavaSdkDirectory="C:\Program Files\Eclipse Adoptium\jdk-21.0.11.10-hotspot"
```

- ถ้าติด `XARDF7024` / ไฟล์ใน `obj/` ถูกล็อก: ปิด Visual Studio ก่อน
  หรือ build ชั่วคราวด้วย `-p:IntermediateOutputPath=obj/agent/` แล้วลบ `obj/agent` ทิ้งภายหลัง
- Build ผ่าน 0 errors เป็นเกณฑ์ขั้นต่ำเท่านั้น ยังไม่ถือว่างาน UI เสร็จ
  ต้องติดตั้งและตรวจบนเครื่องจริงด้วย mobile-mcp

## 4. Deploy ผ่าน mobile-mcp

1. `mobile_install_app` โดยส่ง `path` เป็น absolute path ของ `abc.smapi.gameloader-Signed.apk`
2. `mobile_terminate_app` เพื่อ force-stop แอปเดิมก่อน launch
3. `mobile_launch_app` โดยส่ง `packageName` = `abc.smapi.gameloader`
4. `mobile_get_foreground_app` เพื่อยืนยันว่า foreground คือแอปเรา

Fallback เมื่อ MCP ใช้งานไม่ได้: `adb install -r <apk>`, `adb shell am force-stop abc.smapi.gameloader`
แต่ให้ใช้ MCP เป็นหลัก

## 5. ตรวจ UI ด้วย mobile-mcp

ลำดับที่แนะนำ:

1. `mobile_list_available_devices` / `mobile_get_screen_size` — ตรวจเครื่องและขนาดจอ
2. `mobile_launch_app` → `mobile_take_screenshot` — ดูภาพหน้าจอจริง
3. `mobile_list_elements_on_screen` — ได้ `ref` + พิกัดจริงของทุก element
4. `mobile_click_on_screen_at_coordinates` (ส่ง `ref` ถ้ามี, ไม่งั้นส่ง `x,y`),
   `mobile_long_press_on_screen_at_coordinates`, `mobile_double_tap_on_screen`,
   `mobile_swipe_on_screen`, `mobile_drag_on_screen`, `mobile_type_keys`, `mobile_press_button`
5. `mobile_take_screenshot` ซ้ำหลังทุก action เพื่อยืนยันผล
6. `mobile_get_foreground_app` — ตรวจว่าไม่หลุด activity/แอป
7. `mobile_start_screen_recording` / `mobile_stop_screen_recording` — สำหรับ flow ที่ต้องดูต่อเนื่อง
8. `mobile_get_device_logs` — ดู logcat แบบ filter เช่น
   - `filter: ["process=abc.smapi.gameloader"]`
   - `filter: ["tag=AndroidRuntime", "level=Error"]`
   - เก็บเยอะให้ใช้ `saveTo` เป็นไฟล์แล้วอ่านทีหลัง
9. `mobile_batch_commands` — รวบหลาย step (tap → type → tap) ใน call เดียวเมื่อ flow ชัดแล้ว
10. `adb pull` (นอก mobile-mcp) — ดึง log/ไฟล์จาก
    `/storage/emulated/0/Android/data/abc.smapi.gameloader/files/`

หลักการ:

- อย่าเดาพิกัดจาก screenshot อย่างเดียว ให้ใช้ `mobile_list_elements_on_screen` แล้ว tap ด้วย `ref`
- `mobile_take_screenshot` มักย่อภาพ: tool จะบอกอัตราคูณ (เช่น ภาพ 270x600 ของจอ 1080x2400 = คูณ 4)
  ถ้าจำเป็นต้อง tap จากพิกัดในภาพ ต้องคูณก่อน
- ทุกครั้งที่สรุปว่างาน UI เสร็จ ต้องมี screenshot ยืนยันและระบุหน้าจอ/สถานะที่เห็น
- ห้ามใช้ `adb shell` (`pm clear`, `rm -rf`) ทำลายข้อมูล เว้นแต่ผู้ใช้สั่งชัดเจน
- `ui_dump` แบบ XML ดิบไม่มีใน mobile-mcp — ถ้าต้องการ hierarchy เต็มให้ใช้
  `mobile_list_elements_on_screen` ก่อน แล้วค่อย fallback ไป `adb shell uiautomator dump`

## 6. UI code conventions

- UI สร้างด้วย C# ทั้งหมด (programmatic views) ไม่เพิ่ม XML layout
- การ์ดข้อมูลใหม่ให้สืบทอด `Launcher/InfoCard.cs` และใช้ helper ที่มีอยู่
  (`AddHeader`, `SetStatus`, `AddRow`, `AddTextLine`)
- ใช้ `Ui.Dp(...)`, `ErrorDialogTool`, `ToastNotifyTool`, `TaskTool` ตาม pattern เดิม
- หน้าใหม่ใส่ใน `Launcher/Pages/` และผูกกับ bottom navigation ใน `LauncherActivity`
- คง dark theme / Material 3 และข้อความ UI ภาษาอังกฤษตามเดิม เว้นแต่ผู้ใช้สั่งเปลี่ยน
- งาน UI ต้อง build ผ่าน + ติดตั้ง + ตรวจบนอุปกรณ์จริงด้วย mobile-mcp ก่อนรายงานว่าเสร็จ

## 7. Technical constraints — ห้ามทำลาย

- คง `net9.0-android` และอ้าง game DLLs จาก `../../SMAPI-Android-1.6/src/DependenciesDll/`
  (`MonoGame.Framework`, `StardewValley`, `StardewValley.GameData`) — ห้ามเปลี่ยนเป็น NuGet
- Harmony/MonoMod ใน `SharedLibs/` ต้องเป็น build ที่รองรับ .NET 9 (stock Harmony 2.3.x ใช้ไม่ได้
  เพราะ `System.Reflection.Emit.LocalBuilder` เป็น abstract) — ห้ามแทนด้วย NuGet ปกติ
- ใช้ custom `libmonosgen-2.0.so` จาก `SharedLibs/native/arm64-v8a/` แทน runtime ใน SDK pack
  เพราะต้องปลดล็อก reflection ให้ Harmony/SMAPI
- Assembly store read order — v2: header → hash index → local entries → name table → data;
  v1: header → local entries → hash index. ห้ามสลับ
- Split APK: รองรับ `base.apk` + `split_config.arm64_v8a.apk` + `split_content.apk` (Play Store)
  และกรณี Samsung ที่ assembly อยู่ใน base APK — ดู `Tool/GameApkTool.cs`
- แก้ format ของ assembly store ต้องแก้ reader และ entry parser พร้อมกัน และอัปเดต `docs/building.md`
  ("Assembly Store v2 Format Support") ถ้าพฤติกรรมเปลี่ยน
- 64-bit เท่านั้น (`IntPtr.Size == 8`) — ห้ามเพิ่ม fallback 32-bit
- ห้าม hand-edit binary ใน `SharedLibs/`, `libs/`, `Resources/` โดยไม่ตรวจผลกระทบ

## 8. C# style

ทำตามไฟล์รอบข้างก่อน; ถ้าไฟล์นั้นไม่มี pattern ชัดเจน:

- Namespace: file-scoped สำหรับไฟล์ใหม่ (`namespace SMAPIGameLoader.Tool;`) — อย่าเปลี่ยนไฟล์เดิม
  ที่เป็น block-scoped; namespace ไม่ผูกกับโฟลเดอร์ (`Tool/`+`Game/` ส่วนใหญ่ใช้ `SMAPIGameLoader`,
  `Launcher/` ใช้ `.Launcher`, pages ใช้ `.Launcher.Pages`)
- หนึ่ง type ต่อไฟล์ ชื่อไฟล์ = ชื่อ type
- `PascalCase` type/method/property/const; `_camelCase` private instance field; `camelCase` local/param;
  static helper class ชื่อ `XxxTool` และ `internal static class`
- 4 spaces, Allman braces; idioms `is null`, `is not null`, `is false`, target-typed `new(...)`
- Nullable เปิดเฉพาะ project ที่ csproj เปิด — `SMAPIGameLoader` ปิดอยู่ ห้ามเหมาใส่ `?`/`!`
- ไม่เพิ่ม NuGet ถ้าของเดิมพอ และไม่ bump pin ที่ไม่เกี่ยวกับงาน
- คงคอมเมนต์ภาษาไทย/อังกฤษเดิม; เพิ่มใหม่เฉพาะ constraint ที่โค้ดสื่อเองไม่ได้

## 9. Skills — ใช้เท่าที่จำเป็น

หลักการ: skill เป็นตัวช่วยเสริมคุณภาพงาน UI ไม่ใช่ขั้นตอนบังคับ

- งานปกติ (build, deploy, แก้โค้ด, debug, อ่าน log) → **ไม่ต้องเรียก skill ใด ๆ**
- เรียกเมื่อตรงกับงานจริงเท่านั้น และถือผลเป็น "คำแนะนำ" ไม่ใช่คำสั่ง:
  repo convention + Material 3 theme + ข้อความ UI เดิม **ชนะเสมอ**
- ถ้า skill แนะนำสิ่งที่ขัดกับ `../AGENTS.md` หรือไฟล์นี้ (เพิ่ม NuGet/package, เปลี่ยนเป็น XML layout,
  เปลี่ยน font/theme, เพิ่ม dependency) → ไม่ทำ แล้วรายงานว่าขัดกับ convention
- อย่า copy ผลลัพธ์ดิบของ skill ลงในโค้ด คอมเมนต์ หรือ commit message

### ui-ux-pro-max — ใช้ได้ (ตรวจแล้ว 2026-11-26)

อยู่ที่ `.agents/skills/ui-ux-pro-max` (workspace scope) ใช้กับงานออกแบบ/รีวิว UI ของ Launcher

```bash
python .agents/skills/ui-ux-pro-max/scripts/search.py "<query>" --domain ux -n 3
```

- ใช้ `python` ไม่ใช่ `python3` (บนเครื่องนี้ `python3` เป็น 3.14 แต่ script ผ่านทั้งคู่)
- domain ที่ใช้ได้กับโปรเจกต์นี้: `ux`, `web` (app-interface: iOS/Android guidance), `icons`,
  `style`, `color`, `typography`, `google-fonts`
- **ห้ามใช้ `--stack` เป็น authority**: stack ที่มีทั้งหมดเป็น Kotlin/Flutter/SwiftUI/JS
  (`jetpack-compose`, `flutter`, `react-native`, `swiftui`, `html-tailwind`, ...) ไม่มี C# + Android View
  → อ่านเป็นแนวคิดได้ แต่ต้องแปลงเป็น programmatic C# View เอง
- **ห้ามรัน `--persist` / `--output-dir` ชี้เข้า repo** เพราะจะสร้างโฟลเดอร์ `design-system/` ใหม่;
  `--design-system` ใช้แบบอ่าน output เฉย ๆ ได้
- ใช้เมื่อ: ออกแบบหน้าใหม่, จัด layout/ระยะห่าง, เลือกสีและคอนทราสต์ dark mode,
  ตรวจ touch target / state / label ก่อนส่งงาน

Query ที่คุ้มกับงานนี้:

```bash
python .agents/skills/ui-ux-pro-max/scripts/search.py "touch target icon button" --domain web
python .agents/skills/ui-ux-pro-max/scripts/search.py "dark mode card surface contrast" --domain color
python .agents/skills/ui-ux-pro-max/scripts/search.py "list spacing rhythm" --domain ux
python .agents/skills/ui-ux-pro-max/scripts/search.py "bottom navigation active state" --domain ux
python .agents/skills/ui-ux-pro-max/scripts/search.py "disabled state clarity" --domain ux
```

- ถ้าผลว่างหรือไม่ตรง: retry **1 ครั้ง** ด้วยคำที่แคบลงหรือระบุ domain; ถ้ายังไม่ตรงให้ยึดกฎในไฟล์นี้แทน

### skill ที่ไม่ต้องใช้ในโปรเจกต์นี้

`ui-styling`, `design-system`, `slides`, `banner-design`, `brand`, `design` — เป็น web/CSS/shadcn/Tailwind
หรือเอกสารนำเสนอ ไม่เกี่ยวกับ programmatic C# View ของ Launcher
`find-skills`, `reverse-engineer-anything` — ใช้เมื่อผู้ใช้สั่งเท่านั้น

## 10. Checklist ก่อนส่งงาน UI

- [ ] build 0 errors
- [ ] `mobile_install_app` และ `mobile_launch_app` สำเร็จ
- [ ] `mobile_take_screenshot` หน้า Home และ Mods แสดงผลถูกต้อง
- [ ] ปุ่ม/การ์ดที่แก้ทำงาน — tap แล้ว screenshot ยืนยัน
- [ ] `mobile_get_device_logs` ไม่มี exception ใหม่จากแอป
- [ ] touch target ≥ 48dp และระยะห่างแนวตั้งตามสเกล 4/8dp
- [ ] ข้อความบนพื้น dark mode คอนทราสต์ ≥ 4.5:1 และ state (pressed/disabled) มองออกทั้งในธีมที่ใช้
