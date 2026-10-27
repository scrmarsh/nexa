# Nexa 0.1.21 Alpha ? implementation and validation

The main menu now has the tiny **I just wanna play with bugs** link beside the Settings gear. It opens a battlefield with no dashboard widgets and resumes the same local Infinite Warfare world used by dashboard Arcade. Repeated clicks reuse the window. Opening it does not create a Space, alter widgets, or restart the simulation.

## Implementation status

| Area | Status and behavior |
| --- | --- |
| Infinite Warfare | Implemented continuous world time, no automatic round reset, surviving armies, dominance events, bounded new civilization emergence. Classic Rounds remains a separate transient mode. |
| Local saving | Implemented schema 1 snapshots, deterministic random state, atomic validated replacement, previous-save backup, explicit recovery and confirmed reset. Paused state survives restart. |
| Strategic warfare | Implemented home guards, patrol/scout/reserve/expeditionary roles, strength/resource readiness, rivalries, preparation, rally, raids, larger invasions, occupation detachments, withdrawal and recovery. Normally one organized campaign, at most two. Simplified strategy, not a complete diplomacy system. |
| Territory and conquest | Implemented 12?8 ownership grid, sustained superior occupation, functional settlement ownership, surviving defenders, income/reproduction consequences, homeland reclaim and refuge rebuilding. Controlled conquest tests pass; natural conquest frequency still requires tuning. |
| Territory overlays | Implemented subtle faction colors, exterior borders, labels, contested markings and settlement glyphs, with independent settings and cached frozen drawing data. Default opacity 10%. |
| Spider webs | Implemented seeded irregular branching geometry, shared construction/repair, links, physical strand collisions, brief traps, immunity, spider hunting and cutting. Rendering and collision use the same partially built strand endpoints. |
| Dashboard scrolling | Implemented content-relative transforms, viewport clipping and inverse pointer conversion in both Arcade rule sets. Shared-world coordinates remain fixed. Widgets retain input priority. |
| Standalone Battlefield | Implemented shared controller, no widgets/HUD, pan, zoom, center, civilization follow, selected-unit follow, pause/resume, maximize/F11, backup/reset and return. Uses ordinary maximization rather than exclusive fullscreen. |
| History and identity | Implemented persisted faction IDs, names/colors/species, individual identities/stats, current military state and the up to 2,048 recent events. Relevant events carry faction/opponent/unit/region/settlement references. Full lifetime archive and deceased-warrior biographies remain incomplete. |
| Settings | Implemented rule selection, campaigns/aggression/frequency/emergence, territory presentation, web construction/cap/trapping, and shared-world controls. Existing species/budget settings remain available. Visual-only settings do not regenerate the world. |

## Save and runtime architecture

Production saves live at **%LOCALAPPDATA%\SessionDashboard\arcade-world.json**, alongside the existing app state. The previous valid save is **arcade-world.json.bak**. Isolated test/preview stores deliberately use their own directory. The fixed default world is 1600?1000 world units, independent of either window size.

One dispatcher controller owns simulation advancement for each canonical save path. Dashboard and battlefield render that manager; they never independently advance the shared world. Standalone has activity priority when both views are open. The world stops when no participating surface is active, and explicit pause applies to both. There is no offline catch-up. Classic/decorative simulations stay separate from this save.

Snapshots include units and combat references, leaders and succession state, reserves/resources, strategy/roles/timers, territory and settlements, faction identity/color archives, real web plans/work/damage/links, projectiles, wave/emergence counters, recent history and explicit PRNG state. Autosaves occur every 15 seconds of active time; pause, detach/minimize and orderly close also save. Serialization uses a captured snapshot; background writes do not mutate the simulation.

Writes use a temporary file, flush, read-back validation, then atomic replacement with backup. Corrupt or incompatible data blocks advancement and autosaving. A valid backup is offered explicitly; unreadable originals and selected recovery points are archived. Reset requires confirmation and saves current valid progress before replacing it. Another world ID cannot silently overwrite an existing world. Abrupt termination can lose progress since the last successful save; this is not a transactional database or a multi-process merge system.

## Controls and defaults

- Main menu: **I just wanna play with bugs**. Add Bugs settings also offers **Open Battlefield**.
- Battlefield: right/middle drag pans; wheel zooms about the cursor; Center camera fits the map. Follow dropdown tracks a civilization; Follow a bug changes left-click to selection. Turn it off for normal bug interaction. F11/Maximize toggles maximization.
- Pause, reset and restore operate on the same shared world from either view. Closing/returning retains progress.
- Default population budgets remain 150 soft / 180 hard, with 24?28 visible units per species. Configurable species limits support 30?35 within the global cap.
- Default shared-web maximum is 4, adjustable 1?8. A web has a bounded irregular plan (50 base edges, up to 56 with links); contributors are bounded. Unattended webs expire.
- Territory has 96 cells and at most 24 settlements. Land occupation needs at least three units for 10 territory ticks; nest districts need four for 20 ticks plus strength superiority and adjacency or an organized offensive objective.

