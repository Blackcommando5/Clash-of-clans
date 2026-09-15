# Kingdoms resource milestone

Implemented 15 September 2026. This extends the original village prototype; construction timers and building upgrades remain future work.

> Superseded in part by [Home Village progression](HOME_VILLAGE_PROGRESS.md): building selection/moving, timed upgrades, Town Hall tiers, and version 3 saves are now implemented. The tables below document the original level 1 resource milestone.

## Playing the new resource loop

1. Open `Assets/Scenes/WelcomeScene.unity` and enter Play Mode.
2. Name your Chief if this is a new village.
3. Open **Shop** and choose a Gold Mine or Elixir Collector.
4. Tap or drag on the ground, then choose **Build**. Invalid cells cannot be confirmed. **Cancel** spends nothing.
5. Tap a producer's resource badge to collect from that building, or use **Collect All** for both currencies.
6. Buy storage buildings to raise your village's resource capacity.

| Building | Cost | Production | Producer capacity | Village capacity added | Town Hall 1 limit |
| --- | --- | --- | --- | --- | --- |
| Gold Mine | 150 elixir | 60 gold/minute | 500 gold | — | 3 |
| Elixir Collector | 150 gold | 60 elixir/minute | 500 elixir | — | 3 |
| Gold Storage | 300 elixir | — | — | 5,000 gold | 2 |
| Elixir Storage | 300 gold | — | — | 5,000 elixir | 2 |

All four resource buildings occupy 3 × 3 cells. They are constructed instantly at this milestone.

The Town Hall retains the prototype's base capacity of 10,000 for each currency. With two storage buildings of each kind, each resource capacity becomes 20,000. Starting balances remain 1,000 gold, 500 elixir, and 50 gems.

Production runs while away until each producer is full. Collecting into a full village leaves resources in the producer. Gold and elixir collection are independent: full gold storage does not prevent collecting elixir. A backward clock change pauses production until the previous timestamp is reached; this remains a local device-clock economy, not multiplayer authority.

## Existing village compatibility

- The PlayerPrefs key remains `Kingdoms.Village.v1` so existing villages are discovered.
- The JSON payload advances from version 1 to version 2.
- A valid original payload is copied to `Kingdoms.Village.pre-v2` before the first migrated write.
- Existing balances, positions, stored mine gold, name, and first-collection progress are retained.
- Invalid or unknown-version payloads are rejected and preserved.
- No live user save is modified by the isolated validation runner.

## Files

| File | Role |
| --- | --- |
| `Assets/Scripts/Core/BuildingCatalog.cs` | Shared building costs, sizes, limits, production, and capacity bonuses |
| `Assets/Scripts/Core/VillageState.cs` | Economy, placement rules, individual collection, validation, and migration |
| `Assets/Scripts/UI/VillageGameplay.cs` | Four-card shop, generalized placement, resource HUD, collection badges |
| `Assets/Prefabs/ResourceBuildings/` | Editable collector and storage models |
| `Assets/Art/ResourceBuildings/` | Materials for the new buildings |
| `Assets/Editor/ResourceBuildingFactory.cs` | Reproducible model creation and scene wiring |
| `Assets/Editor/ResourceMilestoneValidation.cs` | Isolated state and Play Mode validation |

The factory keeps existing generated assets rather than overwriting artist edits. Its `Build` entry point opens and saves Main Scene, so use it in an isolated copy when regenerating assets. Ordinary play needs no factory call.

## Validation and remaining work

The validation entry point only runs in a project ending in `/.utmp/ResourceValidation/Assets`, with company `KingdomsResourceValidation` and product `ResourceMilestoneTests`. It clears only that test identity's game keys, tests state rules and migrations, exercises actual shop buttons and scene reload, and captures the village/shop using URP.

See `ResourceMilestoneValidation.txt` for the recorded outcome. Preview images use synthetic test progress rather than the player's saved village.

Physical Android interaction, keyboard/safe-area testing, performance measurement, and a new APK build remain separate checks. The pre-existing APK does not contain this change.

Phase 2 now has its resource features implemented, but dynamic building limits across multiple Town Hall levels remain dependent on Phase 4. Current limits live in shared definitions for Town Hall 1. Phase 1's complete service separation, online clock, and authored/exportable data pipeline remain future work.
