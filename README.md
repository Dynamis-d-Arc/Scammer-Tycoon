# Scammer Town — Unity prototype

Open this folder in **Unity 6000.6.5f1**. Open `Assets/Scenes/ScammerTown.unity` and press **Play**. Recommended Game view: 1440×900 or 16:10. The UI scales to other aspect ratios with letterboxing.

This is the playable Unity game developed from the original Basement and Neon prototype. The current presentation uses the Pixel Office artwork with individually movable furniture. The original sprite sheets and HTML prototypes remain in the repository for reference. See DEVELOPMENT_HISTORY.md for the categorized changes and branch order.

## Idle gameplay and navigation

### Main focus: the scam empire

The game opens on the **Scam Board**, with six schemes targeting fictional townspeople: Phantom Giveaway, Mirage Storefront, Moonshot Club, Velvet Auction, Echo Tickets and Crown Collectibles. **Assign crew** assigns everyone with a placed desk in the current office; current jobs finish before switching, and the chosen scam repeats automatically. Close the board to watch the office, or reopen it with **Scams / 1**. Employee profiles allow individual assignments in a scrollable list.

Scam proceeds fund desks, recruits, skills and additional offices. The first two milestones now require a scam run and $1,000 in scam proceeds. Moonshot and its contract require five scam runs; Harbor requires ten, Neon ownership and $7,500. Partial takes count as runs and contribute only their actual payout. Voucher bonuses and contract rewards do not advance these scam counters. Suspicion still triggers the existing automatic break.

Old saves retain claimed rewards, active work, money, ownership and layouts. Because earlier saves combined repairs and scams, migration preserves up to ten previous completions and $1,000 of previous income as separate milestone/unlock credit. New scam statistics begin at zero and record only subsequent scam runs; repeated loads never add more legacy credit.

Community repairs, repair specialists and free odd jobs have been removed. All work now requires a placed desk. New hires queue Phantom Giveaway and wait if no desk is available; wages still apply. Existing repair specialists become Phantom Giveaway specialists at no cost, retaining skill and salary. Repeating repair assignments become Phantom Giveaway assignments. Unfinished repair shifts and odd jobs are cancelled without a payout; completed earnings remain. A queued scam or a previous stop instruction is preserved. Old odd-job timers are ignored and omitted from new saves.

Verification: all **578 Unity gameplay checks passed**, including old-save migration, removed payout paths, deskless hires, scam timing, and the retained card game and furniture bonuses.

### Optional features retained

The following features remain available as requested.

| Feature | Relationship to the main loop | Consequence if removed |
| --- | --- | --- |
| The Call (replaced the laptop voucher game) | Optional hands-on close for any running scam: bigger takes, less suspicion | Runs become fully idle again. |
| Cosmetic furniture: coffee tables, sideboards, moving boxes, filing cabinets, water coolers, bins and radiators | Room decoration only | Less shop clutter and furniture variety; existing owned items need a refund or migration policy. |
| Decorative furniture bonuses: lamps, shelves, plants, sofa and rug | Indirect speed/morale support | Simplifies office management but changes production balance. |
| Lighting themes and animated city traffic | Presentation only | Changes the atmosphere, with no effect on scam progression. |

Crew, desks, scam skills, suspicion, contracts and property expansion directly support the scam loop. Furniture placement also determines whether desks can work, so removing all arrangement controls would require automatic placement.

### Specialties, décor bonuses and contracts

- In an employee profile, spend $300 to train an idle employee in any unlocked scam. Matching jobs run 15% faster and gain 5 percentage points of success chance. Recruit a chosen specialist for $1,200 at skill 3 and $60/day. Assignment cards show the actual duration, payout, suspicion and chance including bonuses. A running job keeps its original duration.
- Only placed décor grants office-wide bonuses: lamps and shelves add 2% speed each, capped at two of each (+8% total). Plants restore 0.03 morale/second each, capped at two; one sofa and one rug each add 0.04/second, for a 0.14/second furniture cap. Stored furniture grants nothing; existing morale upgrades remain separate. The decoration shop lists each effect and employee profiles show current office totals.
- Open **Contracts** beside Goals. Choose five Phantom Giveaway, four Mirage Storefront, or three Moonshot Club scam runs in 120 seconds for $400, $700, or $1,000. Moonshot requires five completed scam runs. Only the accepting office contributes; partial results count and ordinary earnings continue. Completion pays automatically once. There is no entry fee or failure penalty, and a 30-second cooldown follows completion, expiry or abandonment. Contracts continue while browsing or visiting another property, pause with the game, and save their remaining simulation time. No offline time is consumed.

