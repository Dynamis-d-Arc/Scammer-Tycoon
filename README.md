# Scammer Town — Unity prototype

Open this folder in **Unity 6000.3.25f1 (Unity 6 LTS)**. Open `Assets/Scenes/ScammerTown.unity` and press **Play**. Recommended Game view: 1440×900 or 16:10. The UI scales to other aspect ratios with letterboxing.

This is the playable Unity game developed from the original Basement and Neon prototype. The current presentation uses the Pixel Office artwork with individually movable furniture. The original sprite sheets and HTML prototypes remain in the repository for reference. See DEVELOPMENT_HISTORY.md for the categorized changes and branch order.

## Idle gameplay and navigation

Heat, energy and police raids have been removed from gameplay. Old saves clear active raid timers and resume work without penalties. Legacy fields remain only for save compatibility. Old AI subscriptions migrate into free repeating assignments.

- **Apartment** shows a compact business summary. Click an employee or their name above the desk to open their profile.
- **Assignment** inside the profile shows four job cards. Choose one once; it repeats automatically for free, including while visiting other properties. Changing a job queues it after the current one. **Stop after current job** disables repetition without losing current progress.
- **Desk** inside the profile changes workstation assignments. Busy workers finish before desk swaps. Stored desks pause work until placed again.
- **Team** lists employees; selecting a name opens the same profile. Recruitment is a separate shop section.
- **Shop** separates Equipment, Upgrades, Training and Recruitment. Chair upgrades now give 4% job speed per level; computers give 12%, lighting 5%, and décor restores morale while employees work.
- **City / Properties** switches apartments and provides expansion and purchase controls. Basement has low rent; Neon is faster; Harbor pays more. All owned apartments continue working while the game runs. No offline progress.
- **Drag furniture directly in the office** to move it. A short click still opens employee details. **Arrange** reveals rotation and storage controls. Settings contains theme, sound and reset.

Opening goals, cash rewards, employee skills/mood/salaries, daily rent, security incidents, free recovery jobs, room upgrades, and animated city traffic remain. There are no energy costs, automatic rest requirements, heat restrictions, burner purchases, or police penalties. A day lasts 120 seconds.

Start with Alex and one desktop. Click **Alex → Assignment → Inbox shuffle**. Community repairs is guaranteed and can run without a desk. Standard hires cost $750; specialists cost $1,200. Neon costs $3,000; Harbor costs $7,500 after owning Neon and completing ten jobs. Saves migrate without losing money, layouts or existing job progress.

## Project structure

- `Assets/Scripts/TycoonState.cs`: serialized company state, progression and purchase rules.
- `Assets/Scripts/ScammerTownGame.cs`: game lifecycle, save/load and navigation.
- `Assets/Scripts/Management.cs`: employee/job simulation, daily bills, incidents, goals and migration.
- `Assets/Scripts/ManagementUI.cs`: profiles, job cards, upgrades, alerts and audio feedback.
- `Assets/Resources/ScamDen/`: original 1280×5120 sprite sheets.
- `Assets/Editor/ScamDenSheetImporter.cs`: lossless atlas import settings, no external package dependency.
- `Assets/Editor/ProjectSetup.cs`: scene setup, gameplay verification and macOS build commands.
- `Prototype/OriginalUnityImporter.cs.txt`: original sprite slicing importer preserved as reference. Runtime currently draws directly from the atlas, so the 2D Sprite package is not required.

## Verification and build

Editor menu: **Scammer Town → Run gameplay checks**. The 298 checks cover idle assignments, economy, properties, save migration, room expansion, furniture placement and storage, input behavior, and asset imports. **Create or rebuild main scene** replaces the scene with the starter scene; only use this intentionally.

CLI with this project closed in the editor:

```sh
/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$PWD" -executeMethod ProjectSetup.Verify -logFile /tmp/tycoon-checks.log
/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$PWD" -executeMethod ProjectSetup.BuildMac -logFile /tmp/tycoon-build.log
```

Build output: `Builds/Scammer Town.app` (ignored by Git).

## Scope

This version establishes a playable economy and the requested systems. Furniture supports saved, grid-snapped placement. Walking/pathfinding and production balancing remain future work; employees currently work and take breaks at their desks. The interface uses Unity IMGUI to remain self-contained and easy to run without asset-store packages.

