# Clash-of-clans

Kingdoms: A mobile village strategy game inspired by Clash of Clans, developed with Unity 6 and URP.

## Architecture
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
