# Testing — all headless

Everything is validated **without a window, GPU, audio device or editor UI**. A change is done only
when `tools\verify.ps1` exits 0 (see `agents.md` §8).

## Quick reference

| Goal | Command |
|---|---|
| Full gate (build + unit + engine + audit) | `tools\verify.ps1` |
| Fast loop after code-only changes | `tools\test.ps1 -SkipImport` |
| Unit tests only | `tools\test.ps1 -UnitOnly` (or `dotnet test tests\Forager.Tests\Forager.Tests.csproj`) |
| Engine tests only | `tools\test.ps1 -EngineOnly -Filter smoke -SkipImport` |
| Import assets (first run / after adding art) | `tools\import-assets.ps1` |
| Asset audit | `tools\audit-assets.ps1` |
| Prove the gate fails | `tools\selftest-gate.ps1` |
| Raw engine run | `tools\godot.ps1 -Arguments '--headless','--path','Forager Clone Project','--scene','res://Tests/Headless/TestRunner.tscn' -UserArguments '--filter=all'` |

Logs land in `artifacts\` (`headless-tests.log`, `asset-audit.log`, `gate-selftest.log`).

## Tier 1 — `dotnet test` (no engine)

41 tests today. Coverage: inventory stacking/overflow/atomicity, item data validation (loads the
**real** `Data/items.json` from the Godot project folder), sim clock rollover/night window, seeded RNG
and fork determinism (pinned FNV constants), architecture guards (Core has no Godot reference and no
unexpected dependencies), data-source abstraction.

Why it matters: ~90 % of future rules (crafting, skills, economy, worldgen) can be tested in milliseconds
with no engine in the loop.

## Tier 2 — in-engine headless suite

`res://Tests/Headless/TestRunner.tscn` → `TestRunner.cs`
- discovers every `[HeadlessTest]` class deriving `HeadlessTest`, ordered by full name;
- fresh instance per test inside a live `Node` host, so `_Ready`, physics, raycasts and signals behave
  exactly as in game;
- `--filter=all|tag|NameFragment`, `--seed=<int>`, `--timeout=<seconds>`;
- prints `PASS <Name> (<ms>)` / `FAIL <Name> (<ms>)` + indented failure reasons, then
  `[tests] SUMMARY total=… passed=… failed=…`;
- refuses to run if `DisplayServer.GetName() != "headless"`.

Exit codes: `0` pass · `1` test failure · `2` global timeout · `3` not headless · `4` filter matched nothing.

Writing one:

```csharp
[HeadlessTest(Tags = "input,player")]
public partial class PlayerMovementTests : HeadlessTest
{
    public override async Task RunAsync(HeadlessTestContext context)
    {
        var body = new CharacterBody3D { Name = "Probe" };
        AddChild(body);
        await context.StepPhysics(1);                      // deterministic stepping
        context.AssertNear(1.0, body.GlobalPosition.Y, 0.01, "player settles on the floor");
    }
}
```

Determinism rules: step physics frames (never `CreateTimer`), drive time through `SimClock`, seed through
`context.Rng`/`--seed`, assert with tolerances (`AssertNear`) for anything physics-derived.

Current tests: `HarnessSmokeTests` (headless enforcement, frame stepping, Core usable from the engine),
`InputActionsTests` (code-registered actions are idempotent), `ContentLoadTests` (`res://Data/items.json`
parses through the engine, `Main.tscn` instantiates with meshes and a camera).

## Tier 3 — asset audit

`tools\audit-assets.ps1` loads **all 178 models** (150 OBJ, 14 FBX, 14 GLB) plus counts 237 PNGs, and prints
one `MODEL|…` line each: kind, meshes, surfaces, vertices, bones, clips, materials, bounds, clip names.
Exit non-zero if any asset fails to load. Run it after touching `Assets/`, and copy the numbers into
`Docs/asset_inventory.md`.

## What headless cannot verify

Lighting/material fidelity, particle feel, UI pixel alignment, camera feel, animation smoothness.
Compensate with data/scene-graph assertions and a short manual eyeball checklist when a milestone lands:

1. Player model is the right size vs trees (≈1.5 m vs 3.5–5 m).
2. No magenta/missing materials on chibi or trees.
3. Camera keeps the player centred and does not clip through geometry.
4. UI text is legible at 1280×720 with the pixel TTF.
