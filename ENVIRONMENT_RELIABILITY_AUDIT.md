# Environment Reliability Audit

## Phase 1 - Audit

### What causes broken object placement
- [`C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\PlacementSafety.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\PlacementSafety.cs) previously only used a small overlap box plus floor/navmesh sampling.
- [`C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\RoundRandomizer.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\RoundRandomizer.cs) and [`C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameplayBootstrapper.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameplayBootstrapper.cs) passed coarse clearances with no protected route logic.
- Spawn validation did not understand doorways, corridor bottlenecks, or authored traversal lanes.

### What causes clipping and intersection
- Placement had no protected-space concept, so valid floor contact could still place objects inside visually wrong spaces near structural transitions.
- Important hide props were trigger-only interaction shells without meaningful physical blocking geometry in [`C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs).

### What causes blocked entrances and corridors
- Doorways and hub chokepoints were not reserved against random/fallback objective placement.
- Fallback door placement in `GameplayBootstrapper` used generic power-console placement rules instead of door-specific structural rules.

### What must be fixed before jump is enabled
- Protected no-spawn zones for entrances and corridor lanes.
- Stronger placement rules by object category.
- Reliable physical shells on key hide structures so player motion is predictable.
- A minimal jump only after routes remain clear and geometry is no longer intersecting.

### What parts need structural protection volumes
- Room doorways.
- North exit approach.
- Central hub north/south lanes.
- East/west corridor crossings.
- Any future narrow choke where objective spawn offset could choke traversal.

## Phase 2 - Redesign Plan

### Placement validation
- Use category-based rules instead of one generic box test.
- Require floor support, blocking overlap rejection, and protected-space rejection.
- Keep monster placements NavMesh-safe.

### Collision standards
- Important interactables and hide structures need explicit, predictable collider coverage.
- Decorative small clutter stays lightweight; structural or hide-related pieces need stable blocking colliders.
- Avoid oversized invisible blocking volumes by using thin back/side/top shells where players still need usable hide interiors.

### Placement categories
- `Structural`
- `LargeBlockingProp`
- `MediumClutter`
- `SmallDecor`
- `WallMounted`
- `InteractionObjective`
- `Monster`

### Protected spaces
- Protect doorways and chokepoints with explicit no-spawn volumes.
- Keep corridor lanes and exit approaches open.
- Keep fallback runtime spawns from dropping into traversal-critical space.

### Visual logic
- Props must support room function and route readability first.
- Dressing should reinforce horror only after path clarity is preserved.

## Phase 3 - Jump Feature Plan

### Jump philosophy
- Small human jump only.
- No coyote time.
- Cooldown to stop bunny-hop pacing.
- Use landing feedback for weight, not arcade air control.

### Safety constraints
- No jump until protected spaces and reliable blocking geometry exist.
- Do not tune jump high enough to climb hide props, generators, or wall seams.
- Preserve Mirror-safe movement by keeping the existing authoritative local controller and network transform flow.

## Phase 4 - Prioritized Backlog

### Priority 0 - Environment reliability
- Strengthen placement validation
  - Files: [`PlacementSafety.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\PlacementSafety.cs)
  - Reason: current validation was too blind to route-critical space
  - Result: fewer wall clips, fewer invalid spawns
  - Category: level / collision / navigation

- Protect entrances and corridor lanes
  - Files: [`HorrorPrototypeBuilder.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs), [`PlacementProtectionVolume.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\World\PlacementProtectionVolume.cs)
  - Reason: doorways and crossings were vulnerable to spawn drift
  - Result: authored traversal stays open
  - Category: level / navigation

- Use placement rules in randomizer and fallback bootstrap
  - Files: [`RoundRandomizer.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\RoundRandomizer.cs), [`GameplayBootstrapper.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Core\GameplayBootstrapper.cs)
  - Reason: runtime placement ignored structural intent
  - Result: consistent safe placement in both authored and fallback flows
  - Category: level / collision

### Priority 1 - Placement quality
- Add predictable collider shells to key hide structures
  - Files: [`HorrorPrototypeBuilder.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Editor\HorrorPrototypeBuilder.cs)
  - Reason: trigger-only geometry feels fake and hurts movement readability
  - Result: hide props feel physically present without sealing the player out
  - Category: collision / visuals

### Priority 2 - Jump implementation
- Add grounded jump with short cooldown and landing feedback
  - Files: [`NetworkPlayerController.cs`](C:\Users\alka\Documents\project\AsylumCoopPrototype\Assets\Scripts\Player\NetworkPlayerController.cs)
  - Reason: requested panic-movement option after environment stability pass
  - Result: small, grounded jump without parkour feel
  - Category: gameplay / networking
