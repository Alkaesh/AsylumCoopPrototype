# Atmospheric Horror Audit And Redesign

## Phase 1 - Audit

### What Still Feels Cheap
- The playable map in [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs) already moved past raw prototype state, but too much of the shell still reads as decorated collision geometry instead of authored architecture.
- Lighting is functional, not expressive. Corridors were readable, but not oppressive. Darkness existed mostly as low brightness, not as controlled loss of information.
- Materials repeat too cleanly. Structural surfaces lacked enough wear, stains, failed paint, rust, and dirty tile variation.
- The monster still risked reading as “AI state machine with search/chase states” instead of a stalking body with intent.
- The HUD still exposed too much of the system layer through persistent labels and exact state-like wording.

### What Breaks Immersion
- Flat ambient light and low-contrast fills weaken fear because the level stays visually legible from too many angles.
- Some prompts still sounded like interaction labels instead of fiction.
- Chase feedback leaned too much on gamey audio reinforcement instead of space, breathing, footsteps, and disappearance.
- Scares were better than before, but visual events still needed stronger buildup and more recovery space between major reveals.

### What Breaks Visual Quality
- [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\RuntimeLevelBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\RuntimeLevelBuilder.cs) is still blockout-only logic. The real quality path is scene generation through [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs).
- The built-in render path had no dedicated post-processing pass, so the image lacked tonal cohesion.
- Zone identity existed mostly through prop placement, not enough through light, wear, surface tone, and local contrast.
- Repetition in wall/floor surfaces made rooms feel assembled from utility pieces rather than lived-in or damaged spaces.

### What Breaks Horror Tension
- The monster could still over-commit to chase from visibility alone, which makes encounters feel systemic instead of predatory.
- Search behavior did not linger enough at room edges, doors, and last-known spaces.
- Some false-presence moments still fired as events, not as short directed beats.
- The HUD made too much information explicit, which reduces ambiguity and co-op communication.

### Systems To Remove, Simplify, Or Replace
- Keep [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\RuntimeLevelBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\RuntimeLevelBuilder.cs) as blockout tooling only.
- Keep reducing exact UI text and exposed system language.
- Keep cutting “soft cheating” monster sensing that is not supported by proximity, noise, or ongoing search state.
- Keep reducing dependence on chase-music style signaling.

### Systems With Strong Horror Potential
- [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs): best place to improve atmosphere through lights, wear, structural dressing, and authored zone identity.
- [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Monster\MonsterAI.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Monster\MonsterAI.cs): best place to move from generic pursuit to stalking rhythm.
- [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs): strong base for curated false positives if events remain rare and visibility-aware.
- [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\UI\HudController.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\UI\HudController.cs) and [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameStateManager.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameStateManager.cs): high-impact places to remove prototype language without touching multiplayer.

## Phase 2 - Redesign Plan

### Visual Direction
- Cold, desaturated, underexposed hospital image with localized islands of readable light.
- Stronger contrast between safe reading light and hostile dark pockets.
- Dirty tile, failed paint, rust, leaks, mold, and blood used as layered wear, not random splatter.
- Fog should hide repetition and deepen corridors, not just tint the far clip.

### Gameplay Loop Redesign
- Keep the existing route-based objective spine, but present it as a pressured traversal path instead of a checklist.
- The match should feel like: wake a wing, find access, go deeper, then return through spaces that are no longer trustworthy.
- Maintain 2–3 route variations, but keep the route readable through light, space, and clue text rather than hard UI bookkeeping.

### Monster Redesign
- Monster fantasy: a stalking cadaver-warden that listens, advances, disappears, then commits.
- Sight alone should not always mean full chase. Distant contact should often become an investigate beat first.
- Search must favor doorways, room edges, and last-known lanes rather than broad random wandering.
- Chase should remain dangerous, but rarer and more meaningful.

### Scare System Redesign
- False positives should follow beat logic: space, pause, metal, reveal, silence.
- Visual scares stay rare and need cooldown between major reveals.
- Apparitions always use the real boss identity.
- The aftermath of a scare matters as much as the reveal itself.

### Level Philosophy
- The asylum should read as a sequence of authored pressure spaces, not as a sandbox full of boxes.
- Existing zones stay, but each must carry clearer identity through light and surface treatment:
  - Security / offices: false calm, weak fluorescent pools
  - Archive: cold, tight, obscured aisles
  - Surgery / lab: red clinical violence
  - Service / maintenance: rust, grime, pipe pressure
  - Morgue: cold exposure and finality

### UI Philosophy
- HUD should help with action only.
- If a piece of information can be felt through sound, light, posture, or co-op communication, the HUD should stay quiet.
- Objective text should imply pressure and direction, not list system state.

### Sound, Light, And Silence
- Reduce flat ambient filling.
- Let footsteps, breathing, door movement, and short metallic cues do more work.
- Lower generic chase-music dependence.
- Use silence and low light as pacing tools, not just decoration.

## Phase 3 - Backlog

