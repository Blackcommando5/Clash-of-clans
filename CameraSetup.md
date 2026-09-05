# Kingdoms ground and camera

Open Assets/Scenes/Main Scene.unity and enter Play Mode.

- PC: hold left or middle mouse button and drag; scroll to zoom toward the pointer.
- Mobile: one finger drags; two fingers pan and pinch to zoom toward their midpoint.
- Rotation stays fixed. A short release glide decelerates automatically.
- Gestures starting on uGUI are ignored until release. Finger-count changes rebase without jumping.

The ground is 120 x 120 Unity units, with a centered 44 x 44 visual village grid.
The grid is a visual guide, not a building placement system. The plane includes a collider.
Main Camera uses orthographic projection, 45-degree elevation and 45-degree azimuth.
The controller exposes angles, zoom range, ground bounds, glide and sensitivity in the Inspector.
Camera bounds include all four viewport corners and respond to aspect ratio and zoom changes.
The shader exposes grid visibility and grass colors, and receives directional-light shadows.

Only Main Scene is modified; other scenes and build settings are preserved.
Add Main Scene to Build Profiles when you want it included in a player build.

Touch polling uses Unity's EnhancedTouch API:
https://docs.unity.cn/Packages/com.unity.inputsystem@1.10/manual/Touch.html

Validation: compile the controller against this project's installed assemblies;
check serialized scene references and viewport geometry. Play Mode and physical-device
gesture checks require the Unity Editor and a connected device.
