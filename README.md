# Clash-of-clans

Kingdoms: A mobile village strategy game inspired by Clash of Clans, developed with Unity 6 and URP.

Current work: [Home Village interface and progression](HOME_VILLAGE_PROGRESS.md) adds building selection/movement, two builders, timed upgrades, Town Hall tiers, resource bars, model previews, and a detailed Town Hall. The full game and multiplayer are not complete.

Latest development: [the supplied screenshot reference implementation](SCREENSHOT_REFERENCE_MATCH.md) adds a full-screen categorized shop, compact HUD, builder suggestions, two-column upgrade dialog, new resource models, and village scenery. [The builder queue and upgrade cancellation](HOME_VILLAGE_PROGRESS.md) retain working progression. See [the phase checklist](GAME_DEVELOPMENT_PHASES.md) for completed and remaining work.

Latest army progression: **Shop > Army** now sells Barracks (200 elixir) and Army Camps (250 elixir). Camps add eight spaces per level to your saved roster; **Buildings > Prepare Army** remains free and instant. Campaign now uses that roster. Practice still supplies its own army. See [the beginner walkthrough](BEGINNER_GUIDE.md#barracks-and-army-camps---24-september-2026).

Latest campaign: **Buildings > Prepare Army > Campaign** offers Gate Outpost and Sealed Keep. Starting spends the prepared roster; first victories grant saved, once-only resource rewards. Unclaimed results survive restart. See [the campaign walkthrough](BEGINNER_GUIDE.md#first-offline-campaign-loop---24-september-2026).

Latest troop choice: Barracks unlock Archers (two spaces, 3.5-cell range). Prepare mixed Raider/Archer armies, select a troop type during campaign deployment, and replay the original composition. Version-5 saves migrate to version 6 with pending rewards preserved. See [the mixed-army walkthrough](BEGINNER_GUIDE.md#mixed-armies-and-archers---24-september-2026).

## Architecture

Separate practice mode: [the practice battle](PRACTICE_BATTLE_PROGRESS.md). Tap Attack to deploy eight raiders against a walled practice Town Hall, Cannon and Archer Tower, then watch a replay, retry or return home. Scout without a timer, choose Open Gate or Wall Breach, then press Start Attack. Raiders route through openings and break sealed walls; retry and replay retain the challenge. Results show three-star practice scoring, destruction and army statistics; Surrender ends an attack with replayable results. [Defense village progression](DEFENSE_VILLAGE_PROGRESS.md) supports purchases, upgrades and persistence. Full battles and multiplayer remain outstanding.
Modular architecture built on Unity 6 and URP.

## Controls
Supports multi-touch gestures and mouse input.

## Visual Styling
Handcrafted low-poly palette for mobile performance.

## Village Map
120x120 world boundary with centered 44x44 grid.

Smooth orthographic zoom clamped between 8 and 32 units.

Viewport frustum bounding prevents seeing past ground borders.

Integrated GraphicRaycaster detection blocks world dragging.

DPI adaptive panning ensures uniform feel across devices.

Safe area anchors support Android camera notches.

Rotating hint carousel displays tips during startup.

0.0 - 0.9 scene stream progress mapped smoothly to 100%.

CanvasGroup alpha fade smoothly reveals village.

Documented async transition pipeline.

Virtual keyboard bounds shift dialog upward dynamically.

Provides Chief confirmation step before commit.

Axis-aligned grid intersection prevents overlapping structures.

Atomic serialization prevents corrupted saves on write failures.

Grid system uses 1 world unit per cell.

Deterministic random seed scatters 52 boundary pines.

In-game shop cards display building stats and costs.

Real-time raycast ground snapping with visual footprint.

60 gold/min generation rate with 500 gold capacity.

Resource counters format values using N0 format specifier.

Focus loss smoothly halts camera inertia.

Latest deployment: click or tap inside the green battlefield zone to place the selected troop. Lane buttons remain available, and replay preserves exact positions. See [the ground-deployment walkthrough](BEGINNER_GUIDE.md#ground-deployment--24-september-2026).