## Arranging the room

Drag a desk or piece of equipment directly in the office and release on a clear floor area. A short click on a staffed desk opens the employee profile. **Shop → Workstations → Place** closes the shop and attaches a translucent preview to the cursor; click a green footprint to buy and place it. Red footprints mark blocked positions. You pay only after successful placement. **R** rotates; **Esc** or right-click cancels for free. **Arrange** adds Store selected and Place stored controls. Use the mouse wheel to zoom and middle-drag to pan, including during a purchase preview. Layouts persist independently in each apartment. All furnishings are now individual movable objects: sofa, two shelves, tables, sideboard, lamps, plants, rug, boxes, bin, radiator, and existing equipment. Select an item to reveal Rotate and Store controls. Storage lists each stored object with a preview and its own Place button. Rugs can sit beneath furniture. Walls, windows, doors and built-in wall lights remain architectural scenery.

## Verification

The suite covers recurring assignments, changing and stopping jobs, zero-cash starts, ignored legacy energy/heat, raid migration, save persistence, employee-profile selection, placement, property navigation, recovery, and economic progression. Legacy raid/energy tests were replaced with idle-game behavior checks.

### Office expansion and equipment previews
Use **Expand floor** below the room or in Shop → Equipment. Floors grow in 5-tile steps from 5 × 5 to 100 × 100. The first expansion costs $1,500; later prices scale with floor level. Expansion preserves placed furniture and automatically places overflow equipment, but items deliberately stored stay in storage. Older saves cannot distinguish deliberate storage from overflow until an item is stored again. Shop cards preview the actual workstation artwork and require a clear 2 × 2 space before purchase. Scroll to zoom, drag empty floor to pan, and use **Fit office** to see the entire room.

### Reliability and small windows
New offices start without decorative furniture. Buy sofas, shelves, plants, lamps, rugs and other cosmetic items in **Shop → Decorations**. Each purchase charges the displayed price and adds one item to that office's Storage, even if the floor is full. Placing, storing and moving owned items is free; cancelling placement leaves the purchased item in Storage. Multiple copies are supported, and ownership persists across saves and expansion. Morale upgrades are separate and do not grant free plants.

On migration, previously placed decorations remain owned and keep their positions. Unused automatic starter stock is removed from Storage rather than granted as free purchases. Desks, employee assignments and cash are preserved. Decorative sprites have additional visual clearance so tall furniture is less likely to hide nearby desks.

Stored desks retain their assigned employees and pause desk-dependent work until placed again. Job cycles preserve leftover simulation time. Layout repair runs during initialization and state changes instead of each room draw. Saves use a flushed staging file and atomic replacement with a backup; platforms without replacement support report a save failure rather than overwrite the live save unsafely. In smaller windows the interface keeps at least 85% scale and provides scrollbars to reach the full interface. `ProjectSetup.Verify` includes regression checks for these storage, timing, and save behaviors.

This functionality is included in the current verification suite.

### Simplified game interface
The main navigation is Office, Team, Shop, and City (keyboard shortcuts 1–4; Esc closes panels). Office shows a contextual next step and daily costs. Team supports name search and an unassigned filter. Shop opens a spacious catalog with equipment previews, upgrades, training, and hiring, and shows affordability and space requirements. Connection alerts remain actionable in the catalog. Milestone rewards activate only when complete. Existing saves are preserved. Covered by the current verification suite.

### Full-width office view
The office is the default full-width view with no permanent sidebars. A compact top bar shows funds and status; the bottom dock opens Office, Team, Shop, City, Arrange, and Goals. Office, Team, Goals, and Menu open dismissible panels (Close or Esc). Floor expansion and Fit office remain accessible below the room. Recent activity is inside Goals; saving and settings are inside Menu. Covered by the current verification suite.

Furniture verification: 298 Unity gameplay checks passed, including direct dragging, click versus drag, migration of all furnishings, collision and rug layering, cursor placement, cancellation, charging, storage selection, apartment switching, and save roundtrips. A macOS build was generated successfully. Existing workstation coordinates and progression are preserved; any furnishings that do not fit go into Storage. For a paused editor preview that cannot overwrite progress, use Scammer Town → Preview furniture without saving (Cmd+Shift+J); stop Play mode when done.
