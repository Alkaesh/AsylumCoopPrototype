# Atmospheric Horror Redesign Audit

## Phase 1 - Repository Audit

### What Currently Works

- Mirror listen-server flow, lobby, scene changes, and return-to-lobby already work through [HorrorNetworkManager](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Network\HorrorNetworkManager.cs), [LobbyState](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Network\LobbyState.cs), and [GameStateManager](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameStateManager.cs).
- The project already has a full playable round loop: co-op players spawn, tasks can be completed, the monster patrols and hunts, players can be downed/carried/hooked/rescued, and the round resolves cleanly.
- The monster has real stateful behavior in [MonsterAI](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Monster\MonsterAI.cs): patrol, investigate, suspicious, search, chase, attack, carry, and hook.
- Interaction, doors, batteries, hook flow, revive flow, hide spots, and false-presence systems already exist. The problem is not missing systems.
- The playable hospital level is already generated as a semi-authored scene through [HorrorPrototypeBuilder](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs). That is the real scene authority, not the runtime blockout builder.

### What Makes The Game Feel Cheap Or Placeholder

- [RuntimeLevelBuilder](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\RuntimeLevelBuilder.cs) is still a primitive-box prototype builder. Even when unused, its existence reflects the old architecture and invites the wrong direction.
- [HorrorPrototypeBuilder](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs) still relies on too many cubes as final shell geometry. Props help, but the environment still reads as decorated blockout instead of a believable asylum.
- Decorative placement helpers in the builder were previously placing props with no overlap validation, no floor support validation, and no clearance checks.
- [FalsePresenceDirector](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs) and [ScriptedHorrorMoment](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\ScriptedHorrorMoment.cs) were using primitive silhouette placeholders for some boss reveals. That immediately breaks fear.
- The project has many systems, but several still communicate like tools and prototypes rather than horror presentation.

### What Breaks Fear

- Too much of the game explains itself through HUD text and systemic cues. Fear gets replaced by certainty.
- Some scares were timer-driven instead of authored around visibility, orientation, and current pressure phase.
- Placeholder apparition geometry makes the monster feel fake in the exact moments where it should become iconic.
- The monster can still read as a generic AI state machine if it is always legible through logic and not through presence, pacing, and aftermath.
- Over-randomization of placement weakens dramatic flow. Variety without route logic feels procedural, not scary.

### What Breaks Map Quality And Collision Credibility

- Spawn placement in [RoundRandomizer](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\RoundRandomizer.cs) and fallback spawning in [GameplayBootstrapper](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameplayBootstrapper.cs) previously trusted raw transforms with no support-surface or overlap safety.
- The current level shell uses large primitive partitions and blockers instead of modular wall shells, panels, columns, door surrounds, and themed structure pieces.
- Some decorative set dressing was being placed as if every asset were floor-based. That weakens spatial believability and leads to visual noise.
- Map variation existed mostly at the objective layer. The space itself was not sufficiently authored as a memorable place with clear visual rhythm.

### Which Systems Should Be Removed, Simplified, Or Replaced

- Demote [RuntimeLevelBuilder](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\RuntimeLevelBuilder.cs) to blockout-only tooling. It should not be treated as the main playable level generator.
- Remove primitive boss silhouettes from scare presentation and replace them with real-boss apparition rendering.
- Simplify objective presentation so players receive a directive and a clue, not a checklist dashboard.
- Reduce placement randomness that fights authored route flow or causes credibility problems.
- Keep the monster mechanically broad enough for multiplayer, but keep cutting anything that feels like invisible AI cheating.

### Which Systems Have Strong Horror Potential And Should Be Expanded

- [HorrorPrototypeBuilder](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs) should become the central modular scene-authority: authored flow first, modular shell dressing second, runtime safety rules third.
- [FalsePresenceDirector](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs) has strong potential once events are visibility-aware, phase-aware, and tied to the real monster identity.
- [ScriptedHorrorMoment](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\ScriptedHorrorMoment.cs) should focus on a few deliberate setpieces, not broad systemic repetition.
- [MonsterAI](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Monster\MonsterAI.cs) should keep moving toward a stalking, listening predator instead of a generic chaser.
- [RoundRandomizer](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\RoundRandomizer.cs) has good potential when route logic and safe placement are treated as horror pacing tools rather than simple randomization.

## Phase 2 - Redesign Plan

### New Horror Fantasy

This game should feel like:

> A team pushes through one unstable wing of a ruined asylum while a stalking cadaver-warden uses the building better than they do. It is heard before it is seen, glimpsed before it commits, and most dangerous when the players think they have bought a quiet minute.

