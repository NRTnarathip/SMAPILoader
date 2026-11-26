# AGENTS.md — SMAPI Loader (Stardew Valley Android)

คำแนะนำสำหรับ AI agent ที่ทำงานใน repository นี้ อ่านก่อนแก้โค้ดทุกครั้ง
ไฟล์นี้อยู่ที่ root ของ repo จึงมีผลกับทุก project ใน solution

## 1. Repository map

| Path | Type | Target | หน้าที่ |
|---|---|---|---|
| `SMAPIGameLoader/` | Android app (launcher) | `net9.0-android` | แอปหลัก: clone game, แตก assembly, patch ด้วย Harmony, โหลด SMAPI, UI |
| `AssemblyStore/` | Class library | `net9.0` | อ่าน/แกะ .NET Android assembly store (v1 และ v2) |
| `LibPatcher/` | Console tool | `net8.0` | patch native libs |
| `MemoryViewer/` | Console tool | `net8.0` | เครื่องมือ debug memory |
| `SharedLibs/` | Binaries | — | Harmony/MonoMod ที่ build สำหรับ .NET 9 + native `libmonosgen-2.0.so` |
| `docs/building.md` | Docs | — | ขั้นตอน build ฉบับเต็ม (อ่านก่อน build) |

- Solution: `SMAPILoader.sln` — มี 4 project ข้างต้น
- `global.json` pin .NET SDK `9.0.312` (`rollForward: latestFeature`)
- ไม่มี test project และไม่มี `.editorconfig` → ยึด pattern จากโค้ดจริงในโฟลเดอร์นั้นเป็นหลัก

## 2. Build & verification

ก่อน build ต้องมี: .NET 9 SDK (Microsoft distribution), Android workload, JDK 17, Android SDK,
game DLLs ที่ `../SMAPI-Android-1.6/src/DependenciesDll/`, และ custom Mono runtime ใน SDK pack
(รายละเอียดใน `docs/building.md`)

```bash
# Windows — งาน dev ใช้ config "Dev AI" (เร็วสุด: arm64 เดียว, assembly store, ไม่ trim/AOT)
.\build-dev-ai.cmd

# ปล่อย build
dotnet build SMAPIGameLoader/SMAPIGameLoader.csproj -c Release ^
  -p:AndroidSdkDirectory="%LOCALAPPDATA%\Android\Sdk" ^
  -p:JavaSdkDirectory="C:\Program Files\Eclipse Adoptium\jdk-17.0.13.11-hotspot"
```

- **Verification gate = build ผ่าน** เพราะไม่มี test suite อย่าอ้างว่า "น่าจะทำงาน" โดยไม่ build ถ้าทำได้
- APK อยู่ใต้ `SMAPIGameLoader/bin/<Config>/net9.0-android/` (`Dev AI` หรือ `Release`) — ชื่อไฟล์จริงให้ดูจาก output/build docs อย่า hardcode
- `SMAPIGameLoader.csproj` มี target `RestoreDotnetTools` ที่รัน `dotnet tool restore` ก่อน restore — ถ้าล้มเหลวและไม่เกี่ยวกับงาน ให้รายงาน ไม่ต้องแก้ถาวร
- `docs/building.md` บางส่วนอาจล้าสมัย (เช่นยังอ้างชื่อ `StardewApkTool.cs` ซึ่งปัจจุบันคือ `Tool/GameApkTool.cs`) — ตรวจโค้ดจริงก่อนเชื่อ docs

## 3. C# style

ทำตามไฟล์รอบข้างก่อนเสมอ ถ้าไฟล์นั้นไม่มี pattern ชัดเจนให้ใช้กฎนี้:

- **Namespace**: ใช้ file-scoped (`namespace SMAPIGameLoader.Tool;`) สำหรับไฟล์ใหม่
  ไฟล์เดิมบางไฟล์ยังเป็น block-scoped — อย่าเปลี่ยนรูปแบบ namespace ของไฟล์เดิมถ้าไม่เกี่ยวกับงาน
  namespace ไม่ได้ผูกกับโฟลเดอร์ตรง ๆ: `Tool/` และ `Game/` ส่วนใหญ่ใช้ `SMAPIGameLoader` (บางไฟล์ `.Tool`/`.Game`)
  ส่วน `Launcher/` ใช้ `SMAPIGameLoader.Launcher`, `Launcher/Pages/` ใช้ `.Launcher.Pages`
