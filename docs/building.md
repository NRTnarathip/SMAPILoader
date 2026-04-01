# Building SMAPI Loader from Source

This guide covers building the SMAPI Loader (game launcher) for Android.

The SMAPI Loader extracts game assemblies from the Stardew Valley APK, applies Harmony patches, loads SMAPI, and launches the game.

## Prerequisites

- .NET 9 SDK (Microsoft distribution, not distro-packaged)
- Android SDK (e.g. via Android Studio)
- JDK 17 (required by Android build tooling)
- Game DLLs from Stardew Valley APK (see [SMAPI building guide](../../SMAPI-Android-1.6/docs/building.md))
- Custom Mono runtime with reflection access patches (see step 2)
- .NET 9-compatible Harmony/MonoMod libraries (see step 3)

## 1. Install .NET 9 SDK

### Linux

Distro-packaged .NET SDKs do not include the Android workload. Use Microsoft's install script:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$PATH
```

### Windows

Download from https://dotnet.microsoft.com/download/dotnet/9.0 or:

```powershell
Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
.\dotnet-install.ps1 -Channel 9.0
```

### Install the Android workload

```bash
dotnet workload install android
```

## 2. Custom Mono Runtime

The stock .NET 9 Mono runtime (`libmonosgen-2.0.so`) restricts reflection access to private/protected members. SMAPI and Harmony require unrestricted reflection to patch game methods. A custom-built Mono runtime with these restrictions removed must be used.

### Install the custom runtime

A pre-built custom `libmonosgen-2.0.so` is included in the repo at `SharedLibs/native/arm64-v8a/`. Copy it into the SDK pack:

```bash
# Backup the stock runtime
cp ~/.dotnet/packs/Microsoft.NETCore.App.Runtime.Mono.android-arm64/9.0.14/runtimes/android-arm64/native/libmonosgen-2.0.so \
   ~/.dotnet/packs/Microsoft.NETCore.App.Runtime.Mono.android-arm64/9.0.14/runtimes/android-arm64/native/libmonosgen-2.0.so.stock.bak

# Copy the custom runtime from the repo
cp SharedLibs/native/arm64-v8a/libmonosgen-2.0.so \
   ~/.dotnet/packs/Microsoft.NETCore.App.Runtime.Mono.android-arm64/9.0.14/runtimes/android-arm64/native/libmonosgen-2.0.so
```

Adjust the version path (`9.0.14`) to match your installed runtime pack version. Find it with:

```bash
ls ~/.dotnet/packs/Microsoft.NETCore.App.Runtime.Mono.android-arm64/
```
 
### Windows equivalent path

```
%USERPROFILE%\.dotnet\packs\Microsoft.NETCore.App.Runtime.Mono.android-arm64\9.0.14\runtimes\android-arm64\native\libmonosgen-2.0.so
```

## 3. Harmony and MonoMod Libraries

The loader uses Harmony for runtime method patching. The stock Harmony 2.3.x and MonoMod libraries are **not compatible** with .NET 9 — `System.Reflection.Emit.LocalBuilder` was made abstract in .NET 9, causing Harmony to crash at runtime.

You need .NET 9-compatible builds of these libraries in `SharedLibs/`:

| Library | Description |
|---------|-------------|
| `0Harmony.dll` | Harmony (thin build, ~300KB — MonoMod is separate) |
| `MonoMod.Backports.dll` | MonoMod backports |
| `MonoMod.Core.dll` | MonoMod core (includes RuntimeDetour) |
| `MonoMod.Iced.dll` | MonoMod x86/ARM disassembler |
| `MonoMod.ILHelpers.dll` | MonoMod IL helpers |
| `MonoMod.Utils.dll` | MonoMod utilities |

Note: `MonoMod.RuntimeDetour.dll` no longer exists as a separate assembly — it is merged into `MonoMod.Core.dll`.

If you have .NET 9-compatible builds (e.g. from the slaythespire2mobile project at `android/assets/dotnet_bcl/`):

```bash
cp /path/to/net9-harmony/0Harmony.dll SharedLibs/
cp /path/to/net9-harmony/MonoMod.Backports.dll SharedLibs/
cp /path/to/net9-harmony/MonoMod.Core.dll SharedLibs/
cp /path/to/net9-harmony/MonoMod.Iced.dll SharedLibs/
cp /path/to/net9-harmony/MonoMod.ILHelpers.dll SharedLibs/
cp /path/to/net9-harmony/MonoMod.Utils.dll SharedLibs/

