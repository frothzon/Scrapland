# lessons_learned.md — resolved issues (source of truth)

> **Read this before you change anything. Append to it whenever you resolve an issue.**
> This file is the project's memory: it exists so we never repeat a mistake or re-derive an
> environment quirk. It is **append-only** — never delete or weaken an entry; if one is wrong,
> add a new entry that supersedes it and reference the old id.
>
> Workflow (mirrored in `agents.md` §0 and §7):
> 1. **Read** this file fully at the start of a session (grep by area tag once it grows).
> 2. **Follow** any existing entry that matches what you are seeing.
> 3. **Append** a new entry when you resolve something, using the template below.
> 4. **Guard** the fix with a test/tool so the entry never has to be learned twice.
>
> ```powershell
> Select-String -Path lessons_learned.md -SimpleMatch -Pattern 'Headless'   # find by area
> ```

## Entry template

```markdown
### LL-0NN — <one-line symptom, in the words you would search for>
- **Date / area / status:** YYYY-MM-DD · Tooling | Engine | Assets | Headless | PowerShell | Gameplay · resolved
- **Symptom:** what you observed (verbatim error text if short).
- **Root cause:** why it happened.
- **Fix:** what changed, with paths/commands.
- **Guard:** the test/tool that prevents recurrence (or "none — manual checklist").
- **References:** files or links.
```

## Index by area

| Area | Entries |
|---|---|
| Tooling | LL-001, LL-004, LL-005, LL-010 |
| Engine / C# | LL-003, LL-006, LL-011, LL-012, LL-015, LL-020, LL-021 |
| Assets | LL-007, LL-013, LL-014 |
| Headless | LL-002, LL-008, LL-017, LL-019 |
| PowerShell | LL-009, LL-018 |
| Gameplay / domain | LL-016, LL-022 |

---

## Verified environment facts (2026-10-23)

Measured on this machine, not assumed. Re-verify if the machine changes; don't re-derive blindly.

| Fact | Value |
|---|---|
| Engine | `4.7.2.stable.mono.official.ed1daf0bf`, editor build |
| Mono binary | `Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64(_console).exe` |
| Non-mono binary | `Godot_v4.7.2-stable_win64.exe\…` — **cannot run C#** |
| .NET SDK | 9.0.205 (only SDK installed); runtimes 6.0.36 / 8.0.32 / 9.0.6 |
| Cached ref pack | `Microsoft.NETCore.App.Ref 8.0.15` (so net8.0 builds offline) |
| GodotSharp target | `lib/net8.0` only ⇒ game TFM must be `net8.0` |
| SDK packages | `Godot.NET.Sdk` / `Godot.SourceGenerators` / `GodotSharp` 4.7.2 in `GodotSharp\Tools\nupkgs` |
| nuget.org | reachable (HTTP 200) but **not required** thanks to the local source |
| Headless flags present | `--headless`, `--path`, `--scene`, `--import`, `--quit-after`, `--fixed-fps`, `--log-file`, `--build-solutions`, `-s/--script` |
| First `--import` | 163.5 s (104-step import ×178 models + 237 PNGs) |
| Assets | 178 models (150 OBJ, 14 FBX, 14 GLB), 237 PNGs; audit exit 0 |

---

## Tooling

### LL-001 — Headless run starts the main scene instead of the test scene
- **Date / area / status:** 2026-10-23 · Tooling · resolved
- **Symptom:** `godot --headless res://Tests/Headless/TestRunner.tscn` never ran the suite (the path was
  ignored / main scene booted instead).
- **Root cause:** Godot 4.7 removed the positional scene argument. Pass the scene with `--scene <path>`;
  `--path` and `--scene` are editor-build overrides (`X`-flagged in `--help`).
- **Fix:** `tools/test.ps1` invokes
  `--path "Forager Clone Project" --scene res://Tests/Headless/TestRunner.tscn`.
- **Guard:** `tools/common.ps1` (`Invoke-Godot`) is the only sanctioned way to start the engine;
  `agents.md` §1/§3 documents the flags.
- **References:** `Godot --help`; `tools/test.ps1`.

