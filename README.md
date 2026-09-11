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
