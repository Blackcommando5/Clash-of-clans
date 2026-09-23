# First playable practice battle

23 September 2026. Tap **Attack!** to enter a practice encounter containing a Town Hall, Cannon and Archer Tower. Deploy up to eight raiders with the left, center and right buttons. Raiders approach and attack buildings; surviving defenses automatically target raiders in range. Health, firing traces, destruction percentage and a three-minute timer update during the fight. Victory, defeat and timeout end the encounter. Retry resets the practice army and buildings; Return Home abandons an active encounter and restores the village camera and controls.

Practice has no purchase costs, loot, trophies, progression rewards or saved army. It owns separate combat entities and does not receive or modify the home VillageState. Normal home saving/production behavior resumes after returning. Application pause freezes practice; closing the app discards the encounter. It uses a temporary battlefield root in the village scene, hiding the home models while active.

## Rules

- 100 ms simulation ticks, integer positions at 100 units per cell, stable entity IDs and target ordering.
- Defenses use the level-one HP, damage per second and range from BuildingCatalog, attacking once per second. Defenses act before raiders in each tick. Equally close targets resolve in stable entity order.
- Eight melee raiders, each with 90 HP, 20 damage per attack, one-second attack intervals and 2.8 cells/second movement.
- Raiders use deterministic breadth-first routing on a bounded half-cell grid. Live building and wall footprints block movement with clearance; cardinal movement prevents cutting corners. They prefer reachable non-wall buildings, using the Town Hall enclosure's entrance. If every remaining building is sealed off, they route to a reachable wall and break it. A destroyed structure invalidates routes so raiders can use the new opening. Troops can still overlap.
- Practice walls have 80 HP. Undamaged wall health bars stay hidden. Walls connect to adjacent segments and disappear when destroyed; surviving walls do not count against victory or destruction percentage. Defenses can fire over walls.
- Town Hall has 600 practice HP. Destroy all three buildings to win. Losing every deployed raider after exhausting the army loses the encounter. The timer ends it after 1,800 ticks.
- Terminal outcomes reject deployment and further combat mutation. Identical deployment schedules reproduce identical simulation states in the tested runtime.
- Rendering interpolates movement separately and uses short-lived firing traces. Damage applies on attack ticks; traces do not determine hits.

## Validation and limits

`PracticeBattleValidation.Run` is restricted to the isolated Unity test project. It checks entrance preference, a sealed-enclosure breach and replanning, live-footprint collision avoidance, wall-independent victory scoring, repeatable tick states, deployment bounds/caps, range, damage, cooldowns, both sides attacking, all terminal outcomes, real-time deployment and victory, results/retry/return UI, and exact preservation of the home state and saved JSON during the encounter. Evidence is in `PracticeBattleValidation.txt` and `PracticePreviews`.

This is an offline combat slice, not the completed battle system. Navigation currently supports the fixed practice encounter, with a sealed variant exercised by validation; it is not an arbitrary-village pathfinding system. Remaining work includes unit separation, arbitrary village snapshots, defense levels in battle, troops/army training, manual ground deployment, animations/projectiles, campaign opponents, rewards, replay records, authoritative online battles, and physical-phone performance testing. No APK was rebuilt.

Implementation: `Assets/Scripts/Core/PracticeBattle.cs` contains simulation rules without Unity dependencies. `Assets/Scripts/UI/VillagePracticeBattle.cs` owns the temporary models, controls and presentation. The existing Attack action routes to this mode.