- **โครงสร้าง**: หนึ่ง type ต่อไฟล์ ชื่อไฟล์ตรงกับชื่อ type
- **การตั้งชื่อ**
  - type / method / property / const: `PascalCase`
  - private instance field: `_camelCase` (เช่น `_container`, `_nav` ใน `LauncherActivity`)
  - local variable / parameter: `camelCase`
  - static helper class: `XxxTool` และประกาศ `internal static class` เมื่อใช้ภายในโปรเจกต์
- **Indentation**: 4 spaces, วงเล็บปีกกาแบบ Allman ตามโค้ดเดิม
- **Idioms ที่ repo ใช้**: `is false`, `is not null`, `is null`, target-typed `new(...)`
- **Nullable**: เปิดเฉพาะ project ที่ csproj เปิด (`AssemblyStore`, `LibPatcher`, `MemoryViewer`)
  — `SMAPIGameLoader` ไม่ได้เปิด ดังนั้นอย่าเหมาใส่ `?`/`!` ทั้งโปรเจกต์โดยไม่มีเหตุ
- **Dependencies**: อย่าเพิ่ม NuGet package ใหม่ถ้าทำได้ด้วยของที่มีอยู่ และอย่าขยับ version pin เดิมถ้าไม่เกี่ยวกับงาน
- **Comments**: คงคอมเมนต์ภาษาไทย/อังกฤษเดิมไว้ เขียนคอมเมนต์ใหม่เฉพาะที่อธิบาย constraint ที่โค้ดสื่อเองไม่ได้

## 4. Architecture & UI conventions — `SMAPIGameLoader`

โครงสร้าง namespace/โฟลเดอร์:

- `Game/` — runtime ของเกม: โหลด assembly, clone asset, rewrite, patch (Harmony)
- `Launcher/` — activity และ UI หลัก; `Launcher/Pages/` คือแต่ละหน้า (Home, Mod Manager)
- `Tool/` — static helper ขนาดเล็ก (ไฟล์, zip, dialog, toast, task, apk ฯลฯ)
- `Game/Rewriter/` — IL/audio rewriting

namespace ที่ใช้จริง: `SMAPIGameLoader` (ส่วนใหญ่ของ `Tool/` และ `Game/`), `SMAPIGameLoader.Tool`,
`SMAPIGameLoader.Game`, `SMAPIGameLoader.Game.Rewriter`, `SMAPIGameLoader.Launcher`,
`SMAPIGameLoader.Launcher.Pages`

ข้อกำหนด:

- **UI สร้างด้วย C# ทั้งหมด** (programmatic views) ไม่เพิ่ม XML layout ใหม่
  ใช้ `Resources/Values` และ theme Material 3 DayNight ตามเดิม; dark mode ถูกบังคับใน `LauncherActivity`
- **Dimension** ใช้ `Ui.Dp(context, value)` ไม่ใส่ px ตรง ๆ
- **Error UX** ใช้ `ErrorDialogTool.Show(...)` หรือ `ToastNotifyTool.Notify(...)` ตามบริบท
- **งานเบื้องหลัง** ใช้ `TaskTool.Run(activity, async () => ...)` และ `TaskTool.RunMainThread(...)` เมื่อต้องกลับ main thread
- **Logging** ใช้ `Console.WriteLine(...)` หรือ `Log.It(...)` ตามไฟล์นั้น
- **Singleton/static access** เป็น pattern ที่มีอยู่ (`LauncherActivity.Instance`, static tool classes) — รักษาแนวนี้ ไม่ต้อง introduce DI container
- **Activity lifecycle**: ระวังงานที่โหลด game assembly ต้องไม่อยู่ใน scope ที่ activity ยัง active (ดูคอมเมนต์ใน `Game/EntryGame.cs`)

## 5. Technical constraints — ห้ามทำลาย

