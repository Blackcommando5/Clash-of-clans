# Kingdoms — Complete Game Development Phases

Prepared: 15 September 2026. Status reviewed: 23 September 2026.

Purpose: turn the existing Unity village prototype into a complete mobile strategy game with persistent online villages, attacks, clans, wars, and long-term progression.

Companion document: [Clash of Clans system and phase reference](CLASH_OF_CLANS_REFERENCE_PHASES.md).

Earlier implementation: [resource milestone](RESOURCE_MILESTONE.md) added elixir production, both storage buildings, four-card shop, individual/collect-all actions, and v1 save migration. Multiplayer has not started.

Newer update: [Home Village progression](HOME_VILLAGE_PROGRESS.md) adds selection/moving, two builders, upgrade jobs, three initial levels, Town Hall-dependent resource limits, and confirmed upgrade cancellation with a storage-capped 50% refund. [Walls](WALLS_PROGRESS.md) support individual placement and movement. Phase 3 and Phase 4 remain partial: continuous wall drawing, multi-selection, saved layouts, and full construction jobs are not complete. Reference-game balancing and physical-phone validation remain unverified.

This is a development plan. Unchecked work is not implemented. Phase numbers describe dependency order, not weeks or promised delivery dates. Retain Kingdoms branding and create its production assets.

## Current phase status ? 23 September 2026

We have a playable local village and a tested practice-combat slice. We do not yet have the complete train ? attack ? earn ? upgrade loop, online multiplayer, or a phone-validated release. ?Complete locally? below means the scoped implementation and recorded Editor checks are present, not production readiness. A phase stays partial when any of its acceptance requirements remain missing. No overall completion percentage is assigned because phases differ greatly in size.

| Phase | Status | Implemented | Still needed |
| --- | --- | --- | --- |
| 0. Reliable baseline | Partial | Git history, pinned Unity/packages, scenes, Editor validation evidence | Rebuild latest APK; fresh-install/returning-save phone tests; performance measurements; final identity/asset audit |
| 1. Architecture and data | Partial | Shared building catalog, separate state/progression rules, partial UI files, v3 saves and migrations | Stable village-instance IDs, authored level/content data, clock/repository/command interfaces, revisions and command IDs |
| 2. Resources and storage | Complete locally | Gold/elixir production and collection, storage capacities, Town Hall limits, offline behavior and save validation | Device regression testing and later server authority; these remain cross-phase release requirements |
| 3. Village layout editing | Partial | Select, inspect, move/cancel, occupancy checks, individual walls | Continuous wall drawing, multi-selection, removable obstacles, saved layouts and interrupted-layout recovery |
| 4. Builders and upgrades | Partial | Two builders, queue, timed upgrades, prerequisites, offline completion, capped cancellation refunds, initial level rules | Timed new construction, broader unlocks and level art, complete defense behavior during upgrades, optional notifications |
| 5. Battle simulation | Partial | Cannon/tower fire, raiders, HP/damage, grid routing and wall breach, fixed ticks, timed commands and matching local replay hashes | Immutable arbitrary village snapshots, general-layout navigation, traps/target categories, persisted/versioned replays, server verification |
| 6. Army preparation | Early partial | Eight practice raiders, lane deployment, army limit enforcement | Barracks/camps, owned army and capacity, readiness rules, troop selection, ground deployment zones, ranged/tank troops, research |
| 7. Complete attack loop | Partial | Two practice enclosure variants, untimed scouting, explicit start, timer, surrender, stars/results, retry/return and replay | Authored campaign villages, reward settlement exactly once, saved campaign progress, guided/resumable tutorial |
| 8. Accounts and backend | Not started | No implemented online service | Authentication/recovery, server economy, database, transactions, backups and monitoring |
| 9. Asynchronous PvP | Not started | Local practice is not PvP | Opponent snapshots, battle tickets, server replay validation, atomic loot settlement, attack/defense history |
| 10. Ranked progression | Not started | ? | Matchmaking, trophies, leagues, leaderboards, seasons and fairness checks |
| 11. Clans and social | Not started | ? | Clan membership/roles, chat/moderation, donations and friendly challenges |
| 12. Clan Wars | Not started | ? | Rosters, war scheduling, frozen layouts, attack allowances and rewards |
| 13. Clan leagues/events | Not started | ? | League rounds, standings, cooperative tasks and reliable reward claims |
| 14. Advanced armies | Not started | ? | Heroes, spells, equipment, pets, siege units and reproducible abilities |
| 15. Art/audio/usability | Partial | Welcome art, village/building models, reference-inspired HUD/shop, touch controls and safe-area handling | Production art/level variants, unit animations, projectiles/effects, audio/settings, accessibility/localization and phone profiling |
| 16. Live operations/store | Not started | ? | Events, billing verification, inbox, admin/support tools, account deletion/export and economy monitoring |
| 17. Release qualification | Not started | Editor checks only; no release qualification | Online end-to-end/security/load tests, recovery rehearsals, closed beta, signed releases and rollout |
| 18. Second village | Not started | ? | Separate economy, layouts, troops, mode rules and validated battles |
| 19. Shared clan territory | Not started | ? | Clan districts, contributions, raid reservations, persistent damage and rewards |
| 20. Ongoing content | Future/ongoing phase | ? | Versioned content pipeline, balancing evidence, replay compatibility and release maintenance |