### LL-004 — `Godot.NET.Sdk/4.7.2` is not in the machine NuGet cache
- **Date / area / status:** 2026-10-23 · Tooling · resolved
- **Symptom:** `godot.net.sdk` absent from `%USERPROFILE%\.nuget\packages`; a clean restore depended on the
  network and could pull a GodotSharp that disagrees with the installed engine.
- **Root cause:** Godot ships its .NET packages inside the editor folder (`GodotSharp\Tools\nupkgs`).
- **Fix:** repo-root `NuGet.Config` with `<clear />`, then a **relative** source
  `Godot_v4.7.2-stable_mono_win64\GodotSharp\Tools\nupkgs`, then nuget.org. Restore takes ~1.5 s and works
  offline.
- **Guard:** committed `NuGet.Config`; every gate build exercises it.
- **References:** `NuGet.Config`, `GodotSharp\Tools\nupkgs\Godot.NET.Sdk.4.7.2.nupkg`.

### LL-005 — Paths with spaces break unquoted commands
- **Date / area / status:** 2026-10-23 · Tooling · resolved
- **Symptom:** `--path Forager Clone Project` splits into two arguments.
- **Root cause:** repo root is `…\Forager Clone`, project is `Forager Clone Project`.
- **Fix:** always quote; in PowerShell pass argument **arrays** (`@('--path', $projectPath)`), never build
  command strings by hand.
- **Guard:** `tools/common.ps1` splats arrays for every engine invocation.
- **References:** `tools/common.ps1`.

### LL-010 — Arguments after `--` are dropped when calling a PowerShell script
- **Date / area / status:** 2026-10-23 · Tooling · resolved
- **Symptom:** `--filter=smoke` / `--seed=42` never reached `OS.GetCmdlineUserArgs()`.
- **Root cause:** PowerShell 5.1 treats `--` as end-of-parameters for script/cmdlet binding and drops it.
- **Fix:** `tools/godot.ps1` exposes named `-Arguments` and `-UserArguments`; the wrapper appends `--`
  itself only when user arguments exist.
- **Guard:** every headless run prints `filter=… seed=…` in its header, so a mis-parse is visible.
- **References:** `tools/godot.ps1`, `tools/common.ps1`, `Tests/Headless/RunnerOptions.cs`.

---

## Engine / C#

### LL-003 — The game project must target `net8.0`, but SDK templates refuse `net8.0`
- **Date / area / status:** 2026-10-23 · Engine · resolved
- **Symptom:** `dotnet new classlib -f net8.0` fails with templating **exit code 127**; the template only
  offers `net9.0`, `netstandard2.0`, `netstandard2.1`.
- **Root cause:** only the .NET 9 SDK is installed, but GodotSharp 4.7.2 ships `lib/net8.0` and
  `GodotPlugins.runtimeconfig.json` pins `"tfm": "net8.0"`.
- **Fix:** scaffold with the default TFM, then set `<TargetFramework>net8.0</TargetFramework>` by hand.
  `Microsoft.NETCore.App.Ref 8.0.15` is cached, so the build stays offline-capable.
- **Guard:** `Directory.Build.props` documents the rule; drifting breaks assembly load immediately.
- **References:** `GodotSharp\Api\Release\GodotPlugins.runtimeconfig.json`; `GodotSharp…nupkg/lib/net8.0`.

### LL-006 — `error CS0104: 'FileAccess' is an ambiguous reference`
- **Date / area / status:** 2026-10-23 · Engine · resolved
- **Symptom:** `error CS0104: 'FileAccess' is an ambiguous reference between 'Godot.FileAccess' and
  'System.IO.FileAccess'`.
- **Root cause:** `ImplicitUsings` imports `System.IO`; Godot defines its own `FileAccess`.
- **Fix:** qualify — `Godot.FileAccess.GetFileAsString("res://Data/items.json")`.
- **Guard:** convention in `agents.md` §4; engine build is part of the gate.
- **References:** `Tests/Headless/Cases/ContentLoadTests.cs`.

