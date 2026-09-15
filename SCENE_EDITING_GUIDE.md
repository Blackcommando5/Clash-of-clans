# Editing Kingdoms in the Unity scene

Open **Assets/Scenes/Main Scene.unity** without entering Play mode.

Select the object containing **Village Gameplay**. Its Inspector now includes **Edit scene screens**:
- Show Village HUD
- Show Shop
- Show Building Details
- Show Placement
- Show My Profile, My Clan, Clans, Social, My Buildings, Attack, Settings
- Show Builder Base / Clan Capital
- Show Name Dialog

These controls activate real saved scene objects. Expand **Village HUD > Safe Area** in the Hierarchy and select any panel, button, label or icon. Adjust RectTransform position/anchors/size, text/font, panel gradient colours, border radius and images in the Inspector. Save the scene with Ctrl+S. Welcome Scene already uses editable scene UI.

## What persists into Play mode

The game binds saved button references instead of recreating the interface. Authored positions, sizes, colours and static captions survive. Buttons can be renamed in the Hierarchy without losing their actions. Keep their referenced Button components; replacing an entire button requires updating its Scene Actions reference on Village Gameplay.

Resource balances, builder counts, player names, building counts and upgrade information are live data and update during play. Resource fill widths and safe-area anchors are also calculated. Disable **Fit Windows To Screen** on Village Gameplay if you want to author fixed window scales.

Opening a profile tab activates its saved Page object; it does not delete and reconstruct the page. Runtime starts with menus closed, regardless of which screen you last previewed in the editor.

## Village models

**Starter Village Preview > Town Hall** is a linked prefab instance visible in the scene. Its visual edits are used when the runtime Town Hall is instantiated. Moving it changes the starting location for a NEW local village; existing saves keep their stored building positions.

**Starter Village Preview > Building Model Templates** contains linked instances for the mine, collector and storages. Enable that container in the editor to inspect them. Their mesh, material and child-transform edits are used for runtime buildings. This container is hidden by default to keep templates out of the village view.

**Village Woodland** contains saved, editable tree objects. The runtime keeps this forest instead of regenerating it.

**Collection Badge Template** is a saved inactive UI template cloned for live producers. Edit its appearance in the scene. Placement previews, saved player buildings, collection values and upgrade scaffolding still respond to gameplay; they are not static copies of a saved player session.

Edit original building prefab assets to share changes with other scenes. Save or apply prefab overrides according to whether you want scene-specific or shared art changes.

## Authoring tools

**Kingdoms > Create Editable Scene Interface** creates missing scene UI once. It does not rebuild an existing authored interface or overwrite edits. The editor screen controls do not read or write the player's save.

The pre-conversion scene is backed up at .utmp/MainScene.before-editable.unity. The Android APK has not been rebuilt.

## Walls

The Inspector now includes a Starter wall layout tool. See [Walls milestone](WALLS_PROGRESS.md) for wall lines, costs and save behavior.
