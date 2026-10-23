# Design — Forager mechanics adapted to 3D

Goal: keep Forager's *loop* (gather → craft → build → buy land → automate → prestige-ish perks) while using
3D space and the assets we actually have.

Reference structure (consulted 2026-10-23): Forager combines idle-game resource growth with an adventure game:
harvesting, crafting, cooking, farming, fishing, mining, hunting, magic; everything grants XP; coins buy more
land; a four-branch perk **board** (Economy / Industry / Farming / Magic) unlocks deeper play; automation
(notably the mining rod) becomes the mid-game engine.

## Core loop (target)

1. **Gather** — walk up, hold the tool, multi-hit a node (tree → wood, rock → stone, bush → berry/fiber).
2. **Craft** — workbench unlocks the first tools; the furnace/cauldron/loom gate the next tier.
3. **Build** — place workstations and production buildings on the plot grid.
4. **Expand** — spend coins to buy adjacent 16 m plots, revealing new biomes/resources (totem/lever UI).
5. **Specialise** — spend XP on the perk board; branch into economy, industry, farming or magic.
6. **Automate** — drills/banks/markets generate value while you roam; dungeons gate rare materials.

## 3D translation of the 2D original

| Forager (2D) | Our 3D version | Assets |
|---|---|---|
| Tile-top-down, fixed camera | Fixed angled camera (pitch −55°, yaw 0, 14 m), camera-relative WASD, `E`/LMB to act | — |
| 16×16 pixel land chunks | 16 m × 16 m **plots** on a grid, bought with coins, one biome each | birch/autumn/dead/snow trees |
| Hit a tree 3× for wood | `CharacterBody3D` + `RayCast3D`; node HP, tool tier, hit flash, stump remains, respawn timer | 150 OBJ props |
| Item grid inventory | Core `Inventory` (slots, stack limits) + Sprout Lands HUD | icon sheets |
| Crafting menu | Workbench proximity opens 9-slice craft panel; recipes from `Data/recipes.json` | dialog boxes, TTF |
| Farm plots + animals | Crop plots (wheat/corn/cotton) and pens (cow/sheep/pig/llama) with product cooldowns | animals FBX + crops |
| Skill board | Perk grid with adjacency rules, rendered with Sprout Lands icons/emotes | icons |
| Dungeons | Instanced room scene with waves of knight/ninja chibi enemies, chest loot | chibi GLB |
| Villagers/traders | Chibi NPCs on grid A* patrols, merchant shop UI | chibi GLB |

## Data-driven content (single source of truth)

`Data\items.json` (done) · `Data\nodes.json` · `Data\recipes.json` · `Data\buildings.json` ·
`Data\skills.json` · `Data\biomes.json` · `Data\loot.json`. Every schema is validated by a Tier-1 test
(ids unique, references resolve, icon names exist, no cycles in recipe graph).

## Milestones with testable acceptance criteria

**M1 — vertical slice (next)**
- Player rig: `CharacterBody3D` (capsule r=0.3, h=1.0), walk 3.5 / run 6 m/s, jump 4.2 m/s, energy drain while
  running, `studentpr` model scaled to ~1.5 m, clips resolved by suffix (`anim_walk` → `walkanim_`).
- Camera: fixed pitch/yaw, smooth follow, `SpringArm3D` collision; movement basis from camera yaw.
- Nodes: birch (hp 3, axe, 1–2 wood), rock (hp 4, pickaxe, 1–2 stone), bush (hp 1, hands, 1 berry + 1 fiber).
- Drops: `Pickup3D` with magnet radius, auto-collect into `Inventory`, floating `Label3D` "+1".
- HUD: hotbar + counts, XP bar, coin counter, level-up toast.
- Save/load: `user://saves/slot0.json` via `IPersistence`.
- Tests: `WalkDistance`, `CameraRelativeMoveForward`, `AimRayHitsNode`, `HarvestTreeYieldsWood`,
  `MissingToolDoesNothing`, `NodeRespawnsAfterSimTicks`, `SaveLoadRoundTrip`, `ClipResolutionSuffix`.

**M2 — crafting**: workbench + furnace, tool tiers (axe → iron axe), `CraftGating` (station/unlock/inputs),
`RecipeGraphAcyclic`, `AllIconsInBounds`.

**M3 — land & biome**: plot purchase curve (`cost = base × 1.35^n`), biome tables, streaming activation;
`WorldgenSnapshot` for fixed seed, `BuyPlotUnlocksNodes`, `StreamingActivatesNeighborhood`.

**M4 — farming & animals**: crop growth on `SimClock`, animal product cooldowns, NPC grid A*;
`CropGrowthTicks`, `AnimalProductCooldown`, `AStarPathFound`.

**M5 — combat & dungeons**: sword/bow, enemy FSM (idle → chase → attack → die), loot tables;
`DamageMath`, `EnemyFsmTransitions`, `EnemyDiesAndDropsLoot`.

**M6 — progression**: perk board effects aggregated into a `PlayerStats` snapshot; banks/markets/drills tick;
`SkillBoardAdjacency`, `PerkStacking`, `BankInterest`, `AutomationProduces`.

**M7 — polish**: day/night tint, sfx (null-safe headless), settings, credits screen from `Docs/ATTRIBUTION.md`.

**M8 — performance**: multimesh decoration, plot culling, LOD checks, export presets.

## Open design questions (decide in M3/M4)

- Plot size: 16 m keeps the player busy but may feel small with 3 m trees → tune after M1 playtest.
- Inventory: grid (Forager-like) vs list+hotbar → start with grid, revisit after M2.
- Automation timing: real ticks vs "offline hours" — start with `SimClock` only.