### Revised 15-25 Minute Match Structure

1. Brief, unreliable safe pocket in Security.
2. Players infer the active route from sparse direction, lighting, and environmental clues.
3. They commit into one wing to restore auxiliary power.
4. The route narrows and the first real hunt pressure begins.
5. Access and power objectives pull them deeper into the most compromised spaces.
6. The return path is shorter but more exposed, with fewer trustworthy quiet zones.
7. The final escape is not a victory lap. It is a retreat through a space the monster already owns.

### Revised Map Philosophy

- One authored backbone with a few short loops and branch choices.
- Strong landmarks that players learn quickly: reception, archive aisles, operating bay, server row, pipe choke, morgue cold wall.
- Fewer random blockers, more deliberate line-of-sight breaks.
- Clear transition in mood between zones:
  - Security: administrative false calm
  - Archive: tight, obscured, stalking space
  - Lab / Surgery: clinical red violence
  - Service / Maintenance: noise, grime, machinery
  - Morgue: cold exposure and aftermath

### Revised Level Building Approach

- Keep [RuntimeLevelBuilder](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\RuntimeLevelBuilder.cs) as blockout-only fallback.
- Treat [HorrorPrototypeBuilder](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs) as the main level-authoring pipeline.
- Use primitive shells as collision scaffolding only when necessary.
- Overlay those shells with modular prefab walls, columns, door surrounds, pipes, cabinets, and theme-specific structure so the level reads as a believable place.
- Validate placement before committing spawned objectives or decorative props:
  - support surface check
  - anti-overlap check
  - traversal clearance
  - NavMesh-safe monster/objective placement

### Revised Scare Philosophy

- Ambient dread should dominate the opening.
- False positives should suggest the monster's presence without proving it.
- Silhouette reveals should be rare, highly readable, and tied to visibility and light.
- Spatial audio scares should use space and occlusion, not UI-like global signals.
- Environmental changes should feel like aftermath or intrusion, not random events.
- Boss manifestations should be short, curated, and unmistakably the real boss.
- Fake-outs should imply pursuit without constantly forcing chase.
- Every scare should leave a residue: changed light, changed sound, changed certainty.

### Revised Boss Identity

- The monster is not a generic horror AI. It is a warding body that listens, checks, lingers, and only commits hard when it has enough certainty.
- Its identity comes from:
  - heavy directional footsteps
  - pauses and listening beats
  - sudden short violence
  - rare but unforgettable visual reveals
  - pressure that persists after it is no longer visible

### Revised UI Philosophy

- HUD should support action, not narrate systems.
- Objectives should describe intent and tension, not raw task state.
- Team state should stay coarse.
- Horror information should come from the world first, UI second.
- If the player can learn something through sound, space, and light, the HUD should stay quiet.

### Revised Use Of Sound, Light, And Silence

- Silence is part of pacing. Do not fill every gap.
- Use door movement, ventilation, pipe strain, distant footsteps, and room tone as the base.
- Let light changes carry emphasis: red emergency wash, cold morgue flash, surgical glare, server flicker.
- Reserve strong stingers and reveal flashes for a few curated beats.
- Monster presence should be legible through proximity and space, not constant gamey music.

## Phase 3 - Prioritized Implementation Backlog

### Priority 0 - Foundation Fixes

- Convert spawn placement to collision-safe, floor-safe, NavMesh-safe placement
  - Files: [PlacementSafety.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\PlacementSafety.cs), [RoundRandomizer.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\RoundRandomizer.cs), [GameplayBootstrapper.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameplayBootstrapper.cs)
  - Reason: invalid/object-overlap spawns immediately destroy credibility
  - Expected result: core objectives, batteries, hooks, and monster spawn only in supported, non-broken positions
  - Category: gameplay / level / networking

- Replace placeholder boss scare silhouettes with real-boss apparitions
  - Files: [BossApparitionFactory.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\BossApparitionFactory.cs), [BossApparitionProxy.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\BossApparitionProxy.cs), [FalsePresenceDirector.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs), [ScriptedHorrorMoment.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\ScriptedHorrorMoment.cs)
  - Reason: cheap placeholder geometry ruins the game's most important visual beats
  - Expected result: every short boss reveal uses the actual monster model, silhouette, and timing language
  - Category: visuals / audio / gameplay

- Demote the runtime blockout builder from playable authority
  - Files: [RuntimeLevelBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\RuntimeLevelBuilder.cs)
  - Reason: shipping flow should not depend on prototype cube layout generation
  - Expected result: clear separation between blockout tooling and playable authored level flow
  - Category: level / architecture

