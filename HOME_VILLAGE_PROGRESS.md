# Home Village interface and progression

Updated: 15 September 2026.

This is a working extension of Kingdoms, not a completed or exact Clash of Clans reproduction. The full requested scope remains in [the game roadmap](GAME_DEVELOPMENT_PHASES.md) and [the reference catalogue plan](CLASH_OF_CLANS_REFERENCE_PHASES.md).

## Playable additions

- Resource HUD with proportional gold/elixir fill bars and native illustrated icons.
- Builder availability indicator opens a queue showing both builders, active jobs ordered by completion time, remaining time, target levels, and building coordinates. Tap either active job to inspect or cancel it; free slots explain how to assign a builder. The queue updates as jobs finish.
- Shop with rendered model previews, costs, capacity/production descriptions, limits, and affordability states.
- More detailed Town Hall prefab with tiled roof, timber framing, windows, chimney, and door details.
- Tap/click a building to select it and show its footprint and action bar.
- Move any existing building, including the Town Hall, without paying again.
- Cancel a move to restore its previous visible model and position.
- Reject occupied or out-of-bounds moves without changing saved state.
- Building information and upgrade confirmation dialog.
- Two builders, persisted upgrade jobs, remaining-time display, and construction markers.
- Three initial Town Hall/resource levels; higher Town Hall levels raise building limits.
- Producer output/capacity and storage capacity increase with building level.
- Version 1 and version 2 saves migrate to version 3.

## Controls

1. Open WelcomeScene and enter Play Mode.
2. Tap a building without dragging to select it. Dragging still pans the village.
3. Choose **Move**, drag/tap to position the preview, and confirm or cancel.
4. Choose **Info / Upgrade** to see current stats, requirements, cost, and duration.
5. Confirm the upgrade when affordable and a builder is available.
6. Return later or wait for completion. Completed jobs release their builder automatically.

Gold and elixir in a producer can still be collected while its upgrade runs, but that producer generates no new resources during its upgrade. Production after an offline completion starts at the recorded finish time, using the new level. Upgrading storage retains its old capacity until completion.

## Initial balancing, not exact Clash values

| Item | Level 1 → 2 | Level 2 → 3 |
| --- | --- | --- |
| Town Hall | 1,000 gold; 60 seconds | 4,000 gold; 300 seconds |
| Gold Mine | 300 elixir; 30 seconds | 750 elixir; 120 seconds |
| Elixir Collector | 300 gold; 30 seconds | 750 gold; 120 seconds |
| Gold Storage | 600 elixir; 30 seconds | 1,500 elixir; 120 seconds |
| Elixir Storage | 600 gold; 30 seconds | 1,500 gold; 120 seconds |

Resource buildings can reach level 2 at Town Hall 1 and level 3 at Town Hall 2. Level 3 is the current content cap. New purchases still finish instantly; full construction jobs remain outstanding roadmap work.

Upgrade cancellation (23 September 2026): open an upgrading building's **Info / Upgrade**, choose **Cancel Upgrade**, review the refund, then confirm. Closing the window keeps the upgrade running. Cancellation keeps the current level, immediately frees its builder, and refunds 50% of the upgrade cost, capped by available storage (excess is lost). Producer output resumes from cancellation time without producing during the cancelled job. A job that has reached its finish timestamp completes and cannot be refunded. The action uses the existing copy/save/commit flow and version 3 save format.

The builder queue uses the existing details window and creates its two reusable job buttons on first opening, including in previously authored scenes. Closing the queue restores village camera controls. A hidden upgrade action cannot start another job after cancellation closes the window.

| Town Hall level | Limit for each producer type | Limit for each storage type |
| --- | --- | --- |
| 1 | 3 | 2 |
| 2 | 4 | 3 |
| 3 | 5 | 4 |

The Town Hall retains 10,000 base capacity for each resource. Each storage adds `5,000 × building level`. Each producer generates `60 × building level` per minute and holds `500 × building level`.

These values are explicit Kingdoms test balancing. They require replacement by a verified, version-specific catalogue before claiming exact reference-game progression.