# Remove the old RuntimeDetour (now part of MonoMod.Core)
rm -f SharedLibs/MonoMod.RuntimeDetour.dll
```

## 4. Game DLL Dependencies

The loader references `MonoGame.Framework.dll`, `StardewValley.dll`, and `StardewValley.GameData.dll` from the SMAPI project's `src/DependenciesDll/` directory. These must be extracted from the Stardew Valley APK first.

See the [SMAPI extract-game-dlls guide](../../SMAPI-Android-1.6/docs/extract-game-dlls.md) for instructions.

The csproj references them at:

```
../../SMAPI-Android-1.6/src/DependenciesDll/MonoGame.Framework.dll
../../SMAPI-Android-1.6/src/DependenciesDll/StardewValley.dll
../../SMAPI-Android-1.6/src/DependenciesDll/StardewValley.GameData.dll
```

Ensure these files exist before building.

## 5. Build the Loader APK

### Linux

```bash
~/.dotnet/dotnet build SMAPIGameLoader/SMAPIGameLoader.csproj \
    -c Release \
    -p:AndroidSdkDirectory=$HOME/Android/Sdk \
    -p:JavaSdkDirectory=/usr/lib/jvm/java-17-temurin-jdk
```

### Windows

```powershell
dotnet build SMAPIGameLoader\SMAPIGameLoader.csproj `
    -c Release `
    -p:AndroidSdkDirectory="%LOCALAPPDATA%\Android\Sdk" `
    -p:JavaSdkDirectory="C:\Program Files\Eclipse Adoptium\jdk-17.0.13.11-hotspot"
```

### Build output

The signed APK will be at:

```
SMAPIGameLoader/bin/Release/net9.0-android/abc.smapi.gameloader-Signed.apk
```

## 6. Install on Device

```bash
adb install -r SMAPIGameLoader/bin/Release/net9.0-android/abc.smapi.gameloader-Signed.apk
```

If upgrading over an existing install and the signing key differs:

```bash
adb uninstall abc.smapi.gameloader
adb install SMAPIGameLoader/bin/Release/net9.0-android/abc.smapi.gameloader-Signed.apk
```

Note: uninstalling removes any installed SMAPI zip and mods from the app's data directory.

---

## Changes from Original Codebase

### .NET 9 Upgrade

The game (Stardew Valley 1.6.15+) is built against .NET 9. The loader must also target .NET 9 to reference the game DLLs.

**Files changed:**

- `global.json` — SDK version `9.0.312`
- `SMAPIGameLoader/SMAPIGameLoader.csproj` — `net9.0-android` target framework
- `AssemblyStore/AssemblyStore.csproj` — `net9.0` target framework

**Additional csproj changes for .NET 9 compatibility:**

```xml
<!-- Suppress version mismatch warnings from game DLLs -->
<NoWarn>$(NoWarn);CS1705;MSB3277</NoWarn>

<!-- Work around missing RuntimeConfigParserTask in Android SDK 35 -->
<GenerateRuntimeConfigurationFiles>false</GenerateRuntimeConfigurationFiles>
```

### Custom Mono Runtime

The stock .NET 9 Mono runtime makes `System.Reflection.Emit.LocalBuilder` abstract and restricts reflection access to private members. A custom-built `libmonosgen-2.0.so` with these restrictions removed is required. This file replaces the stock version in the SDK pack at:

```
~/.dotnet/packs/Microsoft.NETCore.App.Runtime.Mono.android-arm64/9.0.14/runtimes/android-arm64/native/libmonosgen-2.0.so
```

### Harmony/MonoMod .NET 9 Update

