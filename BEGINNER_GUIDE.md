# Kingdoms: a beginner's guide to rebuilding and understanding the game

Updated: 10 September 2026.

> Update, 15 September 2026: this guide describes the original prototype. The new [resource milestone](RESOURCE_MILESTONE.md) adds collectors, storage buildings, a four-building shop, both-resource collection, and version 2 village saves. Use that document for the current economy and save behavior.

Project: `E:\Project\Games\Kingdoms`

This guide explains the work completed so far and how you can reproduce it manually. You do not need previous Unity experience. Work through one lesson at a time, test it, and then continue.

The project is a Clash of Clans-inspired village prototype with its own Kingdoms title and starter assets. It is not the complete Clash of Clans game. Our current playable loop is: enter the village, choose a name, build a Gold Mine, collect gold, and return to your saved village.

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
| HUD | Gold, elixir and gem counters, guide message, Shop and Collect buttons |
| Shop | Buy and place up to three Gold Mines |
| Placement | Grid snapping, valid/invalid preview, overlap and boundary checks, cancellation |
| Economy | Gold production and collection with storage limits |
| Persistence | Name, buildings, resources and production progress saved locally |
| Mobile setup | Welcome scene first, village second, landscape orientation, Android IL2CPP/ARM64 settings |

Not implemented yet: Elixir Collectors, building upgrades, construction timers, troops, combat, matchmaking, multiplayer, online accounts, purchases, or a server economy. Showing a gem counter does not mean a gem shop exists.

The Android configuration exists, but an Android build and physical-phone testing have not been completed as part of this work.

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

**Practice:** Find `MineCost` in `VillageState.cs`. Before editing it, find every place the shop displays the cost. Some descriptions are written as text; changing the number alone will not update those descriptions automatically.

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

Open `VillageState.cs`. A building's data is deliberately simpler than its 3D model:

```csharp
public string kind;
public int x, z, storedGold;
```

`kind` identifies TownHall or GoldMine. `x` and `z` identify the minimum corner of its rectangular grid footprint. `storedGold` records gold waiting inside a mine. The record does not store a GameObject; the game recreates the visual instance from the prefab when loading.

### Starting values

| Rule | Current value |
| --- | --- |
| Gold | 1,000 |
| Elixir | 500 |
| Gems | 50 |
| Town Hall corner | `(-2, -2)` |
| Town Hall size | 4 x 4 |
| Mine price | 150 elixir |
| Mine size | 3 x 3 |
| Mine count limit | 3 |
| Mine production | 1 gold per second: 60 per minute |
| Each mine's storage | 500 gold |
| Village gold capacity | 10,000 |

These are our prototype's balance values. They are not a claim about Clash of Clans' current economy.

### Grid position versus model position

The Town Hall starts at corner `(-2, -2)`, but its model center is `(0, 0)` on X/Z:

```text
center X = corner X + size / 2
center Z = corner Z + size / 2
```

A 3 x 3 mine at corner `(5, -1)` is drawn at world `(6.5, 0, 0.5)`. Keeping the prefab pivot centered is what makes this conversion work.

### Placement rules

`CanPlaceMine` checks the mine limit, available elixir, the village boundary, and other footprints. The mine's corner must be at least -22 and no more than 19 on each ground axis, because a size-3 building must end at or before +22.

Two footprints overlap when they overlap on **both** X and Z. The code uses strict comparisons, so adjacent buildings may touch along an edge without overlapping.

```text
Preview: move a temporary model, but spend nothing
Confirm: copy state -> validate -> deduct cost -> add record -> save
Save succeeds: adopt new state and spawn the permanent model
Cancel: remove preview, leaving resources and saved buildings unchanged
```

Using a candidate copy means an unsuccessful purchase does not immediately mutate the live village. Calling Confirm again after success does nothing because the preview has already been removed.

### Production and collection

`Accrue(now)` compares the current timestamp with `lastProduction`. Each existing mine receives the elapsed seconds as gold, up to its capacity. Before adding a new mine, the old village is accrued first, so the new mine does not receive gold for time before it existed.

Example: a mine containing 480 gold receives 60 seconds of production. It ends at 500, not 540. If the village has 9,990 gold and the mine contains 100, collection transfers only 10. The remaining 90 stays in the mine.

The timestamp uses the device's UTC clock. This supports offline progress for a local prototype. A clock moved backward does not award negative gold; production waits until the stored timestamp is passed. An online competitive economy would need a server-authoritative design.

