# Clash of Clans — Feature Reference and Development Phases

Prepared: 15 September 2026.

Companion execution plan: [Kingdoms complete development phases](GAME_DEVELOPMENT_PHASES.md).

## 1. Meaning of “exact Clash of Clans” in this plan

The target is the complete family of Clash of Clans-style gameplay systems: Home Village growth, army preparation, attacks, defense, clans, wars, leagues, heroes, additional villages, shared clan raids, and seasonal operation.

This document maps those systems into implementable phases. It is not Supercell source code, a verified reproduction of its private backend, or an exhaustive table of every current cost, timer, unit statistic, and experiment. Those details require the parity catalogue below before an implementation can honestly be described as exact. Kingdoms remains the project identity; asset creation is a separate tracked deliverable.

Clash changes over time. Official August 2026 notes include Town Hall 18 pet/supercharge additions and revised onboarding, with some onboarding changes explicitly described as a September test rollout. Therefore, even a date alone may not identify one universal player experience. [Official August update](https://supercell.com/en/games/clashofclans/blog/release-notes/august-update-3/)

## 2. Phase R0 — Freeze and catalogue the reference

Dependencies: none. Required before claiming exact feature parity.

- [ ] Record the reference app version/build, platform, date, region, and visible experiment cohort when identifiable.
- [ ] Capture each menu, village mode, battle flow, result screen, and progression gate through observable behavior.
- [ ] List every resource, building, building level, unit, spell, hero, equipment item, pet, trap, and siege unit.
- [ ] Record exact costs, times, capacities, damage rules, targeting, unlocks, and limits with source/date evidence.
- [ ] Record seasonal rules separately from permanent systems.
- [ ] Mark unknown values as unknown; distinguish observation, official documentation, and design choices.
- [ ] Choose which version of historical mechanics is in scope rather than combining incompatible versions.

Use a catalogue row like this:

| System/item | Reference version | Exact rule/value | Evidence | Kingdoms equivalent | Phase | Parity test | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Gold Mine | To record | To verify per level | Official/in-game record | Gold Mine | R2 | Production/capacity comparison | Partial prototype |
| Hero unlock | May 2026 note | Barbarian King unlock moved to TH4 | Source below | First hero unlock | R8 | Eligibility boundary | Not built |

The hero example is supported by the official May 2026 update; older TH7 guides should not silently override it. [Official May update](https://supercell.com/en/games/clashofclans/blog/release-notes/may-update/)

Done when: every in-scope item has an assigned phase and evidence-backed specification; unresolved rules are visibly tracked.

## 3. Phase R1 — Home Village presentation and onboarding

Kingdoms phases: 0–1, 7, 15. Current status: partial.

- [ ] Loading, identity, tutorial, resource HUD, builder status, settings, help, and inbox.
- [ ] Isometric navigation, map boundaries, object selection, contextual buttons, and layout interaction.
- [ ] Introductory build/collect/upgrade/attack lessons and returning-player flow.
- [ ] Village environment, obstacles, decorations, scenery, audio, and feedback.

Acceptance: a fresh player completes onboarding and a returning player resumes the correct state on a phone.

## 4. Phase R2 — Economy, builders, and building progression

Kingdoms phases: 2–4, 8. Current status: gold/elixir producers and storage, movement, two builders, and three initial upgrade levels implemented. Exact reference balancing, full construction, and server authority remain incomplete. See [Home Village progress](HOME_VILLAGE_PROGRESS.md).

- [ ] Gold, elixir, dark-resource progression, premium currency, and mode-specific currencies.
- [ ] Collectors, storages, treasury behavior, capacity, overflow, and loot eligibility.
- [ ] Builders, construction, upgrades, completion, cancellation, and speed-up items.
- [ ] Town Hall unlock graph, building count limits, prerequisites, and level variants.
- [ ] Walls, obstacles, layout slots, movement, copying, and editing rules.
- [ ] Inventory and magic-item-style effects with explicit stacking and overflow behavior.

Acceptance: each catalogue level follows its specified cost, duration, builder requirement, unlock condition, and storage limit.

## 5. Phase R3 — Army buildings and preparation

Kingdoms phases: 5–6. Current status: absent.

- [ ] Barracks, army camps, laboratory, spell production, and relevant advanced production buildings.
- [ ] Troop/spell unlocks, research levels, housing space, army presets, and readiness rules.
- [ ] Reinforcement capacity and donated army components, completed with R9.
- [ ] Every roster category from the frozen catalogue, with individual behavior and animation tasks.

Acceptance: legal army combinations match catalogue limits and invalid combinations are rejected by both UI and rules.

Do not automatically copy old training costs or training timers. Verify the chosen reference build's preparation behavior during R0.

## 6. Phase R4 — Defenses, troops, and battle engine

Kingdoms phases: 5–7. Current status: absent.

- [ ] Defense families: single-target, splash, air-targeting, hidden, beam, and advanced/merged variants as catalogued.
- [ ] Walls, traps, defending troops, defending heroes, and Town Hall weapon behavior where applicable.
- [ ] Ground/air movement, target preferences, retargeting, wall destruction, projectiles, and status effects.
- [ ] Deployment boundaries, troop selection, spell placement, hero activation, scouting, and battle timer.
- [ ] Destruction percentage, stars, ties, surrender, loot, and end-of-battle presentation.
- [ ] Recorded input playback and simulation versioning.

Acceptance: curated reference scenarios reproduce the specified targeting, damage, scoring, and time-limit outcomes. Matching only the visual battle appearance is insufficient.

## 7. Phase R5 — Single-player maps and practice

Kingdoms phase: 7. Current status: absent.

- [ ] Campaign villages, progression, first-completion rewards, repeat rewards, and completion tracking.
- [ ] Practice/tutorial attacks and challenge scenarios.
- [ ] Restart, surrender, reward claiming, and replay behavior.

Acceptance: all maps are completable with their intended armies and rewards cannot be claimed more often than the selected rules allow.

## 8. Phase R6 — Persistent online villages and player attacks

Kingdoms phases: 8–9. Current status: absent.

- [ ] Accounts, linking/recovery, cloud state, sessions, server time, and command validation.
- [ ] Opponent search, scouting, defense snapshots, attack tickets, and authoritative loot settlement.
- [ ] Attack/defense logs, replays, revenge eligibility, shields/guards, and concurrent-attack policy.
- [ ] Disconnect recovery and old-client/content compatibility.

Acceptance: an attacker can raid an offline defender, results persist for both, and forged or duplicated results cannot award resources.

Implementation note: use the authoritative architecture in the Kingdoms roadmap. Supercell's internal implementation is not public evidence for a particular framework, database, or protocol.

## 9. Phase R7 — Competitive matchmaking and seasons

Kingdoms phase: 10. Current status: absent.

- [ ] Catalogue the chosen build's casual/ranked modes, entry conditions, attack allowances, and reward rules.
- [ ] Implement rating/trophies, leagues, leaderboards, season resets, bonuses, and top-tier rules.
- [ ] Test strength matching, queue expansion, population shortages, and season boundaries.

Acceptance: fixtures spanning a complete season produce the specified standings and rewards. Current official gameplay discussion treats Ranked Mode, Legend League, and Clan War Leagues as distinct balancing areas. [Official gameplay discussion](https://supercell.com/en/games/clashofclans/blog/news/the-state-of-gameplay-and-whats-ahead/)

## 10. Phase R8 — Heroes and advanced battle content

Kingdoms phase: 14. Current status: absent.

- [ ] Hero ownership/unlocks, hero management building, levels, active roster, abilities, and skins.
- [ ] Equipment slots, common/epic-style progression categories, upgrade materials, and restrictions.
- [ ] Pets, pairing limits, pet abilities, siege units, and advanced troop variants.
- [ ] Advanced spells, summons, transformations, revives, and effect interactions from the catalogue.
- [ ] Later-tier defense upgrades and temporary/extra upgrade systems where in scope.

Acceptance: every catalogue ability has an interaction test and produces the same verified result in battle and replay.

## 11. Phase R9 — Clans, donations, and friendly play

Kingdoms phase: 11. Current status: absent.

- [ ] Clan creation, search, descriptions, badges, recruitment, applications, invites, and membership requirements.
- [ ] Leadership roles, permissions, transfer/succession, member removal, and contribution records.
- [ ] Clan chat, reporting, blocking, and moderation tooling.
- [ ] Troop/spell/siege reinforcement requests, capacity, donation limits, and request cooldown rules.
- [ ] Clan progression/perks, friendly challenges, friendly wars where selected, and village visits.

Acceptance: concurrent donations and membership changes preserve correct capacity, inventory, and permissions.

## 12. Phase R10 — Clan Wars

Kingdoms phase: 12. Current status: absent.

- [ ] Participation, roster selection, matchmaking, preparation, and battle phases.
- [ ] Dedicated war layouts, scouting, attacks-per-member rules, and target result tracking.
- [ ] War scoring, tie-break rules, history, clan progression, and eligible-member rewards.
- [ ] War scheduling, restart recovery, and opt-out/member-departure behavior.

Acceptance: two full rosters complete a war with correct attack limits and a single final reward settlement. Dedicated War Base preparation is documented in official support. [Participating in a Clan War](https://support.supercell.com/clash-of-clans/en/articles/participating-in-a-clan-war.html)

## 13. Phase R11 — Clan War Leagues and Clan Games

Kingdoms phase: 13. Current status: absent.

- [ ] League sign-up, groups, scheduled rounds, daily rosters/substitutes, standings, and promotion/relegation.
- [ ] League-specific battle rules/modifiers and currency rewards for the selected version.
- [ ] Reward shop and leader-distributed bonuses where specified.
- [ ] Clan challenge tasks, individual contribution, shared tiers, reward choices, and expiry.

Acceptance: an entire scheduled league and a cooperative challenge event complete correctly under simulated time and worker restarts.

Historical release notes establish Clan War Leagues as a distinct competitive clan system, but their original numerical rules must be rechecked against R0's chosen version. [Original Clan War Leagues introduction](https://supercell.com/en/games/clashofclans/blog/news/october-2018-update-patch-notes/)

## 14. Phase R12 — Builder Base-style second village

Kingdoms phase: 18. Current status: absent.

- [ ] Independent map, buildings, builders, currencies, storages, progression, and layouts.
- [ ] Dedicated troops/heroes, abilities, army preparation, stages, and defense interactions.
- [ ] Mode-specific matchmaking, attack/defense rewards, trophies, and daily progression rules.
- [ ] Travel between villages and any specified cross-village unlocks.

Acceptance: a complete independent economy/battle loop with no accidental resource or layout crossover.

Builder availability is version-sensitive: the official May 2026 notes describe an additional Builder Base builder and unlock requirements. [Official May update](https://supercell.com/en/games/clashofclans/blog/release-notes/may-update/)

## 15. Phase R13 — Clan Capital-style shared territory

Kingdoms phase: 19. Current status: absent.

- [ ] Clan-owned capital, districts, hall progression, shared buildings, and editing roles.
- [ ] Member contributions, shared upgrade projects, contribution currency, and production/conversion rules.
- [ ] Shared army unlocks and raid-event matchmaking.
- [ ] Persistent destruction across attacks, attack budgets, district completion, and raid rewards.
- [ ] Raid-currency spending and reinforcement acquisition where specified.

Acceptance: multiple members contribute and attack concurrently without lost contributions, reverted damage, or duplicate rewards.

Official launch notes describe collaborative construction, multiple districts, Raid Weekends, Capital Gold, and Raid Medals. Treat their original unlock numbers as historical until checked against the selected version. [Clan Capital release notes](https://supercell.com/en/games/clashofclans/blog/release-notes/clancapital-releasenotes/)

## 16. Phase R14 — Events, cosmetics, store, and live operation

Kingdoms phases: 15–17, 20. Current status: absent apart from early visual assets.

- [ ] Achievements, seasonal tasks, free/premium reward tracks, limited events, and special challenges.
- [ ] Cosmetics, skins, scenery, decorations, inventory items, and event currencies.
- [ ] Shop offers, verified purchases, restoration, refunds, and claim expiry.
- [ ] Notifications, localization, accessibility, support, moderation, and account recovery.
- [ ] Content deployment, minimum version, maintenance, analytics, balance updates, and recovery drills.

Acceptance: a complete test event can be configured, launched, purchased through in a sandbox, ended, rewarded, and audited without changing game code.

## 17. Exact-parity release gate

These gates are stricter than releasing an original Kingdoms game:

- [ ] Every row in R0's feature catalogue is implemented or explicitly excluded from the declared scope.
- [ ] Every cost, time, unlock, limit, and ability has versioned evidence and a passing comparison test.
- [ ] All Home Village tiers in scope have complete buildings, level art, troops, and unlock graphs.
- [ ] All included modes have their own economy, matchmaking, rewards, UI, and persistence tests.
- [ ] Server behavior matches the observable rules even when private implementation details are unknown.
- [ ] Device testing verifies the full player journey, multiplayer concurrency, reconnects, and upgrades.
- [ ] Experiments, temporary events, and post-freeze updates are separately labelled.

Until these gates pass, describe the result as a Clash of Clans-style implementation with documented coverage, not an exact current replica.

## 18. Practical build order for this repository

1. Continue the existing village through R1–R2.
2. Build R3–R5 as a small playable combat slice.
3. Add R6–R7 for verified online attacks and ranked progression.
4. Add R9–R11 for clans, wars, leagues, and cooperative tasks.
5. Expand R8 army content while completing R14 production readiness.
6. Release the stable Home Village game.
7. Add R12 and R13 as substantial expansions.
8. Continue the R0 catalogue and parity tests whenever the selected reference scope expands.

For exact completion of the broad reference scope, expansions remain required work. They are staged after the Home Village release so that the core game can be tested and operated before adding two more game economies.
