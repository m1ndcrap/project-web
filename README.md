# Project WEB

A Unity 2D framework for building web-slinging action platformers. It gives you a complete,
working movement and combat system: rope-swinging with real pendulum physics, aimed zip-lines,
full wall and ceiling crawling with corner turning, a melee combat system with counters and
combos, three enemy AI types, a multi-phase boss fight, and the level plumbing to tie it together.

It is meant to be a starting point. The code is organised so you can drop in your own art,
levels, and mechanics without rewriting the parts that are hard to get right.

> **Assets:** the code in this repository is free to use. The art, audio, and music are **not**.
> See [Licensing](#licensing) before you use, fork, or redistribute anything here.

---

## Requirements

- **Unity 2022.3.61f1 LTS** (other 2022.3.x versions will very likely work)
- Universal Render Pipeline (2D Renderer)
- No external packages required

## Getting started

1. Clone the repo and open the project folder in Unity Hub.
2. Open `Assets/Scenes/Title Screen.unity` and press Play.
3. `Assets/Scenes/Test.unity` is the sandbox scene, it is the easiest place to experiment.

### Controls

| Action                                     | Key               |
| ------------------------------------------ | ----------------- |
| Move                                       | Arrow keys / WASD |
| Jump, attach to swing point, release swing | Space             |
| Quick-zip to nearest ledge corner          | I                 |
| Aim web / zip (hold), fire with Space      | U                 |
| Attack                                     | O                 |
| Uppercut                                   | L                 |
| Counter                                    | P                 |
| Pause                                      | Enter             |

---

## What's included

**Movement**

- Rope swinging driven by an actual pendulum simulation, not a scripted arc. Reel in and out mid-swing.
- Two zip modes: a one-press zip to the nearest exposed ledge corner, and a free-aimed zip along any direction you point.
- Wall and ceiling crawling with automatic inner and outer corner turning, so the player wraps around geometry smoothly.
- Landing detection with soft and hard landing reactions.

**Combat**

- Target resolution that picks the best enemy in range, respecting facing, line of sight, and live hazards.
- Dash-to-target attacks with hit-pause on impact, combo tracking, and launching uppercuts.
- A counter system: enemies telegraph attacks, the player can counter during the window.
- Swing kicks that sweep the arc ahead of you, and a stationary crawl kick.

**Enemies**

- `RobotStep`, a patrolling ground enemy with hazard avoidance, ledge detection, and evasion.
- `ShockerStep`, a ranged enemy with a scripted chase encounter.
- `GoblinStep`, a multi-phase boss that fights on foot and from a glider, with bombs, spinners, and blocking.

**Level systems**

- Breakable doors, switches, generators, cars, and explosives.
- Colour-coded key doors with a key inventory.
- Hostage rescue objectives with countdown timers.
- Electrical hazards, fire hydrants, barriers that block traversal.

**Presentation**

- Adaptive three-layer music that crossfades by combat intensity.
- Pause menu that captures a screenshot of gameplay and frames it in a comic panel.
- Health bars that follow characters and fade when idle, or stay fixed for bosses.

---

## Project structure

```
Assets/Scripts/
  _Core/          Reusable helpers with no game-specific knowledge
  Player/         Player controller and its split-out components
  Enemies/        Standard enemies
    Boss/         Boss fight, glider, and its projectiles
  Interactables/  Things the player breaks, opens, or collects
  Environment/    Hazards and scenery that react to the player
  UI/             On-screen elements and menus
  Managers/       Scene-wide, mostly single-instance controllers
  Camera/         Camera follow behaviour
  TitleScreen/    Title screen only
```

### `_Core` is the part worth stealing

Nothing in `_Core` knows about the player, enemies, or any specific object. It is safe to copy
straight into an unrelated project.

| File                    | Purpose                                                              |
| ----------------------- | -------------------------------------------------------------------- |
| `AnimationDriver.cs`    | Narrows the Animator API to the handful of calls gameplay code needs |
| `AudioController.cs`    | One-shot sounds with the random-clip picking handled in one place    |
| `CharacterPhysics2D.cs` | Ground checks and nudge-away, shared by player and enemies           |
| `TerrainSensor2D.cs`    | "What's ahead of me?" checks for walking enemies                     |
| `Singleton.cs`          | Base class for one-per-scene managers                                |

### How the player is organised

`PlayerStep.cs` is the main controller and state machine. The pieces that could be cleanly
separated live in their own files and are composed in at runtime:

- `PlayerInputReader` reads the keyboard, and is the only script that touches `Input` directly
- `PlayerGroundMovement` run velocity and landing detection
- `PlayerCrawlMovement` surface-relative crawl velocity
- `PlayerRopePhysics` the pendulum simulation
- `PlayerRopeRenderer` draws the rope from pooled segments
- `PlayerCombatTargeting` decides which enemy is a valid attack or counter target

The swing, zip, and corner-turning code stays inside `PlayerStep` on purpose. Those systems each
depend on the player's live Transform, Rigidbody2D, collider, current state, and the level Tilemap
at the same time. Splitting them out would mean passing eight or nine dependencies into a new
class that reads exactly as coupled as before, so they are kept together and documented in place
instead. The same reasoning applies to each enemy's state machine.

---

## Extending it

**Add an enemy.** Implement `IEnemyBarrier` so the player's blocking and targeting systems
recognise it. Use `CharacterPhysics2D` for grounding and `TerrainSensor2D` if it walks. Copy the
shape of `RobotStep` as a starting point.

**Add a breakable object.** `BreakableSwitch` is the simplest example: a `phase` integer another
script sets to 1, and animation state names exposed in the Inspector.

**Retune the feel.** Most values are serialised. Swing acceleration is on `PlayerStep`, glider
positioning is on `GliderScript`, projectile arcs are on the prefabs.

**Swap the input scheme.** Rewrite `PlayerInputReader` and nothing else needs to change.

---

## Licensing

**This project has two different licences, and they are not the same.**

### Code: MIT

Everything under `Assets/Scripts/` is original work and is released under the MIT licence. Use it
commercially, modify it, redistribute it, no attribution required (though it is appreciated).

### Art, audio, and music: not licensed, do not reuse

The assets in this repository are **not** covered by the MIT licence and are **not** mine to
license. They are included so the project runs as a demonstration. If you build anything with this
framework, **replace them.**

Specifically:

| Asset type                    | Source                                                                                                                          | Status                                                  |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------- |
| Character sprites             | Custom models pre-rendered in Blender                                                                                           | Original renders, but depict characters owned by Marvel |
| Environment and level sprites | Ripped from _Ultimate Spider-Man_ (GBA), AI-upscaled and hand-edited                                                            | Derivative of Activision / Marvel work                  |
| Some sprites                  | Ripped from _Sonic Rush_                                                                                                        | Sega                                                    |
| Sound effects                 | _Sonic Unleashed_, _Spider-Man 2_, _Spider-Man 3_, _Ultimate Spider-Man_, _Spider-Man: The Movie_, plus some royalty-free clips | Mixed, mostly Activision / Sega                         |
| Music                         | _Ultimate Spider-Man_, _Spider-Man: Shattered Dimensions_                                                                       | Activision / Marvel                                     |

Spider-Man, Green Goblin, Shocker, and related characters are trademarks of Marvel Characters,
Inc. Sonic the Hedgehog is a trademark of Sega. This project is an unofficial fan work, is not
affiliated with, endorsed by, or sponsored by Marvel, Disney, Activision, Insomniac, or Sega, and
no ownership of their intellectual property is claimed.

---

## Credits

Framework, gameplay code, and character models by the repository owner.

Ripped assets sourced via [The Spriters Resource](https://www.spriters-resource.com/) and
[The Sounds Resource](https://www.sounds-resource.com/). Original credit belongs to the
respective development teams.
