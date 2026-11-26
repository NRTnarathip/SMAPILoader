## แผน Refactor Launcher UI

### 1) สร้าง `Launcher/InfoCard.cs` (base class ใหม่)
`public abstract class InfoCard : CardView` เก็บ scaffold ที่ซ้ำกัน:

- ตั้งค่าการ์ด: background `#1A1D24`, radius 16, elevation 2, `UseCompatPadding`, `MarginLayoutParams`
- `Content` (LinearLayout แนวตั้ง + padding 20/16)
- `AddHeader(title)` → คืน `TextView` สำหรับ badge สถานะ (title ฝั่งซ้าย, badge ชิดขวา)
- `SetStatus(status, isActive, activeText, inactiveText)` → เขียว `#2E7D32` / แดง `#C62828`
- `AddDivider()` → เส้นบาง `#22FFFFFF`
- `AddRow(label)` → คืน value `TextView` (key จาง 60%, value ตัวหนา)
- `AddTextLine(text)` → บรรทัดข้อความสีเทา `#9AA0A6`
- รวม color constants ไว้ที่เดียว

### 2) `Launcher/GameInfoCard.cs` (เปลี่ยนชื่อไฟล์จาก `GameCardInfo.cs`)
- สืบทอด `InfoCard`
- ใช้ `AddHeader("App Game")` + `SetStatus(...)` + `AddDivider()`
- แสดง 5 บรรทัด: Game Version, Verify, Support Minimum Version, Launcher Version, Build
- แยก helper `GetVerifyText(bool installed)` และ `GetLauncherBuildText()` ออกจาก constructor
- ลบ field ที่ไม่ได้ใช้ (`_gameVersion`, `_gameVerify`, `_launcherVersion`, `_launcherBuild`, `_suportGameVersion`)

### 3) `Launcher/SMAPIInfoCard.cs`
- สืบทอด `InfoCard`
- ใช้ `AddHeader("SMAPI")` + `AddDivider()` + `AddRow(...)` 3 แถว
- helper `CreateActionButton(text, handler)` สำหรับปุ่ม Install/Uninstall
- `Refresh()` อ่าน `GetBuildCode()` ครั้งเดียวแล้วใช้ทั้ง Build Code และ Build Date
- แยก `FormatBuildDate(long)` ออกมา

### 4) Rewrite `Launcher/Pages/HomePage.cs`
- เก็บเฉพาะ field ที่ใช้จริง (`_playButton`) และทำเป็น private
- แยกเมธอด: `CreateHeader(context)`, `CreateBody(context)`, `CreatePrimaryButton(...)`, `CreateButtonBackground(...)`, `UpdatePlayButton()`
- ลบ dead code `InstallSMAPIButton_Click` (ไม่ถูก wire ที่ไหน)
- คง public `OpenPlayStore(...)` และพฤติกรรมเดิม (ถ้าเกมยังไม่ติดตั้ง กดปุ่มแล้วเปิด Play Store)

### 5) การเปลี่ยนแปลงเชิงภาพเล็กน้อย (ตั้งใจให้สม่ำเสมอ)
- title ของการ์ดทั้งสองขนาด 18sp เท่ากัน
- badge สถานะชิดขวาทั้งสองการ์ด
- GameInfoCard มีเส้น divider ใต้ header เหมือน SMAPIInfoCard

### 6) ตรวจสอบ
- `dotnet build SMAPIGameLoader/SMAPIGameLoader.csproj -c Debug` (ถ้า Android SDK/workload ไม่พร้อมในเครื่อง จะรายงานผลตามจริง)
- ตรวจว่าไม่มีไฟล์อื่นอ้าง `GameCardInfo.cs`, property `PlayButton`/`SmapiCard`/`GameInfoCard` ของ HomePage (grep แล้วไม่พบ)