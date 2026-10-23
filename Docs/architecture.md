# Architecture — 3D Forager Clone

Engine: **Godot 4.7.2 (mono/.NET 8)** · Language: **C# only** · Validation: **headless only**
Camera/control model: **fixed top-down-ish angled camera + camera-relative WASD** (Forager feel)

## 1. Layering

```
                 Data/*.json  (content: items, recipes, nodes, perks, biomes)
                        │
                        ▼
   ┌──────────────────────────────────────────────┐
   │ Forager.Core  (net8.0, NO Godot reference)   │   ← 100 % unit-testable (Tier 1)
   │ Items · Inventories · Crafting · Economy     │
   │ Skills · World · Agents · Sim · Save · Rng   │
   └──────────────────────────────────────────────┘
                        │  ProjectReference
                        ▼
   ┌──────────────────────────────────────────────┐
   │ ForagerClone (Godot assembly)                │   ← thin adapters (Tier 2 tests)
   │ Scripts/**: Player, Interaction, Nodes, UI   │
   └──────────────────────────────────────────────┘
                        │
                        ▼
   Scenes/*.tscn + Assets/*   (vendor art, read-only)
```

Rules enforced by tests/tooling:
- `Forager.Core` never references `Godot*` (`ArchitectureTests.CoreAssembly_DoesNotReferenceGodot`).
- Content is data, not code; `ItemDatabase.Validate()` + data tests guard `Data/*.json`.
- Godot nodes contain no game rules: input → Core call → presentation.

## 2. Assemblies

| Project | TFM | Purpose |
|---|---|---|
| `src/Forager.Core/Forager.Core.csproj` | net8.0 | pure rules & state |
| `Forager Clone Project/ForagerClone.csproj` | net8.0 (`Godot.NET.Sdk/4.7.2`) | game assembly, `AssemblyName=ForagerClone` |
| `tests/Forager.Tests/Forager.Tests.csproj` | net8.0 (xUnit) | Tier-1 suite, references Core only |

Build output lands in `Forager Clone Project/.godot/mono/temp/bin/Debug/` (Godot.NET.Sdk default) — that is
why the gate always runs `dotnet build` before launching the engine.

## 3. Core modules (current + planned)

| Module | Status | Responsibility |
|---|---|---|
| `Forager.Core.Items` (`ItemDefinition`, `ItemDatabase`, `ItemStack`, `IStackLimits`) | **done** | item data, stack limits, validation |
| `Forager.Core.Data` (`IDataSource`, `FileSystemDataSource`, `InMemoryDataSource`) | **done** | same loaders in engine and tests |
| `Forager.Core.Inventories.Inventory` | **done** | slot grid, stacking, add/remove, change events |
| `Forager.Core.Sim.SimClock` | **done** | tick-based time, day rollover, night window |
| `Forager.Core.Randomness.SeededRng` | **done** | deterministic RNG + stable forks |
| `Forager.Core.Crafting` | M2 | recipes, station tiers, unlock gates |
| `Forager.Core.Skills` | M6 | 4-branch perk board, adjacency validation, effect aggregation |
| `Forager.Core.Economy` | M6 | coins, land-price curve, market spread, bank interest |
| `Forager.Core.World` | M3 | 16 m plots, seeded biome/node layout, purchase state |
| `Forager.Core.Agents` | M4 | grid A* + simple state machines for NPCs/enemies |
| `Forager.Core.Save` | M1/M7 | versioned save DTOs + migrations behind `IPersistence` |

## 4. Godot layer

```
Main (Node3D)                     Scripts/Game/Main.cs  — boots input actions, reports state
├── WorldEnvironment, Sun         flat stylized lighting (no SSAO/SSR)
├── CameraRig (Node3D)            fixed pitch −55°, yaw 0, distance ~14 m, smooth follow
│   └── SpringArm3D               collision-safe camera distance
├── Player (CharacterBody3D)      camera-relative WASD, run/jump, anim via AnimationTree
│   └── Model (GLB chibi)         scale normalised, clip names resolved by suffix (LL-014)
├── WorldRoot                     PlotStreamer → active 16 m plots + neighbours
└── UiLayer (CanvasLayer)         Sprout Lands theme: HUD, inventory, craft, skill board
```

- **Input**: registered in code (`Scripts/Game/InputActions.cs`) rather than `project.godot`, so bindings are
  reviewable and a headless test can assert them (`InputActionsTests`). Idempotent `EnsureRegistered()`.
- **Interaction** (M1): mouse → `Camera3D.ProjectRayNormal` → `RayCast3D` on the `Interactable` layer;
  hold LMB uses the equipped tool, `E` is the keyboard fallback (also what tests drive).
- **Physics layers**: 1 World · 2 Player · 3 Interactable · 4 Enemy · 5 Building · 6 Trigger · 7 Water.
- **Scale**: 1 unit = 1 metre. Cell size 2 m. Trees 3.5–5 m, player ~1.5 m, animals normalised to ~1–1.5 m.

## 5. Data schemas (target)

```jsonc
// Data/items.json  (implemented)
{ "items": [ { "id": "wood", "name": "Wood", "maxStack": 99, "value": 2,
               "tags": ["resource"], "icon": "icon_wood" } ] }

// Data/nodes.json  (M1)
{ "nodes": [ { "id": "birch", "kind": "tree", "tool": "axe", "hp": 3,
               "drops": [ { "item": "wood", "min": 1, "max": 2 } ],
               "respawnSeconds": 45, "prefab": "res://Scenes/Nodes/Birch.tscn" } ] }

// Data/recipes.json (M2)
{ "recipes": [ { "id": "wood_axe", "station": "workbench", "seconds": 1.5,
                 "inputs": { "wood": 5, "fiber": 2 }, "outputs": { "wood_axe": 1 },
                 "unlock": "skill.industry.tools_1" } ] }
```

## 6. Determinism & save

- Time only advances through `SimClock`; randomness only through `IRng`.
- Save model (planned): `{ version, seed, simSeconds, coins, xp, perks[], inventory[], plots[], nodes{} }`,
  written through `IPersistence` (Godot impl uses `user://saves/slot0.json`, tests use an in-memory store).
- Same DTOs are exercised by both tiers, so a save written in a test loads in the game.

## 7. Performance strategy

- Static decoration → `MultiMeshInstance3D`; only harvestable nodes near the player get physics bodies.
- Plot-based streaming (active plot + 8 neighbours); everything else hidden, not freed.
- One material override per stylized group (unlit chibi, flat-colour trees) to keep draw calls low.
- Target: 1280×720 @ 60 fps, 60 physics ticks/second (matches the harness `--fixed-fps 60`).

## 8. Milestone map

| # | Scope | Gate evidence |
|---|---|---|
| M0 ✅ | toolchain, guardrails, Core skeleton, headless harness, asset audit | `tools/verify.ps1` exit 0; 41 unit + 3 engine tests; audit 178 models |
| M1 | player + camera, harvest trees/rocks/bushes, drops, inventory HUD, XP, save | + player/harvest/save tests |
| M2 | crafting stations + recipes + tool tiers + icon atlas | + recipe/atlas tests |
| M3 | plot grid, buy land, biomes, streaming | + worldgen snapshot tests |
| M4 | farming, animals, villagers, grid A* | + sim/path tests |
| M5 | combat, enemies, dungeons | + FSM/damage tests |
| M6 | skill board, economy, automation | + perk/economy tests |
| M7 | UI polish, audio, day/night, save slots | + regression suite |
| M8 | perf pass, export presets | perf budget + export check |