Energy remains removed. Police raids return through Detective Plodd's case file (see *Underworld* below). Raid timers from older saves are cleared on load and never resume. Old AI subscriptions migrate into free repeating assignments.

- **Apartment** shows a compact business summary. Click an employee or their name above the desk to open their profile.
- **Scams** inside the profile shows all six schemes and their unlock requirements. Choose one once; it repeats automatically for free, including while visiting other properties. Changing a job queues it after the current one. **Stop after current job** disables repetition without losing current progress.
- **Desk** inside the profile changes workstation assignments. Busy workers finish before desk swaps. Stored desks pause work until placed again.
- **Team** lists employees; selecting a name opens the same profile. Recruitment is a separate shop section.
- **Shop** separates Equipment, Upgrades, Training and Recruitment. Chair upgrades now give 4% job speed per level; computers give 12%, lighting 5%, and décor restores morale while employees work.
- **City / Properties** switches apartments and provides expansion and purchase controls. Basement has low rent; Neon is faster; Harbor pays more. All owned apartments continue working while the game runs. No offline progress.
- **Drag furniture directly in the office** to move it. A short click still opens employee details. **Arrange** reveals rotation and storage controls. Settings contains theme, sound and reset.

Opening goals, cash rewards, employee skills/mood/salaries, daily rent, security incidents, room upgrades, and animated city traffic remain. There are no energy costs, automatic rest requirements, heat restrictions, burner purchases, or police penalties. A day lasts 120 seconds.

Start with Alex and one desktop. Choose **Scams → Assign crew to Phantom Giveaway**. Scams have no entry fee, so a staffed, placed desk can earn even at zero cash. Standard hires cost $750; specialists cost $1,200. Neon costs $3,000; Harbor costs $7,500 after owning Neon and completing ten scam runs. Saves preserve money, layouts and existing scam progress.

## Scam den: marks, the live feed and town reactions

Every scam run now targets a fictional mark with a name, a portrait, a type (Greedy, Lonely, Gullible, Skeptical, Show-off, Bored) and something they've been talked into believing. The mark is chosen when the run starts and saved on the worker (`markSeed`), so it survives reloads. Marks are presentation only: payouts, timing and odds are unchanged.

- **Scam Board.** The board opens with the *Scammer Town Gazette*, then an **On the hook** wall of polaroids for current runs, joined by red string, with progress bars. The six schemes are pinned case files showing take, partial take, suspicion and town wariness. Clicking the **Marks board** in the office also opens it.
- **On the line feed.** The top-right of the office narrates each run through four hook stages, then the payout or a partial ("Doris smelled a rat. Only $90."). It also reports suspicion thresholds, cooldowns and each day's headline. Commentary is rate-limited so large crews don't flood it, and loading or switching offices never replays a backlog. **Hide** collapses it.
- **Crew chatter.** Desks show speech bubbles a few seconds out of every nine, matched to the stage of the call. They turn paranoid at high suspicion and hushed during cooldowns, and every result gets a reaction plus a floating cash pop. Desk labels show who is calling whom ("Alex → Gerald P.").
- **Town reactions.** Gazette headlines follow suspicion, town wariness of overused schemes and the town calendar. At 75+ suspicion the walls flash red and blue; during a cooldown the room dims. In the city, patrol cars with roof lights appear at 40+ suspicion (two at 75+), and hot properties flash a siren light on their marker. The burner-phone table rings mid-call.
- **Den gear** (Shop → Den & décor). Bought into Storage and effective only when placed. Each bonus counts once per office: paper shredder ($350, 10% less suspicion), cash counter ($600, +5% successful take), police scanner ($450, automatic cooldowns 5s shorter). The pallet of cash ($250) is cosmetic. Art lives in `Assets/Resources/PixelOffice/denprops.png` and is regenerated with `python Tools/denprops.py <output.png>`.

