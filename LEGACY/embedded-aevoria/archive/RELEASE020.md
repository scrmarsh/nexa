# Nexa 0.1.20 Alpha - Arcade armies and shared webs

This session extends the existing WPF Colony Wars simulation. Other modes, widgets and session storage retain their existing paths.

## Inspection findings

- All six species already had combat statistics, melee damage, spatial neighbor filtering and retaliation. Nearby threats could be superseded by strategic leader targets; ladybugs could abandon an acquired pursuit. Retreats lacked a bounded re-engagement interval.
- The six-unit counts are initial leaders and maximum wave size, not a faction army limit. Previous faction maxima were 13-28 and the global budget was 80/120. Defaults now allow 24-28 per faction and 150/180 globally; the global maximum is configurable up to 240, faction maxima up to 35. Previously saved explicit budgets remain respected.
- Current Arcade bypasses the legacy habitat encounter path. Its spider webs were non-colliding decorations with a 12-second lifetime. The retired habitat/nest widgets are not reintroduced.
- Leader death forcibly killed every soldier. There was no colony reserve, survivor succession or faction identity/history model.

## Implemented behavior

Combat keeps nearby targets and valid retaliation, defends attacked leaders, discards dead units and uses the existing spatial grid. Retreats last 2.5 seconds with a 10-second interval before another voluntary retreat. Survivors regroup at home after leader loss for up to 30 seconds; after that, remaining troops resume fighting. Strategic objectives track surviving armies when leaders die, preventing marches toward empty nests. Friendly and protected damage does not fabricate retaliation or kills. Simultaneously selected melee hits still resolve. Attack strokes retain the actual struck entity; centipede movement alone does not produce a damage stroke.

Ant support bonuses, roach armor and escape preference, fly aerial regrouping and limited spider projectiles remain. Ladybugs sidestep between attacks and heal at most two allies per cooldown, avoiding the old mass-healing multiplication. Centipedes have 56 health and 12 damage and pass an isolated duel against an armored roach. Injured ordinary units recover slowly near home when no enemies are nearby.

Spiders seek inset construction sites, share nearby radial structures, patrol them and contribute work over time. Up to three builders accelerate construction. Each web grows to four connected closed rings with ten spokes, at most 50 drawn strands; a simulation holds at most four shared webs. Workers repair cut sections after another construction interval. Unattended structures expire after 90 seconds. Built strands briefly slow or stun enemies, followed by four seconds of immunity; roaches resist control. Spiders prioritize nearby slowed prey. Existing cut gestures and batched DrawingContext rendering remain in use.

Reinforcements cost one reserve unit; colonies start with 90 and regenerate 0.25 per second while their leader lives, up to 90. The global rendered budget includes fading bodies. Leader death preserves soldiers and reserves and stops spawning. Non-ant reproductive adults are explicitly marked at birth. An eligible healthy survivor may become leader after 12 uninterrupted safe seconds within 100 pixels of home, spending 25 reserves. Recovery is limited to once per colony and preserves the survivor's unit ID, name, kill count and health fraction. Ordinary ants cannot replace their queen. A faction becomes extinct only after its last living member dies.

Faction IDs remain unique across matches in the running simulation. Names use an approximately 85/15 single-word/title selection with no active duplicates. The initial libraries include 20 civilization names, four army titles and 24 warrior names. Every individual has an identity; battlefield labels appear for leaders and warriors with three confirmed kills. Five confirmed kills award an earned title while preserving the original name. Faction and individual kill counts increment only once per actual death.

The bounded in-memory history records founding, leader loss, retreat, recovery, extinction, final battle victory and legendary achievements. Records include match number, simulation seconds, faction IDs, opposing IDs when applicable and unit IDs when applicable. Active display names resolve at presentation time; retired names remain available while referenced by retained events. The history retains at most 2048 records and survives automatic match restarts, but clears when the simulation is cleared or reconfigured.

## Files changed in this session