### Evidence and limits

Current evidence: [resource milestone](RESOURCE_MILESTONE.md), [home progression](HOME_VILLAGE_PROGRESS.md), [defense validation](DefenseVillageValidation.txt), [builder queue validation](BuilderQueueValidation.txt), [upgrade cancellation validation](UpgradeCancellationValidation.txt), and [practice validation](PracticeBattleValidation.txt). Practice tests cover both challenges, scouting/start, scoring, surrender, replay determinism and preservation of the home save. Existing screenshots show actual Editor renders. The latest features have not been rebuilt into an APK or tested on a physical phone. Reference-style menus and placeholder options are not evidence that their corresponding systems are implemented.

### Milestone status

- **A ? Sustainable village:** playable locally; the full 0?4 acceptance gate remains partial.
- **B ? Offline battle slice:** practice combat works; an owned army, campaign opponents and rewards are missing.
- **C?F ? Online, social, release and expansion:** not reached.

## 1. Original starting point (before the resource milestone)

Verified by source inspection, project settings, and saved artifacts; no new runtime or phone test was performed for this plan.

| System | Current state |
| --- | --- |
| Engine | Unity 6000.3.13f1, URP 17.3.0, Input System 1.19.0 |
| Scenes | WelcomeScene then Main Scene in the enabled build list |
| Village | 120 × 120 ground, centered 44 × 44 placement grid |
| Controls | Touch/mouse pan, pinch/wheel zoom, camera bounds, UI blocking |
| Onboarding | Validated Chief name and local persistence |
| Buildings | Town Hall, purchasable Gold Mine, decorative pines |
| Economy | 1,000 starting gold, 500 elixir, 50 gems; mine costs 150 elixir |
| Production | 60 gold/minute/mine, 500 per-mine capacity, three-mine limit |
| Persistence | PlayerPrefs JSON; production uses device time |
| Android | ARM64/IL2CPP configuration and approximately 50 MB APK present |
| Validation | Earlier checks recorded in VillageValidation.txt; repeat on the evolving implementation |
| Missing | Elixir production, upgrades, troops, combat, accounts, server, multiplayer, sound |

The original economy stopped progressing after three mines. The resource milestone adds an Elixir Collector bought with gold and storage buildings, addressing this initial dead end.

## 2. Completion milestones

| Milestone | Phases | Demonstrable result |
| --- | --- | --- |
| A: Sustainable village | 0–4 | Build, produce, spend, upgrade, save, return |
| B: Offline battle slice | 5–7 | Train an army, attack an AI village, earn and spend rewards |
| C: Online multiplayer alpha | 8–10 | Two real accounts can attack persisted villages and receive authoritative results |
| D: Social strategy beta | 11–14 | Clans, donations, wars, leagues, advanced army progression |
| E: Home Village release | 15–17 | Polished, monitored, recoverable production service |
| F: Extended game | 18–20 | Second village, shared clan territory, and an ongoing content pipeline |