- `SMAPIGameLoader` ต้องคง `net9.0-android` และอ้าง game DLLs จาก
  `../../SMAPI-Android-1.6/src/DependenciesDll/` (`MonoGame.Framework.dll`, `StardewValley.dll`,
  `StardewValley.GameData.dll`) — อย่าเปลี่ยนเป็น NuGet แทน
- Harmony/MonoMod ใน `SharedLibs/` ต้องเป็น build ที่รองรับ .NET 9 (stock Harmony 2.3.x ใช้ไม่ได้เพราะ
  `System.Reflection.Emit.LocalBuilder` เป็น abstract) — อย่าแทนด้วย NuGet รุ่นปกติ
- ต้องใช้ custom `libmonosgen-2.0.so` (อยู่ใน `SharedLibs/native/arm64-v8a/`) แทน runtime ใน SDK pack
  เพราะต้องปลดล็อก reflection สำหรับ Harmony/SMAPI
- **AssemblyStore v2** อ่านตามลำดับ: header → hash index → local entries → name table → data
  (v1 คือ header → local entries → hash index) อย่าสลับลำดับ
- **Split APK**: รองรับ `base.apk` + `split_config.arm64_v8a.apk` + `split_content.apk`
  (Play Store) และกรณี Samsung ที่ assembly อยู่ใน base APK — ดู `Tool/GameApkTool.cs`
- รองรับ **64-bit เท่านั้น** (`IntPtr.Size == 8`) — อย่าเพิ่ม fallback 32-bit โดยไม่ได้รับคำสั่ง
- ห้าม commit keystore, password, secret หรือ binary ขนาดใหญ่ที่ไม่เกี่ยวกับงาน
- อย่าแก้ไฟล์ใน `SharedLibs/`, `libs/`, `Resources/` แบบ binary โดยไม่ตรวจสอบผลกระทบ

## 6. Editing discipline

- อ่านไฟล์ที่จะแก้และไฟล์ข้างเคียงก่อนเขียน เพื่อจับ pattern เดิม
- แก้ให้น้อยที่สุดตาม scope ของงาน — อย่า reformat ทั้งไฟล์ อย่า rename โดยไม่จำเป็น
- ถ้าเจอโค้ดที่ดูแปลก (เช่น busy-wait ใน `TaskTool.RunMainThread`) อย่า "แก้ให้สวย" เองถ้าไม่ใช่งานนั้น
- รักษาคอมเมนต์และข้อความ UI เดิม ยกเว้นได้รับคำสั่งให้เปลี่ยน
- ถ้า `docs/` ขัดกับโค้ด ให้ยึดโค้ดเป็นความจริงและรายงานความต่าง

## 7. Git

- Branch หลัก: `master`
- รูปแบบ commit message ที่ใช้ใน repo: `Update: ...`, `Fixed: ...`, `BumpVersion: ...`
- **อย่า commit หรือ push เอง** จนกว่าผู้ใช้จะสั่ง
- ก่อน commit ให้ `git status` และตรวจว่าไม่มีไฟล์ `bin/`, `obj/`, keystore หรือ secret หลุดเข้ามา

## 8. Per-project notes

- Workflow เฉพาะแอป (build → deploy → ตรวจ UI ด้วย scrcpy-mcp, ห้ามใช้ VS debug/hot reload):
  ดู `SMAPIGameLoader/AGENTS.md`
- `AssemblyStore`: เปิด `Nullable` + `ImplicitUsings` — เขียนโค้ดให้ nullable-safe; namespace คือ
  `Xamarin.Android.AssemblyStore` (เลียนแบบ upstream) ไม่ใช่ `SMAPIGameLoader.*`
- `LibPatcher`, `MemoryViewer`: console app `net8.0`, เปิด `Nullable` + `ImplicitUsings`
- เมื่อแก้ format ของ assembly store ให้แก้ทั้ง reader และไฟล์ที่ parse entry ให้สอดคล้องกัน และอัปเดต
  `docs/building.md` ส่วน "Assembly Store v2 Format Support" ถ้าพฤติกรรมเปลี่ยน