- Reduce explicit objective telemetry
  - Files: [GameStateManager.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameStateManager.cs), [HudController.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\UI\HudController.cs)
  - Reason: exact counts and checklist language weaken uncertainty
  - Expected result: directive-driven HUD with less system exposure
  - Category: UI / gameplay

### Priority 1 - Horror Experience Upgrades

- Convert environment assembly toward modular prefab shell dressing
  - Files: [HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)
  - Reason: the current level still reads as decorated blockout
  - Expected result: walls, door zones, service areas, and zone transitions look built rather than boxed
  - Category: level / visuals

- Add deterministic visual variants and wear states to environment props
  - Files: [HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)
  - Reason: repeated identical materials flatten the world
  - Expected result: believable variation in grime, tone, and wear across props and structure pieces
  - Category: visuals

- Make false-presence events presentation-aware
  - Files: [FalsePresenceDirector.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs)
  - Reason: scare events should depend on player orientation, sightlines, and pacing
  - Expected result: fewer events, stronger reads, better buildup and cooldown
  - Category: gameplay / audio / visuals

- Continue moving monster logic away from omniscience and toward stalking
  - Files: [MonsterAI.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Monster\MonsterAI.cs), [NoiseSystem.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Monster\NoiseSystem.cs)
  - Reason: fear improves when the monster seems predatory, not magical
  - Expected result: more credible near-misses, investigations, and recoverable stealth mistakes
  - Category: AI / gameplay

### Priority 2 - Polish

- Add stronger authored zone signatures through lighting and dressing
  - Files: [HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)
  - Reason: memorable rooms matter more than broad content quantity
  - Expected result: Security, Archive, Surgery, Service, and Morgue all feel distinct in light and texture
  - Category: level / visuals / audio

- Reduce remaining technical-feeling prompts and system labels
  - Files: [HudController.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\UI\HudController.cs), [Assets/Scripts/Tasks/*](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Tasks)
  - Reason: UI language still exposes too much of the machinery
  - Expected result: cleaner and more diegetic presentation
  - Category: UI / gameplay

- Refine post-processing, fog, and ambient beds around zone transitions
  - Files: [HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs), [PlayerAudioController.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Player\PlayerAudioController.cs)
  - Reason: atmosphere is carried by continuity, not by single effects
  - Expected result: stronger tonal cohesion across the full 15-25 minute match
  - Category: visuals / audio

## Phase 4 - Code Changes Implemented In This Pass

### 1. Safe placement layer for runtime objectives and fallback spawns

- Added [PlacementSafety.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\PlacementSafety.cs)
- Integrated it into [RoundRandomizer.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\RoundRandomizer.cs) and [GameplayBootstrapper.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameplayBootstrapper.cs)

Why it improves fear and presentation:

- Broken spawns and clipped objects immediately expose the game as a prototype.
- Safe placement removes a class of immersion-breaking failures without adding new mechanics.
- Stable objective and monster placement makes route flow feel authored instead of fragile.

### 2. Real boss apparition system

- Added [BossApparitionFactory.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\BossApparitionFactory.cs)
- Added [BossApparitionProxy.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\BossApparitionProxy.cs)
- Replaced primitive scare silhouettes in [FalsePresenceDirector.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs) and [ScriptedHorrorMoment.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\ScriptedHorrorMoment.cs)

Why it improves fear and presentation:

- The same monster identity now owns both AI gameplay and horror presentation.
- Short reveals no longer feel fake or placeholder.
- This strengthens anticipation and memory because the player sees the actual threat, not an abstract stand-in.

### 3. Presentation-aware scare gating

- Updated [FalsePresenceDirector.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs) so visual scare anchors must pass basic sightline/orientation checks

Why it improves fear and presentation:

- Apparitions now happen where the player can meaningfully read them.
- That makes scare language feel deliberate instead of arbitrary.

### 4. Builder upgraded toward modular shell dressing and safer decorative assembly

- Updated [HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)
- Added modular shell dressing pass with existing prefab assets
- Added deterministic prop tint variation
- Added placement validation for floor-based decorative props

Why it improves fear and presentation:

- The level now leans less on raw cubes as final visual output.
- Visual repetition is reduced without adding content noise.
- Decorative props are less likely to overlap invalidly or clip into traversal space.

### 5. Further reduced explicit objective telemetry

- Updated [GameStateManager.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameStateManager.cs)

Why it improves fear and presentation:

- The objective panel now pressures the player with intent instead of raw counts.
- That keeps attention on the environment and the monster rather than on system bookkeeping.
