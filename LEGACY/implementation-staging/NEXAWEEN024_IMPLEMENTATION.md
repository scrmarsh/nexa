# Nexa v0.1.24 Alpha — NEXAWEEN

Your dashboard just got a little spooky.

## Delivery and verification

The current build is `M:/Nexa/Nexa.exe`, a self-contained Windows x64 executable. Its version remains **0.1.24**. The portable publish copy is `publish/win-x64/Nexa.exe`. Splash and home display **nexa v0.1.24 alpha**, with green branding; NEXAWEEN remains the release identity and changelog title.

The core, new ecosystem and animation milestones are implemented. Release compilation passed with **zero warnings and zero errors**. Eight verification routes passed **259 checks** on October 27, 2026:

| Route | Checks |
|---|---:|
| Weekly timer, editor and productivity UI | 23 |
| Reliability, zones, history and notifications | 19 |
| Core lifecycle, settings and seasonal structure | 64 |
| Continuity, background saves and recovery protection | 42 |
| Shared widget layout and normal appearance | 10 |
| Menus and settings integration | 7 |
| New ecosystem, persistence, modes and controls | 47 |
| Species anatomy, gait, motion and effect lifecycle | 47 |

Logs: `verification/release-acceptance`. Isolated published-startup verification passed: x64, version 0.1.24, requested lowercase label, per-monitor-v2 awareness, 759 pixel cells, four default presets, and all three embedded Bible translations. Package hash and archive location are recorded in `verification/release-package.json`.

This is a tested local candidate, with the manual and remaining polish limitations below. It is not a claim that every advanced detail in the master prompt has been exhaustively completed or verified.

## Core features and fixes

- Central Notes & History contains readable reports, notes, tasks, counters/water, pins, timer activity, provider summaries/references and productivity. Search, date/preset filters, archived-note editing, text export and confirmed deletion never replay an old session. Widget history uses the same workspace service.
- Configurable 30-second, 1-, 3-, 5-, 10-, 15- and 30-minute checkpoints apply immediately. Five minutes is the default. Debounced/event-driven saves operate separately from checkpoints. Save Now flushes without clearing or duplicating the current session.
- Background writes use detached captures, serialization and revision protection. Failed writes retain dirty state and retry. Close flushes edits. Recovery reconciles time boundaries using actual timestamps.
- Session completion preserves the dashboard. The five-minute review checkpoint updates its existing record. Warning, postponement and save-before-reset protect the next session's five-minute pre-start reset. Permanent data, layouts, presets and archived history remain.
- The direct Pixel Timer editor supports recurring weekday classes, shortcuts, start/end/duration and saved presets. Per the user's override, **digits count up during class while pixels deplete**; between classes both count down to the next occurrence. Deterministic pixel indexing, overnight/week boundaries, time-zone changes and DST calculations are covered.
- Global automatic/manual time-zone selection supports Windows/IANA identifiers and selected-zone schedule/display calculations. Historical instants retain UTC identity. Progress displays exactly three decimals without premature 100.000%.
- Canvas scrolling retains a viewport of empty space beneath widgets, with placement/drag scrolling and contextual Add Widget. The fixed footer no longer reserves space. Native work-area bounds handle maximization; F11 uses true fullscreen bounds.
- Floating notifications wrap, stack, suppress duplicates and remain above the window's lower edge. Tips rotate around 12 seconds and can be disabled independently. Autosave uses the actual saving/success/failure states and one transitioning notification; routine notices are optional, errors remain visible.
- Session Timeline remains dedicated to manual/Hedy/recording notes and interval summaries. Productivity Meter is separate. Scoring continues while its optional widget is hidden, prevents repeated-toggle farming and retains the bounded 20-second 2× Bug Frenzy.
- Bugs Squashed is offered only in Settings → Add Bugs, with direct-user session and all-time totals. A durable queue retries squash history writes without duplicating scores or losing the original session/timestamp.

Principal files: `PixelTiming.cs`, `MainWindow.ClassSchedule.cs`, `NexaReliability.cs`, `DashboardContinuity.cs`, `MainWindow.Continuity.cs`, `MainWindow.Reliability.cs`, `MainWindow.NotesHistory.cs`, `MainWindow.Productivity.cs`, `MainWindow.TimeWidgets.cs`, `WidgetActivity.cs`, `SessionContent.cs`, `SettingsDialog.Reliability.cs`, `NexaNotifications.cs`, `NexaWindowBounds.cs`, `Models.cs`, and the affected widget/integration/settings partials.

## Nexaween

- Automatic October 1–November 2 activation uses the selected time zone; On/Off overrides work year-round. Normal appearance preferences survive seasonal disablement.
- The 0.1.24 changelog alone has its purple background, green text, orange border and six requested section titles. Older release history remains intact.
- Density-limited decorations include pumpkins, ghosts, witches, webs, varied candy, buckets, leaves, skulls, slime and iron cauldrons. Placement excludes controls and leaves animation margins.
- Six independent overlays: candy-collector ants, haunted-web spiders, vampire ladybugs, zombie roaches, skeleton centipedes and phantom flies. Appearance preferences remain independent.
- The main/splash/header/executable branding is green. Wine is green and bubbly, beer purple, shots orange. The old Aevoria home launcher is removed.

Principal files: `Nexaween.cs`, `Nexaween.Animation.cs`, `Theme.cs`, `ReleaseNotes.cs`, `BugVisuals.cs`, `NexaLogo.cs`, `DrinkArtwork.cs`, branding assets and splash/home partials.