### Saving

`VillageSave` converts the state to JSON with `JsonUtility` and stores it in PlayerPrefs under `Kingdoms.Village.v1`. Purchases and collections save immediately. Background progress saves every 30 seconds and on pause/quit.

On load, `IsValid()` checks the version, resources, building kinds, positions, counts and overlaps. Invalid saved data is preserved and an error is displayed. The code does not silently erase the player's village to hide the problem.

**Practice on paper:** Starting from 500 elixir, calculate the balance after one, two and three mines. Answers: 350, 200 and 50. Calculate the stored gold after 90 seconds for one empty mine: 90.

## 12. Connect the village, HUD and shop

### Manual scene setup

1. Open Main Scene and create an empty root named `Village Gameplay`.
2. Leave its position and rotation at zero and scale at one.
3. Add `VillageGameplay`.
4. Assign every field below by dragging assets or scene objects into the Inspector.

| Field | Assign |
| --- | --- |
| Town Hall Prefab | TownHall prefab asset |
| Gold Mine Prefab | GoldMine prefab asset |
| Pine Prefab | PineTree prefab asset |
| Footprint Material | Placement material |
| Title Font | Bangers.ttf |
| Body Font | Roboto-Bold.ttf |
| View Camera | Main Camera's Camera component |
| Camera Controller | Main Camera's VillageCameraController component |

Do not attach VillageGameplay to Main Camera. Buildings and trees are created beneath its transform; using a stationary world root keeps them from following camera motion.

Your manually saved scene should have the ground, Directional Light, Main Camera with camera/onboarding components, and the Village Gameplay root. Do not also place a permanent Town Hall manually: VillageGameplay creates the saved state's Town Hall during Start.

### Where are the HUD objects?

`BuildUI()` creates them when you press Play. Expand Village Gameplay during Play Mode to inspect the Village HUD, safe-area container, counters, guide, buttons, placement controls and Building Shop.

The HUD canvas sorts at 100; the player identity canvas sorts at 300. The higher sorting order lets name entry appear above village controls. A safe-area container adjusts anchors around usable screen space.

The resources are anchored top-right. The Chief nameplate is top-left. Shop is bottom-right and Collect bottom-left. Placement controls appear near the bottom center. The shop uses a dimmer behind a centered panel.

`Button.onClick.AddListener(...)` connects the generated buttons directly in code:

| Control | Method |
| --- | --- |
| Shop | `OpenShop` |
| Back | `CloseShop` |
| Buy Gold Mine | `BeginMinePlacement` |
| Build | `ConfirmPlacement` |
| Cancel | `CancelPlacement` |
| Collect | `Collect` |

You do not need to add the same button handlers again through Inspector events for the existing generated UI.

### Follow a click through the code

1. Shop opens its panel and blocks camera gestures.
2. Buy creates a temporary mine and colored ground footprint.
3. Pointer movement is projected onto the ground and converted to integer grid coordinates.
4. `SetPreviewCell` moves the model, asks VillageState whether the cell is valid, updates color/text, and enables or disables Build.
5. Confirm performs the saved transaction and creates the permanent mine.
6. Cancel removes temporary objects and unlocks the camera.
7. `RefreshHUD` updates resource labels and guidance.

The camera remains fixed during placement, so dragging positions the mine. Outside placement, dragging moves the camera. Touch gestures and UI hits are checked so pressing a button does not also move the preview beneath it.

`DefaultExecutionOrder(50)` on VillageGameplay makes its Start setup run before onboarding's order 100. This lets the village provide the EventSystem that both systems share.

**Check:** Build a mine, cancel a second preview, collect gold, stop, and play again. The confirmed mine should return; the cancelled one should not.

**Practice:** Change the shop panel's title or a color in `BuildUI()`, then restart Play Mode. This teaches why editing a runtime object in the Inspector does not permanently change the generated UI.

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
| Build three mines | Fourth mine unavailable |
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
| Can't buy a fourth mine | Current prototype limit is three |
| Can't buy another mine | Budget may be below 150 elixir; elixir production is not implemented |
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

The next planned feature is an Elixir Collector, then building selection/upgrades, followed by a guided first battle. Before extending the economy, consider turning building-specific costs, sizes and production rules into reusable definitions; the current code intentionally supports only the starter Town Hall and Gold Mine.

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