## Files created for this update

Application: ArcadeRandom.cs; ArcadeWorld.Controller.cs; ArcadeWorld.Models.cs; ArcadeWorld.Store.cs; BattlefieldWindow.cs; ColonyWars.Infinite.cs; ColonyWars.Persistence.cs; ColonyWars.Strategy.cs; ColonyWars.Territory.cs; ColonyWars.TerritoryRendering.cs; DashboardBugs.Camera.cs; SettingsDialog.ArcadeWorld.cs.

Checks: tests/Infinite021Checks.cs; tests/NaturalWeb021Checks.cs; tests/Territory021Checks.cs; tests/Strategy021Checks.cs; tests/Endurance021Checks.cs; tests/Battlefield021Checks.cs. Documentation: RELEASE021.md.

## Existing files modified for this update

HomeWindow.cs; App.xaml.cs; MainWindow.Bugs.cs; MainWindow.WebSweep.cs; MainWindow.Continuity.cs; DashboardBugs.cs; DashboardBugs.Interaction.cs; DashboardBugs.Arcade.cs; ColonyWars.Models.cs; ColonyWars.Simulation.cs; ColonyWars.Civilizations.cs; ColonyWars.Webs.cs; ColonyWars.Rendering.cs; SettingsDialog.Bugs.cs; SettingsDialog.ColonyWars.cs; SessionDashboard.csproj; app.manifest; ReleaseNotes.cs; tests/Program.cs; tests/ColonyWars019Checks.cs; tests/ColonyWarsUi019Checks.cs; tests/Arcade020Checks.cs; README.md; CHANGELOG.md; VALIDATION.md; CODEX_SESSION_STATE.md.

Several existing application files were already untracked or modified before this update. This list describes the v0.1.21 work, not ownership of every entry in git status. No pre-existing changes were reset.

## Validation

Validation includes the new Infinite Warfare suite and the retained Classic/Arcade regressions. Reproduce with PowerShell from the repository root:

~~~powershell
dotnet build SessionDashboard.csproj -t:Rebuild --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
dotnet build tests/SessionDashboard.Checks.csproj --no-restore -p:BuildProjectReferences=false -p:UseSharedCompilation=false
dotnet tests/bin/Debug/net10.0-windows/SessionDashboard.Checks.dll --war021
dotnet tests/bin/Debug/net10.0-windows/SessionDashboard.Checks.dll --colony019
dotnet tests/bin/Debug/net10.0-windows/SessionDashboard.Checks.dll --arcade020
~~~

Focused routes --battlefield021 and --render021 isolate view and software-render checks. Test screenshots and JSON reports are placed in timestamped test-output directories. The endurance suite runs one 75-minute world plus five 10-minute worlds (125 simulated minutes total), checks bounds and real unprotected combat, then compares exact resumed state for each developed world. Separate protected fixtures isolate strategy lifecycle and web construction; those are not evidence of natural combat balance. Attack samples are global active-animation counts, not spatially clustered battles.

The initial native software-render probe exposed expensive per-region opacity groups. Cached translucent brushes reduced the same workload from roughly 350?400 ms to roughly 33 ms without reducing native artwork resolution. The 180-unit check includes 96 territory cells and one developed web; it does not establish worst-case performance with eight webs, mixed DPI or maximum zoom. Physical mixed-monitor interaction remains a manual validation task.

## Final measured results