### LL-011 — `Mesh.SurfaceGetArrayLen` does not exist in 4.7
- **Date / area / status:** 2026-10-23 · Engine · resolved
- **Symptom:** `error CS1061: 'Mesh' does not contain a definition for 'SurfaceGetArrayLen'`.
- **Root cause:** only the virtual `_SurfaceGetArrayLen` is bound; the public API is the array accessor.
- **Fix:** `var arrays = mesh.SurfaceGetArrays(surface);
  vertices += arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array().Length;`
- **Guard:** audit compiles and reports plausible vertex counts (`allinone.fbx` = 23 439).
- **References:** `GodotSharp.xml` → `M:Godot.Mesh.SurfaceGetArrays`; `Tests/Headless/AssetAudit.cs`.

### LL-012 — `ResourceLoader.Load<PackedScene>` throws `InvalidCastException` on `.obj`
- **Date / area / status:** 2026-10-23 · Engine · resolved
- **Symptom:** `System.InvalidCastException: Unable to cast object of type 'Godot.ArrayMesh' to type
  'Godot.PackedScene'`; the audit died after 7 of 178 models.
- **Root cause:** `.obj` imports as an `ArrayMesh` resource; `.fbx`/`.glb` import as `PackedScene`. A
  typed load does not return null — it throws.
- **Fix:** load untyped and dispatch on the result:
  `switch (ResourceLoader.Load(path)) { case Mesh m: …; case PackedScene s: …; default: fail }`.
- **Guard:** the audit walks all 178 models and exits 0 (`models=178 png=237 failures=0`).
- **References:** `Tests/Headless/AssetAudit.cs`.

### LL-015 — CA1711 wants the domain type `ItemStack` renamed
- **Date / area / status:** 2026-10-23 · Engine · resolved
- **Symptom:** `warning CA1711: Rename type name ItemStack so that it does not end in 'Stack'`.
- **Root cause:** CA1711 targets collection types; `ItemStack` is a game-domain value type.
- **Fix:** `NoWarn $(NoWarn);CA1711` in `Directory.Build.props` with a justification comment, rather than
  contorting the domain model.
- **Guard:** the build must stay at 0 warnings (`agents.md` §8).
- **References:** `Directory.Build.props`, `src/Forager.Core/Items/ItemStack.cs`.

### LL-020 — Asset audit was extremely slow (`Mesh.GetFaces()` on 178 models)
- **Date / area / status:** 2026-10-23 · Engine · resolved
- **Symptom:** the audit exceeded 300 s and never finished.
- **Root cause:** `GetFaces()` materialises a full triangle array per mesh (some models are 23 k+ verts and
  skinned), repeated across 178 models.
- **Fix:** drop triangle counting; read only the vertex array via `SurfaceGetArrays`. Full audit now
  completes in seconds.
- **Guard:** `tools/audit-assets.ps1` runs inside `verify.ps1`; a regression shows up as a timeout.
- **References:** `Tests/Headless/AssetAudit.cs`.

### LL-021 — `Engine.GetVersionInfo()["string"]` is a `Variant`
- **Date / area / status:** 2026-10-23 · Engine · resolved
- **Symptom:** dictionary indexing returns `Variant`; interpolation is unreliable and a missing key throws.
- **Root cause:** `GetVersionInfo()` returns `Godot.Collections.Dictionary`.
- **Fix:** `Engine.GetVersionInfo().TryGetValue("string", out var v) ? v.AsString() : "unknown"`.
- **Guard:** both the game and the test runner print a version header on every run.
- **References:** `Scripts/Game/Main.cs`, `Tests/Headless/TestRunner.cs`.

---

## Assets

### LL-007 — `string.GetExtension()` has no leading dot
- **Date / area / status:** 2026-10-23 · Assets · resolved
- **Symptom:** `extension == ".obj"` was never true, so every `.obj` went down the scene path (which then
  crashed — see LL-012).
- **Root cause:** Godot's `GetExtension()` mirrors GDScript's `get_extension()` and returns `"obj"`.
- **Fix:** don't branch on extensions at all (LL-012); where a comparison is unavoidable, compare against
  `"obj"` with `OrdinalIgnoreCase`.
- **Guard:** audit exit 0 across all 150 OBJ files.
- **References:** `Tests/Headless/AssetAudit.cs`.

