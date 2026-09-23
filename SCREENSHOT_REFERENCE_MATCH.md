# September screenshot reference implementation

Reference folder: `Screenshot of clash of clans`. All eight supplied JPEGs were inspected directly. Their shared landscape reference size is 1600 × 702.

| Supplied image suffix | Screen and visual measurements | Kingdoms implementation |
| --- | --- | --- |
| `10.36.23 AM` | Chief at upper left; compact builder/shield area; three resources at upper right; icon rail; large Attack and Shop corner buttons | Compact HUD, bold outlined text, icon rail, proportional resource bars, corner actions; Kingdoms Town Hall tier occupies the unimplemented shield area |
| `10.36. AM` | Narrow translucent builder suggestions below the top controls | Compact builder queue with live jobs and affordable build/upgrade suggestions |
| `10.36.24 AM` | Full-screen Army catalogue, five cards | Army tab and five unavailable entries, clearly labelled as future content |
| `10.36.25 AM` | Cyan resource cards, model portraits, counts, currency costs, bottom balances | Resource tab with live prices/counts/affordability; resource purchases and placement work; additional builder ownership is unavailable |
| `10.36.AM` | Defenses category, available cyan and unavailable grey cards | Defenses tab; working Cannon, Archer Tower and wall purchases; Mortar and Air Defense unavailable |
| `10.36.27 AM` | Traps category with five grey cards | Five unavailable trap entries |
| `10.36.29 AM` | Centered building name and square Info/Upgrade buttons | Info, Upgrade and Move icon buttons; existing movement kept accessible |
| `10.36.30 AM` | Wide upgrade modal: model left, stats/description right, green confirmation and duration below | Two-column upgrade dialog with real production/storage gains, time, prerequisites, and confirmation; cancellation uses the same window |

The shop follows the screenshots' approximate normalized bounds: header above 84.5% of screen height, category tabs at 66–72.8%, cards at 12–64.2%, and a resource footer below 9.6%. Five card columns occupy about 91% of the width. The interface respects the existing safe area and scales the upgrade window to fit.

## Village art

- Grass gains procedural large-scale variation and fine texture; the cell grid appears only during placement.
- A sandy shoreline and water sit outside the buildable area.
- Broadleaf forest and shore rocks are combined into four meshes with shared materials; they do not obstruct playable cells.
- New editable resource prefabs live in `Assets/Prefabs/ScreenshotReference`. Their rendered portraits also appear in the shop and upgrade screen.
- The mine uses a low timber entrance, rails and ore cart; the collector uses a vat, brass bands and bent pipe; gold storage uses a low enclosure and gold pile; elixir storage uses a rounded vessel with supports and a glass dome.

## Behavior and authoring

Prices, ownership limits, balances, production and upgrade times come from Kingdoms rules. Screenshots do not establish the complete rules of the source game, so the existing Kingdoms economy is retained. Unimplemented army, defense and trap entries cannot charge resources or start placement. Their info buttons explain availability in the shop.

The screenshot interface is an additive conversion: the prior shop remains inactive, existing village saves are retained, and original building prefabs remain available. New controls can be stored in Main Scene and rebound after reload. The Inspector's **Apply screenshot reference interface** button adds the interface to older scenes; it does not rebuild a converted scene. Village scenery is generated when the scene starts.

## Remaining visual and gameplay differences

This is a measured reference implementation, not a pixel-identical or feature-complete reproduction. Original character art, font, decorative props, animated villagers, exact Town Hall and wall models, shield mechanics, troop buildings, remaining defenses, traps, combat, and the reference game's economy are not recreated by this pass. Cannon and Archer Tower village progression is covered in [the defense milestone](DEFENSE_VILLAGE_PROGRESS.md). The new models use native meshes and Kingdoms materials. Screenshot price/timer labels are not substituted for working game rules.

Validation results and rendered previews are recorded in `ScreenshotReferenceValidation.txt` and `ReferenceMatchPreviews`.

Executed in Unity 6000.3.13f1 on 23 September 2026 with an isolated save identity. Passed scene serialization, all four shop tabs, unavailable/insufficient-funds behavior, resource and wall purchases, builder-suggestion placement and cancellation, upgrade confirmation, upgrade cancellation/refund, camera locks, and scene reload. Screenshots were rendered at 1600 × 702 and 1920 × 1080; the village, resource shop, selection, builder and upgrade/cancellation views were visually inspected. No physical-phone test or APK rebuild was performed.

Main Scene contains the validated interface. Its pre-conversion backup is `.utmp/MainScene.before-screenshot-reference.unity`. Existing user material/settings changes and saved player villages were retained.