- Clean application rebuild and check-project build: **0 warnings, 0 errors**.
- **92 Infinite Warfare checks passed**: test-output/20261019-194158. This includes local serialization/replay, backup and corruption recovery, pause/reset, multiple views, scrolling, natural web gameplay, territory capture/reclaim, strategy lifecycles and continuous simulation.
- **138 Classic Colony Wars checks passed**: test-output/20261019-194052. The two-hour simulated soak completed 52 matches, with peak 113 entities, two queued announcements and 18 projectiles. Live dashboard cadence was 28.9 updates/second while accepting 38 timed text edits.
- **105 retained Arcade checks passed**: test-output/20261019-193927. This includes the existing combat, recovery, population, web and dashboard/counter regression coverage. The recovery-rule fixture now protects its subject from unrelated combat; web assertions describe the new irregular construction.
- Six unprotected Infinite seeds simulated 125 minutes in total, produced 20,580 attacks, peaked at 155 entities and retained continuous world identity. Each developed world resumed exactly through 120 further steps. Controlled migration separately validates new identities after extinction.
- Natural nest ownership changed twice (one Ladybug and one Centipede conquest/reclaim); this verifies functioning invasion occupation, while also showing that conquest remains rare. Other species did not record nest captures in these seeds. Campaign victories also include successful raids and should not be read as nest captures.
- At 180 units with 96 territory cells and one developed web: simulation CPU p95 **0.44 ms**, forced software rendering p95 **34.22 ms**. This meets the checked 40 ms responsive fallback budget; it is not a universal 30/60 FPS guarantee. Continuous simulation CPU p95 ranged 0.36?0.42 ms.
- Normal and Arcade executable startup probes both exited 0, reporting 0.1.21 Alpha and PerMonitorV2; Arcade initialized six civilizations and ten bugs. Evidence: test-output/startup021-final/startup.json and test-output/arcade-startup021-final/startup.json. No diagnostic app process was left running.
- Home-menu, battlefield and 180-unit render screenshots were visually inspected. The actual menu button click and repeated open/close/rebind paths are tested.

Observed campaign outcomes across these seeds (raid victories and nest ownership changes are different measures):

| Species | Launched | Withdrawn | Battle victories | Nest captures/reclaims |
| --- | ---: | ---: | ---: | ---: |
| Ants | 46 | 45 | 0 | 0 |
| Centipedes | 28 | 23 | 4 | 1 |
| Ladybugs | 30 | 27 | 3 | 1 |
| Flies | 49 | 49 | 0 | 0 |
| Roaches | 25 | 19 | 5 | 0 |
| Spiders | 3 | 3 | 0 | 0 |

Two-species active-attack samples: 729/1332; three-or-more-species samples: 4/1332. These count simultaneous attack flashes globally, so separate fights may share a sample. Peaceful/scouting/recovering faction samples: 29018/40045. This supports quieter intervals without proving spatial battle quality.

## Remaining limits and unfinished work

Milestones 1?6 have working implementations and focused coverage. Integration/regression and documentation are completed for this bounded alpha implementation; the broader simulation vision remains partial.

- Natural nest conquests are uncommon in seeded default runs. Controlled occupation, defense and reclaim work; default invasion strength, commitment, capture duration and home defense need further balance work. No claim that every species is equally effective.
- Scouting is a movement role; there is no fog of war or persistent intelligence map. Rivalries influence targets, but formal alliances, diplomacy and rebellions are not implemented.
- Territories use a coarse fixed grid. Resources are a bounded ledger with passive income; there are no physical gathering routes or general obstacle pathfinding. Refuge rebuilding is simplified.
- At most one active faction per species (six slots). Emergence uses a new identity in an extinct slot only when at most two factions remain and a safe founding location is available. It is not an unlimited simultaneous civilization generator.
- History retains the up to 2,048 recent events and relevant identity metadata. There is no full Chronicles UI, unlimited lifetime archive, complete historical warrior biography, Hall of Legends or 500-name library.
- Standalone has manual and follow cameras, but no automatic cinematic spectator. Exclusive fullscreen is deferred. Camera position is view-local rather than part of the civilization save.
- Saves support this schema with validation; future schema migrations and multi-process conflict resolution are not implemented. Cloud storage, accounts and online synchronization remain out of scope.

Recommended next session: add a repeatable natural occupation scenario, tune major invasions against intact defenses until conquests and recovery happen at an appropriate rate, then broaden multi-web/high-zoom and mixed-DPI performance checks. Extend historical archival before presenting the simulation as preserving every event forever.

## Build and packaging

Version metadata and in-app release notes are 0.1.21 Alpha. The executable built by the commands above is **bin\Debug\net10.0-windows\Nexa.exe**. Launch it from PowerShell with:

~~~powershell
& '.\bin\Debug\net10.0-windows\Nexa.exe'
~~~

Repackaging completed on October 19, 2026 using the existing Publish-Exe.ps1 process: self-contained Windows x64, single-file Release executable. Both publish/win-x64/Nexa.exe and root Nexa.exe are version 0.1.21.0 and byte-identical (145,089,334 bytes). Normal and Arcade startup probes both exited 0 and reported 0.1.21 Alpha with PerMonitorV2; Arcade initialized six civilizations and eleven bugs. Evidence: test-output/package021.json, test-output/package021-startup/startup.json and test-output/package021-arcade/startup.json. The older Nexa-0.1.19.zip remains unchanged; no new ZIP was requested. No NuGet configuration or dependencies were changed.

Launch the repackaged executable with:

~~~powershell
& '.\Nexa.exe'
~~~