Phase 8 architecture decisions begin during Phase 1. Build a local implementation behind the same interfaces so online support does not require rewriting every screen.

## 3. Phase-by-phase work

### Phase 0 — Establish a reliable baseline

Dependencies: existing project.

- [x] Preserve the current playable version in version control and record its Unity/package versions.
- [ ] Run welcome → naming → placement → collection → restart on Editor and an Android phone.
- [ ] Record device model, OS, FPS, memory, launch time, and observed defects.
- [ ] Inventory assets and distinguish production files, examples, caches, and backups.
- [ ] Correct application identity and agree the Kingdoms package identifier before account integration.
- [ ] Update stale build/scene documentation without deleting historical validation evidence.

Done when: the baseline can be reproduced from source and a fresh install completes the existing loop without blocking errors.

### Phase 1 — Separate data, rules, presentation, and services

Dependencies: 0.

- [ ] Introduce stable IDs for buildings, units, levels, resources, and content versions.
- [ ] Define building/level/resource data in ScriptableObjects or authored tables, exportable to the server.
- [x] Split village presentation and interaction code into partial source files.
- [ ] Finish separating those responsibilities into independent services; partial files alone do not complete this architecture.
- [ ] Separate economy/placement rules from MonoBehaviours and scene objects.
- [ ] Introduce IGameClock, IVillageRepository, and a command service with local implementations.
- [x] Add save versions and migrations from Kingdoms.Village.v1; preserve failed migrations for recovery.
- [ ] Define command IDs, expected state revision, success responses, and actionable errors.

Done when: a new building type can be added through definitions and a small behavior module; old prototype progress migrates correctly.

### Phase 2 — Complete resource production and storage

Dependencies: 1.

- [x] Add Elixir Collector, Gold Storage, and Elixir Storage.
- [x] Purchase collectors with gold and mines with elixir, using shared definition costs.
- [x] Calculate total village capacities from storage buildings and the current Town Hall base capacity.
- [x] Support individual and collect-all actions, visible stored amounts, and full indicators.
- [x] Replace the three-mine hard limit with Town Hall-dependent building limits.
- [x] Define timestamp, pause, offline production, and overflow behavior explicitly.
- [x] Display all costs and production rates from the same definitions used by rules.

Current limits start at three producers and two storages of each type at Town Hall 1, then each limit increases by one per Town Hall tier through level 3. See the Home Village document for validation and device-testing scope.

Done when: a player can earn both resources, spend both, increase capacity, and continue growing after reopening the game.

### Phase 3 — Building interaction and layout editing

Dependencies: 2.

- [x] Tap/select a building; show its name, level, stats, and relevant actions.
- [x] Move existing buildings with preview, cancel, and occupancy validation.
- [x] Add individual wall placement and movement.
- [ ] Add continuous wall drawing and multi-selection tools.
- [ ] Add removable obstacles with configured costs and rewards.
- [ ] Support saved layouts and validate that every owned required building is placed once.
- [ ] Keep the last valid layout when editing is cancelled or interrupted.

Done when: a village can be rearranged without losing buildings, creating duplicates, or overlapping footprints.

### Phase 4 — Builders, construction, and Town Hall progression

Dependencies: 2–3.

- [x] Add two fixed builders with available/busy state and upgrade jobs.
- [ ] Add builder ownership progression and timed new-building construction jobs.
- [x] Implement upgrade prerequisites, costs, durations, level transitions, and cancellation policy.
- [ ] Define whether upgrading buildings defend or produce; reflect the rule in every mode.
- [x] Add upgrade finish timestamps and completion on return.
- [ ] Add optional completion notifications.
- [ ] Author an initial three-level Town Hall progression with new unlocks and art states.
- [x] Add a builder queue view and explain blocked upgrades.

Done when: spend → builder assignment → waiting → completion → stronger building works across restarts and cannot complete twice.

### Phase 5 — Defense and battle simulation foundation