`MarksChecks` covers deterministic marks, save roundtrips, the run-finished event, feed throttling and isolation, chatter, headlines, den-gear bonuses and caps, the den atlas, the marks-board click and patrol counts. **Scammer Town → Build Windows player** writes a development build to `Builds/Windows/`.

## Underworld: the Call, the detective, dirty money and the Syndicate

These systems add real but recoverable risk. Nothing ever takes property, desks, upgrades or clean money.

- **The Call.** Open a crew member's profile (**Take the call**) or click **Take the call** on a polaroid on the Scam Board. The run pauses while you talk. Each turn, pick one of three approaches: Charm, Urgency, Proof or Sweeten. Each mark type has favorites, and the mark's line hints at them. Repeating yourself costs trust and adds doubt. Fill Trust to 100 to **close**: 1.5–2× the success take, half the suspicion, and less town wariness. Let Doubt reach 100 and the mark **hangs up**: partial take, 1.5× suspicion, more wariness. After six turns the mark stalls and the run carries on as normal. Calls have a 45s cooldown. Closing the call screen hands the call back with no penalty. The voucher card game is gone; open voucher sessions in old saves end without a payout.
- **Detective Plodd's case file** (top bar; Town & risk). Every run adds to it (20% less with burner phones), as does every automatic cooldown (+10), snitches (+12) and Syndicate tips (+8). Dirty cash over $15,000 also adds to it. It fades while every office stays under 40 suspicion. At 75% a raid is scheduled against the hottest office, with a 90s warning (120s with a police scanner). A banner appears, and you can respond:
  - **Shred evidence** (free with a shredder, otherwise $400): halves the seizure; case file −10.
  - **Ditch the burner phones**: nobody is arrested.
  - **Go dark**: the office stops working until the raid; no arrests, half the seizure, a 20s shutdown instead of 60s.
  - **Bribe the desk sergeant** ($2,500, +$1,500 per previous bribe): cancels the raid; case file drops to 40%.

  An unanswered raid seizes 50% of dirty cash, that office's burner phones and any pallet of cash. It arrests one crew member for 240s (bail costs $500 + 10× daily salary) and shuts the office for 60s. Unbanked Moonshot pots are lost as with any shutdown. Burner phones are sold in packs of three ($300) in Shop → Den & décor.
- **Dirty money.** Scam takes, Moonshot cash-outs and contract payouts arrive dirty; milestone, rank and race rewards are clean. The top bar splits clean and dirty money. Rent, wages, hires, gear, training and upgrades spend dirty money first. Neon, Harbor, floor expansions, fronts and the Harbor renovation need clean money. Buy **fronts** in the City or Money panel. Each launders a daily amount for a 10% fee: Suds & Duds Laundromat ($2,500 clean, $1,500/day), Sparkle Car Wash ($8,000, $4,000/day), Totally Legit Pizza ($20,000, $10,000/day). Saves store `dirty` as the part of `cash` still to be washed, so older saves start fully clean.
- **Crew loyalty** (top of each profile). Loyalty starts at 70 and moves each payday. It goes up when crew are paid in full and happy and with salary above $35. It goes down with low mood, payroll shortfalls, custody, and skill 4+ crew paid under $60. A raise (+$10/day) adds 12 loyalty; a $100 bonus adds 5; bail adds 10; coming back unbailed costs 15. Below 30, crew may tip off the detective. Below 20, once you have 25 runs, the Velvet Syndicate courts them; a courted crew member leaves at the next payday unless you win them back. Your last crew member never leaves.
- **The Velvet Syndicate** (Goals). From 25 runs, Vivienne Velvet makes a move every two days. She may undercut your busiest scheme (−20% for a day; running quotes are kept), tip off the detective, offer to launder up to $5,000 of dirty cash at a 35% cut (60s to accept), or court your least loyal crew. A truce ($3,000) buys three quiet days; winning a race buys two.
- **Story.** Claiming each empire rank shows a short story beat. The Gazette now also covers raids, warrants, the detective's file and Syndicate knock-offs.

