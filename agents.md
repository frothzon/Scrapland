# agents.md — Operating rules for AI agents on **Forager Clone**

**Status: authoritative.** Read this file *and* `lessons_learned.md` before changing anything.
Last verified: **2026-10-23** against Godot **4.7.2-stable.mono**, .NET SDK **9.0.205**, target **net8.0**.

---

## 0. Read-first protocol (do this every session)

1. Read `lessons_learned.md` **completely** before planning work. It is the source of truth for
   "we already hit this, here is the fix". Grep it by area tag (`Tooling`, `Engine`, `Assets`,
   `Headless`, `PowerShell`, `Gameplay`) once it grows past a screen.
2. Do not re-derive anything that has a lesson. If you believe a lesson is wrong, **amend the
   entry with evidence** — never silently ignore or delete it.
3. Plan before editing. State the plan, then execute.
4. When you **resolve** an issue — anything that took more than one attempt, any environment or
   engine quirk, any bug class that could come back — append an entry using the template in §7.
   No entry, no fix: this is how the project stops repeating mistakes.
5. Regression guard: every bug-class lesson should end with a test or a tool that would catch it
   again. Add it in the same change.

---

## 1. Locked toolchain (change only with explicit human approval)

| Concern | Locked value |
|---|---|
| Engine | Godot **4.7.2-stable mono** (editor build) |
| Engine binary | `Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe` |
| Godot project root | `Forager Clone Project\` (this folder *is* `res://`) |
| Game assembly | `ForagerClone.csproj` → `Godot.NET.Sdk/4.7.2`, `TargetFramework=net8.0` |
| Domain library | `src\Forager.Core\Forager.Core.csproj` (net8.0, **no Godot reference**) |
| Unit tests | `tests\Forager.Tests\Forager.Tests.csproj` (xUnit, references Core only) |
| SDK | .NET SDK 9.0.205 builds net8.0 (ref pack 8.0.15 is cached locally) |
| NuGet | root `NuGet.Config`: local `GodotSharp\Tools\nupkgs` first, then nuget.org |

Rules:
- **Never** use `Godot_v4.7.2-stable_win64.exe` (non-mono) — it cannot load C# assemblies.
- **Never** bump the engine, SDK package version or TFM as a "cleanup". GodotSharp 4.7.2 ships
  `lib/net8.0` only; anything else breaks at load time (see `LL-003`).
- **Never** launch a windowed run to validate work. All validation is headless (§3).
- All commands go through `tools\*.ps1` or quote their paths — the repo path contains spaces.

---

## 2. Repository layout and layering

```
Forager Clone\                              repo root (git)
  agents.md  lessons_learned.md  NuGet.Config  Directory.Build.props  .gitignore
  tools\        common.ps1  godot.ps1  build.ps1  import-assets.ps1  test.ps1  audit-assets.ps1  verify.ps1
  src\Forager.Core\        pure C#: rules, data, simulation. NO Godot types.
  tests\Forager.Tests\     Tier-1 xUnit suite (runs without the engine)
  Docs\                    architecture.md, testing.md, asset_inventory.md, design\, ATTRIBUTION.md
  artifacts\               logs + generated reports (gitignored)
  Forager Clone Project\   Godot project root
    project.godot  ForagerClone.csproj
    Assets\                vendor art (READ ONLY)
    Scripts\               Godot node scripts (thin adapters over Core)
    Scenes\  Data\  Tests\Headless\
```

Hard layering rules:
1. `Forager.Core` must never reference Godot. `ArchitectureTests` fails the build if it does.
2. Godot scripts (`Scripts\**`) hold **no game rules** — input, physics, presentation, glue only.
   If it is balance, math or state that could be tested without a scene, it belongs in Core.
3. All content (items, recipes, nodes, perks, biomes) lives in `Data\*.json`. No hardcoded ids or
   numbers in code; balance changes must be data-only diffs.