23 September update: a [practice battle](PRACTICE_BATTLE_PROGRESS.md) now runs fixed integer ticks with Cannon/Archer Tower targeting, eight melee raiders, HP/damage, destruction, results, and retry/return. Raiders navigate around footprints, use a wall entrance, and breach sealed walls in the selectable Wall Breach challenge. Open Gate and Wall Breach can be selected while scouting or after results, with challenge-aware retry and replay. Its state is isolated from the home village. The full phase remains partial: arbitrary snapshots, general village navigation, persisted replay records and online validation are outstanding. Watch Replay now reconstructs the current practice attack from timed deployment commands and checks its final state hash; validation compares every tick. Replay export and cross-version support remain outstanding.

Dependencies: 1, 3; coordinate with 4.

- [ ] Build a separate battle scene from an immutable village snapshot.
- [ ] Add stable entity IDs, health, damage, range, attack intervals, target categories, and destruction.
- [ ] Add melee/ranged targeting, grid pathfinding, wall breaking, and unreachable-target handling.
- [ ] Add initial cannon, ranged tower, wall, and trap definitions.
- [ ] Use fixed simulation ticks, seeded randomness, and stable target ordering.
- [x] Keep local practice combat calculations independent of Unity physics and visual frame rate.
- [ ] Establish server authority for online combat.
- [x] Record timed practice commands and compare final state hashes during in-memory replay.
- [ ] Persist/version replay records and validate battles on the server.

Done when: identical snapshot, rules version, seed, and commands reproduce the same battle outcome in repeat runs.

### Phase 6 — Army preparation and deployment

Dependencies: 5.

- [ ] Add barracks, army camps, army capacity, unlock rules, and saved army presets.
- [ ] Build a melee fighter, ranged attacker, and defense-targeting tank with original art.
- [ ] Define army readiness/replenishment rules as data; do not assume historical Clash training rules.
- [ ] Add deployment-zone validation, troop counts, troop selection, and deploy feedback.
- [ ] Add troop research and a laboratory job tied to progression.

Done when: a legal army can be prepared and deployed, and its roster/capacity cannot be exceeded.

### Phase 7 — Complete the first attack loop

Dependencies: 4–6.

- [ ] Author several AI villages and a guided first attack.
- [x] Implement practice scouting, explicit battle start, timer, surrender, destruction percentage and three-star scoring.
- [ ] Integrate those controls with campaign opponents and army preparation.
- [ ] Calculate loot, victory rewards, defeat outcomes, and first-completion rewards.
- [x] Add practice results UI, retry/return flow and in-memory replay playback.
- [ ] Integrate reward claims and persistent battle history with the full attack loop.
- [ ] Teach building, collection, upgrading, and deployment through resumable tutorial steps.

Done when: a new player can finish an AI attack, claim its reward once, and spend the reward on village progression.

### Phase 8 — Accounts and authoritative backend

Dependencies: 1; implement before online progression testing.

- [ ] Choose hosting, database, account provider, staging environment, and operational budget.
- [ ] Build a modular server with authentication, village/economy commands, content delivery, and background jobs.
- [ ] Add guest accounts, account linking/recovery, session expiration, logout, and multi-device conflict behavior.
- [ ] Move balances, inventory, timers, unlocks, purchases, and production calculations to server authority.
- [ ] Implement transactional mutations, idempotent command handling, state revisions, and audit records.
- [ ] Define how local prototype saves become test online accounts; do not accept arbitrary client balances into production.
- [ ] Add database migrations, backups, restore rehearsal, metrics, and structured error logging.

Done when: the same account resumes on another device, duplicate requests have one effect, and changing client time or save values cannot grant resources.

### Phase 9 — Asynchronous player-versus-player battles

Dependencies: 5–8.

- [ ] Add eligible opponent selection, scouting, battle tickets, expiry, and immutable defense snapshots.
- [ ] Define shielding, player availability, loot reservations, and concurrent-attack policies.
- [ ] Bind each battle to account, army, snapshot, seed, content version, and simulation version.
- [ ] Validate command timing, deployment legality, troop counts, duration, and abilities on the server.
- [ ] Re-simulate the battle server-side; calculate loot and outcome from the verified result.
- [ ] Settle attacker gains and defender losses atomically with a unique settlement ID.
- [ ] Add attack/defense history, replays, notifications, and disconnection/retry handling.