## Persistence

The live PlayerPrefs key remains `Kingdoms.Village.v1`, while the payload version is now 3. Building records gain `level`, `upgradeStarted`, and `upgradeFinishes`.

Version 1 payloads retain the original `Kingdoms.Village.pre-v2` backup behavior. Version 2 payloads are backed up under `Kingdoms.Village.pre-v3`. Existing names, resources, building cells, and stored production survive migration. The old fixed Town Hall position restriction is retained when validating old saves, then removed for valid version 3 movement.

## Validation

23 September 2026: `BuilderQueueValidation.Run` passed in the isolated Unity 6000.3.13f1 project. It exercised both queue row callbacks, job ordering, confirmation back-out and refund, a hidden-action guard, available-builder rows, completion refresh, camera blocking, and scene reload. See `BuilderQueueValidation.txt`, `BuilderQueuePreview.png`, and `UpgradeCancellationPreview.png`. Both captured dialogs were visually checked at 1600 × 900. Physical-phone testing and an updated APK remain outstanding.

The isolated Unity runner verifies both the previous resource milestone and the new progression/movement paths. See `HomeVillageValidation.txt` and the saved Home Village previews for the executed result.

Coverage includes old-save migration, corruption rejection, capacity overflow, individual collection, producer limits, move cancellation, invalid movement, two-builder limits, upgrade cost/duplicate prevention, paused production, production after offline completion, Town Hall unlocks, level caps, storage upgrades, active-job serialization, actual UI button callbacks, and scene reload.

The test environment uses a separate company/product identity. No user village is reset. Phone gestures, physical-device performance, and a rebuilt APK are still outstanding. Unity's unrelated editor search-index startup exception is recorded separately from gameplay results.

## Remaining full-game scope

- Verified exact UI screens, assets, costs, timings, unlocks, and all level variants for a chosen reference version.
- Walls/layout tools, obstacles, construction cancellation, builder ownership, and full progression content.
- Barracks, army camps, troop models/animations, pathfinding, defenses, spells, heroes, and combat.
- Campaign maps, battle results, loot, and replays.
- Accounts, authoritative economy/server clock, opponent matching, asynchronous player attacks, and cloud recovery.
- Clans, chat, donations, wars, leagues, events, and live operations.
- Builder Base and Clan Capital-style modes.
- Sound, full animation/VFX, mobile optimization, release signing, and deployment.

The [reference image search](https://www.clubic.com/telecharger-fiche431837-clash-of-clans.html) informed the general early-village arrangement. It is not evidence that this implementation matches a particular current build pixel-for-pixel. Existing wall models were located in the user's Downloads library but were not imported or integrated in this change.

## Implementation map

| File | Responsibility |
| --- | --- |
| `Assets/Scripts/Core/VillageProgression.cs` | Upgrade rules, builder count, Town Hall limits, movement validation |
| `Assets/Scripts/Core/VillageState.cs` | Version 3 save validation/migration and time-aware production |
| `Assets/Scripts/UI/VillageInteractions.cs` | Selection, move workflow, information dialog, upgrade UI |
| `Assets/Scripts/UI/VillageIcon.cs` | Native HUD illustrations |
| `Assets/Scripts/UI/VillageGameplay.cs` | Resource UI, shop, and existing village integration |
| `Assets/Editor/HomeVillageArtFactory.cs` | Town Hall prefab and model-preview generation |
| `Assets/Editor/ResourceMilestoneValidation.cs` | Isolated regression entry point for both milestones |

## Screenshot reference UI pass

See [Reference UI progress](REFERENCE_UI_PROGRESS.md) for the compact HUD, profile navigation, inventory and settings additions. Combat and multiplayer remain under development.

## Editable scene interface

The main scene now stores the HUD, menus, profile pages, onboarding dialog, model templates and woodland. See [Scene editing guide](SCENE_EDITING_GUIDE.md). Edit-mode persistence and Play mode navigation checks passed; see EditableSceneValidation.txt.
