# Nexa 0.1.19 Alpha - Colony Wars Overhaul

Arcade now runs six autonomous colonies from invisible edge territories. All six royals spawn before soldiers, followed by small protective waves. Shared targeting, damage, retaliation, defense, retreat and regroup behavior replaces the former species-specific encounter gaps. Royal deaths eliminate colonies, queue final-hit-credited announcements, and lead to a victory or draw followed by a clean automatic restart.

## Gameplay and pacing

- Queen Ant, Mama Spider, King Roach, Queen Ladybug, King Centipede and Queen Fly have larger silhouettes, species accents, titles, health bars, five seconds of starting protection and cautious movement.
- Default pacing: 80-unit soft cap, 120-unit hard cap, 0.5-second wave interval, approximately 3-5 soldiers per wave. Births are spread across frames. Reinforcements slow as the battlefield fills and stop for eliminated colonies.
- Territories and species order randomize each match. Armies rally around their leader, invade, defend, separate overlapping bodies and converge gradually during long matches to avoid permanent edge retreats.
- Compact scoreboard: living units, leader HP, kills, match number and wins. Wins persist across matches in the running simulation, and reset when the simulation is disabled or reconfigured.
- Add Bugs includes pacing, frame-rate, scoreboard and advanced per-species balance settings. Arcade always includes six species; the species selector applies to decorative modes.
- Old habitat widgets are removed from active layouts and the catalog. Their legacy kinds remain readable for safe workspace/history validation. Unrelated widgets retain their content and identity.

## Default combat balance

| Species | Soldier HP | Damage | Attack cooldown | Speed | Leader HP | Colony limit | Role |
|---|---:|---:|---:|---:|---:|---:|---|
| Ants | 32 | 5.5 | 0.65 s | 76 | 390 | 28 | Ground formations and up to 24% nearby-ally attack bonus |
| Spiders | 44 | 7 | 0.95 s | 86 | 400 | 20 | Hunters with ranged temporary control |
| Roaches | 56 | 7.5 | 1.0 s | 64 | 400 | 22 | 18% damage resistance and reduced web slowing |
| Ladybugs | 44 | 6 | 0.85 s | 66 | 400 | 22 | 14% resistance, queen defense and limited nearby healing |
| Centipedes | 44 | 10 | 1.05 s | 112 | 410 | 13 | Strong melee and short cooldown-limited dashes |
| Flies | 30 | 6 | 0.7 s | 158 | 390 | 24 | Fast aerial squads, smooth flight and hit-and-run movement |

Spider projectiles travel visibly toward one enemy. Control lasts at most 1.6 seconds, with at most 0.35 seconds immobilized, and 3.8 seconds immunity between hits. Roaches receive still shorter effects. Decorative webs have no capture or damage logic, require a living spider, stay at least 155 DIP apart, cap at four and expire after twelve seconds.

## Rendering and reliability

The existing BugManager, overlay, assets and dashboard lifecycle are reused. One shared WPF rendering callback advances Arcade; decorative modes retain the shared timer. Spatial buckets limit neighbor searches, hit records are values, and expired units, effects and announcements are removed. Shared poses are cached at or above their physical monitor resolution and rebuilt after DPI changes. Late rendering reduces cadence to 30 FPS while preserving resolution. Minimizing pauses, and disabling Arcade removes the render subscription and clears the simulation.

## Files changed for this release

- ColonyWars.Models.cs - shared settings, species statistics, colony state and effect types (new).
- ColonyWars.Simulation.cs - armies, combat, abilities, conquest, victory and match lifecycle (new).
- ColonyWars.Rendering.cs - device-resolution pose cache, royal indicators, projectiles, scoreboard and banners (new).
- DashboardBugs.cs - combat entity fields and integration with the existing manager and renderer.
- MainWindow.Bugs.cs - immediate Arcade startup, shared render callback, viewport battlefield, adaptive cadence and cleanup.
- MainWindow.WebSweep.cs - viewport-coordinate decorative web slicing.
- BugHabitats.cs - retirement of automatic stationary habitat creation.
- Widgets.cs - obsolete habitat migration and safe validation without catalog entries.
- Models.cs - pacing and species-stat preference persistence.
- SettingsDialog.Bugs.cs - updated Arcade description and decorative species selector behavior.
- SettingsDialog.ColonyWars.cs - pacing and advanced species balance controls (new).
- App.xaml.cs and StartupVerification.cs - isolated packaged Colony Wars startup probe.
- SessionDashboard.csproj and app.manifest - 0.1.19 version metadata.
- ReleaseNotes.cs, README.md, CHANGELOG.md, VALIDATION.md and RELEASE019.md - release documentation.
- tests/Program.cs - Colony Wars test entry point.
- tests/ColonyWars019Checks.cs - seeded consecutive matches and balance sampling (new).
- tests/ColonyWarsCombat019Checks.cs - all-species combat, all thirty conquest pairings, draws, webs and cleanup (new).
- tests/ColonyWarsSoak019Checks.cs - two-hour accelerated simulation and resource bounds (new).
- tests/ColonyWarsUi019Checks.cs - migration, preferences, editing, lifecycle and performance checks (new).
- Nexa.exe and publish/win-x64/Nexa.exe - self-contained x64 release packages.

## Validation and limits

Run: dotnet run --no-restore --project tests/SessionDashboard.Checks.csproj -p:UseSharedCompilation=false -- --colony019

Final results and artifact locations are recorded in VALIDATION.md. Tests cover combat and lifecycle invariants, eighteen seeded pairs of matches, two simulated hours, preference round-trips, live editing, mode isolation and rendering at the population limit. This is accelerated simulation, not two hours of physical unattended observation. Frame-rate measurements depend on the host and rendering backend; the 60 FPS target is not a guarantee on all monitors. Balance sampling demonstrates competitiveness for the tested seeds rather than equal win rates for every configuration.

The optional on-disk lifetime victory counter is not implemented; wins are counted for the running simulation. Physical mixed-monitor and prolonged hands-on visual checks remain manual. No required core gameplay feature is intentionally deferred.

## Final build and package results

- Build/publish completed successfully with the existing self-contained Windows x64 pipeline. Version: Nexa 0.1.19 alpha.
- Full current suite: 138 checks passed; artifacts: test-output/20261015-174943/. Eighteen seeds completed 36 matches, and the two-hour accelerated run completed another 74 matches with bounded resources.
- At 120 living units: CPU p95 0.1675 ms; forced software rendering p95 23.8496 ms. Live dashboard cadence measured 29.1 updates per second during timed editing in the fallback path.
- Packaged normal startup: test-output/release019-package/startup.json. Packaged Arcade startup: test-output/release019-colony-package/startup.json. Both succeed, report 0.1.19 Alpha and PerMonitorV2, and confirm x64 resources. Arcade has six colonies and ten bugs after the initial 700 ms delay.
- Ready executable: M:/Session Dashboard/Nexa.exe; matching publish copy: M:/Session Dashboard/publish/win-x64/Nexa.exe.