Done when: account A can attack account B's saved village while B is offline; retries cannot duplicate loot and a fabricated victory is rejected.

### Phase 10 — Matchmaking and ranked progression

Dependencies: 9.

- [ ] Define casual and competitive queues, eligibility, power bands, rating, and search expansion.
- [ ] Add trophy/rating changes, ranked tiers, leaderboards, seasons, and reset policies.
- [ ] Add shield/guard timing and revenge rules if included in the chosen ruleset.
- [ ] Handle low population explicitly through longer searches or labelled AI opponents.
- [ ] Record match fairness, search time, queue abandonment, and suspicious repeated pairings.
- [ ] Finalize season results and reward claims exactly once across server restarts.

Done when: multiple test accounts complete a season with reproducible standings and correct rewards.

### Phase 11 — Clans and social systems

Dependencies: 8–10.

- [ ] Add clan creation, discovery, profiles, applications, invites, membership limits, and leaving.
- [ ] Implement member/elder/co-leader/leader permissions and leadership succession.
- [ ] Add clan chat, mute/block/report controls, moderation records, and rate limits.
- [ ] Add reinforcement requests, donation limits, capacity, and contribution history.
- [ ] Add friendly challenges, village visits, and shared layout links.
- [ ] Make membership changes and donations transactional under simultaneous requests.

Done when: a group can form a clan, communicate, donate, and challenge one another with enforced permissions and consistent inventories.

### Phase 12 — Clan Wars

Dependencies: 9–11.

- [ ] Add war opt-in, roster selection, matchmaking, preparation, battle, and results states.
- [ ] Freeze war defense layouts at the defined boundary and keep home layouts independent.
- [ ] Track per-member attack allowances and best results per target.
- [ ] Define scoring, ties, stars/destruction, clan rewards, and member eligibility.
- [ ] Persist scheduler transitions and make reward processing restart-safe.

Done when: two clans finish a complete war, including offline members and server restarts, without extra attacks or duplicate rewards.

### Phase 13 — Clan leagues and cooperative challenges

Dependencies: 12.

- [ ] Add league registration, grouped clans, round scheduling, substitutes, and standings.
- [ ] Add promotion/relegation rules, league currency, and a reward shop.
- [ ] Add Clan Games-style individual tasks contributing toward shared reward thresholds.
- [ ] Configure task expiry, contribution limits, membership changes, and claim windows.

Done when: a full league season and clan challenge event finish automatically with an auditable reward ledger.

### Phase 14 — Heroes, spells, pets, and advanced armies

Dependencies: 4–10; deploy additions incrementally.

- [ ] Add hero unlocks, progression, battle slots, active abilities, and readiness policy.
- [ ] Add spells and effects: area damage, healing, buffs, displacement, and stacking rules.
- [ ] Add equipment slots, equipment progression, upgrade currencies, and loadout validation.
- [ ] Add pets, siege units, advanced troop variants, and complex defense behavior.
- [ ] Add counterplay scenarios and deterministic replay checks for every ability.

Done when: loadouts and abilities are validated by the server and reproduce across live battles and replays.

### Phase 15 — Production art, audio, and usability

Dependencies: begin asset work after 1; final gate follows 14.

- [ ] Establish a consistent Kingdoms visual style across welcome artwork, village, units, and menus.
- [ ] Create building level variants, troops, animations, projectiles, destruction, and feedback effects.
- [ ] Add music, ambient audio, action sounds, volume controls, and haptics settings.
- [ ] Improve touch targets, safe areas, readable text, color-independent placement feedback, and localization.
- [ ] Profile pooling, materials, overdraw, textures, loading, memory, and battery use on target phones.

Done when: the full player journey is usable on agreed minimum and representative devices within measured performance budgets.

### Phase 16 — Seasons, events, store, and operations tools

Dependencies: 8, 13–15.

