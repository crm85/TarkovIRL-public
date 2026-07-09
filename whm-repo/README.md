# TarkovIRL

TarkovIRL is a BepInEx/SPT weapon-handling plugin for Escape From Tarkov. The project targets .NET Framework 4.8 and depends on game/SPT/Realism assemblies referenced from the local `spt_311` install in `TarkovIRL.csproj`.

## Source Layout

- `Core/` contains the plugin entry point, lifecycle helpers, and animation curve setup.
- `Configuration/` contains BepInEx configuration metadata helpers.
- `Controllers/` contains runtime systems for player motion, sway, parallax, reloads, transitions, and efficiency.
- `Patches/` contains Harmony/SPT `ModulePatch` implementations plus `PatchRegistry`.
- `Integration/` contains wrappers over external mod/game APIs.
- `Models/` contains small domain models.
- `Utilities/` contains shared logging/math/debug helpers.
- `Legacy/` contains source files that are intentionally not compiled.

## Build

```powershell
dotnet build TarkovIRL.sln -v:minimal
```

The project currently builds cleanly with zero warnings when the referenced SPT assemblies are present.