## New bug ecosystem

- Cozy gathering/rest/social behavior, Creepy hiding/scuttling/occasional hunting, and Combative territorial defense/raids alter actual activity. No Classic Rounds, Infinite Warfare, old battlefield or Living Worlds dependency returns.
- Normal, Cozy and Creepy drawings use distinct shape/shading treatments. Match Appearance to Behavior, manual override, Adorable Mode, 75–400% size and Performance/Balanced/High quality persist.
- One authoritative local world is shared by dashboard and ecosystem views. Nests/habitats, resources, personalities, webs, positions and paired carrying persist. World pause, backup restore and confirmed reset are functional.
- Low/Normal/High density caps are 12/36/96 creatures. Simulation/render cadence is bounded at 15/30/45 Hz; offscreen simulation is throttled. Dashboard projection follows scrolling/resizing; widget interiors are protected from decorative hitboxes.
- Legacy Aevoria/Colony Wars/Living Worlds source and obsolete output artifacts are retained under `M:/Aevoria-old/Nexa-embedded-20261026`. The separate `M:/Aevoria` project was not edited.

Principal files: `NexaEcosystem.Models.cs`, `NexaEcosystem.Store.cs`, `NexaEcosystem.Simulation.cs`, `NexaEcosystem.Window.cs`, `SettingsDialog.Bugs.cs`, `MainWindow.Bugs.cs`, `DashboardBugs.cs`, `BugOverlay.cs`, `BugHabitatVisuals.cs`.

## Animations and species accuracy

- Insects have six articulated legs, spiders eight. Insect walking uses alternating tripod groups; spiders use coordinated leg groups attached to the cephalothorax. Movement advances gait from actual distance, with bounded smooth turning.
- The modeled stone-centipede form has 15 leg pairs, traveling leg waves, a flexible segmented body and specialized trailing ultimate legs. Rest uses a loose curve, not a millipede spiral.
- Worker ants stay wingless. Ants have antenna movement and paired candy carrying; roaches have faster scuttling/long probing antennae. Spiders have short pedipalps rather than insect antennae, and build webs at their working location.
- Flies have one functional wing pair plus halteres, land to feed and groom, and do not carry food like ants. Ladybugs expose the abdomen and open separate wing covers over a functional hindwing pair. A translucent blur represents rapid flight; the effect does not pretend a slow flap is the real wingbeat.
- Cartoon eyes ease toward nearby activity, blink, rest closed and react to hits. Natural eyes do not acquire cartoon pupil tracking. Frozen body-only assets avoid duplicate static and animated legs/wings. Phantom wing colors follow the animated pair; hover shadows stay on the ground.
- Seasonal effects include gentle pumpkin light/wobble, floating/reactive ghosts, flying witches/trails, bubbling cauldrons, steam/slime and bounded ambient particles. Sounds are opt-in.
- Optional task/timer/note/point/milestone feedback and seasonal witchfire use a reusable overlay, capped at eight effects/72 particles. Saving icons rotate only while a persistence operation is running; completion stops rotation. Quick-tip bulbs have a gentle yellow pulse.
- Reduced motion respects the application option and Windows animation preference. Decorative timers stop on unload/minimize/disposal; overlays do not intercept controls.

Principal files: `BugAnimation.cs`, `BugVisuals.Animation.cs`, `BugArtwork.cs`, `NexaMotion.cs`, `NexaFeedback.cs`, `MainWindow.Feedback.cs`, `Nexaween.Animation.cs`, `NexaNotifications.cs`, `InterfaceAudio.cs`, `tests/Nexa024AnimationChecks.cs`.

The renderer is stylized, not a biological physics model. Research anchors and scope are documented in `ANIMATION_NOTES.md`.

## Reliability and data preservation

The normal LocalAppData workspace location and owner identity remain unchanged. Existing backups/migrations are reused. The world uses its own versioned, owner-validated atomic local file and recoverable backup. Rendering does not write individual frames to storage. Production data was not used as a test fixture; the old candidate is closed normally and its saved profile is copied before the new build is launched. No historical game archive was overwritten.

The save-race regression now waits up to five seconds for actual I/O and isolates the independent edit debounce. It verifies both the old capture on disk and newer edits remaining dirty, rather than assuming disk writes always finish in 100 ms.

## Outstanding issues and manual acceptance

- Physical sleep/hibernate, forced power interruption, multi-monitor mixed-DPI movement, taskbar/fullscreen behavior across Windows configurations, and long-running memory/GPU performance still need manual/system testing.
- All autosave intervals are checked for configuration/application/persistence; continuous typing was timed at 30 seconds, not for each interval's full duration.
- Live Hedy/account connections, microphone capture/transcription and actual provider requests were not exercised with personal credentials. Synthetic history/provider data verifies local integration.
- Leg motion is procedural planar animation, not full 3D surface-conforming foot locking. Cooperative carrying is paired movement with a shared load; advanced coordinated lifting, detailed multi-creature choreography, and further species-specific nest-construction animation remain polish work.
- Offline measurement: 48 six-species 960×620 frames including PNG encoding took about 1.45 seconds in this run. A 400-second/36-creature synthetic simulation took about 85 ms CPU wall time. Neither measurement establishes interactive FPS, GPU load or extended-session performance.
- Isolated UI screenshots and startup checks passed. An end-to-end real class, resume cycle and final user visual acceptance remain outstanding.

No installer, online release, Git commit or push was created.