- [ ] Add server-configured daily/season tasks, free rewards, cosmetics, and optional premium tracks.
- [ ] Add platform billing with server receipt verification, duplicate protection, restore, and refund handling.
- [ ] Add inbox, maintenance mode, minimum-client version, feature flags, and event scheduling.
- [ ] Add restricted admin tools for support, moderation, compensation, and economy inspection.
- [ ] Add account deletion/export workflows and operational access auditing.
- [ ] Define currency sources/sinks and monitor inflation, progression stalls, and spending errors.

Done when: test purchases, event expiry, support actions, and refunds behave correctly in staging with traceable state changes.

### Phase 17 — Multiplayer release qualification and launch

Dependencies: 0–16.

- [ ] Run end-to-end tests from new account through upgrades, PvP, clan membership, war, and recovery.
- [ ] Test concurrent commands, cross-account access, replay tampering, stale clients, and network loss.
- [ ] Load-test the agreed concurrent-player target and battle-validation throughput.
- [ ] Rehearse database restoration, rollback, interrupted deployment, and scheduler recovery.
- [ ] Run a closed Android beta, fix blocking defects, and monitor retention and match fairness.
- [ ] Prepare signed releases, store assets, support channel, release notes, and staged rollout.

Done when: acceptance targets are met, no known progress-loss/duplication blockers remain, and the service can be operated and recovered by its maintainers.

### Phase 18 — Second village / Builder Base-style mode

Dependencies: 17 and reusable combat/progression modules.

- [ ] Add a separate village, currencies, builders, progression, army definitions, and layouts.
- [ ] Add mode-specific attack/defense pairing, stages, abilities, scoring, and daily rewards.
- [ ] Define any cross-village unlocks without mixing the two economies accidentally.
- [ ] Add dedicated onboarding, matchmaking, art, and server validation.

Done when: this mode has its own complete growth-and-battle loop and its rewards cannot corrupt Home Village state.

### Phase 19 — Shared Clan Capital-style territory

Dependencies: 11–14, 17.

- [ ] Add clan-owned districts, shared upgrade projects, contribution currency, and editing permissions.
- [ ] Add scheduled raid events, per-member attack budgets, and persistent district damage.
- [ ] Serialize conflicting attacks or use explicit reservations and revision checks.
- [ ] Add district progression, shared army unlocks, raid rewards, and a reward store.

Done when: multiple members can contribute and attack safely, district damage persists correctly, and event rewards settle once.

### Phase 20 — Long-term content and maintenance

Dependencies: 17; expand 18–19 as released.

- [ ] Extend Town Hall tiers through versioned definitions, level art, and tested unlock graphs.
- [ ] Add units, defenses, equipment, maps, events, and cosmetics through a repeatable content pipeline.
- [ ] Maintain old battle replay compatibility or publish an explicit replay-expiry policy.
- [ ] Balance using collected gameplay evidence; test changes against saved battle scenarios.
- [ ] Review hosting costs, matchmaking health, stability, and device performance each release.

Done when: each update can be staged, verified, released, monitored, and rolled back safely. This phase continues for the life of the game.

## 4. Multiplayer architecture contract

This is the proposed Kingdoms architecture, not a claim about Supercell's private implementation.

Home Village attacks use asynchronous multiplayer: the defender need not be connected. Clan chat may use a persistent connection; gameplay commands can use authenticated request/response APIs. A two-player live scene connection alone does not implement persistent village multiplayer.

```text
Unity client
  -> authenticated command API
       -> account / village / economy modules
       -> matchmaking / battle tickets
       -> clans / wars / seasons
       -> transactional database and audit ledger
       -> durable jobs and battle validation workers
  <- authoritative state revision, verified result, or actionable error
```

| Record | Minimum information |
| --- | --- |
| Account | ID, linked identity, session state, creation time |
| Village | Owner, revision, rules version, buildings, balances, production timestamp |
| Building/job | Instance ID, definition, level, cell, job ID, start/finish timestamp |
| Battle | Ticket, owner, defense snapshot, seed, versions, command log, verified result |
| Transaction | Unique command ID, account, reason, deltas, resulting revision |
| Clan/war | Membership roles, roster snapshot, stage, deadlines, attacks, scores |
| Season/event | Rules version, eligibility, schedule, progress, reward claims |

