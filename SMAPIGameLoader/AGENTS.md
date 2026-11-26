# AGENTS.md — SMAPIGameLoader (Android App)

ไฟล์นี้เป็นคำสั่งเฉพาะโปรเจกต์ `SMAPIGameLoader/` เสริมจาก `../AGENTS.md` ที่ root
เป้าหมาย: พัฒนา UI/ฟีเจอร์ของ launcher แล้วตรวจผลบนอุปกรณ์จริงผ่าน **scrcpy-mcp** เท่านั้น

## 1. กฎหลัก: ห้ามใช้ Visual Studio debug / hot reload

- ห้ามใช้ Visual Studio debugger, `devenv`, Hot Reload, `dotnet watch`
- ห้าม deploy แบบ fast deployment (`-t:Install`, `AndroidFastDeploymentType=Assemblies`)
- วงจรที่ใช้เท่านั้น: **build APK ที่ฝัง assemblies → install → launch → ตรวจด้วย scrcpy-mcp**
- อย่าเปิด/แก้ solution ใน Visual Studio ระหว่าง build เพราะ VS จะ lock ไฟล์ใน `obj/`
  ทำให้ build ล้มด้วย `XARDF7024: ... Java.Interop.dll ... being used by another process`

## 2. Environment (ตรวจก่อนทุกครั้ง)

- Device: เรียก `device_list` ก่อน; ถ้ามีเครื่องเดียวไม่ต้องส่ง `serial`
- Package name จริงของแอป: **`abc.smapi.gameloader`**
  (มาจาก `SMAPIGameLoader/AndroidManifest.xml` — ไม่ใช่ `ApplicationId` ใน csproj)
- APK output: `SMAPIGameLoader/bin/<Config>/net9.0-android/abc.smapi.gameloader-Signed.apk`
- `scrcpy-mcp` ตั้งค่าใน `.zcode/config.json` (`npx scrcpy-mcp`, Node.js 24+) พร้อม
  `SCRCPY_SERVER_PATH` ชี้ไปที่
  `C:\Users\narat\Desktop\Stardew Valley Android\scrcpy-win64-v3.3.4\scrcpy-server`
  (ต้องชี้ที่ไฟล์ `scrcpy-server` ไม่ใช่ `scrcpy.exe`)
  ถ้าเปิด session อยู่ก่อนเพิ่ม/แก้ config จะยังไม่เห็น tool — ต้อง reload หรือเปิด session ใหม่
- `start_session` ใช้งานได้แล้ว (ทดสอบกับ scrcpy 3.3.4: เชื่อมต่อ ~0.9s, screenshot ~30ms)
  ถ้า fail ให้ตรวจ `SCRCPY_SERVER_PATH` และตรวจว่า session ถูก reload หลังแก้ config หรือยัง
- ถ้า `start_session` ยัง fail ให้ใช้ ADB fallback ต่อ: `screenshot` / `tap` / `ui_*` ยังใช้ได้
  แต่ช้ากว่า (~500ms ต่อ screenshot)
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

`Dev` แทนที่ `Debug` เดิมทั้งหมด (fast deploy ใช้กับ workflow scrcpy ไม่ได้) และ `Release` ใช้เฉพาะตอนปล่อย build
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
  ต้องติดตั้งและตรวจบนเครื่องจริงด้วย scrcpy-mcp

## 4. Deploy ผ่าน scrcpy-mcp

1. `app_install` โดยส่ง `apkPath` เป็น absolute path ของ `abc.smapi.gameloader-Signed.apk`
2. `app_start` โดยส่ง `packageName` = `+abc.smapi.gameloader`
   (prefix `+` เพื่อ force-stop แอปเดิมก่อน launch)
3. `app_current` เพื่อยืนยันว่า foreground activity คือแอปเรา

Fallback เมื่อ MCP ใช้งานไม่ได้: `adb install -r <apk>`, `adb shell am force-stop abc.smapi.gameloader`
แต่ให้ใช้ MCP เป็นหลัก

## 5. ตรวจ UI ด้วย scrcpy-mcp

ลำดับที่แนะนำ:

1. `device_list` / `device_info` — ตรวจเครื่องและขนาดจอ
2. `start_session` — เปิด fast path (ถ้า fail เพราะไม่มี scrcpy ให้ใช้ ADB fallback ต่อ)
3. `app_start` → `screenshot` — ดูภาพหน้าจอจริง
4. `ui_dump` หรือ `ui_find_element` (`text`, `resourceId`, `className`, `contentDescription`)
   — หา element และพิกัด tap แทนการเดาจากภาพ
5. `tap` / `long_press` / `swipe` / `scroll` / `input_text` / `key_event`
6. `screenshot` ซ้ำหลังทุก action เพื่อยืนยันผล
7. `app_current` — ตรวจว่าไม่หลุด activity/แอป
8. `screen_record_start` / `screen_record_stop` — สำหรับ flow ที่ต้องดูต่อเนื่อง
9. `shell_exec` — ดู logcat เช่น
   - `logcat -d -t 400 -s AndroidRuntime:E mono-stdout:V mono-stderr:V DOTNET:V`
   - `dumpsys activity activities | grep -i mResumedActivity`
10. `file_pull` — ดึง log/ไฟล์จาก `/storage/emulated/0/Android/data/abc.smapi.gameloader/files/`

หลักการ:

- อย่าเดาพิกัดจาก screenshot อย่างเดียว ให้ใช้ `ui_find_element` ก่อน
  ถ้า element ไม่มี text ให้ใช้ `ui_dump` ดู hierarchy
- ทุกครั้งที่สรุปว่างาน UI เสร็จ ต้องมี screenshot ยืนยันและระบุหน้าจอ/สถานะที่เห็น
- ห้ามใช้ `shell_exec` ทำคำสั่งทำลายข้อมูล (`pm clear`, `rm -rf`) เว้นแต่ผู้ใช้สั่งชัดเจน

## 6. UI code conventions

- UI สร้างด้วย C# ทั้งหมด (programmatic views) ไม่เพิ่ม XML layout
- การ์ดข้อมูลใหม่ให้สืบทอด `Launcher/InfoCard.cs` และใช้ helper ที่มีอยู่
  (`AddHeader`, `SetStatus`, `AddRow`, `AddTextLine`)
- ใช้ `Ui.Dp(...)`, `ErrorDialogTool`, `ToastNotifyTool`, `TaskTool` ตาม pattern เดิม
- หน้าใหม่ใส่ใน `Launcher/Pages/` และผูกกับ bottom navigation ใน `LauncherActivity`
- คง dark theme / Material 3 และข้อความ UI ภาษาอังกฤษตามเดิม เว้นแต่ผู้ใช้สั่งเปลี่ยน
- งาน UI ต้อง build ผ่าน + ติดตั้ง + ตรวจบนอุปกรณ์จริงด้วย scrcpy-mcp ก่อนรายงานว่าเสร็จ

## 7. Checklist ก่อนส่งงาน UI

- [ ] build 0 errors
- [ ] `app_install` และ `app_start` สำเร็จ
- [ ] `screenshot` หน้า Home และ Mods แสดงผลถูกต้อง
- [ ] ปุ่ม/การ์ดที่แก้ทำงาน — tap แล้ว screenshot ยืนยัน
- [ ] `logcat` ไม่มี exception ใหม่จากแอป
