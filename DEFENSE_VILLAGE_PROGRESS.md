# Cannon and Archer Tower village milestone

23 September 2026. The reference Defenses shop now supports Cannon and Archer Tower purchases, placement, movement, inspection, upgrades and cancellation. Existing version 3 villages remain readable without a reset or migration.

| Building | Purchase | Footprint | Limits at Town Hall 1 / 2 / 3 | Level-one stats |
| --- | --- | --- | --- | --- |
| Cannon | 250 gold | 3 × 3 | 2 / 3 / 4 | 420 HP; 9 damage/second; 9-cell range |
| Archer Tower | 1,000 gold | 3 × 3 | 1 / 2 / 3 | 380 HP; 11 damage/second; 10-cell range |

Purchase prices and first-tier limits follow the supplied shop screenshot. Combat stats, later limits, upgrade costs and durations are provisional Kingdoms values. Both buildings finish construction instantly under the existing placement rules. Upgrades use one of the two builders: level 2 costs twice the purchase price and takes 30 seconds; level 3 costs five times the purchase price and takes 120 seconds. Level 3 requires Town Hall 2. HP and damage scale with building level; range stays fixed. Cancellation returns half the upgrade cost, limited by available storage.

Select a defense to see its range circle. Info/Upgrade displays its stats and points to practice combat under Attack. Home defenses remain idle; the separate [practice encounter](PRACTICE_BATTLE_PROGRESS.md) uses level-one copies that acquire targets, fire and take damage. Defenses generate no resources and add no storage capacity.

The Cannon has a timber carriage, wheels and open muzzle. The Archer Tower has timber legs, bracing, a ladder, platform and mounted bow. Editable prefabs are in `Assets/Prefabs/ScreenshotReference`; shop and upgrade portraits are rendered from those same models. Existing authored shop cards gain the new portraits and gold price icons without rebuilding the interface. Both buildings appear in the inventory and eligible builder suggestions.

Validation: `DefenseVillageValidation.Run` is restricted to the isolated ResourceValidation project and identity. It checks prices, overlap/bounds, limits, Town Hall unlocks, production/storage exclusion, movement, upgrade jobs, refunds, save round trips and offline completion. Play mode checks cover the real shop buttons, purchases, range renderer, stats, movement, two concurrent upgrades, cancellation and scene reload. See `DefenseVillageValidation.txt` and `DefensePreviews` for executed evidence. Phone testing and APK rebuilding are separate work.