`UnderworldChecks` covers all of the above: call outcomes and payouts, raid scheduling, counterplay, seizures, custody and bail, dirty and clean spending, laundering fees, loyalty, snitching, poaching and every Syndicate move, plus save validation and migration.

## Expanded scam progression and visible upgrades

| Scheme | Unlock in this office | Base success / time / successful take | Base suspicion |
|---|---|---|---|
| Phantom Giveaway | Starting scheme | 80% / 12s / $220 | 4 |
| Mirage Storefront | Starting scheme | 60% / 24s / $600 | 7 |
| Moonshot Club | 5 completed runs | 45% / 40s / $1,250 | 12 |
| Velvet Auction | 10 runs + computers level 1 | 68% / 30s / $1,050 | 8 |
| Echo Tickets | 20 runs + computers level 2 | 48% / 22s / $1,450 | 15 |
| Crown Collectibles | 35 runs + computers level 3 | 40% / 55s / $3,600 | 22 |

Run progress is shared across the empire; required computers belong to each office. New contracts require 3 Velvet, 3 Echo or 2 Crown results in 120 seconds and pay $1,200, $1,800 or $2,600. Partial results still count. All six schemes have recruitable specialists and focus/expertise branches (14 skill nodes total). Existing mastery keeps its original three expertise prerequisites and grants its global bonus to all six schemes.

**Shop → Office upgrades** now has eight three-level tracks. Computers visibly progress from starter equipment to flat screens/slim laptops, dual-screen setups, and triple-screen command desks, including empty desks, occupied desks, storage and shop previews. All four crew appearance sheets and all four rotation columns have matching equipment variants. Upgraded workstations use a pulsing activity indicator; starter workstations retain their two typing poses. Floor footprints remain 2×2.

The four new tracks are headsets (+2 success points per level), office network (+8% speed per level), deal room (+8% successful take and +5% suspicion per level), and break room (+0.04 morale/sec per level and 3s shorter automatic breaks per level). Three operating styles trade speed against suspicion: balanced; cautious (20% less speed, 30% less suspicion); high pressure (20% more speed, 10% more successful take, 35% more suspicion). Duration changes start with the next job; payout and suspicion changes apply at the next result.

Version-three saves retain all existing equipment, skills and assignments. Pre-version-two repair index 3 is retired before migration; version-three index 3 is Velvet Auction and is never treated as an old repair job. Version-two saves retain their three original jobs. New upgrades default to zero and operating style defaults to balanced.

Generated assets live at `Assets/Resources/PixelOffice/equipment_{crew,vest,beanie,leather,empty}.png`; built-in image-generation prompts are in `EQUIPMENT_UPGRADE_PROMPTS.json` and `EQUIPMENT_REPAIR_PROMPTS.json`. **Scammer Town → Verify and render upgrades** runs gameplay/asset checks plus room and equipment previews, exporting `Logs/visuals/upgrades-desktop.png` and `upgrades-laptop.png`. Rows show tiers 0–3; columns show empty desk, original crew, vest, beanie and leather. **Preview upgrades without saving** (Ctrl+Shift+U) provides a separate six-worker test state with $50,000 and 40 runs; stop Play mode to return to normal. It cannot overwrite the player's save.

## Project structure

