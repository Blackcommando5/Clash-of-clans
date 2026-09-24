# Kingdoms: a beginner's guide to rebuilding and understanding the game

Updated: 24 September 2026.

> Keep this guide current with every development update. Sections 1, 11, 12 and 14 describe the current game. The manual ground, camera, welcome and primitive-model lessons explain the original foundations; inspect the current scenes for the latest artwork and Inspector values.

Project: `E:\Project\Games\Kingdoms`

This guide explains the work completed so far and how you can reproduce it manually. You do not need previous Unity experience. Work through one lesson at a time, test it, and then continue.

The project is a Clash of Clans-inspired village prototype with its own Kingdoms title and starter assets. It is not the complete Clash of Clans game. The current loop is: enter the village, choose a name, build and collect resources, move and upgrade buildings, manage two builders, prepare an army, clear four authored local campaign missions, claim first-victory rewards, and try an isolated practice battle.

## Contents

1. [What has been built](#1-what-has-been-built)
2. [How to practise without losing your work](#2-how-to-practise-without-losing-your-work)
3. [Unity words and windows](#3-unity-words-and-windows)
4. [Project files and scenes](#4-project-files-and-scenes)
5. [C# basics used in this project](#5-c-basics-used-in-this-project)
6. [Create the ground](#6-create-the-ground)
7. [Set up the camera](#7-set-up-the-camera)
8. [Build the welcome and loading screen](#8-build-the-welcome-and-loading-screen)
9. [Ask for and save the player name](#9-ask-for-and-save-the-player-name)
10. [Make the building prefabs](#10-make-the-building-prefabs)
11. [Understand resources, placement and saving](#11-understand-resources-placement-and-saving)
12. [Connect the village, HUD and shop](#12-connect-the-village-hud-and-shop)
13. [Prepare the mobile scenes](#13-prepare-the-mobile-scenes)
14. [Test your work](#14-test-your-work)
15. [Solve common problems](#15-solve-common-problems)
16. [Your learning schedule](#16-your-learning-schedule)
17. [Record of the AI-assisted work](#17-record-of-the-ai-assisted-work)

## 1. What has been built

| Feature | What currently works |
| --- | --- |
| Ground | 120 x 120 world-unit plane with a centered 44 x 44 village grid |
| Camera | Fixed angled orthographic view; drag, pinch/scroll zoom, release glide and ground bounds |
| Welcome screen | Kingdoms artwork, title, loading progress, tips and fade into the village |
| New player | Name input, validation, confirmation and saved Chief nameplate |
| Village | Starter Town Hall and surrounding trees |
| HUD and shop | Resource bars, builder queue, categorized full-screen shop and model portraits |
| Buildings | Gold Mine, Elixir Collector, both storage types, Cannon, Archer Tower, individual walls, Barracks and Army Camps |
| Placement | Grid snapping, overlap/boundary checks, preview cancellation and moving existing buildings |
| Economy | Gold and elixir production, collection, storage limits and offline accrual |
| Progression | Two builders, timed upgrades through level 3, Town Hall limits, confirmed cancellation with a storage-capped 50% refund |
| Defenses | Purchase, move, inspect range and upgrade; home defenses remain idle |
| Practice battle | Eight raiders, three enemy buildings, walls, entrance routing, wall breach logic, star scoring, surrender, results, watch replay, retry and return |
| Ground deployment | Click/tap within a visible southern zone; selected troop, half-cell snapping, placement feedback and exact-position replay; lane buttons retained |
| Army preparation | Buildings > Prepare Army; free instant mixed Raider/Archer/Tank roster, weighted housing, troop selection, eight starter spaces plus camp capacity, readiness and immediate saving |
| Campaign | Four authored missions, distinct layouts/deployment zones, dynamic scouting, prepared-army commitment, durable results, once-only first-clear rewards and saved unlocks |
| Battle history | Latest 20 campaign results, newest first; outcome, stars, destruction, duration, committed army, claimed rewards and saved replay playback after restart |
| Battle recovery | Resume checkpointed campaign attacks with the same timer, damage, troop positions and reserves; automatic saves, Save & Return, and paused retry on save failure |
| Persistence | Local version 9 village saves; migrations from versions 1-8; saved guide milestones, hint preference and campaign history |
| Guided onboarding | Eight-step Village Guide, contextual shortcuts, saved milestones, pause/resume hints and recognition of existing progress |
| Mobile setup | Verified development APK, Welcome scene first, village second, landscape orientation, Android IL2CPP/ARM64; physical-phone checks pending |

Not implemented yet: arbitrary enemy villages, unit separation, matchmaking, multiplayer, online accounts, purchases or a server economy. The fixed practice battlefield is separate from your home village. Gem counters and reference-style buttons do not imply that all reference-game features exist.

The existing Android development APK builds and passes package checks, but predates battle history, saved replays and battle recovery. Rebuild it to include these features. Physical-phone testing remains pending and has been deferred by the user. See [Android validation](ANDROID_BUILD_VALIDATION.md).

## 2. How to practise without losing your work

There are two useful ways to learn:

**Inspect the working project first.** Open it, play it, and find the objects and scripts described below. This lets you see the result before learning how it is made.

**Rebuild in a separate practice project.** Use Unity Hub to create a Universal 3D project named `KingdomsLearning`, ideally using Unity `6000.3.13f1`. This is where you should create the objects manually and experiment with scripts.

Before experimenting with the original project, close Unity and make a separate copy of its `Assets`, `Packages`, and `ProjectSettings` folders. Keep the `.meta` files alongside the assets. Unity uses those files to remember asset identities and references. `Library` is a generated cache and is not the source of your game.

For a practice project copied from the original, change **Project Settings > Player > Company Name and Product Name** to learning-specific values before testing saves. PlayerPrefs belongs to the application identity; a copied project can otherwise use the same local preferences.

Remember these habits:

- Save scenes with **Ctrl+S**.
- Stop Play Mode before making changes you want to keep. Most Inspector changes made during Play Mode disappear when you stop.
- Read the first red Console error before trying to solve the errors after it.
- Change one thing, test it, and explain the result in your own words.
- Move assets through Unity's Project window so their `.meta` files move with them.

## 3. Unity words and windows

| Term | Beginner explanation | Example here |
| --- | --- | --- |
| Scene | A saved arrangement of objects | `Main Scene.unity` |
| GameObject | A container that exists in a scene | Main Camera, Village Gameplay |
| Component | A feature attached to a GameObject | Camera, MeshRenderer, a script |
| Transform | An object's position, rotation and scale | Ground scale `(12, 1, 12)` |
| Mesh | The triangles that form a 3D shape | Plane, cube, tree cone |
| Material | Appearance settings assigned to a renderer | Timber, stone, grass |
| Shader | Code that determines how a material is drawn | Village ground grid shader |
| Collider | A shape used for physics or ray detection | Ground MeshCollider |
| Prefab | A saved reusable object template | GoldMine prefab |
| Instance | One copy of a prefab in a scene | Your purchased mine |
| Canvas | The container used for this game's screen UI | Welcome Canvas |
| RectTransform | Position and size information for UI | Shop window rectangle |
| Anchor | A reference to part of the parent UI rectangle | Resource counters anchored top-right |
| HUD | Information and controls shown during gameplay | Counters and buttons |
| Inspector reference | A field pointing to another object or asset | Gold Mine Prefab field |
| Runtime | The period when the game is playing | When the village HUD is created |

The **Hierarchy** lists objects in the open scene. The **Project** window lists saved assets. The **Inspector** shows the selected object's settings. **Scene** view is your workspace; **Game** view shows the camera's output. The **Console** contains messages and errors.

Unity uses X for left/right, Y for height, and Z for the other ground direction. Our village occupies the X/Z plane. One ground unit corresponds to one building-grid cell in this prototype.

## 4. Project files and scenes

Open Unity Hub, add `E:\Project\Games\Kingdoms` if necessary, and open it with Unity `6000.3.13f1`.

The relevant installed packages are Input System `1.19.0`, Universal Render Pipeline `17.3.0`, and Unity UI/uGUI `2.0.0`. You can inspect installed packages in Unity's Package Manager. Use the installed versions while learning this project.

### Current scenes

| Scene | Role | Enabled build position |
| --- | --- | --- |
| `Assets/Scenes/WelcomeScene.unity` | Opening loading screen | 0: first |
| `Assets/Scenes/Main Scene.unity` | Actual playable village | 1: second |
| `Assets/Scenes/SampleScene.unity` | Existing sample scene | Excluded |
| `Assets/Scenes/VillageScene.unity` | Existing unused template | Excluded |

The playable village is named **Main Scene**, not VillageScene. The spelling, including the space, matters when a script loads it by name.

### Files to study

These links are relative to this guide in the project root.

| File | Its responsibility |
| --- | --- |
| [VillageCameraController.cs](Assets/Scripts/Core/VillageCameraController.cs) | Camera view and input |
| [VillageGround.shader](Assets/Art/Materials/VillageGround.shader) | Grass, grid and border drawing |
| [WelcomePanel.cs](Assets/Scripts/UI/WelcomePanel.cs) | Rounded gradient UI graphics |
| [WelcomeScreen.cs](Assets/Scripts/UI/WelcomeScreen.cs) | Loading, progress, layout and transition |
| [PlayerProfile.cs](Assets/Scripts/Core/PlayerProfile.cs) | Name validation and saving |
| [NewPlayerOnboarding.cs](Assets/Scripts/UI/NewPlayerOnboarding.cs) | Name dialog and nameplate |
| [VillageState.cs](Assets/Scripts/Core/VillageState.cs) | Village data, rules and persistence |
| [VillageGameplay.cs](Assets/Scripts/UI/VillageGameplay.cs) | World instances, shop, HUD and placement input |

The complete working code is in these files. This guide explains and connects it rather than keeping a second, potentially outdated copy of every script.

For a fresh practice project, create matching `Assets/Scripts/Core` and `Assets/Scripts/UI` folders. Copy or manually type the reference scripts into matching filenames. Study them in this order: `WelcomePanel`, `PlayerProfile`, `VillageState`, `VillageCameraController`, `WelcomeScreen`, `NewPlayerOnboarding`, `VillageGameplay`. Install the required packages first; later scripts depend on earlier classes.

Create each class only once. Do not paste a full class underneath the default class Unity created. Replace the template contents. Do not copy the original `.meta` files into an unrelated practice project just to import individual scripts; let Unity assign new identities there, then connect Inspector fields manually.

## 5. C# basics used in this project

You do not need to understand every line immediately. Start with these patterns:

```csharp
public int gold = 1000;
```

`int` means a whole number. `gold` is the variable's name. `1000` is its starting value. `public` allows other code to access it; supported public fields on components can also appear in the Inspector.

```csharp
if (elixir < MineCost)
{
    return false;
}
```

An `if` checks a condition. Here, the operation refuses to continue when the player cannot afford it. `return` exits the method.

```csharp
foreach (var building in buildings)
{
    // Do something for each building.
}
```

A list stores multiple records; `foreach` visits them one at a time. A method is a named group of instructions, such as `CollectGold`.

`Vector3` stores three numbers, commonly X/Y/Z. `bool` means true or false. `string` means text. `null` means that an object reference has no object assigned. `out` allows a method to return extra information, such as the reason placement failed.

`MonoBehaviour` is the base class for scripts Unity runs as components. `VillageState` and `PlayerProfile` are data/helper classes, so you do **not** attach them to scene objects. `VillageGameplay` is a component, so you do attach it.

### Unity's main callbacks

| Callback | Purpose in this project |
| --- | --- |
| `Awake` | Prepare the loading UI |
| `OnEnable` | Enable touch support |
| `Start` | Set up the village or begin loading |
| `Update` | Check input and update UI over time |
| `LateUpdate` | Apply camera movement after normal updates |
| `OnDisable` | Release touch support and cancel placement |
| `OnApplicationPause` | Save when a mobile application pauses |
| `OnApplicationQuit` | Try to save when the application closes |

An `IEnumerator` method with `yield return` can spread work over frames. The welcome screen uses this pattern so loading and fading can progress while the screen continues drawing.

**Practice:** Find the Gold Mine definition in `BuildingCatalog.cs`. Before editing it, find every place the shop displays the cost. Some descriptions are written as text; changing the number alone will not update those descriptions automatically.

## 6. Create the ground

Do this in the practice project:

1. Create a scene and save it as `Assets/Scenes/Main Scene.unity`.
2. In the Hierarchy, create **3D Object > Plane**.
3. Rename it `Kingdoms Ground`.
4. Set Position to `(0, 0, 0)`, Rotation to `(0, 0, 0)`, and Scale to `(12, 1, 12)`.
5. Keep its MeshFilter, MeshRenderer and MeshCollider.
6. Keep a Directional Light in the scene so buildings can be lit.

A built-in Unity Plane is 10 units wide and 10 units deep. Scaling X and Z by 12 makes our ground 120 x 120 units, covering -60 to +60 on both axes.

### Add the grass material

1. Bring the reference `VillageGround.shader` into `Assets/Art/Materials` in your URP practice project.
2. Create a Material named `VillageGround` in the same folder.
3. Select its shader as **Kingdoms/Village Ground**.
4. Assign the material to the plane's MeshRenderer.
5. Use Village Half Size `22`, Grid Cell Size `1`, and Grid Visibility around `0.28`.

The build area is smaller than the full ground. Half-size 22 produces a 44 x 44 village. The outer grass gives the camera room around the buildable area.

The shader reads each visible point's world X/Z coordinates. It uses those coordinates to draw repeating grid lines, a subtle alternating-cell pattern and a border. The grid is not made from thousands of cube objects.

The shader only **draws** the grid. `VillageState` separately enforces placement rules. Changing only the shader's border size will not change the allowed building area.

**Check:** You should see green ground and a square village boundary from above. Pink ground indicates a shader/pipeline problem.

**Practice:** Change Grid Visibility and observe the result. Restore it before continuing.

## 7. Set up the camera

The village uses an **orthographic** camera. Unlike perspective projection, distant objects do not shrink with distance. This helps the ground grid and building sizes stay consistent.

1. Select Main Camera.
2. Set its tag to `MainCamera` if needed.
3. Set Projection to **Orthographic**.
4. Add the `VillageCameraController` component.
5. Enter these current scene values:

| Setting | Value |
| --- | --- |
| Elevation | 45 |
| Azimuth | 45 |
| Distance | 100 |
| Focus | `(0, 0, 0)` |
| Ground Height | 0 |
| Ground Min | `(-60, -60)` |
| Ground Max | `(60, 60)` |
| Minimum Zoom | 8 |
| Maximum Zoom | 32 |
| Starting Zoom | 16 |
| Wheel Sensitivity | 0.0015 |
| Damping | 10 |
| Maximum Glide Speed | 45 |

The script's default Starting Zoom is 24, but the current village scene overrides it to 16 to show the starter buildings more closely. Inspector values saved in a scene can override defaults written in code.

Elevation describes how steeply the camera looks down. Azimuth chooses its direction around the village. The controller calculates the camera Transform, so manually moving the camera during Play Mode will be overridden.

### How movement works

1. Convert a screen pointer position into a camera ray.
2. Intersect the ray with an invisible mathematical plane at ground height.
3. Compare the old and new ground points.
4. Move the camera focus to keep the ground under the pointer as it is dragged.
5. After release, reduce the remaining velocity gradually to produce glide.

Zoom changes `orthographicSize`, which is half the visible vertical world height. Smaller values mean a closer view. The controller adjusts focus during zoom to keep the point under the mouse, or the midpoint between fingers, stable.

The camera checks all four viewport corners against the ground bounds. Clamping only its center would still allow the screen edges to show beyond the ground.

### Controls and UI interaction

- Mouse: left or middle drag to pan; scroll to zoom.
- Touch: one finger drags; two fingers pan and pinch.
- A gesture that starts over UI is ignored by camera movement until it ends.
- The shop and placement use `InputBlocked` to pause camera gestures.
- The name dialog temporarily disables the camera controller and restores it after confirmation.

EnhancedTouch support is enabled and disabled with the component lifecycle. The input system also tracks changes in finger count so a second finger does not cause a sudden jump.

**Check:** Pan in each direction, zoom in/out, and approach the edges. Try a wide and a less-wide landscape Game view.

**Practice:** Change Starting Zoom to 12 while outside Play Mode, play again, observe the closer view, then restore 16.

## 8. Build the welcome and loading screen

The welcome screen's objects are saved in `WelcomeScene.unity`. You can inspect and edit them before pressing Play.

### Understand the UI parts

A Canvas draws the UI. A CanvasScaler adapts its layout to screen sizes. A RawImage displays the background texture. Text objects display the title, tips and progress. A CanvasGroup allows the entire screen to fade together.

This project uses `UnityEngine.UI.Text`, `InputField` and other uGUI components. They are not TextMeshPro components. A TMP text object cannot be assigned to a field whose type is `UnityEngine.UI.Text`.

### Manual reconstruction

1. Create a new scene and save it as `Assets/Scenes/WelcomeScene.unity`.
2. Create a Canvas named `Welcome Canvas`; use **Screen Space - Overlay**.
3. Set its CanvasScaler to **Scale With Screen Size**, reference resolution `1920 x 1080`, and Match `0.5`.
4. Add CanvasGroup and `WelcomeScreen` components to the Canvas.
5. Create a full-screen RawImage child named `Backdrop`. Stretch its RectTransform to all four edges and set offsets to zero.
6. Assign `Assets/UI/Welcome/KingdomsWelcome.png` as its texture. This artwork already exists in the original project. Import your own illustration if rebuilding with different art.
7. Create a stretched UI container named `Safe Area`. Keep important text and the loading bar inside it; the backdrop can extend behind notches.
8. Add a large `KINGDOMS` title near the upper center. Use the Bangers font for the stylized heading. Layer slightly offset text copies for shadow/highlight if you want the existing layered appearance.
9. Add a lower-center loading-bar background and a child rectangle for its purple progress fill.
10. Make the fill left-anchored: Anchor Min `(0, 0)`, Anchor Max `(0, 1)`, zero offsets. The script expands Anchor Max X from 0 to 1 as loading progresses.
11. Add status, percentage, tip and version text objects. Use Roboto-Bold for body text. Keep decorative text and the backdrop's Raycast Target off where they do not need input.
12. Assign the following references on `WelcomeScreen`.

| Script field | Assign |
| --- | --- |
| Village Scene | `Main Scene` |
| Minimum Display Time | 3 |
| Safe Area | Safe Area RectTransform |
| Backdrop | Background RawImage component |
| Progress Fill | Fill RectTransform |
| Status Label | Loading status Text |
| Percentage Label | Percentage Text |
| Tip Label | Tip Text |
| Canvas Group | CanvasGroup on Welcome Canvas |
| Version Label | Version Text |

The font files are in `Assets/TextMesh Pro/Examples & Extras/Fonts/`: `Bangers.ttf` and `Roboto-Bold.ttf`. Their folder name does not mean every component using these fonts is TextMeshPro.

For exact visual positions, select the corresponding objects in the existing WelcomeScene and copy their RectTransform and color settings into your practice scene. Rebuild one group at a time: background, title, loading bar, then supporting labels. Exact decoration is less important than understanding anchors and references initially.

### Rounded panels

`WelcomePanel` draws rounded rectangles with a top-to-bottom gradient. It derives from `MaskableGraphic`, a uGUI graphic type.

To create one manually, use a UI object with RectTransform and CanvasRenderer, then add WelcomePanel. Set `radius`, `topColor` and `bottomColor`. Remove an existing Image graphic if it would draw an unwanted second background. When creating these objects in code, explicitly add CanvasRenderer before WelcomePanel; missing that component previously caused invisible panels.

### Real loading, not a pretend timer

The script calls `SceneManager.LoadSceneAsync("Main Scene")` and temporarily prevents activation. Unity's load progress reaches approximately 0.9 before activation, so the screen normalizes it using `loading.progress / 0.9f`. Displayed progress moves smoothly toward that value.

The screen stays visible for at least three seconds. After loading is ready, it finishes the progress display, fades the CanvasGroup, and allows the village scene to activate. It also reports a useful error if Main Scene is missing from enabled build scenes.

**Check:** Configure scene order as described in lesson 13, open WelcomeScene, and press Play. It should open Main Scene automatically.

**Practice:** Change one tip and increase Minimum Display Time to 5. Observe the difference, then restore 3.

## 9. Ask for and save the player name

Two files work together:

- `PlayerProfile` handles the name data and rules.
- `NewPlayerOnboarding` creates and manages the dialog.

### Manual setup

1. Open Main Scene.
2. Add `NewPlayerOnboarding` to Main Camera.
3. Assign Bangers to Title Font and Roboto-Bold to Body Font.
4. Assign the camera's `VillageCameraController` component to Camera Controller.
5. Save and press Play.

You do not manually create the full name dialog when using this component: it creates the UI at runtime. Expand Main Camera in the Hierarchy during Play Mode to inspect `Player Identity UI`. Those generated objects disappear when Play Mode ends; this is expected.

### The flow to understand

```text
Read saved name
  -> Valid name exists: show Chief nameplate
  -> No valid name: lock camera and show input
       -> Validate typed name
       -> Done: show confirmation, without saving yet
       -> Edit: return to input
       -> Confirm: save, close popup, restore camera, show nameplate
```

Names must contain 2 to 15 characters after normalization. Allowed characters are ASCII letters, digits, spaces, hyphens, underscores and apostrophes. At least one letter or digit is required. Leading/trailing spaces are removed; repeated spaces are collapsed. Rich-text formatting is disabled for the name display.

The confirmed name is stored with PlayerPrefs using the key `Kingdoms.PlayerName.v1`. PlayerPrefs is local application storage, not an online login or a database shared between phones. Name uniqueness is not checked.

### Learn to build the popup yourself

In a separate practice scene, create an overlay Canvas, a dim background, a centered panel, a legacy InputField, guidance Text, and Done/Edit/Confirm buttons. Give the InputField a text component and placeholder, and set its character limit to 15.

Then compare your hierarchy to the code in `ShowNameDialog()`. Notice how `Rect`, `Panel`, `Label` and `MakeButton` are helper methods that perform the same operations you performed in the Editor. `onValueChanged.AddListener(ValidateInput)` connects typing to validation. `onClick.AddListener(Continue)` connects the button to the next step.

This manually drawn practice popup will not automatically connect to the current script's private runtime fields. To use it as a replacement, you would refactor the script to accept serialized references. First learn the existing version; do not run two separate name-dialog systems together.

An EventSystem with `InputSystemUIInputModule` delivers UI input. In the complete village, VillageGameplay creates it first, and onboarding reuses it. This prevents confirming the name from destroying the EventSystem needed by the shop.

**Check:** Try an empty name, one letter, punctuation only, and a valid name. Use Edit before confirming. Stop and play again: the valid saved name should skip the prompt.

## 10. Make the building prefabs

The starter models use Unity shapes and shared materials. They are simple original placeholder art; building a detailed production art set is a separate task.

### Create materials

Create materials in `Assets/Art/StarterVillage` with **Universal Render Pipeline/Lit** and low Smoothness, around `0.15`.

| Material | Approximate RGB, on a 0 to 1 scale |
| --- | --- |
| Honey Timber | `(0.55, 0.28, 0.10)` |
| Terracotta Roof | `(0.74, 0.16, 0.055)` |
| Warm Stone | `(0.53, 0.51, 0.43)` |
| Gold | `(1.00, 0.70, 0.08)` |
| Dark Timber | `(0.19, 0.10, 0.055)` |
| Pine | `(0.14, 0.35, 0.075)` |

Unity's color picker may display 0-255 values or another format. Multiply the values above by 255 if needed.

### Town Hall

1. Create an empty root named `TownHall`, positioned at `(0, 0, 0)` with rotation zero and scale one.
2. Add Cube children. Use the following **local** positions/scales, relative to that root.

| Part | Local position | Local scale | Material |
| --- | --- | --- | --- |
| Foundation | `(0, 0.18, 0)` | `(3.9, 0.36, 3.9)` | Warm Stone |
| Walls | `(0, 1.25, 0)` | `(3.2, 2, 3.1)` | Honey Timber |
| Left roof slope | `(-0.97, 2.65, 0)` | `(2.35, 0.22, 3.85)` | Terracotta Roof |
| Right roof slope | `(0.97, 2.65, 0)` | `(2.35, 0.22, 3.85)` | Terracotta Roof |
| Roof ridge | `(0, 3.22, 0)` | `(0.22, 0.25, 3.9)` | Gold |
| Door | `(0, 0.95, -1.58)` | `(0.85, 1.5, 0.12)` | Dark Timber |
| Door lintel | `(0, 1.76, -1.67)` | `(1.15, 0.22, 0.2)` | Gold |
| Chimney | `(0.9, 3.1, 0.85)` | `(0.45, 1.1, 0.45)` | Warm Stone |

3. Rotate the left roof slope around local Z by +28 degrees and the right by -28 degrees.
4. Add four dark corner beams at X +/-1.5, Y 1.3, Z +/-1.45. Scale each to `(0.28, 2.3, 0.28)`.
5. Add two gold-colored windows at X +/-1.08, Y 1.35, Z -1.58. Scale each to `(0.48, 0.58, 0.12)`.
6. Add a stone step at `(0, 0.16, -1.85)`, scale `(1.3, 0.32, 0.3)`.
7. Remove the individual primitive colliders. Add one BoxCollider on the root, Center `(0, 1.5, 0)`, Size `(4, 3, 4)`.
8. Drag the root from the Hierarchy into `Assets/Prefabs/StarterVillage` to create the prefab.

Its footprint is 4 x 4 grid cells. Keep the root pivot at ground level and centered horizontally. The door faces negative Z in the current model, making it visible from our camera.

### Gold Mine

1. Create an empty origin-centered root named `GoldMine`.
2. Add a stone base Cube at `(0, 0.12, 0)`, scale `(2.9, 0.24, 2.9)`.
3. Place seven stone Sphere children in a rough mound near the center/rear, around Y 0.72, with scales close to `(1.4, 1.5, 1.35)`.
4. Add a dark tunnel face at `(0, 0.8, -0.92)`, scale `(1.3, 1.35, 0.2)`.
5. Add timber posts at X +/-0.72, Y 0.85, Z -1.08, each scaled `(0.24, 1.7, 0.26)`.
6. Add a timber lintel at `(0, 1.66, -1.08)`, scale `(1.85, 0.3, 0.4)`.
7. Add two narrow rails near the ground and a timber cart at `(0, 0.51, -1.02)`, scale `(0.75, 0.4, 0.60)`.
8. Put several small gold-colored spheres on the cart and a gold sign above the entrance.
9. Remove primitive colliders; add one root BoxCollider, Center `(0, 1.5, 0)`, Size `(3, 3, 3)`.
10. Save the root as a prefab in the same folder.

Inspect the existing GoldMine prefab in Prefab Mode for every exact child transform. Manual placement of the rocks does not have to match exactly to learn how a prefab works.

### Pine Tree

The existing tree uses a cube trunk and three layers of a custom cone mesh. Unity's built-in primitive menu does not provide a cone.

For this lesson, use the existing `Assets/Art/StarterVillage/Pine Cone.asset` mesh in your practice project. Create a trunk centered at Y 0.65, scale `(0.35, 1.3, 0.35)`. Create three children with MeshFilter and MeshRenderer, assign the cone mesh and Pine material, and place them at Y `0.65`, `1.30`, and `1.95`, with uniform scales `1.20`, `0.97`, and `0.74`. Save as `PineTree`.

To learn custom mesh generation later, inspect `VillageFactory.cs` in the tooling folder listed in lesson 17. It builds the cone from eight triangles, assigns vertices and triangle indices, then calculates normals for lighting. That is a separate mesh-programming exercise, not a prerequisite for learning gameplay.

Create a final material called `Placement`, using **Universal Render Pipeline/Unlit**. The gameplay script creates a temporary copy and changes its color to green or red.

**Check:** Drag each prefab into a practice scene and inspect its scale and pivot. Delete those test instances before using VillageGameplay, which spawns its own buildings.

## 11. Understand resources, placement and saving

Start with [BuildingCatalog.cs](Assets/Scripts/Core/BuildingCatalog.cs). Each definition supplies a building's ID, footprint, price, resource type and base stats. [VillageState.cs](Assets/Scripts/Core/VillageState.cs) holds the saved village; [VillageProgression.cs](Assets/Scripts/Core/VillageProgression.cs) adds levels, builder jobs, limits and cancellation. These partial class files form one class and must stay together.

A new village starts with 1,000 gold, 500 elixir, 50 gems and a Town Hall. Existing saves retain their balances. Current purchase prices are:

| Building | Cost | Size | Town Hall 1 limit |
| --- | --- | --- | --- |
| Gold Mine | 150 elixir | 3 x 3 | 3 |
| Elixir Collector | 150 gold | 3 x 3 | 3 |
| Gold Storage | 300 elixir | 3 x 3 | 2 |
| Elixir Storage | 300 gold | 3 x 3 | 2 |
| Cannon | 250 gold | 3 x 3 | 2 |
| Archer Tower | 1,000 gold | 3 x 3 | 1 |
| Wall | 25 gold | 1 x 1 | 25 |

Purchases complete immediately. Upgrades take time and occupy one of two builders. Producers generate 60 resources per minute per level and hold 500 per level. Each storage adds 5,000 capacity per level to the Town Hall's 10,000 base capacity for that resource. A producer stops producing during an upgrade; its already stored resources remain collectable. See [Home Village progression](HOME_VILLAGE_PROGRESS.md) for upgrade prices, durations and Town Hall requirements.

Building coordinates describe the lower corner of the footprint. Its model is drawn at `(x + size / 2, 0, z + size / 2)`. Placement and movement reject overlaps and cells outside the 44 x 44 build area. Cancelling a placement does not charge you; cancelling a move restores the previous position.

Saving uses JSON in PlayerPrefs under `Kingdoms.Village.v1`; the key name stays the same even though the payload version is 3. Old valid saves migrate. Invalid data is preserved for investigation. Mutating UI actions validate a copy and save it before accepting the changed state. Do not reset your save just to see a new feature.

**Practice:** Buy an Elixir Collector, collect its output, and restart Play Mode. Confirm that both its location and your resources return. Then start an affordable upgrade, open the builder queue, and inspect the job. Choose Cancel Upgrade and read the confirmation: the refund is half the cost, limited by free storage. Closing the confirmation keeps the job running.

## 12. Connect the village, HUD, shop and practice battle

Open `Assets/Scenes/Main Scene.unity` to inspect the village. Use [SCENE_EDITING_GUIDE.md](SCENE_EDITING_GUIDE.md) for editable scene objects. Do not add a second gameplay component or regenerate the scene just to change a label. Many game controls are authored in the scene, while dialogs and the practice battlefield are created at runtime. Play Mode edits to generated objects do not persist.

The main component is [VillageGameplay.cs](Assets/Scripts/UI/VillageGameplay.cs). Its partial files share the same class: `VillageEditableUI` wires authored UI, `VillageInteractions` handles selection and progression actions, `VillageScreenshotReference` adapts the reference-style shop and dialogs, and `VillageReferenceScenery` supplies surroundings. Keep all the partial files when copying code into a learning project. Current model assets are in `Assets/Prefabs/ScreenshotReference`; the earlier primitive-model lesson is an exercise, not a replacement for these assets.

### Try the current controls

1. Start from WelcomeScene and enter or confirm your name if prompted.
2. Open Shop, select a supported resource or defense building, choose a clear grid location, and confirm placement.
3. Tap a building without dragging. Choose Move or Info / Upgrade. Select a defense to see its range circle.
4. Tap the builder indicator to inspect both builder slots and active jobs. Select a job to open its details.
5. Tap Attack! to scout the practice battlefield. Inspect the defense summary, choose a challenge, then press Start Attack. Deploy Left, Center or Right spends one of eight practice raiders per tap.
6. Watch raiders avoid footprints and approach the enclosed Town Hall through its entrance. If all remaining buildings are sealed off, the simulation can target and break a wall; choose Try Wall Breach while scouting to play the sealed layout.
7. Destroy all three buildings to win. Walls do not count toward destruction percentage. Losing all eight deployed raiders is defeat; the timer ends the battle after three minutes. You may return home early.
8. After a result, choose Watch Replay to watch the same attack again. Deployments play automatically and deployment buttons are disabled. The result reports whether playback matches the original. Choose Retry to start a new playable encounter or Return Home to restore your village and camera.

[PracticeBattle.cs](Assets/Scripts/Core/PracticeBattle.cs) runs combat in integer positions at 100 ms ticks, independently of Unity rendering. It uses a half-cell navigation grid, stable route ordering and route rebuilding when a structure is destroyed. [VillagePracticeBattle.cs](Assets/Scripts/UI/VillagePracticeBattle.cs) creates models, buttons, health bars and firing traces. Eight raiders may overlap; unit separation is not implemented.

Practice has no costs, loot, trophies or saved army. Its state never receives your home VillageState. Pausing the application freezes practice; closing the application discards it. Home economy processing resumes when you return. See [PRACTICE_BATTLE_PROGRESS.md](PRACTICE_BATTLE_PROGRESS.md) for detailed rules and limitations.

## 13. Prepare the mobile scenes

These steps describe the Unity 6 project configuration. The exact panel layout can differ between editor versions.

1. Open **File > Build Profiles**.
2. In the scene list used by the active build profile, enable WelcomeScene first and Main Scene second. If the profile overrides the shared scene list, check that override too.
3. Exclude SampleScene and VillageScene from this prototype's launch sequence.
4. For Android, ensure Unity Hub has installed Android Build Support plus the SDK/NDK and OpenJDK modules for this editor.
5. Select or create an Android build profile and switch platform when ready to build.
6. In **Project Settings > Player**, select the Android settings.
7. Under orientation settings, use Auto Rotation, allow Landscape Left and Landscape Right, and disable both portrait orientations.
8. Under Other Settings, use IL2CPP and ARM64, matching the current project.
9. Verify Active Input Handling uses the Input System Package, matching the scripts. Unity may request a restart when this changes.
10. Use a distinct application identifier for your learning build and choose a build output folder outside Assets.

Setting a landscape Game view only changes the Editor preview; it does not configure the phone application's orientation. Player settings do that.

After a successful Android build, test on a device: loading, name keyboard, safe areas, one-finger drag, pinch, placement, UI taps, pause/resume, and relaunch persistence. Use Build And Run only after the device is connected and available to Unity. These are steps for you to perform; this guide does not claim that the APK or those device checks already passed.

## 14. Test your work

Use these as checkpoints after each lesson, rather than waiting until everything is built.

| Test | Expected result |
| --- | --- |
| Open WelcomeScene and play | Loading screen opens Main Scene |
| Enter invalid name | Cannot confirm it as a valid profile |
| Enter valid name and choose Edit | Input is retained; not saved early |
| Confirm and restart | Nameplate appears; name dialog is skipped |
| Open shop | Camera gestures pause |
| Place preview over Town Hall | Red/invalid; no charge |
| Place outside border | Rejected |
| Cancel preview | Elixir unchanged; camera works again |
| Build first mine | One mine placed; elixir falls from 500 to 350 |
| Wait about 60 seconds | About 60 gold produced in that mine |
| Collect | Village gold increases; mine storage decreases |
| Build three mines at Town Hall 1 | Fourth mine unavailable until the Town Hall limit increases |
| Upgrade and open builder queue | Job, target level and remaining time appear |
| Confirm upgrade cancellation | Current level retained; builder freed; storage-capped half refund |
| Open Attack and deploy | Separate practice battle; defenses fire and raiders route through the entrance |
| Finish, retry and return | Encounter resets; home village state is preserved |
| Stop and play again | Confirmed buildings and resources reload |
| Change landscape aspect ratio | UI remains usable and camera remains bounded |
| Pause/relaunch on phone | Saved village returns; capped offline progress applies |

### Reset only your learning game's progress

For first-player testing, use a separate practice application identity as described earlier. If you want a reset command there, create `Assets/Editor/KingdomsLearningReset.cs` with this complete editor-only script:

```csharp
using UnityEditor;
using UnityEngine;

public static class KingdomsLearningReset
{
    [MenuItem("Tools/Kingdoms Learning/Reset Local Progress")]
    public static void ResetProgress()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Stop Play Mode before resetting progress.");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Reset learning progress?",
            "This deletes the saved name and village for this application's Editor preferences.",
            "Reset", "Cancel")) return;

        PlayerPrefs.DeleteKey("Kingdoms.PlayerName.v1");
        PlayerPrefs.DeleteKey("Kingdoms.Village.v1");
        PlayerPrefs.Save();
        Debug.Log("Learning progress reset. Start Play Mode again.");
    }
}
```

This example is provided in the guide, not installed as an active project command. It deletes only the two named keys. Do not replace it with `PlayerPrefs.DeleteAll()`, which deletes other preferences too. Reset while stopped, because the running village could save its in-memory state again.

### What was already verified

The scripts were compiled and relevant features were exercised in an isolated Unity project. Welcome checks covered UI rendering and asynchronous activation with a minimal destination scene. Naming checks covered validation, confirmation, camera locking, saved names and returning-player behavior.

The latest village checks used URP and covered overlap/bounds, cost, cancellation, production, offline cap, backward clock handling, collection capacity, mine limit, preservation of corrupt saves, name integration, EventSystem lifetime, camera locking, purchase and scene-reload persistence. Actual village/shop images were rendered and inspected.

See [VillageValidation.txt](VillageValidation.txt), [VillagePreview.png](VillagePreview.png), [VillageShopPreview.png](VillageShopPreview.png), [WelcomePreview.png](WelcomePreview.png), and [PlayerNamingPreview.png](PlayerNamingPreview.png). Preview screenshots contain test-session data, not a promise that your existing local save has those values.

These checks do not replace a complete launch-to-village run on the target phone or a mobile performance test.

## 15. Solve common problems

| Problem | First things to check |
| --- | --- |
| Script cannot be attached | Filename/class name match; class derives from MonoBehaviour; Console has no compile errors |
| Duplicate class error | Same source was pasted or imported twice |
| Pink materials | Project uses URP; material has the intended URP-compatible shader |
| Loading cannot open village | Exact `Main Scene` name and enabled build scene list |
| Name popup does not appear | A valid name is already saved; use the learning reset only if desired |
| Name popup/HUD absent before Play | These objects are created at runtime |
| Buttons do not respond | EventSystem exists with InputSystemUIInputModule; Canvas has GraphicRaycaster |
| Camera moves behind a UI interaction | Inspect UI raycast targets and camera blocking state |
| Rounded panel invisible | RectTransform has size; CanvasRenderer and WelcomePanel exist; colors have nonzero alpha |
| Missing prefab or NullReferenceException | Inspect VillageGameplay's prefab, material, font and camera assignments; open the exact reported code line |
| Buildings follow camera | Village Gameplay is parented to camera or another moving object; restore a stationary origin root |
| Two Town Halls appear | A manually placed scene instance exists as well as the runtime-spawned one |
| Mine appears offset from its footprint | Prefab pivot, root scale and child local positions are wrong |
| Elixir change does not take effect | Existing save overrides new starting defaults |
| UI edit disappears | Edit was made to a generated object during Play Mode; change its creation code instead |
| Camera zoom default edit does not take effect | Scene's serialized Inspector value overrides the code initializer |
| Can't buy a fourth mine | Town Hall 1 allows three; inspect your current Town Hall level |
| Can't buy another mine | Check elixir balance, Town Hall limit and available placement space |
| Village save error | Data was rejected and preserved; investigate before choosing to reset a learning save |

When debugging, write down: what you expected, what actually happened, the first Console error, and the last change you made. This is more useful than changing several settings at once.

## 16. Your learning schedule

Treat these as sessions, not deadlines.

| Session | Build or study | You should be able to explain |
| --- | --- | --- |
| 1 | Open project; inspect scenes and components | Scene versus prefab versus GameObject |
| 2 | Recreate ground and material | Scale, world coordinates, shader versus collider |
| 3 | Configure camera and test controls | Orthographic size, focus, ray-to-ground movement |
| 4 | Rebuild a basic loading Canvas | Anchors, scaling, references and async loading |
| 5 | Read name validation and reproduce onboarding setup | Validate before saving; returning-player detection |
| 6 | Build Town Hall and mine from primitives | Local transforms, pivots, shared materials and prefabs |
| 7 | Trace one mine purchase in the code | Preview versus confirmed data; overlap and cost checks |
| 8 | Test production, collection and reload | Timestamp calculation, capacity and JSON persistence |
| 9 | Rebuild a practice HUD | Generated UI, button callbacks, safe areas and input blocking |
| 10 | Prepare and test Android | Build scene order versus open Editor scene; device-specific checks |

After each session, add a short note below:

```text
Date:
What I built manually:
What each important component does:
One problem I solved:
One thing I still do not understand:
What I will try next:
```

Collectors, selection, upgrades and the first practice battle are now implemented. Further work includes trained armies, more opponents, battle rewards, unit separation and device testing. Use [GAME_DEVELOPMENT_PHASES.md](GAME_DEVELOPMENT_PHASES.md) to choose the next unfinished milestone.

## 17. Record of the AI-assisted work

The existing Unity project was the starting point. The following systems/assets were added or configured during our development work:

1. Inspected the project and its scenes to identify the playable village scene.
2. Added the village ground material/shader and configured the ground plane.
3. Added the angled camera controller with mouse/touch movement, zoom, glide, bounds and UI filtering.
4. Created the opening welcome scene with generated Kingdoms artwork, layered title, rounded loading graphics, progress, tips and transition.
5. Added local player-name validation/persistence and the first-visit name dialog/nameplate.
6. Configured the mobile scene order and landscape settings.
7. Added original primitive-based Town Hall, Gold Mine and Pine Tree assets.
8. Added the resource state, shop, grid placement, production, collection and local village saving.
9. Added camera input blocking for shop/placement and set the scene's closer starting zoom to 16.
10. Ran the checks described above, saved preview images, and installed the changes with backups.
11. Wrote this beginner guide from the current source files and settings.

### How the automation relates to your manual work

The AI used scripts to create files, construct model assets through Unity's Editor API, configure scene references, run checks, and copy verified results into the project. These are shortcuts for many of the same operations you can perform in Unity's Inspector and Hierarchy. You do not need the installation scripts to play the game.

The tooling folder is `C:\Users\subas\KingdomsCameraSetup`. It contains staged source files, preparation/installation scripts and the isolated `WelcomeValidation` Unity project. Despite its name, that isolated project was also used for onboarding and village checks.

Useful advanced references in that folder:

- `WelcomeValidation/Assets/Editor/VillageFactory.cs`: code that constructs the starter materials, meshes and prefabs.
- `WelcomeValidation/Assets/Editor/VillageValidation.cs`: isolated village validation code.
- `OnboardingValidation.cs`: onboarding validation source.
- `prepare_village.py`, `export_village.py`, `install_village.ps1`: preparation, export and verified installation workflow.

Read those after the gameplay lessons. Do not rerun the installers as a way to learn ordinary Unity editing; they may refuse changed files or overwrite files they were specifically prepared to update.

Existing backup folders in the game project preserve files before earlier changes. The village installation backup is `VillageGameplayBackup-20260910-190112`. It contains the previous Main Scene and camera script; it is a change backup, not a full-project backup.

### Older notes

[CameraSetup.md](CameraSetup.md), [WelcomeUI.md](WelcomeUI.md), [PlayerNaming.md](PlayerNaming.md), and [VillageGameplay.md](VillageGameplay.md) describe individual development steps. Some statements in the first two refer to an earlier stage: the grid now has gameplay placement rules, and SampleScene is no longer enabled in the build list. Use this guide's current scene table and the actual project settings when rebuilding.

For tomorrow's development session, reopen the project and this guide, read your learning notes, and start with the next unfinished checkpoint. The source files remain the complete implementation you can compare against while recreating each part manually.


### 23 September 2026 development update

Added the screenshot-inspired HUD, shop, resource models and scenery; builder queue and upgrade cancellation; Cannon and Archer Tower progression; and isolated practice combat with wall routing and breach logic. See [SCREENSHOT_REFERENCE_MATCH.md](SCREENSHOT_REFERENCE_MATCH.md) and [DEFENSE_VILLAGE_PROGRESS.md](DEFENSE_VILLAGE_PROGRESS.md). The supplied images are visual references; this remains a partial Kingdoms implementation.

Validation evidence: [builder queue](BuilderQueueValidation.txt), [upgrade cancellation](UpgradeCancellationValidation.txt), [reference UI](ScreenshotReferenceValidation.txt), [defenses](DefenseVillageValidation.txt), and [practice battle](PracticeBattleValidation.txt). Practice checks passed entrance preference, sealed-wall breach, route rebuilding, footprint avoidance, deterministic combat, terminal outcomes, retry/return controls and exact home-state/save preservation. [Current battle preview](PracticePreviews/practice-combat.png).

These checks used the isolated `.utmp/ResourceValidation` Unity project and its separate PlayerPrefs identity. Do not run validation entry points against your real village or change their identity guards to force them to run. No APK was rebuilt for these additions; phone performance and the full mobile flow remain unverified.


### Practice battle replay update

The results screen now offers **Watch Replay**. Each accepted deployment records its lane and simulation tick. `PracticeReplay` in [PracticeBattle.cs](Assets/Scripts/Core/PracticeBattle.cs) rebuilds the fixed encounter and applies these commands at the same ticks. A stable numeric state hash checks the final outcome, positions, health and attack timers against the recording. The recording includes its enclosure setting and uses rules version 1.

**Try it:** Deploy two raiders, wait a few seconds, deploy the others, and finish the attack. Watch Replay and observe the same delay between deployments. Manual deployment is disabled during playback. Retry leaves replay mode and restores eight playable raiders. Return Home also works during playback.

Recordings stay in memory for the current encounter. Retry, returning home or closing the application discards access to that replay. There is no replay export, saved replay browser, speed control or compatibility with older combat rules. This is local fixed-layout playback, not server verification. Validation checks every tick for open and sealed layouts, timed surrender, timeout, mismatch detection, UI controls and home-save preservation. See [PracticeBattleValidation.txt](PracticeBattleValidation.txt) and [replay preview](PracticePreviews/practice-replay.png). No APK rebuild or physical-phone replay test was performed.


### Selectable practice challenges

Attack opens **Open Gate**, where raiders use the entrance in the Town Hall enclosure. While scouting, choose **Try Wall Breach** at the upper left to close that entrance. The same eight-raider army must now destroy a wall to reach the Town Hall. Neither challenge charges resources or grants rewards.

You can change challenges while scouting or after a result. Start Attack locks the challenge even before the first deployment. Switching starts a fresh encounter and replaces the previous replay. The switch is disabled during an active attack and during replay playback. **Retry** returns to scouting with the current challenge; **Watch Replay** reconstructs the same enclosure. Returning home and opening Attack again defaults to Open Gate. Challenge selection is not saved.

Validation covers selecting both layouts, rejecting mid-attack changes, winning the sealed challenge with wall destruction, replaying both layouts, keeping the selected challenge on retry, and preserving the home save. See [challenge preview](PracticePreviews/practice-wall-challenge.png) and [validation report](PracticeBattleValidation.txt). No APK rebuild or phone testing was performed.


### Battle results and surrender

Practice results now show **stars out of three**, buildings destroyed, destruction percentage, deployed raiders, surviving deployed raiders, and elapsed simulation time. Stars are practice scores only; they do not grant loot, trophies or progression.

| Objective | Stars earned |
| --- | --- |
| Destroy the Town Hall | 1 |
| Destroy at least half of the non-wall buildings | 1 |
| Destroy all non-wall buildings | 1 |

With three buildings, half destruction requires two destroyed buildings. Destroying only the Town Hall earns one star; destroying both defenses also earns one star. Destroying the Town Hall plus one defense earns two; destroying all three earns three. Walls never count toward these objectives. Full destruction remains the condition for Victory; stars do not turn a surrendered or timed-out encounter into a victory.

**Try it:** Open Attack, press Start Attack, deploy a raider, wait briefly, then press **Surrender** at the lower left. Combat stops and the results panel appears. Choose Watch Replay to reproduce the attack up to that surrender, Retry to reset the selected challenge, or Return Home. Surrender is unavailable during replay and after results. Returning home directly still discards the encounter without showing its results.

The score is derived from combat state in [PracticeBattle.cs](Assets/Scripts/Core/PracticeBattle.cs); the results panel and surrender control live in [VillagePracticeBattle.cs](Assets/Scripts/UI/VillagePracticeBattle.cs). Validation covers all eight combinations of the three destroyed buildings, wall exclusion, survivors versus undeployed reserves, victory results, surrender/replay/retry controls, and the existing combat/save checks. See [surrender preview](PracticePreviews/practice-surrender.png) and [PracticeBattleValidation.txt](PracticeBattleValidation.txt). No APK was rebuilt or phone testing performed for this update.


### Scout before starting an attack

Opening Attack now enters **Scouting**. Inspect the battlefield and the bottom summary: Cannon and Archer Tower damage/range come from the combat entities, and the enclosure description explains whether raiders can use an entrance or must break through. You may switch Open Gate / Wall Breach or return home at no cost.

Scouting has no countdown: simulation ticks, production of combat events and deployments remain stopped. Press **Start Attack** to begin the three-minute battle and reveal the deployment buttons. From that point, the timer runs even if you deploy nobody, and challenge switching is locked until results. Surrender becomes available once the attack starts.

Retry and challenge changes return to scouting; Watch Replay immediately plays the recorded battle without repeating scouting time. This preparation phase is UI state, not part of the recorded combat timeline or village save. The camera remains fixed during scouting; defense selection and range overlays are not included yet.

Validation waits across real Play Mode frames to check the tick stays zero, rejects deployment/surrender while scouting, starts at tick zero, locks challenges before deployment, and verifies retries and both existing replay flows. See [scouting preview](PracticePreviews/practice-ready.png) and [PracticeBattleValidation.txt](PracticeBattleValidation.txt). The guide, preview and code are updated together; APK and physical-phone testing remain outstanding.


### Historical roadmap review - 23 September 2026

The [phase plan and status table](GAME_DEVELOPMENT_PHASES.md#current-phase-status---24-september-2026) now distinguish completed local work from partial and future phases. Phase 2 resource production/storage is complete within its local scope. Phases 0?1, 3?7 and 15 are partial; online/social/release/expansion phases are not complete. Scouting, replay and stars are practice features, not a complete campaign or multiplayer game.

Next priorities are: validate the latest mobile build, add an owned army and preparation/capacity rules, support multiple troop types and ground deployment, load authored enemy snapshots, and award campaign rewards exactly once. That creates the complete build ? prepare ? attack ? earn ? upgrade loop. Accounts, PvP and clans follow that foundation. See the [next implementation plan](GAME_DEVELOPMENT_PHASES.md#5-next-implementation-plan) for acceptance gates. This roadmap update changes documentation only; it does not add gameplay or claim new runtime tests.


### Historical owned army foundation - 24 September 2026

Open **Buildings**, then **Prepare Army**. Press **Add Raider** to prepare one unit, **Fill Army** to use all eight starter spaces, **Remove 1** to reduce the roster, or **Clear Army** to empty it. Preparation is free and instant; no gold, elixir or builder is spent. A nonempty legal roster shows READY. Full and empty rosters disable the corresponding add/remove controls. Close and reopen the screen, or restart the game, to confirm the roster persists.

At this first owned-army increment, capacity was fixed and barracks/camps were not yet implemented. The facilities update below adds them. At this historical point only Raiders existed; later campaign and Archer increments below add roster consumption and troop choice. Attack continues to provide its separate free practice army; preparing or clearing your saved roster does not change practice difficulty.

Study [the troop catalog and army rules](Assets/Scripts/Core/VillageArmy.cs), [the preparation screen](Assets/Scripts/UI/VillageArmy.cs), and [save migration](Assets/Scripts/Core/VillageState.cs). Each action edits a copy, validates it, writes it, then replaces the live state only after a successful save. Save version 4 adds the roster. Versions 1-3 migrate to an empty army without charging resources; version 3 JSON is backed up at `Kingdoms.Village.pre-v4` before later saves overwrite it. Unknown troops, duplicate stacks, nonpositive stack counts and excessive housing are rejected.

Validation: [ArmyPreparationValidation.txt](ArmyPreparationValidation.txt) records the isolated Unity checks; [the validation source](Assets/Editor/ArmyPreparationValidation.cs) covers migration, invalid requests, persistence, actual UI controls and practice isolation. [Resource/save regression](ArmyResourceRegression.txt) also passed 66 existing assertions. No Android build or physical-phone test is included. Earlier v3-save explanations are historical; the campaign update below advances the format to v5.


### Barracks and Army Camps - 24 September 2026

**Try it:** Open **Shop > Army**. Buy **Barracks** for **200 elixir**, then place it on a free 3 x 3 area. Army Camp becomes available for **250 elixir**. Place a camp, then open **Buildings > Prepare Army** and press **Fill Raiders**. You can now prepare 16 Raiders: eight starter spaces plus eight from the camp. Preparation itself remains free and instant. Old saves keep their existing starter roster; this increment used save version 4 (now migrated to version 5).

Select the camp and open **Info / Upgrade**. Level 2 costs 500 elixir, occupies one builder for 30 seconds, and increases that camp's contribution from 8 to 16 spaces (24 total with one camp). Current capacity and roster readiness remain available during the upgrade. Completion, including completion while the app is closed, adds capacity once. Cancelling uses the existing 50% refund rule and keeps the previous capacity. Move the building freely without losing its contribution.

There is one Barracks per village; it unlocks camps. The original facilities increment had no Barracks upgrades; the Tank increment below adds level 2. Camp limits are one, two and three at Town Hall levels 1, 2 and 3. Each camp supports levels 1-3; level 3 requires Town Hall 2, costs 1,250 elixir and takes 120 seconds. Maximum current army capacity is 80 (eight starter spaces plus three level-3 camps). Neither facility generates or stores currency. New construction is still instant.

Study [building definitions](Assets/Scripts/Core/BuildingCatalog.cs), [army capacity](Assets/Scripts/Core/VillageArmy.cs), [building progression](Assets/Scripts/Core/VillageProgression.cs), [preparation UI](Assets/Scripts/UI/VillageArmy.cs) and [editable model authoring](Assets/Editor/ArmyBuildingAuthoring.cs). Models live in `Assets/Resources/ArmyBuildings`; the shop uses rendered portraits in `Assets/Resources/BuildingIcons`. The shop explains the Barracks prerequisite and building details show the actual camp capacity gain.

Validation evidence: [ArmyFacilitiesValidation.txt](ArmyFacilitiesValidation.txt), generated by [the isolated Editor validation](Assets/Editor/ArmyFacilitiesValidation.cs), covers prerequisites, costs, limits, capacity, upgrades/cancellation/offline completion, save validation, real purchases, moving, preparation and scene reload. [Preparation preview](ArmyFacilitiesPreviews/army-facilities-preparation.png) and [shop preview](ArmyFacilitiesPreviews/army-facilities-shop.png) show the tested screens. The existing [starter-army/migration checks](ArmyPreparationValidation.txt) were rerun and passed with the new camp rules. Prefab material references were also checked against the main project assets. No APK or physical-phone test was performed.

Known limits: camps enlarge the saved roster used by the campaign below. Practice still supplies its own eight Raiders, and neither consumes nor deploys this roster. Barracks upgrades, tank troops and level-specific building art remain future work.


### First offline campaign loop - 24 September 2026

1. Open **Buildings > Prepare Army**, then **Fill Raiders**. Preparation is still free and instant.
2. Press **Campaign**, then **Gate Outpost - Scout**. Scouting has no timer or cost; Return Home keeps your roster.
3. Press **Start Attack**. This commits and spends the **entire prepared roster**, including any Raiders you never deploy. The commitment saves before combat starts. Deploy with the left, center and right buttons; your saved counts control the separate Raider and Archer deployment limits.
4. Destroy all three non-wall buildings to win. Surrender, defeat and timeout do not earn resources. Partial stars are informational. Return Home during a live attack surrenders it.
5. On the victory screen, press **Claim**. The first Gate Outpost victory grants **500 gold and 300 elixir**, and unlocks **Sealed Keep**. Its first victory grants **1,000 gold and 600 elixir**. Repeat victories give no resources. Finish/Dismiss clears nonreward results.
6. Return home and spend the reward on buildings or upgrades. Prepare another army for free before attacking again. Camps increase the number of Raiders you can commit.

Results are saved before rewards are claimed. If you leave a result or restart, reopen **Prepare Army > Campaign** and use **Claim / Finish**. Both reward currencies must fit in storage; if they do not, spend resources or build/upgrade storage and claim later. The complete reward stays pending. Reward balances, the cleared flag and removal of the pending result are written together, preventing a second claim after reload. A pending result must be resolved before another campaign attack.

Historical behavior before battle recovery: an unfinished attack could not resume after closing the app or reloading the village scene. The campaign menu offers **Abandon Attack**; assigned Raiders stay spent, and you can prepare replacements for free. Pausing the application keeps a live battle frozen in memory. Replay reconstructs the current completed attack, including its original army size, without consuming troops or awarding resources. Replays themselves are not saved to disk.

The two missions reuse the tested Open Gate and Wall Breach enclosures. This is a small fixed-layout campaign, not arbitrary village snapshots, PvP or a complete campaign content pipeline. The main **Attack!** button still opens free practice; campaign is reached through army preparation. Home buildings take no damage. Resource production and upgrades catch up when returning home.

Study [campaign definitions and settlement](Assets/Scripts/Core/VillageCampaign.cs), [campaign menus and battle integration](Assets/Scripts/UI/VillageCampaign.cs), [combat and replay budgets](Assets/Scripts/Core/PracticeBattle.cs) and [save migration](Assets/Scripts/Core/VillageState.cs). Save version 5 adds campaign progress and the active/pending attempt; migration preserves version-4 armies and buildings and backs up the old payload at `Kingdoms.Village.pre-v5`. Replay rules version 2 includes the army budget. Completed results are locally re-simulated before acceptance; local saves and hashes are not server security.

Validation: [CampaignValidation.txt](CampaignValidation.txt) and [its source](Assets/Editor/CampaignValidation.cs) cover settlement/restart rules, migrations, army limits, both missions, replay and the actual campaign UI. See [campaign menu](CampaignPreviews/campaign-menu.png), [results](CampaignPreviews/campaign-result.png) and [claimed reward](CampaignPreviews/campaign-claimed.png). The existing practice/replay suite and army preparation/migration checks were rerun successfully; [resource/save regression](ArmyResourceRegression.txt) passed 66 assertions with v5 saves. Android builds, physical-phone performance, interrupted combat resumption, persisted replays, tank troops and arbitrary layouts remain outstanding.


### Mixed armies and Archers - 24 September 2026

Build **Barracks** to unlock **Archers**, then open **Buildings > Prepare Army**. Use the **Raiders / Archers** buttons to choose a troop. **Add** and **Remove 1** change the selected type. **Fill Raiders / Fill Archers** adds as many of that type as fit while retaining your other troops. **Clear Army** removes both. All preparation remains free and instant; the roster saves after each change.

| Troop | Army spaces | Health | Damage per second | Attack range |
| --- | --- | --- | --- | --- |
| Raider | 1 | 90 | 20 | 0.85 cells |
| Archer | 2 | 55 | 14 | 3.5 cells |

Both move at 2.8 cells per second and attack once per second. Archers wear green and carry a bow and quiver; Raiders retain blue tunics and spears. Damage applies immediately on the simulation attack tick, with a visual firing trace. Archer shots require a clear line to the target: intact walls block them, and Archers can attack a blocking wall. This does not add projectile travel time or new unit animations.

**Try a mixed army:** With one level-1 Army Camp (16 spaces), add eight Raiders, select Archers, then press **Fill Archers**. The result is 12 troops using 16 spaces: eight Raiders and four Archers. If only one space remains, an Archer will not fit; switch to Raiders to use it.

Open **Campaign**, scout a mission and press **Start Attack**. When the roster contains Archers, the battle shows **Raiders / Archers** selectors above the three deployment buttons. Select a type, then deploy left, center or right. Counts are independent: exhausting Raiders does not deploy an Archer automatically. Archer-only armies start with Archers selected. The entire composition is committed at attack start, including undeployed units. Practice continues to supply only its eight free Raiders.

Save version 6 preserves version-5 armies, campaign progress, active attempts and unclaimed rewards; it adds the committed Archer count. Version-5 payloads are backed up at `Kingdoms.Village.pre-v6`. Campaign verification checks both troop counts, so a recording with the same total count but a different composition is rejected. Replay rules version 3 records each deployment's troop type and both budgets. The 80-space village maximum permits at most 80 Raiders or 40 Archers, or a mixture within that housing limit.

Study [troop definitions and roster rules](Assets/Scripts/Core/VillageArmy.cs), [mixed combat and replay](Assets/Scripts/Core/PracticeBattle.cs), [preparation controls](Assets/Scripts/UI/VillageArmy.cs), [deployment controls and models](Assets/Scripts/UI/VillagePracticeBattle.cs), and [campaign commitment](Assets/Scripts/Core/VillageCampaign.cs). Earlier Raider-only and save-version examples above are historical where superseded by this increment.

Validation evidence: [MixedArmyValidation.txt](MixedArmyValidation.txt) and [validation source](Assets/Editor/MixedArmyValidation.cs) cover housing, unlocks, typed deployment, range/cooldown/wall behavior, both campaign wins, per-tick replay equality, migrations and live UI/save/reload flows. See [mixed preparation](MixedArmyPreviews/mixed-army-preparation.png) and [mixed combat](MixedArmyPreviews/mixed-army-combat.png). Original practice/replay, army preparation/migration and campaign/reward suites were rerun successfully; [resource regression](ArmyResourceRegression.txt) passed 66 assertions with v6 saves. At this increment, tank troops, manual ground deployment, unit separation, arbitrary enemy layouts, phone validation and an updated APK remained outstanding. Ground deployment is added below.


### Ground deployment ? 24 September 2026

Practice and campaign attacks now accept clicks or taps inside the green southern deployment outline. The three lane buttons remain available. Each accepted ground tap places one selected troop, snapped to a half-cell; rejected taps spend no troops. Deployment is free and instant, consistent with army preparation.

**Try it:** Open **Attack!** for practice, or prepare a mixed army and choose a campaign mission. Inspect the green outline while scouting, press **Start Attack**, then click or tap inside it. For a mixed army, select **Raiders** or **Archers** first. Dragging, pressing a UI control, or using multiple fingers cancels ground placement. Lift all fingers before trying again. The instructions show placement feedback. Surrender or finish the attack, then **Watch Replay** to see the same troop positions and timing.

The zone covers world X -10 through 10 and Z -14 through -8. Simulation rules validate bounds, available troops, live building footprints and battle state. Both ground taps and lane buttons record exact integer X/Z coordinates. In-memory replay rules are now version 4; village saves remain version 6. Older replay-version descriptions above are historical; replay export and cross-version loading are not implemented.

Study [deployment legality and recorded coordinates](Assets/Scripts/Core/PracticeBattle.cs), [screen-to-ground projection, gestures and outline](Assets/Scripts/UI/VillageGroundDeployment.cs), and [battle lifecycle integration](Assets/Scripts/UI/VillagePracticeBattle.cs). Evidence: [ground deployment checks](GroundDeploymentValidation.txt), [validation source](Assets/Editor/GroundDeploymentValidation.cs), and [rendered deployment preview](GroundDeploymentPreviews/ground-deployment.png). The mixed-army suite also passed with replay rules v4, including campaign results, once-only claims and save migration. The practice suite passed scouting, both layouts, lane controls, replay, retry, surrender and home-save preservation.

Known limits: the deployment zone is a fixed rectangle for the two existing layouts. Troops can overlap; there is no hold-to-deploy or drag-to-deploy. Automated gesture checks call the shared handlers directly; physical multitouch, phone performance and an updated APK remain unvalidated. At that increment, Tank troops and broader enemy layouts were still future work. Tanks are added below.


### Tank troops and Barracks upgrades ? 24 September 2026

Armies now support **Raiders, Archers and Tanks**. The Tank is a large shield-bearing melee unit with **300 HP**, **16 damage per second**, **0.85-cell attack range**, and **four housing spaces**. It moves at 1.8 cells/second versus 2.8 for Raiders and Archers. It prioritizes living defenses, breaches walls when needed, and attacks other buildings once defenses are destroyed. It has no taunt; defenses retain their usual target selection.

**Beginner walkthrough:** Build Barracks, open its building details, and upgrade it to **level 2** for **400 elixir and 30 seconds** using a builder. Tanks unlock when the upgrade finishes. Canceling keeps them locked and uses the normal half-cost refund. Level 2 is currently the Barracks maximum. Go to **Buildings > Prepare Army**, select **Tanks**, and use Add or Fill. Preparation remains free and instant. Clear removes all three types.

A level-1 Army Camp provides 16 total spaces: try **two Tanks, four Raiders and two Archers**. In a campaign battle, select Tanks and tap the green deployment zone, or use the lane buttons. Tanks have larger purple bodies and shields. A Tank-only army selects Tanks automatically. Practice still supplies eight free Raiders.

Save version **7** adds Tank counts to committed campaign armies. Version-6 saves migrate with zero committed Tanks while preserving prepared armies, mission progress and pending rewards; the original is backed up as `Kingdoms.Village.pre-v7`. Replay rules **5** include Tank composition, movement and exact deployment positions. Earlier version descriptions in this guide are historical. Recordings remain in memory only.

Source links: [troop stats and unlocks](Assets/Scripts/Core/VillageArmy.cs), [Barracks upgrade rules](Assets/Scripts/Core/VillageProgression.cs), [combat and replay](Assets/Scripts/Core/PracticeBattle.cs), [campaign composition checks](Assets/Scripts/Core/VillageCampaign.cs), [save migration](Assets/Scripts/Core/VillageState.cs), [preparation UI](Assets/Scripts/UI/VillageArmy.cs), and [battle models and selection](Assets/Scripts/UI/VillagePracticeBattle.cs).

Validation evidence is recorded in [TankValidation.txt](TankValidation.txt), with [test source](Assets/Editor/TankValidation.cs), [preparation preview](TankPreviews/tank-preparation.png) and [battle preview](TankPreviews/tank-deployment.png). Regression checks also passed for mixed Raider/Archer armies, facility progression and the Barracks upgrade UI; resource/save rules passed 66 assertions on v7. Phone controls/performance and an updated APK remain unvalidated. Balance is provisional; unit separation, broader enemy layouts and research are still outstanding.


### Authored enemy villages ? 24 September 2026

The Border Campaign now has four sequential missions. **Crossfire Pass** unlocks after Sealed Keep; **Hillfort** unlocks after Crossfire Pass. Their first-clear rewards are **1,500 gold + 900 elixir** and **2,000 gold + 1,200 elixir**. Full storage defers the entire reward, and repeat clears give no extra currency.

**Try it:** Prepare your army and open **Campaign**. Clear the first two missions, then scout Crossfire Pass. Its four buildings include three defenses around an open approach. Hillfort has five buildings: a sealed Town Hall, three defenses and a Gold Storage. The storage counts toward destruction and victory; destroying it does not award separate loot. Walls do not count toward destruction. All non-wall buildings must fall for a three-star victory.

Scouting displays building/defense counts, defense damage and range, approach hints, and the reward status. The green deployment outline follows the selected village. Crossfire uses X -12 to 12, Z -15 to -9.5; Hillfort uses X -9 to 9, Z -15 to -10. The original practice zones and lane-button positions remain available. The larger villages use a wider fixed camera view.

One validated army for both new missions is **eight Raiders, four Archers and four Tanks** (32 spaces). Town Hall 2 and two level-2 Army Camps provide 40 total spaces; level-2 Barracks unlock Tanks. Deploy Tanks toward the front, support them with Raiders and Archers, and inspect where the defenses overlap. Preparation remains free and instant; starting an attack spends the full assigned roster.

**How the data works:** [EnemyLayout.cs](Assets/Scripts/Core/EnemyLayout.cs) holds immutable authored building lists and deployment rectangles. Each battle copies them into independent combat entities, so damage cannot alter another attack. New layouts validate unique building IDs below 100, one Town Hall, navigation bounds, non-overlapping footprints and an empty deployment zone. The original encounters preserve their historical edge-overlapping defense/wall geometry. This is a fixed authored catalog, not an arbitrary village importer or in-game editor.

Replay rules **6** retain the actual immutable layout alongside troop composition and deployment commands. The state hash includes layout identity/revision and entity kind; campaign verification rejects another layout even if it has the same open/sealed setting. Village saves remain **version 7**, with existing armies, clear flags and pending rewards preserved. Older notes describing only two missions or fixed practice-only layouts are historical.

Source links: [combat/replay](Assets/Scripts/Core/PracticeBattle.cs), [mission rewards and unlocks](Assets/Scripts/Core/VillageCampaign.cs), [campaign menu](Assets/Scripts/UI/VillageCampaign.cs), [scouting and rendering](Assets/Scripts/UI/VillagePracticeBattle.cs), and [ground deployment](Assets/Scripts/UI/VillageGroundDeployment.cs). Validation: [EnemyLayoutValidation.txt](EnemyLayoutValidation.txt) and [test source](Assets/Editor/EnemyLayoutValidation.cs). Previews: [campaign menu](EnemyLayoutPreviews/expanded-campaign.png), [Crossfire](EnemyLayoutPreviews/crossfire-scout.png), [Hillfort](EnemyLayoutPreviews/hillfort-scout.png).

The original campaign and practice regression suites also passed, covering pending rewards, legacy mission wins, replay/retry, surrender and home-save preservation.

Known limits: four authored villages, a fixed navigation grid, fixed defense stats, no unit separation, no external snapshot import and no persisted/exported replays. Phone performance and an updated APK remain unvalidated.


### Resumable Village Guide ? 24 September 2026

Click **Village Guide** near the top of the home village to open an eight-step checklist:

1. Build a Gold Mine (150 elixir).
2. Collect gold from it; production alone does not finish this step.
3. Build an Elixir Collector (150 gold).
4. Build Barracks (200 elixir).
5. Build an Army Camp (250 elixir).
6. Prepare an army for free.
7. Win **Gate Outpost** and claim its reward.
8. Complete a building upgrade. The guide opens the Gold Mine upgrade (300 elixir, 30 seconds), but any completed building upgrade counts.

Use the guide's action button to open the relevant shop category, collect resources, prepare troops, open Campaign, or inspect the upgrade. Buying, placing, starting attacks and upgrades still use their normal controls. Your first camp may require more elixir: let the collector produce and collect it. The guide grants no extra resources and charges no tutorial fee.

Home hints show the next unfinished step. **Pause Hints** hides those hints while keeping the guide available; **Resume Hints** restores them. Progress continues while hints are paused. Ordinary collection/placement/error messages take precedence over the hint. The guide does not interrupt battles or require finishing a step before using other features.

Completed steps are saved and never undone by clearing or spending an army. Existing buildings, collected gold, owned/committed armies, claimed campaign progress and completed upgrades are recognized. An unclaimed victory remains on the attack step; losses can be dismissed and retried, and interrupted attacks can be abandoned through Campaign. Starting or canceling an upgrade does not count as completing it. After completion, the guide remains available and its action opens Campaign.

Saves now use **version 8**. Version-7 villages migrate without losing troops, buildings or pending rewards; the old payload is backed up as `Kingdoms.Village.pre-v8`. Existing evidence initializes guide progress. Milestones and the hint preference are written with the village, using the same save-success checks as other actions. Earlier save-version descriptions above are historical. Replay rules remain 6.

Study [milestone evidence and guidance](Assets/Scripts/Core/VillageTutorial.cs), [guide UI and action routing](Assets/Scripts/UI/VillageTutorial.cs), [save/migration integration](Assets/Scripts/Core/VillageState.cs), and [home hints](Assets/Scripts/UI/VillageGameplay.cs). Validation evidence: [TutorialValidation.txt](TutorialValidation.txt) and [test source](Assets/Editor/TutorialValidation.cs). Screenshots: [home entry and hint](TutorialPreviews/village-guide-home.png), [starting guide](TutorialPreviews/village-guide-start.png), [attack step](TutorialPreviews/village-guide-attack.png), [completed guide](TutorialPreviews/village-guide-complete.png).

Regression checks passed for campaign rewards and pending-result reload, army preparation and legacy migrations, plus 66 resource/progression assertions with v8 saves.

Known limits: guidance is a checklist with screen shortcuts, not animated arrows or a scripted battle. Pausing hints does not skip objectives. Progress is local to this device. Phone layout/performance testing and an updated APK remain outstanding.


### Android development build workflow - 24 September 2026

The project now has a repeatable Android development APK workflow. Open PowerShell in the project folder and run `./Tools/Build-Android.ps1`. It copies the current source into a fresh ignored validation project and builds with Unity 6000.3.13f1, leaving the open editor separate. The initial package import, shader compilation and Gradle downloads can take substantial time. Android Build Support, SDK/NDK and OpenJDK must be installed with Unity.

Find the result at `Builds/Android/Kingdoms-development.apk` and check `build-report.txt` beside it for `Result: Succeeded`. Run `./Tools/Verify-AndroidApk.ps1` to check the package, signature, native libraries and ZIP alignment. The APK uses package ID **com.kingdoms.prototype**, debug signing and ARM64 IL2CPP. It starts with WelcomeScene and then Main Scene. It is a local development build; phone performance still needs measurement.

For a phone test, enable USB debugging, connect an ARM64 Android phone and approve the computer's debugging prompt. Install using `adb install -r` to preserve this package's data. A prior installation under Unity's template package ID has separate storage. Follow [the device walkthrough](ANDROID_BUILD.md#device-smoke-test): load the village, use the guide, prepare troops for free, attack, replay, claim a result, and restart to check persistence. Never clear app data as a substitute for checking returning saves.

Source links: [PowerShell build launcher](Tools/Build-Android.ps1), [Unity build entry point](Assets/Editor/AndroidDevelopmentBuild.cs), [APK verification](Tools/Verify-AndroidApk.ps1), and [build instructions](ANDROID_BUILD.md). Generated APKs, logs, signing keys and isolated project caches are ignored and must remain uncommitted. Earlier Android status notes describe the evidence available at those historical increments; this section and the build validation record track the current increment.

Validation: [Android build evidence](ANDROID_BUILD_VALIDATION.md) records the successful build, package checks and SHA-256. The APK is 75,881,750 bytes (72.37 MiB). Unity reported zero errors and 1,049 warnings, including installed inference-package shader variants and duplicate assembly versions. No connected device was detected, so installation, touch, resume, frame rate and memory are not yet verified.

### Persistent campaign battle history - 24 September 2026

Open **Buildings > Prepare Army > Campaign > Battle History** to review your latest **20 campaign results**, with the newest first. Use **Older** and **Newer** to browse two entries per page; **Campaign** returns to scouting and result resolution. An empty history explains how to create the first entry.

Each finished attack records its mission, outcome, stars, destruction percentage, simulated battle duration, completion date and full committed Raider/Archer/Tank composition, including undeployed troops. Finishing a verified battle saves the summary alongside its pending result. Returning home, reopening the screen and restarting do not create duplicate entries. A repeat victory, defeat, surrender or timeout earns no additional resources under the existing reward rules.

**Try it:** Prepare eight Raiders, scout Gate Outpost, start and deploy the army. After victory, return home and open Battle History. It shows a pending reward. Return to Campaign and press **Claim / Finish**, then reopen history: the same entry now shows the actual **500 gold + 300 elixir** payout. Restart the scene or game and inspect it again. If storage is full, both the reward and its history status remain pending until the complete reward fits.

At the initial history increment, an interrupted attack stayed in Campaign until abandonment; checkpoint recovery is added below. Abandonment creates an **Abandoned** entry and retains the committed troop counts; it grants no reward and does not invent destruction or duration. The date is the time of abandonment. Ordinary finished results use local-device completion time, while duration comes from the battle simulation. History order follows when entries were added, even if the device clock changes.

Saves now use **version 9**. Versions 1-8 migrate with their village, army, guide settings, unlocks and pending results preserved. Version-8 originals are backed up under `Kingdoms.Village.pre-v9`. A completed pending result from an older save becomes one history entry with its known outcome, stars and army; the missing date, duration and destruction are displayed as unavailable. Past attacks already dismissed or claimed before this update cannot be recovered. Earlier save-version notes are historical; replay rules remain version 6.

Study [history records, retention and validation](Assets/Scripts/Core/VillageBattleHistory.cs), [campaign finish/claim/abandon integration](Assets/Scripts/Core/VillageCampaign.cs), [history menu and paging](Assets/Scripts/UI/VillageBattleHistory.cs), and [save migration](Assets/Scripts/Core/VillageState.cs). Evidence: [BattleHistoryValidation.txt](BattleHistoryValidation.txt) and [validation source](Assets/Editor/BattleHistoryValidation.cs). Previews: [empty history](BattleHistoryPreviews/history-empty.png), [pending victory](BattleHistoryPreviews/history-pending.png), [claimed reward](BattleHistoryPreviews/history-claimed.png), and [older results at 16:9](BattleHistoryPreviews/history-older-16x9.png).

Validation passed in isolated Unity 6000.3.13f1: all five recorded outcomes (including abandonment), full mixed composition, rejection of malformed/duplicate entries, 20-result retention, versions 1-8 migration, backup preservation, storage-blocked and repeated claims, deep-copy isolation, live battle-to-history UI, claim update, scene reload, page boundaries and camera release. Existing campaign, mixed-army, Tank, tutorial and resource/progression rule checks also passed. Screenshots were inspected at 1600x702 and 1280x720.

Historical limits of the initial history increment: this was a local campaign summary list without persisted replay playback or interrupted battle resumption. Saved replay playback is added below. Practice battles are excluded. Only the newest 20 summaries remain, and history cannot claim rewards directly. Phone testing is deferred at the user's request. The existing Android APK predates this change and must be rebuilt to include battle history.

### Saved campaign replay playback - 24 September 2026

New completed campaign attacks now keep a replay alongside their Battle History summary. Open **Buildings > Prepare Army > Campaign > Battle History**, find an attack, and press **Watch Replay**. The battlefield plays the original accepted troop placements, troop types and timing automatically. **Watch Replay** on the finished battlefield starts it again; **Back to History** returns to the same history page, including when you leave playback early.

**Try it:** Complete a Gate Outpost attack, return home, claim the reward and restart the scene or game. Open Battle History and watch that attack. Its destruction and final result should match the saved summary. You can also watch an unclaimed result; watching never claims its reward. A separate active campaign attempt and any prepared troops remain unchanged during playback. Claim rewards through Campaign as before.

The saved replay includes the authored layout ID/revision, combat rules version, army composition, accepted deployment commands, ending tick/outcome and final state hash. Playback first reconstructs and verifies the recording, then starts a fresh visual replay. At most 80 deployments can be recorded, and recordings leave storage when their summaries leave the newest-20 history. The verifier rejects oversized data, invalid commands and mismatched outcomes. This is local verification, not server authority.

Village saves remain **version 9**: the recording is an optional additional field on a history entry, so existing v9 villages load without a migration or reset. Older history entries and abandoned attacks show **No Saved Replay**. Missing recordings cannot be reconstructed from summary statistics. Unsupported recording formats, combat rules or layout revisions show **Replay Expired**; corrupt recordings are unavailable or report a verification failure without discarding the village, summary or pending reward. Future changes to combat behavior must increment the combat rules version; layout edits must increment the layout revision. Historical descriptions of campaign replays being memory-only are superseded by this increment.

Source links: [recording serialization and verification](Assets/Scripts/Core/SavedBattleReplay.cs), [history recording attachment](Assets/Scripts/Core/VillageBattleHistory.cs), [history replay controls](Assets/Scripts/UI/VillageBattleHistory.cs), and [isolated visual playback](Assets/Scripts/UI/VillagePracticeBattle.cs). Validation: [SavedReplayValidation.txt](SavedReplayValidation.txt) and [test source](Assets/Editor/SavedReplayValidation.cs). Previews: [history replay controls](SavedReplayPreviews/saved-replay-history.png), [16:9 history](SavedReplayPreviews/saved-replay-history-16x9.png), [playback](SavedReplayPreviews/saved-replay-playing.png), and [verified result](SavedReplayPreviews/saved-replay-complete.png).

Validation covers per-tick reconstruction, four layouts, mixed troops and exact ground coordinates, victory/defeat/surrender/timeout, zero-tick surrender, maximum command count, incompatible and corrupt recordings, unchanged village loading and economy, UI playback after reload, replaying again, returning to the same history page, and preserving a separate active campaign. The existing battle-history, campaign, mixed-army, Tank, tutorial and resource/progression rule suites also passed. The new suite passed in isolated Unity 6000.3.13f1, and screenshots were inspected at 1600x702 and 1280x720.

Known limits: saved playback applies to campaign attacks completed after this update; practice replays remain in memory. Playback runs at normal speed with no seeking or export. Incompatible recordings expire, while summaries remain. At this replay-only increment, interrupted combat could not yet resume; recovery is added below. Phone testing remains deferred, and the existing APK needs a rebuild to include this update.

### Resumable campaign attacks - 24 September 2026

Open **Buildings > Prepare Army > Campaign > Resume Attack** to continue a checkpointed campaign battle after returning home or restarting. The restored battle retains the committed army, deployed troop positions and health, building/wall damage, attack cooldowns, remaining troops and elapsed simulation time. It reconstructs the recorded commands and verifies the resulting state before opening. Resuming does not charge another army, create a history entry or claim a reward. The battle does not advance while it is closed.

**Beginner walkthrough:** Prepare eight Raiders, scout Gate Outpost and press Start Attack. Deploy two Raiders, wait a few seconds, then press **Save & Return**. Open Campaign and press **Resume Attack**. The timer and two deployed troops continue from the checkpoint, and the remaining six Raiders can still deploy. Try restarting the scene/game between Save & Return and Resume. To end the attempt instead, use **Surrender** during combat or **Abandon Attack** in Campaign. Finishing normally creates one result and full replay; reward claims keep the existing once-only rules.

Checkpoints are saved when the army is committed, after accepted lane or ground deployments, every **two seconds of simulated combat**, on application pause/focus loss/quit or scene teardown when callbacks run, and before Save & Return closes a battle. Periodic saves are not frame-by-frame crash recovery: an abrupt process kill may roll back to the last successful checkpoint, normally up to two simulation seconds when writes succeed. Selected troop type is chosen from available reserves on resume; transient visual effects are rebuilt.

If a checkpoint cannot be saved, combat and deployment stop and **Retry Save** appears. The prior successful save remains intact. Retrying saves the live state and permits combat to continue; Save & Return only closes after saving successfully. The automatic test exercises a rejected save through invalid candidate data; actual disk-full and platform storage failures remain device-test work.

Village saves remain **version 9**, with an optional `campaignCheckpoint` field. Each checkpoint is bound to its campaign run ID, mission, army composition, recording format, combat rules and layout revision. Missing, corrupt or incompatible checkpoints refuse resume without discarding the village or pending attempt; the menu explains that abandonment remains available. Older attacks without a recorded checkpoint cannot be recovered. Completion, claim or abandonment clears the checkpoint. The Village Guide now directs interrupted players to Resume Attack.

Source links: [capture and campaign lifecycle](Assets/Scripts/Core/VillageCampaign.cs), [command reconstruction and verification](Assets/Scripts/Core/SavedBattleReplay.cs), [resume and save-retry UI](Assets/Scripts/UI/VillageBattleRecovery.cs), [live battle integration](Assets/Scripts/UI/VillagePracticeBattle.cs), [ground deployment saving](Assets/Scripts/UI/VillageGroundDeployment.cs), and [application lifecycle saves](Assets/Scripts/UI/VillageGameplay.cs). Evidence: [BattleRecoveryValidation.txt](BattleRecoveryValidation.txt) and [test source](Assets/Editor/BattleRecoveryValidation.cs). Previews: [Resume Attack menu](BattleRecoveryPreviews/recovery-campaign.png), [16:9 menu](BattleRecoveryPreviews/recovery-campaign-16x9.png), [restored battle](BattleRecoveryPreviews/recovery-resumed.png), and [save-retry feedback](BattleRecoveryPreviews/recovery-save-retry.png).

Validation passed in isolated Unity 6000.3.13f1: all four authored layouts, mixed troops, checkpoint and continued per-tick state equality, resumed deployment and full replay, wrong-run/army rejection, corrupt/incompatible/legacy checkpoint handling, once-only victory payout, immediate and periodic saves, simulated pause plus scene reload, Save & Return, save-rejection pause/retry, explicit surrender cleanup and separate practice behavior. Saved replay, history, campaign, mixed-army, Tank, tutorial and resource/progression rule regressions also passed. Screenshots were inspected at 1600x702 and 1280x720.

Known limits: recovery is local and campaign-only. Practice battles still reset when closed. Checkpoints require compatible combat rules and authored layouts; there is no cross-version recovery engine or online verification. Physical-phone lifecycle, abrupt termination and storage faults remain unverified. The Android APK has not been rebuilt for this increment; phone testing remains deferred.