4. Tests outside `res://` never get compiled into the game assembly (they live under `tests\`).

---

## 3. Testing policy — **everything runs headless** (non-negotiable)

No test, tool or validation step may require a window, GPU, audio device, display server or
real-time waiting. There are three tiers, all fanned in by `tools\test.ps1`:

### Tier 1 — pure logic (`dotnet test`, no engine)
```
dotnet test tests\Forager.Tests\Forager.Tests.csproj -c Debug
```
Covers inventory/stacking, item data validation, crafting rules, economy curves, skill-board
graph, worldgen determinism, save round-trip, sim clock, RNG, and architecture guards.

### Tier 2 — in-engine headless integration
```
tools\test.ps1                # build + Tier 1 + import + Tier 2
tools\test.ps1 -SkipImport    # when assets are already imported
tools\test.ps1 -EngineOnly -Filter smoke
```
The runner is `res://Tests/Headless/TestRunner.tscn` (`Tests/Headless/TestRunner.cs`). It is
started with `--headless --fixed-fps 60 --scene …` and receives `--filter=`, `--seed=`,
`--timeout=` after the `--` separator. Exit codes:

| Code | Meaning |
|---|---|
| 0 | all tests passed |
| 1 | at least one test failed (or an assertion in a test) |
| 2 | global timeout (watchdog) |
| 3 | refused: suite was not started with `--headless` |
| 4 | no tests matched the filter |

Writing a headless test:
```csharp
[HeadlessTest(Tags = "input,player")]
public partial class MyTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        await context.StepPhysics(10);                 // deterministic stepping
        context.AssertNear(1.0, actual, 0.01, "why it matters");
    }
}
```

### Tier 3 — asset audit
```
tools\audit-assets.ps1   # loads every model under res://Assets, prints MODEL|... lines
```
Any asset that fails to load fails the gate. Regenerate `Docs\asset_inventory.md` numbers from
`artifacts\asset-audit.log` when assets change.

### Determinism rules (enforced by review)
- No `DateTime.Now`, `Thread.Sleep`, `Task.Delay` inside gameplay logic. Time comes from
  `Forager.Core.Sim.SimClock`; randomness from `IRng`/`SeededRng`.
- Tests advance **physics frames**, never wall-clock timers.
- Seed is passed in (`--seed=1337` default) and logged in the run header.

### What headless cannot verify
Lighting, material fidelity, particle feel, UI pixel alignment, camera feel. Those need a human
eyeball pass; compensate with scene-graph/data assertions (materials assigned, meshes present,
icon rects in bounds, expected node counts).

---

## 4. Code conventions

- C# only. **No `.gd` files** — one language keeps the toolchain and tests uniform.
- File-scoped namespaces, one public type per file, nullable enabled, `partial` on every
  `GodotObject` subclass (source generator requirement).
- `[Export]` for tunables; `[Signal] public delegate void XEventHandler(...)` then
  `EmitSignal(SignalName.X, …)`.
- Namespaces: `Forager.Core.*` (logic), `ForagerClone.Scripts.*` / `ForagerClone.Game` (engine),
  `ForagerClone.Tests.Headless.*` (tests).
- Node lookup: `GetNode<T>("Path")` (typed) or `GetNodeOrNull<T>`; no string-based `Get("…")`.
- **Inject collaborators, don't discover them**: pass `CameraRig`/`GameSession`/`Clock` explicitly
  (`PlayerFactory.Create(..., rig: rig)`); a failed `FindChild` in `_Ready` is silent and skews results
  (`LL-023`). Name lookups are only a last-resort fallback.
- Register input actions idempotently (`InputActions.EnsureRegistered()`) in any node that reads input,
  so headless tests that skip `Main` never hit "InputMap action doesn't exist" (`LL-027`).
- Do not call `GD.Print` from Core. Logging happens in adapters/tests.
- Prefer composition over inheritance; small nodes with one job.

---

## 5. Asset rules (facts verified by the audit — do not "rediscover")

Vendor art is **read-only**: never rename, move, re-export or "tidy" it.

| Group | Use for | Notes |
|---|---|---|
| `ChibiCharacters\glb\*.glb` | player + NPCs | **Use the GLB, not the FBX.** Instantiates at 1.4–2.2 m of scene bounds with 11 clean clips (`anim_iddle`, `anim_walk`, `anim_run`, `anim_jump`, `anim_dying`, `anim_crouch`, `anim_crouchiddle`, `anim_uncrouch`, `anim_flip`, `anim_push`, `anim_iddle_001`) — scale the pivot to ~1.5 m of visible height. `glb emission\*` is the flat/unlit variant recommended by the asset readme. |
| `ChibiCharacters\fbx\*.fbx` | fallback only | Mesh AABB reports ~0.01 m (100× off vs GLB) and clip names carry a `root_armature_<char>` prefix. Avoid. |
| `3D Animals\FBX\*.fbx` | farm animals | Animated: Cow/Horse/Zebra have 6 clips (`Armature|Idle|Walk|WalkSlow|Run|Jump|Death`), Llama/Pig/Pug/Sheep have 2 (`Armature|Idle`, `Armature|Jump`). Tiny scale (~0.02–0.10 m) → normalize with an explicit scale node. |
| `3D Trees\OBJ\*.obj` | world resources | 150 untextured, flat-colour meshes (birch/willow/pine/palm/common, rocks, bushes, crops, logs, stumps, grass, lilypad, cactus). Heights 0.22–5.06 m. Origins are **not centred** — wrap in a pivot node. |
| `Sprout Lands - UI Pack` | HUD, menus, dialogs | 237 PNGs, 9-slice dialog boxes, buttons, icon sheets, pixel TTF (`pixelFont-7-8x14-sproutLands.ttf`), emoji, cursors. |

Material policy: stylized flat look → chibi models unlit/emission (or the `glb emission` set),
`metallic ≈ 0`, `roughness = 1`; trees keep MTL colours with roughness 1, no specular.

Clip-name resolution must be **suffix-based** (`name.EndsWith("anim_walk")`), because importers
and formats disagree on prefixes. Never hardcode a prefix (`LL-014`).

---

## 6. Shell / quoting rules for this machine

- Repo path contains spaces (`Forager Clone`, `Forager Clone Project`). Quote every path; use
  argument arrays in PowerShell, never string concatenation into a command line.
- PowerShell 5.1 consequences already learned: `--` is swallowed by parameter binding (`LL-010`),
  native stdout leaks into function return values (`LL-009`), regex escaping in `Select-String -Pattern`
  produces silent false negatives — use `-SimpleMatch` with plain substrings.
- Long-running Godot invocations: always pass `--log-file artifacts\<name>.log` so output survives
  a hung terminal, and check `artifacts\` before assuming a run produced nothing.
- If a Godot process is stuck (headless tool scene with an unhandled exception), kill stray
  `Godot_*` processes before re-running (`LL-008`).

---

## 7. `lessons_learned.md` protocol (the memory of this project)

`lessons_learned.md` is the **single source of truth for resolved problems**. It is append-only.

When to add an entry:
- an issue took more than one attempt to solve;
- an environment/engine behaviour surprised you;
- a bug class could plausibly return (regression risk);
- you found that a documented assumption was wrong.

Entry template (keep the headings exactly, they are greppable):
```markdown
### LL-0NN — <one-line symptom>
- **Date / area / status:** 2026-10-23 · Tooling · resolved
- **Symptom:** what you observed (verbatim error if short).
- **Root cause:** why it happened.
- **Fix:** what you changed, with commands/paths.
- **Guard:** the test/tool/check that prevents recurrence (or "none — manual checklist").
- **References:** files or links.
```
Rules: never rewrite or delete history; supersede with a new entry that references the old one;
put the *symptom* in the title so future greps match what they are seeing.

---

## 8. Definition of Done (every change)

1. `tools\verify.ps1` exits **0** (build + Tier 1 + Tier 2 + asset audit), or `tools\test.ps1` when
   assets are untouched. Paste/record the summary line in your report.
2. New behaviour has at least one automated test at the right tier; bug fixes add a guard test.
3. `lessons_learned.md` updated if §7 applies; `Docs\architecture.md` / `Docs\asset_inventory.md`
   updated if structure or assets changed.
4. No new files in `Assets\`, no edits to `.godot\`, no committed build output.
5. Everything still runs windowless; nothing depends on the editor being open.

---

## 9. Do-not-do list

- ❌ Run or require the Godot **editor UI**, a window, a GPU or an audio device for validation.
- ❌ Add GDScript, or hand-edit `.import` / `.godot\` files.
- ❌ Put game rules in Godot nodes, or Godot types in `Forager.Core`.
- ❌ Hardcode balance numbers/item ids in code (put them in `Data\*.json`).
- ❌ Rename, move, re-export or delete vendor assets.
- ❌ Change engine version, TFM, or NuGet sources to "make it build".
- ❌ Use `Task.Delay`/`DateTime.Now` for gameplay timing (breaks determinism).
- ❌ Commit `artifacts\`, `bin\`, `obj\`, `.godot\`, or the engine binaries.
- ❌ Ignore a `lessons_learned.md` entry that applies — amend it with evidence instead.