- `Assets/Scripts/TycoonState.cs`: serialized company state, progression and purchase rules.
- `Assets/Scripts/ScammerTownGame.cs`: game lifecycle, save/load and navigation.
- `Assets/Scripts/Management.cs`: employee/job simulation, daily bills, incidents, goals and migration.
- `Assets/Scripts/ManagementUI.cs`: profiles, job cards, upgrades, alerts and audio feedback.
- `Assets/Scripts/Marks.cs`: fictional marks, hook stages, crew chatter, Gazette headlines and portraits.
- `Assets/Scripts/ScamDenUX.cs`: Scam Board marks wall, live feed, speech bubbles and siren effects.
- `Assets/Scripts/TheCall.cs`, `Heat.cs`, `Laundering.cs`, `Crew.cs`, `Syndicate.cs`, `Underworld.cs`: the Call, the case file and raids, dirty money and fronts, loyalty, the rival, and shared validation and story.
- `Assets/Scripts/UnderworldUI.cs`: call screen, top-bar readouts, raid and offer banners, Heat/Money/Syndicate panels, city fronts and the story modal.
- `Assets/Resources/ScamDen/`: original 1280×5120 sprite sheets.
- `Assets/Editor/ScamDenSheetImporter.cs`: lossless atlas import settings, no external package dependency.
- `Assets/Editor/ProjectSetup.cs`: scene setup, gameplay verification and macOS build commands.
- `Prototype/OriginalUnityImporter.cs.txt`: original sprite slicing importer preserved as reference. Runtime currently draws directly from the atlas, so the 2D Sprite package is not required.

## Verification and build

Editor menu: **Scammer Town → Run gameplay checks**. The gameplay checks cover idle assignments, economy, properties, save migration, room expansion, furniture placement and storage, input behavior, and asset imports. **Create or rebuild main scene** replaces the scene with the starter scene; only use this intentionally.

CLI with this project closed in the editor:

```sh
/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$PWD" -executeMethod ProjectSetup.Verify -logFile /tmp/tycoon-checks.log
/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath "$PWD" -executeMethod ProjectSetup.BuildMac -logFile /tmp/tycoon-build.log
```

Build output: `Builds/Scammer Town.app` (ignored by Git).

## Scope

This version establishes a playable economy and the requested systems. Furniture supports saved, grid-snapped placement. Walking/pathfinding and production balancing remain future work; employees currently work and take breaks at their desks. The interface uses Unity IMGUI to remain self-contained and easy to run without asset-store packages.

## Arranging the room

Drag a desk or piece of equipment directly in the office and release on a clear floor area. A short click on a staffed desk opens the employee profile. **Shop → Workstations → Place** closes the shop and attaches a translucent preview to the cursor; click a green footprint to buy and place it. Red footprints mark blocked positions. You pay only after successful placement. **R** rotates; **Esc** or right-click cancels for free. **Arrange** adds Store selected and Place stored controls. Use the mouse wheel to zoom and middle-drag to pan, including during a purchase preview. Layouts persist independently in each apartment. All furnishings are now individual movable objects: sofa, two shelves, tables, sideboard, lamps, plants, rug, boxes, bin, radiator, and existing equipment. Select an item to reveal Rotate and Store controls. Storage lists each stored object with a preview and its own Place button. Rugs can sit beneath furniture. Walls, windows, doors and built-in wall lights remain architectural scenery.

## Verification

The suite covers recurring assignments, changing and stopping jobs, zero-cash scam starts, ignored legacy energy/heat, raid migration, save persistence, employee-profile selection, placement, property navigation, retirement of repair/odd-job work, and economic progression. Legacy raid/energy tests were replaced with idle-game behavior checks.

### Office expansion and equipment previews
Use **Expand floor** below the room or in Shop → Equipment. Floors grow in 5-tile steps from 5 × 5 to 100 × 100. The first expansion costs $1,500; later prices scale with floor level. Expansion preserves placed furniture and automatically places overflow equipment, but items deliberately stored stay in storage. Older saves cannot distinguish deliberate storage from overflow until an item is stored again. Shop cards preview the actual workstation artwork and require a clear 2 × 2 space before purchase. Scroll to zoom, drag empty floor to pan, and use **Fit office** to see the entire room.

