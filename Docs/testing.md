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

189 tests. Coverage: inventory stacking/overflow/atomicity/capacity + snapshot rollback, item data validation (loads
the **real** `Data/items.json`), tool kind/tier gating and best-tool selection, node data validation
(`Data/nodes.json`: tools, tiers, drops, mesh paths, caps), balance validation (`Data/balance.json`), harvest
rules (tool gating, cooldown, depletion, drop ranges, respawn), recipe data validation (stations, unlocks,
cycles, unknown items), recipe book unlock semantics, craft atomicity (missing inputs, wrong station, locked,
no-space rollback), crafting-content reachability, XP curve + profile level-ups/coins, save round-trip (v2 +
v1 payloads), seeded RNG/fork determinism, clip-name resolution for all four asset naming styles, fixed-camera
maths, icon-atlas bounds, deterministic plot placement, sim clock rollover/night window, architecture guards.

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

M2 suite (22 tests). Added on top of the M1 set below: `StationProximityTests` (data-driven station radius),
`StationInteractTests` (E opens the workbench, prompt text, close clears state), `UiBlocksMovementTests`
(panel open freezes movement, closing resumes), `CraftingPanelTests` (recipes listed, have/need text,
craft via row, level-locked row disabled with "Lv 2"), `InventoryPanelTests` (24 slots, icons, UI flag),
`ToolTierTests` (stone axe outranks wood axe; gold vein needs a tier-2 pickaxe), `OreNodeTests` (iron vein →
ore in inventory), `CraftingSaveTests` (crafted set survives save/load).

M1 suite (14 tests): `HarnessSmokeTests` (headless enforcement, frame stepping, Core usable from the
engine), `InputActionsTests` (code-registered actions are idempotent), `ContentLoadTests` (`res://Data/*.json`
parse through the engine, `Main.tscn` builds the slice), `MovementTests` (walk distance, run > walk, fixed
camera geometry), `CameraRelativeMoveTests` (yaw 90 ⇒ forward is −X), `PlayerAssetTests` (GLB normalises to
~1.5 m and all four clips resolve), `NodeFactoryTests` (mesh + collider + grounded pivot per node type),
`AimRayTests` (in-reach hit, out-of-reach rejected, sky rejected), `HarvestTreeTests` (3 swings ⇒ wood,
xp, depletion), `MissingToolTests` (rock without pickaxe ⇒ no damage + HUD reason), `PickupMagnetTests`,
`RespawnTests`, `SaveLoadRoundTripTests`, `HudBindingTests`.

Test seams that make this possible (all documented in code):

| Need | Seam |
|---|---|
| No keyboard | `PlayerController.ApplyMoveInput(Vector2, bool run)` |
| No mouse | `Interactor.AimAt(Vector3)` + `TryHarvestCurrentTarget()` |
| No wall clock | `GameSession.AdvanceClock = false`, then `Session.Clock.Advance(...)` |
| No real save slot | `session.Persistence = new InMemoryPersistence()` |
| Known camera | `CameraRig.SetYawDegrees(deg)` (injected, never discovered by name) |
| No mouse for stations | `Interactor.TryInteractCurrentTarget()` |
| No UI clicks | `CraftingPanel.TryCraft(rowIndex)` after `session.OpenCrafting(kind)` |
| Free items | `SliceFixture.GiveItems(("wood", 6), …)` |

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
