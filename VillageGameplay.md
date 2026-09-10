# Starter village gameplay

Open `Assets/Scenes/WelcomeScene.unity` and press Play. The loading screen leads to `Main Scene`; a new player confirms their name, then enters the village.

## Play

- Open **SHOP**, then **BUILD - 150 ELIXIR**.
- Tap or drag the ground to position the Gold Mine. Green means valid; red means occupied or outside the 44 x 44 build area.
- Press **BUILD** to pay and place it, or **CANCEL** to leave your resources unchanged.
- Press **COLLECT** to move produced gold into storage.
- Outside placement, drag to pan and pinch or use the mouse wheel to zoom.

These are prototype balance values: a new village starts with one Town Hall, 1,000 gold, 500 elixir and 50 gems. A Gold Mine costs 150 elixir, occupies 3 x 3 cells, and produces one gold per second. Up to three mines can be built. Each mine holds 500 gold; village gold capacity is 10,000. Elixir and gems are displayed but have no production or purchase system yet.

Village layout, resources and collection progress are stored locally in PlayerPrefs under `Kingdoms.Village.v1`. Purchases and collections save immediately; background production saves every 30 seconds and on application pause/quit. Offline production uses the device clock and is capped by each mine's capacity. This is a local prototype, without an online account or server economy. Invalid saves are preserved and display an error.

## Implementation

- `Assets/Scripts/Core/VillageState.cs`: economy, footprint validation and persistence.
- `Assets/Scripts/UI/VillageGameplay.cs`: HUD, shop, touch/mouse placement and world instances.
- `Assets/Prefabs/StarterVillage`: Town Hall, Gold Mine and Pine Tree prefabs.
- `Assets/Art/StarterVillage`: shared materials and tree mesh.
- `Village Gameplay` scene root references the assets, fonts and main camera.
- `VillageCameraController.InputBlocked` pauses gestures during shop and placement without changing zoom.

Buildings are original Unity primitive models intended as starter art. No combat, upgrades or construction timers are included in this step. Android build and physical-device performance remain to be tested.

The next gameplay step is an Elixir Collector and building selection/upgrades, followed by a first guided battle.