### Priority 0 - Foundation Fixes
- Add lightweight built-in post-process path
  - Files: [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\HorrorScreenFX.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\HorrorScreenFX.cs), [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Shaders\HorrorScreenFX.shader](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Shaders\HorrorScreenFX.shader), [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)
  - Reason: built-in renderer had no image-level horror pass
  - Expected result: colder, darker, more cohesive image in gameplay and lobby
  - Category: visuals

- Strengthen lighting and fog direction in the main scene builder
  - Files: [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)
  - Reason: flat fill light weakens space and fear
  - Expected result: clearer light islands, deeper darkness, more readable zone mood
  - Category: level / visuals

- Reduce monster over-commit and soft omniscience
  - Files: [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Monster\MonsterAI.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Monster\MonsterAI.cs)
  - Reason: instant chase and broad sensing feel artificial
  - Expected result: more investigate beats, more credible stealth, stronger stalking identity
  - Category: AI / gameplay

### Priority 1 - Horror Experience Upgrades
- Add material wear pass and surface variation in authored zones
  - Files: [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)
  - Reason: repeated clean materials keep the level looking synthetic
  - Expected result: dirty tile, mold, rust, failed paint, and local damage patches that support zone identity
  - Category: visuals / level

- Make scare beats more directed and less scheduler-like
  - Files: [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs)
  - Reason: pacing matters more than scare count
  - Expected result: rarer, cleaner reveals with buildup and cooldown
  - Category: visuals / audio / gameplay

- Reduce UI and prompt exposure
  - Files: [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\UI\HudController.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameStateManager.cs), [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Tasks\*.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Tasks\), [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Player\DownedReviveInteractable.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Player\DownedReviveInteractable.cs)
  - Reason: exposed system text keeps reminding the player they are inside a prototype
  - Expected result: quieter HUD, more atmospheric wording, fewer permanent labels
  - Category: UI / gameplay

### Priority 2 - Polish
- Continue replacing structural cube reads with modular overlays and landmark lighting
  - Files: [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)
  - Reason: the level still partially reads as a dressed blockout
  - Expected result: more believable corridors, thresholds, and room transitions
  - Category: visuals / level

- Push local sound over chase-signaling
  - Files: [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Player\PlayerAudioController.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Player\PlayerAudioController.cs)
  - Reason: generic chase reinforcement weakens grounded fear
  - Expected result: more reliance on space, footsteps, breathing, and silence
  - Category: audio

## Phase 4 - Implemented In This Pass

### 1. Lighting, fog, and post-processing
- Added [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\HorrorScreenFX.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\HorrorScreenFX.cs)
- Added [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Shaders\HorrorScreenFX.shader](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Shaders\HorrorScreenFX.shader)
- Updated [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)

Result:
- gameplay and lobby cameras now get a light image pass with cold grade, low exposure, subtle vignette, grain, and restrained highlight bloom
- hospital render settings now use darker ambient, stronger fog, and lower reflections
- hospital lights now create better contrast and stronger zone accents instead of broad flat fill

Why it improves fear:
- players lose information in darkness more intentionally
- corridors get depth and atmosphere instead of plain dimness
- the image reads as one hostile place instead of default lit geometry

### 2. Material variation and wear
- Updated [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)

Result:
- added wall-damage, dirty-tile, rusted-metal, wood, and fabric style variants
- added atmosphere wear pass with mold, leaks, rust, scorched wall patches, and dirty tile patches across key zones
- added a few extra modular shell overlays so thresholds read less like plain cube cuts

Why it improves fear:
- repetition drops
- rooms gain history and neglect
- the level looks less like a prototype and more like a place that failed violently over time

### 3. Monster stalking simplification
- Updated [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Monster\MonsterAI.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Monster\MonsterAI.cs)

Result:
- distant visibility no longer always means immediate chase
- the monster now often converts first contact into an investigate beat
- investigate and search now linger briefly instead of sliding instantly through states
- search now prefers doorways and patrol-linked spaces before random wandering
- passive sense pulse is restricted to actual tracking states or recent corroborating noise
- chase breaks sooner into search when visibility drops and distance opens up

Why it improves fear:
- the monster feels more like a hunter and less like a finite-state machine
- players get more near-misses and stalking pressure
- hard chase becomes more meaningful because it happens after intent is established

### 4. Directed scare pacing
- Updated [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\FalsePresenceDirector.cs)

Result:
- visual scare categories now have their own cooldown layer
- flash and doorway scares now include short buildup with sound/pause before reveal
- repeated visual spam is reduced further

Why it improves fear:
- reveals feel staged instead of timer-driven
- the player gets buildup, impact, and aftermath instead of flat event cadence

### 5. UI reduction
- Updated [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\UI\HudController.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\UI\HudController.cs)
- Updated [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameStateManager.cs](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameStateManager.cs)
- Updated prompt wording in task scripts under [C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Tasks](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Tasks)

Result:
- local status hides entirely when the player is simply stable
- teammate panel hides when no teammate is in trouble
- interaction prompt hides when nothing is targeted
- objective text now speaks in route/pressure language instead of labeled directive/checklist language
- common prompts sound less like editor labels

Why it improves fear:
- the game stops narrating itself as loudly
- more information comes from the world and the co-op situation
- the player feels less like they are reading systems and more like they are surviving a space