### LL-013 — Chibi FBX characters import ~100× too small; animals ~15–20× too small
- **Date / area / status:** 2026-10-23 · Assets · resolved
- **Symptom:** the same character reports `size=0.01x0.01x0.02` via FBX but `1.87x2.23x1.84` via GLB.
  Animal FBX are `0.02–0.10 m` tall (a cow at 9 cm).
- **Root cause:** the FBX exports carry a unit scale the importer doesn't normalise; the GLB exports are
  authored at metre scale.
- **Fix:** use `ChibiCharacters\glb\*.glb` for all characters (also matches the asset readme's material
  advice). Animals stay FBX but get an **explicit** scale node (~15–20×) inside their prefab.
- **Guard:** the audit prints per-model sizes; character/animal prefabs must assert their normalised height
  in a headless test (added in M1).
- **References:** `artifacts/asset-audit.log`, `Docs/asset_inventory.md`.

### LL-014 — Animation clip names differ per format and per character
- **Date / area / status:** 2026-10-23 · Assets · resolved
- **Symptom:** looking up `"run"` fails: GLB chibis expose `anim_walk` / `anim_run` (and the typo
  `anim_iddle`); knight/ninja use `walkanim_` / `runanim_`; FBX chibis prefix `root_armature_student|anim_walk`;
  animals use `Armature|Walk`, `Armature|Idle`, plus `WalkSlow`/`Jump`/`Death`.
- **Root cause:** three exporters, three naming conventions.
- **Fix:** resolve clips by **suffix** (`name.EndsWith("anim_walk")`, then `"walkanim_"`, then `"|Walk"`) behind
  one mapping table; never hardcode a prefix or exact string.
- **Guard:** must be covered by a headless clip-resolution test when the player animation is wired (M1).
- **References:** `artifacts/asset-audit.log` (94 distinct clip names across the project).

---

## Headless testing

### LL-002 — Headless runs cannot load models until `--import` has run once
- **Date / area / status:** 2026-10-23 · Headless · resolved
- **Symptom:** on a fresh checkout, `ResourceLoader.Load` of any `.glb/.fbx/.obj` fails or returns null.
- **Root cause:** import happens in the editor pass and the cache lives in `.godot/` (gitignored).
- **Fix:** `tools/import-assets.ps1` → `--headless --path "<proj>" --import` (first run 163.5 s / 224 steps).
  `tools/test.ps1` does this automatically unless `-SkipImport`.
- **Guard:** gate ordering (build → test → import → engine tests); import failure is a gate failure.
- **References:** `tools/import-assets.ps1`, `tools/test.ps1`.

### LL-008 — An unhandled exception in a headless tool scene hangs forever
- **Date / area / status:** 2026-10-23 · Headless · resolved
- **Symptom:** after the audit threw, the process lived past 300 s and left four stray `Godot_*` processes;
  `GetTree().Quit()` was never reached.
- **Root cause:** `_Ready` is a void entry point — exceptions are logged by the bridge and execution simply
  stops, so nothing calls `Quit`.
- **Fix:** wrap tool entry points in `try/catch/finally` and call `GetTree().Quit(code)` in `finally`;
  per-item `try/catch` so one bad asset doesn't abort the whole report. Kill stray `Godot_*` processes
  before a re-run.
- **Guard:** `AssetAudit._Ready` structure + the `--timeout` watchdog in `TestRunner` (exit code 2).
- **References:** `Tests/Headless/AssetAudit.cs`, `Tests/Headless/TestRunner.cs`.

### LL-017 — Tests could not locate the repo root during bootstrap
- **Date / area / status:** 2026-10-23 · Headless · resolved
- **Symptom:** three `ItemDatabaseTests` failed with
  `DirectoryNotFoundException: Could not locate the repository root above '…\bin\Debug\net8.0\'`.
- **Root cause:** `TestPaths` required `agents.md` to exist, but the docs are written after the code.
- **Fix:** marker is now the project folder **plus any of** `Directory.Build.props` / `agents.md` / `.git`.
- **Guard:** the whole Tier-1 suite passes (41 tests).
- **References:** `tests/Forager.Tests/TestPaths.cs`.