### Reliability and small windows
The starter office has 5×5 usable cells across its wood floor. Expansion adds repeated wood-floor and wall sections; doors, windows, wall height and foundation depth retain a constant scale relative to desks and people. The camera refits automatically after expansion, and Fit office frames the floor and walls together. Furniture coordinates and ownership are unchanged. Large offices show more floor at once; zoom in to inspect individual workers.

Occupied workstations use the new `Assets/Resources/PixelOffice/scam_crew.png` atlas: dark hoodies, caps, call-center headsets, phones and cash bundles. Desktop and laptop workers retain idle/typing frames, and empty desks retain the original artwork. The built-in image-generation tool produced this atlas; the full prompt set is saved in `CREW_ART_PROMPTS.json`. Workstation artwork has been enlarged from 65% to 90% of its original tile-relative size for readability, while its placement footprint stays 2×2.

Three additional crew sheets add a burgundy-waistcoat operator, teal-beanie techie and silver-haired leather-jacket caller. Existing employees and new hires automatically receive a mix of looks, saved per employee so they persist when swapping desks or reloading. The new sheets use built-in image generation; prompts are in `CREW_VARIATION_PROMPTS.json`.

Room surfaces are assembled once per office size into a cached render target, retaining the existing pixel-art source textures. **Scammer Town → Verify and render room proportions** runs the gameplay suite and exports 5×5, 10×10, 25×25 and 100×100 previews to `Logs/visuals/`. This rendering check needs a graphics device; omit `-nographics` when running `RoomVisualChecks.VerifyAndRender` from the command line.

New offices start without decorative furniture. Buy sofas, shelves, plants, lamps, rugs and other items in **Shop → Decorations**. The shop shows whether each item grants a capped bonus or is cosmetic. Each purchase charges the displayed price and adds one item to that office's Storage, even if the floor is full. Placing, storing and moving owned items is free; cancelling placement leaves the purchased item in Storage. Multiple copies are supported, and ownership persists across saves and expansion. Morale upgrades are separate and do not grant free plants.

On migration, previously placed decorations remain owned and keep their positions. Unused automatic starter stock is removed from Storage rather than granted as free purchases. Desks, employee assignments and cash are preserved. Decorative sprites have additional visual clearance so tall furniture is less likely to hide nearby desks.

Stored desks retain their assigned employees and pause desk-dependent work until placed again. Job cycles preserve leftover simulation time. Layout repair runs during initialization and state changes instead of each room draw. Saves use a flushed staging file and atomic replacement with a backup; platforms without replacement support report a save failure rather than overwrite the live save unsafely. In smaller windows the interface keeps at least 85% scale and provides scrollbars to reach the full interface. `ProjectSetup.Verify` includes regression checks for these storage, timing, and save behaviors.

This functionality is included in the current verification suite.

### Simplified game interface
The main navigation is Scams, Team, Shop, and City (keyboard shortcuts 1–4; Esc closes panels). Office shows a contextual next step and daily costs. Team supports name search and an unassigned filter. Shop opens a spacious catalog with equipment previews, upgrades, training, and hiring, and shows affordability and space requirements. Connection alerts remain actionable in the catalog. Milestone rewards activate only when complete. Existing saves are preserved. Covered by the current verification suite.

### Full-width office view
Close the Scam Board to see the full-width office with no permanent sidebars. A compact top bar shows funds and status; the bottom dock opens Scams, Team, Shop, City, Arrange, Contracts, and Goals. Office, Team, Goals, and Menu open dismissible panels (Close or Esc). Floor expansion and Fit office remain accessible below the room. Recent activity is inside Goals; saving and settings are inside Menu. Covered by the current verification suite.

Furniture verification: 298 Unity gameplay checks passed, including direct dragging, click versus drag, migration of all furnishings, collision and rug layering, cursor placement, cancellation, charging, storage selection, apartment switching, and save roundtrips. A macOS build was generated successfully. Existing workstation coordinates and progression are preserved; any furnishings that do not fit go into Storage. For a paused editor preview that cannot overwrite progress, use Scammer Town → Preview furniture without saving (Cmd+Shift+J); stop Play mode when done.
