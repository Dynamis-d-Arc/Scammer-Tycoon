# Development branches and changes

The original repository contained the HTML prototype, an asset kit, and a Unity
sprite importer. The work since that initial commit is grouped below by its
current functionality. Superseded experiments are retained where useful; this
is a categorization of the final implementation, not a reconstruction of every
intermediate version.

## Branch order

These branches are cumulative. Review or merge them in this order, using the
previous branch as the comparison base. `codex/game-checks-and-docs` contains the
complete game, artwork, checks, and documentation. `main` remains the original
asset-kit baseline until these changes are merged.

| Branch | Based on | Contents |
| --- | --- | --- |
| `codex/unity-project-setup` | `main` | Unity 6.3 LTS configuration, package manifest/lock, original atlas migration, texture importer, original importer reference, Git exclusions |
| `codex/isometric-art-assets` | `codex/unity-project-setup` | Pixel Office room/city/workstation/furniture artwork, typing frames, original Soft 3D assets and baker, art direction and generation prompts |
| `codex/idle-gameplay-and-furniture` | `codex/isometric-art-assets` | Playable scene and all interconnected runtime systems: idle economy, employees, properties, navigation, animations, expansion, placement and furniture storage |
| `codex/game-checks-and-docs` | `codex/idle-gameplay-and-furniture` | Gameplay/input/art checks, editor build tools, save-free furniture preview, current instructions and this change map |

The gameplay systems share partial classes and serialized models, so their
runtime files are kept together on one branch to avoid publishing feature
branches that cannot compile independently. The first two branches contain
project/art foundations; the playable game arrives on the third branch.

## Changes by feature

### Unity conversion and project setup

- Converted the original prototype into a Unity 6000.3.25f1 project with a playable
  scene, camera, controller, package lock, and platform settings.
- Moved the original Basement and Neon atlases into Unity Resources, retained
  the HTML prototypes, and preserved the original sprite-slicing importer.
- Added lossless atlas import settings and Unity metadata.
- Excluded caches, generated builds, user settings, and emergency recovery
  scenes from version control. Existing recovery files remain local.

### Idle economy, employees, and progression

- Added a starting employee and workstation, employee profiles, recruitment,
  workstation assignments, skills, morale, salaries, and training.
- Added repeating jobs, queued assignment changes, stop-after-current-job
  behavior, recovery jobs, rewards, and milestones.
- Added daily rent, security incidents, upgrades, and property bonuses.
- Removed heat, energy costs, and police-raid penalties from active gameplay;
  retained legacy fields only for save migration.
- Added persistent saves, backup recovery, migration, and validation. All owned
  apartments keep working while the game runs; offline earnings are not implemented.

### City and property operations

- Added Basement, Neon, and Harbor purchase/progression and apartment switching.
- Fixed returning to the original apartment after selecting Neon.
- Added an isometric city, property markers/cards, moving cars, and moving boats.
- Kept layouts, employees, and operations separate for each apartment.

### Office layout, equipment, and furniture

- Added floor expansion from 5 × 5 through 100 × 100 tiles, zoom, pan, and fit-office
  controls while preserving existing placements.
- Added equipment thumbnails, affordability checks, and space requirements.
- Equipment purchases now attach a placement preview to the cursor and charge
  only when a valid position is confirmed; cancellation is free.
- Added direct furniture dragging, grid snapping, rotation, collision checks,
  and separate handling of a click versus a drag on staffed workstations.
- Separated sofas, shelves, tables, lamps, plants, cabinets, rugs, boxes, bins,
  radiators, and other furnishings into movable objects.
- Added a storage list with thumbnails and individual Place actions. Furniture
  that cannot fit during migration remains owned in storage. Rugs can sit below
  solid furnishings.

### Interface and visual presentation

- Replaced persistent sidebars with a full-width office, compact status bar,
  bottom navigation, and dismissible panels.
- Added contextual employee assignment controls, a searchable/filterable team
  view, organized shop categories, goals, settings, and keyboard shortcuts.
- Replaced the intermediate cream/sage Soft 3D look with the supplied Pixel
  Office direction: detailed wood furniture, warm light, navy shadows, and a
  nighttime isometric city.
- Added visible keyboards, seated workers, typing frames, and animated screens.
- Preserved source artwork, prior art assets, the procedural baker, and generation
  prompts. Current runtime uses the PixelOffice artwork.

### Verification and development tools

- Added 298 Unity checks covering rules, economy, idle jobs, property navigation,
  employee interactions, placement, storage, collisions, migration, save
  roundtrips, camera behavior, and art imports.
- Added editor commands to verify gameplay, rebuild the starter scene, and build
  the macOS application.
- Added a paused furniture preview that suppresses disk saves for safe manual
  interaction testing.
- The final furniture implementation passed all 298 checks and a macOS build.
  Manual Unity checks verified sofa dragging and storing/replacing a bookshelf
  without changing the player's saved progress.

## Using the complete version

Check out `codex/game-checks-and-docs`, open the project in Unity 6000.3.25f1,
open `Assets/Scenes/ScammerTown.unity`, and press Play. See README.md for controls
and validation/build commands. Generated `.app` files and local save data are
not part of the source branches.
