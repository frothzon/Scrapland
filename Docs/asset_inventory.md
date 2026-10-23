# Asset inventory (generated from the headless audit)

Source of truth: `tools\audit-assets.ps1` → `artifacts\asset-audit.log`
Last audit: **2026-10-23** · `[audit] SUMMARY models=178 png=237 failures=0`

Regenerate after changing anything in `Assets\`:
```powershell
tools\import-assets.ps1
tools\audit-assets.ps1
```

## Totals

| Kind | Count |
|---|---|
| OBJ meshes (untextured, flat MTL colours) | 150 |
| FBX scenes (7 chibi + 7 animals) | 14 |
| GLB scenes (7 chibi + 7 chibi-emission) | 14 |
| PNG textures / UI sprites | 237 |
| Distinct animation clip names | 94 |

## OBJ world props (150)

| Family | Files | Height (m) | Use |
|---|---|---|---|
| `CommonTree_*` | 25 | ~2–5 | generic tree, any biome |
| `Willow_*` | 25 | ~2–5 | river / swamp biome |
| `BirchTree_*` (+ `_Autumn_`, `_Dead_`, `_Snow_`) | 25 | 2.25–5.06 | tutorial biome trees (green / autumn / dead / snow) |
| `Rock_*` | 21 | 0.49–0.88 | stone / ore nodes |
| `PineTree_*` | 15 | ~2–5 | winter biome |
| `Plant_*`, `Flowers_*`, `Grass_*` | 9 | 0.22–0.8 | fiber / decorative |
| `Cactus_*`, `CactusFlowers_*` | 10 | ~0.5–2 | desert biome |
| `Bush_*`, `BushBerries_*` | 6 | 1.11–1.28 | berry / fiber nodes |
| `PalmTree_*` | 4 | ~3–5 | beach / desert |
| `TreeStump_*`, `WoodLog_*` | 6 | ~0.4–1 | post-harvest remains, fuel |
| `Corn_*`, `Wheat_*`, `Lilypad_*` | 4 | small | farming / water dressing |

Notes: meshes are untextured with 1–5 surfaces (flat-colour materials from `.mtl`), and their origins are
**not centred** — always wrap them in a pivot node before placing/colliding.

## Chibi characters — use `glb\*.glb`

| Model | Meshes | Vertices | Bones | Clips | Materials | Bounds (m) |
|---|---|---|---|---|---|---|
| `archerpr` | 9 | 6 731 | 78 | 11 | 5 | 1.37 × 1.90 × 0.90 |
| `basemeshpr` | 4 | 5 372 | 78 | 11 | 2 | 1.37 × 2.23 × 0.91 |
| `knightpr` | 16 | 10 167 | 78 | 11 | 11 | 1.37 × 1.58 × 1.25 |
| `merchantpr` | 9 | 6 755 | 78 | 11 | 7 | 1.87 × 1.90 × 1.84 |
| `ninjapr` | 10 | 7 633 | 78 | 11 | 5 | 1.37 × 2.11 × 1.15 |
| `studentpr` | 9 | 7 331 | 78 | 11 | 7 | 1.37 × 2.06 × 0.96 |
| `allinonepr` | 37 | 23 313 | 78 | 11 | 27 | 1.87 × 2.23 × 1.84 |

> Sizes are the **scene** bounds including all modular parts, so they read taller than the visible body.
> Scale the character pivot to ~1.5 m of visible height (e.g. `studentpr` ≈ 0.73) instead of trusting the
> raw number.

Clips (GLB, unprefixed): `anim_iddle`, `anim_iddle_001`, `anim_walk`, `anim_run`, `anim_jump`,
`anim_crouch`, `anim_crouchiddle`, `anim_uncrouch`, `anim_flip`, `anim_push`, `anim_dying`.
`glb emission\*.glb` is the same set with flat/emissive materials (recommended by the pack's readme).

Do **not** use `fbx\*.fbx` for characters: bounds report ~0.01 m and clips are prefixed
`root_armature_<char>|…` (LL-013/LL-014).

## Animals — `3D Animals\FBX\*.fbx`

| Model | Vertices | Bones | Clips | Clip names | Bounds (m) |
|---|---|---|---|---|---|
| `Cow` | 1 644 | 39 | 6 | Death, Idle, Jump, Run, Walk, WalkSlow | 0.02 × 0.09 × 0.05 |
| `Horse` | 1 436 | 39 | 6 | Death, Idle, Jump, Run, Walk, WalkSlow | 0.03 × 0.09 × 0.07 |
| `Zebra` | 2 776 | 39 | 6 | Death, Idle, Jump, Run, Walk, WalkSlow | 0.03 × 0.09 × 0.07 |
| `Llama` | 1 366 | 35 | 2 | Idle, Jump | 0.02 × 0.05 × 0.06 |
| `Pig` | 1 158 | 35 | 2 | Idle, Jump | 0.03 × 0.10 × 0.05 |
| `Pug` | 1 284 | 35 | 2 | Idle, Jump | 0.01 × 0.04 × 0.03 |
| `Sheep` | 1 264 | 35 | 2 | Idle, Jump | 0.03 × 0.07 × 0.09 |

Clip names carry an `Armature|` prefix; all animals need an explicit scale node (~15–20×) to reach a
believable in-world size. Roles: Cow→milk, Sheep→wool, Pig→leather, Llama→mount/pack, Pug→companion,
Horse/Zebra→mounts.

## UI — `Sprout Lands - UI Pack - Basic pack`

- 9-slice dialog boxes: `dialog box.png`, `dialog box small/medium/big.png`, `Premade dialog box *.png`
- Buttons: `Small Square Buttons.png`, `Square Buttons 19x26 / 26x19 / 26x26.png`
- Icons: `Icons\All Icons.png`, `Icons\white icons.png`, `Icons\special icons\*`
- Cursors: `Mouse sprites\*` (paw / catpaw / triangle pointer)
- Fonts: `fonts\pixelFont-7-8x14-sproutLands.ttf` (+ preview PNG)
- Emotes/emoji: `Dialouge UI\Emotes\*`, `emojis-free\emoji style ui\*`

The icon sheets are atlases: `Data\icon_atlas.json` (M2) maps logical icon names (`icon_wood`, …) to rects.
Until then, `ItemDefinition.Icon` only stores the logical name.