Command examples: PlaceBuilding, MoveBuilding, CollectResource, StartUpgrade, FinishUpgrade, RequestOpponent, BeginBattle, SubmitBattleCommands, DonateTroops, SubmitWarAttack.

The client requests actions; it never supplies an authoritative new balance, finished upgrade, battle score, or reward. Validate ownership, prerequisites, capacity, revision, and idempotency before committing. Use server time for online production and jobs. Treat disconnected screens as cached state; explicitly resolve queued commands on reconnect.

A small deployment may start as one modular service, one database, and a worker. Split services only when measured operational needs justify it. Hosting/provider selection and cost estimates remain a Phase 8 deliverable.

## 5. Next implementation plan

Prioritize finishing the offline game loop before adding more practice-only controls or social screens. The order below is a recommendation, not a promised schedule.

| Order | Work package | Phases | Acceptance gate |
| --- | --- | --- | --- |
| 1 | Rebuild and test the current mobile baseline | 0, 15 | Fresh install and returning save complete village ? scouting ? battle ? replay ? return on a target phone; record device/FPS/memory and fix blocking issues |
| 2 | Owned army foundation | 1, 6 | Data-defined troops, barracks/camp capacity, readiness rules and saved army survive restart; invalid/over-capacity rosters are rejected |
| 3 | Troop choice and deployment | 5, 6 | Add a ranged troop and tank alongside melee; select troops and deploy into validated ground zones; commands replay identically |
| 4 | Snapshot-based campaign battles | 5, 7 | Load several authored immutable enemy layouts instead of a hardcoded encounter; navigate different layouts and complete battles without modifying the home village |
| 5 | Rewards and campaign progression | 4, 7 | Victory/defeat rules and first-clear rewards are explicit; claims settle once even after restart; earned resources can fund a village upgrade |
| 6 | Finish village progression and onboarding | 3, 4, 7 | Construction jobs, wall/layout tools and a resumable tutorial support the entire build ? prepare ? attack ? reward ? upgrade loop |
| 7 | Offline mobile milestone review | 0?7, 15 | Run the complete loop on target phones, fix input/performance/save defects, and publish an updated test build and guide |
| 8 | Accounts and server authority | 8 | Choose hosting/budget, build authentication and authoritative transactional village commands, verify recovery and multi-device behavior |
| 9 | PvP and ranked alpha | 9?10 | Two accounts attack saved villages; server verifies results and settles loot once; then add ratings and seasons |
| 10 | Social, advanced content and release | 11?17 | Build clans/wars/events and advanced armies, then complete production art/audio, operations and release gates |
| 11 | Optional expansion modes | 18?20 | Start only after the core service is stable and maintainable |

**Next code milestone:** owned army data and capacity/readiness rules, with a simple preparation screen. Agree the Kingdoms readiness design before implementing timers or consumable army costs; historical reference-game rules are not assumed. In parallel with planning, the current build needs the Phase 0 phone validation gate. Backend providers, hosting spend and production releases remain future decisions.

Every development increment must update `BEGINNER_GUIDE.md`, record relevant validation evidence, and use a meaningful commit. Local commits remain valid progress even if remote authentication prevents a push; do not describe an unpushed commit as published.

Do not mark multiplayer complete when login or cloud saving alone works. The first multiplayer completion gate is Phase 9's verified attack and single settlement between two accounts.

## 6. Tracking template

For each phase record:

```text
Phase:
Status: Not started / In progress / Blocked / Verified
Owner:
Dependencies satisfied:
Implemented tasks:
Build or commit:
Test devices and environment:
Acceptance evidence:
Known defects:
Next concrete task:
```

Related project evidence: [Beginner guide](BEGINNER_GUIDE.md), [village validation](VillageValidation.txt), [current state rules](Assets/Scripts/Core/VillageState.cs), [current gameplay controller](Assets/Scripts/UI/VillageGameplay.cs).