The old Harmony 2.3.4 (fat ILRepack'd DLL with embedded MonoMod and Mono.Cecil) crashes on .NET 9 due to the `LocalBuilder` abstract class change. Replaced with a .NET 9-compatible thin Harmony build + separate MonoMod libraries.

**Files changed:**

- `SharedLibs/0Harmony.dll` — Replaced with thin .NET 9-compatible build (~300KB vs ~2.4MB)
- `SharedLibs/MonoMod.Core.dll` — Updated (now includes RuntimeDetour)
- `SharedLibs/MonoMod.Iced.dll` — Updated
- `SharedLibs/MonoMod.Utils.dll` — Updated
- `SharedLibs/MonoMod.Backports.dll` — Updated
- `SharedLibs/MonoMod.ILHelpers.dll` — Updated
- `SharedLibs/MonoMod.RuntimeDetour.dll` — **Removed** (merged into MonoMod.Core)

**`SMAPIGameLoader/SMAPIGameLoader.csproj`**
- Removed `MonoMod.RuntimeDetour` reference
- Removed `Mono.Cecil` `Aliases` attribute (no longer needed without fat Harmony)

**`SMAPIGameLoader/Game/StardewAssembliesResolver.cs`**
- Removed `extern alias MonoCecilAlias;` directive

### Assembly Store v2 Format Support

Stardew Valley 1.6.15+ uses .NET Android's v2 assembly store format. Assemblies are no longer stored as `assemblies/*.blob` files inside the APK. Instead, they are wrapped in ELF binaries at `lib/{arch}/libassemblies.{arch}.blob.so`.

#### V2 data layout

```
payload section:
  [0x000] Header (20 bytes): magic + version + local_count + global_count + index_byte_size
  [0x014] Hash index (global_count * 12 bytes)
  [0x62C] Local entries (local_count * 28 bytes)
  [0xD48] Name table (length-prefixed UTF-8 strings)
  [0x14EA] XALZ compressed assembly data
```

#### Format differences (v1 vs v2)

| Feature | v1 | v2 |
|---------|----|----|
| Location in APK | `assemblies/assemblies.*.blob` | `lib/arm64-v8a/libassemblies.arm64-v8a.blob.so` |
| Container | Raw blob | ELF binary with `payload` section |
| XABA version | `1` | `2` (with flags in high bits) |
| Header field 5 | `store_id` | `index_byte_size` |
| Data layout order | header, local entries, hash index | header, hash index, local entries, name table |
| Hash index entries | 20 bytes (hash64 + mapping + local_idx + store_id) | 12 bytes (hash64 + mapping) |
| Local entry size | 24 bytes | 28 bytes (index prefix + 24 bytes) |
| Name resolution | Manifest file (`assemblies.manifest`) | Inline name table (length-prefixed strings) |
| Assembly data | Raw or XALZ compressed | XALZ compressed |

#### Files changed

**`AssemblyStore/AssemblyStoreReader.cs`**
- Accepts v2 version numbers (base version extracted from low byte, flags in high bits)
- Reads v2 header (5th field is `index_byte_size` instead of `store_id`)
- V2 read order: hash index first, then local entries (opposite of v1)
- Reads v2 global index (12-byte entries instead of 20-byte)
- Reads v2 name table (length-prefixed UTF-8 strings after local entries)

**`AssemblyStore/AssemblyStoreAssembly.cs`**
- Reads v2 local entries (28 bytes: index + offset + size + reserved)
- Fallback name extraction from DLL PE metadata when no name table/manifest exists

**`AssemblyStore/AssemblyStoreHashEntry.cs`**
- Added v2 constructor for 12-byte entries (hash64 + mapping_index)

**`AssemblyStore/AssemblyStoreExplorer.cs`**
- Added `ReadStoreSetFromArchiveElfFormat()` — searches APK for `lib/*/libassemblies.*.blob.so` entries
- Added `ExtractElfPayload()` — parses ELF64 section headers to find and extract the `payload` section containing the XABA data
- Falls back to ELF format when no `assemblies/*.blob` entries are found

### Split APK Support

The Play Store version of Stardew Valley uses split APKs:

- `base.apk` — Java/Kotlin code, no .NET assemblies
- `split_config.arm64_v8a.apk` — native libs and .NET assembly blob
- `split_content.apk` — game assets (Content/)

**`SMAPIGameLoader/Tool/StardewApkTool.cs`**
- Added `Arm64ApkPath` property — locates the arm64 split APK via `SplitSourceDirs`

**`SMAPIGameLoader/Game/GameAssemblyManager.cs`**
- `VerifyAssemblies()` now falls back to `Arm64ApkPath` when the base APK contains no assemblies

### Minor Fixes

**`SMAPIGameLoader/SMAPIGameLoader.csproj`**
- `RestoreDotnetTools` target — `IgnoreExitCode="true"` to avoid build failure when `dotnet tool restore` fails

---

## Troubleshooting

### `error MSB4036: The "RuntimeConfigParserTask" task was not found`

This is a known issue with Android SDK 35.0.105 on .NET 9. The workaround is already applied:

```xml
<GenerateRuntimeConfigurationFiles>false</GenerateRuntimeConfigurationFiles>
```

### `error CS1705: Assembly uses System.Runtime Version=9.0.0.0`

The game DLLs target .NET 9. Make sure the project targets `net9.0-android` and the `NoWarn` includes `CS1705`.

### `error NETSDK1139: The target platform identifier android was not recognized`

The Android workload is not installed. Run `dotnet workload install android`.

### `HarmonyException: Cannot create an instance of System.Reflection.Emit.LocalBuilder`

The Harmony/MonoMod libraries are not .NET 9 compatible. Replace `SharedLibs/` with .NET 9-compatible builds (see step 3). Also ensure the custom Mono runtime is installed in the SDK pack (see step 2).

### `dotnet tool restore` fails during build

This is non-fatal with `IgnoreExitCode="true"`. The tool restore is for development tooling and not required for the build.

### `ArgumentOutOfRangeException` during assembly cloning

The v2 assembly store format reads data in a different order than v1 (hash index before local entries). Ensure the `AssemblyStoreReader` uses the correct read order for v2.