- ColonyWars.Models.cs: population defaults, colony state, damage attribution and event models.
- ColonyWars.Simulation.cs: targeting, retaliation, retreat, resources, survivor defeat handling and confirmed kills.
- ColonyWars.Civilizations.cs (new): naming, bounded history, reserve regeneration and recovery.
- ColonyWars.Webs.cs (new): cooperative web construction, repair, patrol and collision control.
- ColonyWars.Rendering.cs: faction names, leader identities and accurate attack strokes.
- DashboardBugs.cs: entity identity fields, squad extension point and shared web rendering.
- SettingsDialog.ColonyWars.cs: configurable global budgets up to 240.
- SessionDashboard.csproj, app.manifest, ReleaseNotes.cs: target 0.1.20 release metadata.
- tests/Arcade020Checks.cs (new), tests/Program.cs: focused 0.1.20 checks and command route.
- tests/ColonyWars019Checks.cs, tests/ColonyWarsCombat019Checks.cs, tests/ColonyWarsUi019Checks.cs: update obsolete version/decorative-web expectations and increase rendering workload to 180 living units.
- README.md, CHANGELOG.md, RELEASE020.md: implementation and validation notes.

## Verification

Builds use the installed .NET 10 SDK and cached assets. The initial normal build encountered stale WPF generated intermediates, and implicit restore could not read the restricted user NuGet.Config. A rebuild without restore and without shared compilation succeeds without changing dependencies:

~~~powershell
dotnet build SessionDashboard.csproj -t:Rebuild --no-restore -p:UseSharedCompilation=false
dotnet build tests/SessionDashboard.Checks.csproj --no-restore -p:BuildProjectReferences=false -p:UseSharedCompilation=false
dotnet tests/bin/Debug/net10.0-windows/SessionDashboard.Checks.dll --arcade020
dotnet tests/bin/Debug/net10.0-windows/SessionDashboard.Checks.dll --colony019
~~~

The focused suite exercises six-species acquisition, chase, cooldowns, retaliation, leader defense, dead targets, retreat, protected damage, individual kill credit, leader-loss survival, recovery requirements, identity preservation, extinction, cooperative web growth, trapping, immunity expiry, repair, population budgets, six seeded scenarios and the existing UI regression checks. A two-hour simulation soak completes repeated matches with bounded entities, projectiles, history and announcements.

The final pre-handoff performance sample uses 180 living units: CPU p95 approximately 0.4 ms and WPF software-render p95 approximately 27 ms, within the existing 30 FPS fallback budget. A separate established-colony test reaches all 152 default faction slots. Live editing, settings, minimize/resume, unrelated counters and decorative Low/Normal/Infestation modes are checked. These measurements describe this machine and do not guarantee 60 FPS on every device.

Normal and Arcade startup verification from bin/Debug/net10.0-windows/Nexa.exe succeeds in isolated state directories, confirms version 0.1.20, PerMonitorV2 awareness, four presets, populated session controls and all three embedded Bible translations. Arcade starts all six colonies and troops. The root Nexa.exe, ZIP and publish directory have not been repackaged by this session; launch the newly built executable to review these changes.

Visual QA confirms closed connected web rings, inset placement, readable faction names and separate leader labels in generated WPF screenshots. Manual validation remains appropriate for physical mixed-DPI monitors, drag-cutting webs over real dashboard widgets and lower-end hardware.

Final results: both projects build with zero warnings and zero errors. The final focused suite passes 105 checks; the inherited Colony Wars regression suite passes 138 checks. The final focused run is in test-output/20261019-170829, with 52 matches completed in 7200 simulated seconds, a peak of 113 total entities, and 180-unit CPU/render p95 values of 0.37/26.30 ms. The six-seed sample completes twelve consecutive matches. Full logs are test-output/arcade020-run.log and test-output/colony020-regression.log; executable startup evidence is in test-output/startup020-exe and test-output/startup-colony020-exe.

## Deliberate limits and next session

- Full squads and partial squad casualties are deferred. RepresentedMembers remains 1 as an explicit extension point, so casualties are individual and never multiplied.
- Persistent civilization saves, a 500-name library and a Chronicles UI are deferred. History is bounded run-only data.
- Nest capture/destruction events are absent because this implementation has no active nest capture/destruction system; no synthetic events are emitted.
- Recovery is a simple, bounded adult-succession rule, not a full sex/genetics or reproduction simulation. Recovery thresholds and web limits are code constants; population and species combat values remain adjustable in existing settings.
- Match balance still favors roaches and ladybugs in the soak sample. Ant and fly colonies participate in combat but did not win the final two-hour sample. Equal species win rates are not established.
- The recommended next session is a broader seeded balance study focused on ant formations and fly attack/escape efficiency, followed by persistent history and colony saves. Validate manual cutting and mixed-monitor rendering before producing a new distributable.
