# Attribution & asset licensing — ACTION REQUIRED before any public build

This project is a **learning clone** of *Forager* (HopFrog / Humble Bundle). Do not ship Forager's name,
logo, art or audio. The assets currently in `Assets\` were provided in this workspace; **their licenses have
not been verified in-repo**, so treat them as "local development only" until checked.

| Asset folder | Provided metadata | Status |
|---|---|---|
| `ChibiCharacters` (Styloo) | `Read Me .txt`, `https://styloo.itch.io/`, Discord invite | Free asset per the readme; **re-check the itch.io page for the exact license (attribution / commercial use / redistribution)** before shipping |
| `Sprout Lands - UI Pack - Basic pack` | `read_me.txt` — **author: Cup Nooble**; *"You can modify the assets. You can not redistribute or resale, even if modified. You can only use these assets in non-commercial projects. [Also No NFT's pls]"* | ⚠️ **Non-commercial + no redistribution.** Fine for this learning project; it must never be published with the assets, and shipping a commercial build requires the Premium pack / permission from Cup Nooble (`https://cupnooble.itch.io/sprout-lands-asset-pack`) |
| `3D Trees` (OBJ set) | No license file found | **Verify source/license** |
| `3D Animals` (FBX set) | No license file found | **Verify source/license** |

Checklist before shipping:
1. Record the source URL, license name and author for each folder above.
2. Add a `LICENSES/` folder with the original license texts.
3. Keep the chibi pack's `Read Me .txt` (it carries usage guidance from the author).
4. Replace anything unlicensed with original or CC0 art.
5. Add an in-game credits screen crediting every author whose work ships.

Reference used for mechanics research (not assets): *Forager* skill-tree breakdown on Pocket Gamer, and the
Wikipedia entry for *Forager*, both consulted 2026-10-23.
