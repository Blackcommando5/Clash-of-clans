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

This is an offline combat slice, not the completed battle system. Navigation currently supports the fixed practice encounter, with an open entrance and a selectable sealed variant; it is not an arbitrary-village pathfinding system. Remaining work includes unit separation, arbitrary village snapshots, defense levels in battle, troops/army training, manual ground deployment, animations/projectiles, campaign opponents, rewards, persisted/exportable replay records, authoritative online battles, and physical-phone performance testing. No APK was rebuilt.

Implementation: `Assets/Scripts/Core/PracticeBattle.cs` contains simulation rules without Unity dependencies. `Assets/Scripts/UI/VillagePracticeBattle.cs` owns the temporary models, controls and presentation. The existing Attack action routes to this mode.


## Watch Replay

Completed encounters now offer Watch Replay. Accepted deployments record their tick and lane; a read-only recording copies these commands, enclosure choice, end tick, outcome and final state hash. Rules version 1 applies to this fixed practice layout. `PracticeReplay` reconstructs the battle, applies tick-zero and delayed commands in original order, and reproduces timed surrender when present. Playback disables manual deployment and compares its final numeric state hash with the original. Retry starts a fresh live encounter; Return Home closes either mode.

The recording lasts only for the current encounter in memory. There is no disk format, cross-version migration, export, replay gallery or speed control. Hashes detect differences but are not security proofs or server authority. Tests compare every tick of delayed-command runs in both enclosure layouts, plus surrender, timeout, mismatch detection and the rendered replay/retry flow. Evidence includes `PracticePreviews/practice-replay.png`.


## Selectable challenges

Attack defaults to Open Gate. The upper-left Try Wall Breach / Try Open Gate button switches the fixed enclosure while scouting or after a result. Once Start Attack is pressed, switching is locked until the result; active replay also locks it. Wall Breach seals the entrance and requires a wall to be destroyed to reach the Town Hall. Army, defense stats and victory rules remain the same. Retry retains the selection and replay records it. Returning home discards it. Both challenges are free practice without progression rewards.

Play-mode validation now completes both challenges, checks wall destruction in the sealed layout, verifies both replays and retry behavior, and confirms the home state and saved JSON remain intact.


## Scoring and surrender

Results show 0?3 stars, destroyed/total non-wall buildings, destruction percentage, deployed count, surviving deployed count and elapsed simulation time. One independent star is awarded for the Town Hall, at least 50% non-wall destruction, and 100% destruction. For this three-building layout the halfway objective requires two destroyed buildings. Walls never contribute; undeployed reserves are not counted as survivors. Scores are informational and award no economy rewards. Only destroying all three buildings produces Victory.

Surrender stops a live attack immediately and displays its current results while keeping the battlefield open for replay, retry or return. Its tick is preserved in the recording, so replay ends at the same moment with the same score. Replay ignores manual surrender. Return Home continues to abandon and close an encounter directly. Validation checks every building-destruction combination, wall exclusion, survivor counts and the real surrender/results/replay/retry UI.


## Untimed scouting

Opening Attack, Retry and challenge changes now enter an untimed scouting phase. A summary displays damage and range from the defense entities, plus the selected enclosure's approach. Simulation ticks do not advance; deployment and surrender are guarded in both UI state and action handlers. Start Attack clears the frame accumulator and starts at combat tick zero, reveals deployment, and locks challenge changes even with no troops deployed. The normal three-minute timeout then applies.

Replay bypasses scouting and retains its original combat timing. Returning home from scouting restores village controls. Scouting does not change the home save or recording rules version. The fixed camera remains blocked; this update does not add scouting pan, zoom, selectable defenses or range overlays. Validation checks a real-time scouting wait and all start/retry/replay transitions.


## Campaign integration - 24 September 2026

The simulation now accepts an explicit 1-80 Raider budget, and replay rules version 2 records that budget. Practice retains its free eight Raiders and both existing challenge layouts. The separate campaign flow reuses those layouts with an owned roster, saved results and first-clear rewards; see [the current guide](BEGINNER_GUIDE.md#first-offline-campaign-loop---24-september-2026). Historical rules-version-1 references above describe earlier in-memory recordings, which were never persisted.


## Mixed troops - 24 September 2026

Replay rules version 3 adds Raider/Archer budgets and typed commands. Campaign battles use the saved composition, constrained to 80 housing spaces (one per Raider, two per Archer). Archer range is 3.5 cells with wall-blocked line of sight, 55 HP and 14 damage per second. Practice retains eight Raiders. Earlier rules-version-1/2 notes describe superseded in-memory formats. See [mixed validation](MixedArmyValidation.txt) and [the current guide](BEGINNER_GUIDE.md#mixed-armies-and-archers---24-september-2026).