### LL-019 — A green suite proves nothing until you have seen it go red
- **Date / area / status:** 2026-10-23 · Headless · resolved
- **Symptom:** the harness reported `PASS`/exit 0, but there was no evidence a failing test would actually
  fail the build gate (assertion plumbing, exit-code propagation, summary counting were all unproven).
- **Root cause:** exit-code wiring is exactly the kind of thing that silently "works" while always
  returning 0.
- **Fix / verification:** injected a deliberately failing test and confirmed
  `FAIL GateSelfCheck (2 ms)` + `SUMMARY total=1 passed=0 failed=1` + **exit code 1**, then removed it and
  rebuilt.
- **Guard:** `tools/selftest-gate.ps1` automates that proof (inject → expect exit 1 → clean up → rebuild).
  Run it after touching `TestRunner` or `tools/test.ps1`.
- **References:** `tools/selftest-gate.ps1`, `artifacts/gate-selftest.log`.

---

## PowerShell / Windows

### LL-009 — PowerShell functions return native stdout, so exit codes become arrays
- **Date / area / status:** 2026-10-23 · PowerShell · resolved
- **Symptom:** `FAILED: asset import (exit Godot Engine v4.7.2… [  0% ] first_scan_filesystem … 0)` — the
  "exit code" was the whole engine log with `0` appended.
- **Root cause:** a function emits everything written to its pipeline; `$code = Invoke-Godot …` captured the
  native stdout plus the returned int, and `$code -ne 0` was then always true.
- **Fix:** capture native output into a variable, `| Out-Host` it, and `return [int]$LASTEXITCODE`.
  Also flatten stderr to strings (`| ForEach-Object { "$_" }`), otherwise engine error lines render as
  PowerShell `RemoteException` blocks around the real message.
- **Guard:** `tools/common.ps1` `Invoke-Godot` is the single implementation; `verify.ps1` exits 0 end-to-end.
- **References:** `tools/common.ps1`.

### LL-018 — `Inventory.IsFull(item)` semantics were ambiguous
- **Date / area / status:** 2026-10-23 · PowerShell · resolved
- **Symptom:** a test asserted `IsFull("stone") == false` while both slots held full wood stacks.
- **Root cause:** two reasonable readings: "no empty slot" vs "cannot fit *this* item now".
- **Fix:** implemented (and documented) the gameplay-relevant one — "this item cannot fit now": true when
  there is no empty slot and no partial stack of that item. Two tests pin both directions.
- **Guard:** `InventoryTests.Add_ReturnsLeftover_WhenInventoryIsFull`,
  `InventoryTests.IsFull_IsFalse_WhenAnEmptySlotRemains`.
- **References:** `src/Forager.Core/Inventories/Inventory.cs`.

---

## Gameplay / domain

### LL-016 — `Inventory.TryRemove` removed items even when it returned `false`
- **Date / area / status:** 2026-10-23 · Gameplay · resolved
- **Symptom:** `TryRemove("wood", 5)` with 3 held returned `false` **and** deleted the 3 wood.
- **Root cause:** the implementation delegated to `Remove` (which partially removes) and compared counts
  afterwards.
- **Fix:** check `Count(item) >= amount` first; only then mutate.
- **Guard:** `InventoryTests.TryRemove_IsAtomic_WhenInsufficient`.
- **References:** `src/Forager.Core/Inventories/Inventory.cs`.

### LL-022 — `string.GetHashCode()` is randomised per process; worldgen seeds must not use it
- **Date / area / status:** 2026-10-23 · Gameplay · resolved
- **Symptom:** (prevented) fork labels would produce different streams between runs, so seeded worlds could
  not be reproduced or snapshot-tested.
- **Root cause:** .NET randomises string hashing per process (hash-flooding protection).
- **Fix:** `SeededRng.StableHash` uses FNV-1a over UTF-16 chars; `Fork(seed, label)` mixes it with the seed.
  Pinned values: empty → `0x811C9DC5`, `"worldgen"` → `0xB505B30F`.
- **Guard:** `SeededRngTests.StableHash_IsIndependentOfProcessRandomization`,
  `SeededRngTests.Fork_IsDeterministic_AndLabelDependent`.
- **References:** `src/Forager.Core/Randomness/SeededRng.cs`.




