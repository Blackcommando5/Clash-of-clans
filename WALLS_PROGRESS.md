# Walls milestone

- Editable Stone Wall prefab with pillar, coping, stone courses and four connection pieces.
- Adjacent one-cell segments connect in edit mode and Play mode. Moving a segment updates both ends.
- Resource shop has a Wall button: 25 gold per segment. These are provisional Kingdoms values.
- Wall limit is 25 per Town Hall level (25 / 50 / 75).
- Placement rejects overlap, out-of-bounds cells, insufficient gold and the wall limit.
- Existing walls can be selected, inspected and moved without another purchase.
- Walls persist in the existing local village save.
- A starter enclosure with a two-cell opening is visible in the scene. Only new villages adopt this starter layout; existing saved villages are unchanged.

## Edit in Unity

Open Main Scene and select Village Gameplay. Under Starter wall layout, choose Grid X, Grid Z, Length and Along Z, then Add wall line. Valid lines become real prefab instances under Starter Village Preview / Starter Wall Layout. Duplicate cells are skipped and invalid cells are rejected. Remove segments through the Hierarchy with Unity Undo support. Save the scene.

Edit Assets/Prefabs/Walls/StoneWall.prefab to change the shared wall model and its materials. Keep the WallSegment component and its four connection references. Move scene segments on the one-unit grid, with centres at half coordinates (for example x=3.5, z=2.5).

The starter design uses Town Hall level 1 limits. Existing saved villages retain their purchased walls and stored positions; use the in-game shop to extend those villages.

## Remaining work

Wall upgrades, hit points, troop pathfinding, attack damage and destruction are not implemented. This milestone provides base layout, connected visuals and persistence, not functional combat defenses.
