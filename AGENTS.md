# AGENTS.md — SMAPI Loader

Stardew Valley Android launcher: clone the installed game, extract its assemblies, Harmony-patch them,
load SMAPI, and drive it from a small C# Material 3 launcher UI.

Real work happens in `SMAPIGameLoader/`; `AssemblyStore/` is its assembly-store reader.
`LibPatcher/` and `MemoryViewer/` are throwaway experiments — do not invest in them.

**Read `SMAPIGameLoader/AGENTS.md` before touching that project.** Build/deploy/verify workflow,
UI conventions, C# style and the hard technical constraints live there.

## Non-negotiables

- Build with `.\tools.cmd dev` (build + install); variants: `build|install <dev|release>`, `release`.
  Build passing is the only gate — there is no test suite.
- Never use Visual Studio debug/hot reload, `dotnet watch`, or fast deployment; verify on a real device.
- Never replace `SharedLibs/` Harmony/MonoMod or `libmonosgen-2.0.so` with stock/NuGet builds.
- arm64 only, 64-bit.
- Never commit or push unless asked. No keystores, secrets, or unrelated binaries.
  Commit prefixes: `Update: `, `Fixed: `, `BumpVersion: `.
- If `docs/` disagrees with the code, the code wins — report the difference.
