# Nexa v0.1.24 Alpha — NEXAWEEN implementation report

October 27, 2026. Version remains **0.1.24**. Seasonal tagline: **Nexa — A space for whatever haunts you next.**

## Verification

Release build passed with **zero warnings and zero errors**. Ten acceptance routes passed **335 checks** after the LEGACY reorganization:

| Route | Passed checks |
|---|---:|
| followup-design | 30 |
| followup | 46 |
| master | 23 |
| reliability | 19 |
| core | 64 |
| continuity | 42 |
| shared | 10 |
| menus | 7 |
| ecosystem | 47 |
| animation | 47 |

Local detailed evidence is in the ignored `verification/final-024-acceptance` directory. `tools/Verify.ps1` reproduces these routes against isolated stores. No personal dashboard data is a test fixture. A fresh build of the staged Git snapshot also passed with zero warnings/errors, and all 17 binary source assets were checked for byte-exact Git storage. Packaged-startup evidence and executable SHA-256 are recorded locally under `verification/followup-package` after publication.

## Root causes and functional changes

- **Dynamic time/progress:** `PixelTiming.cs` provides phase boundaries, captured UTC clock instant, elapsed/remaining fraction and deterministic pixel counts. Session pixels fill from the top-left; intersession pixels clear from the bottom-right. The Pixel Timer and linked bars share the same reading. Independent bars have their own ranges. Missed recurrences use the latest class end, and timezone rebasing honors the schedule's local calendar day.
- **Editing:** the Pixel Timer editor owns weekday selection, one-time dates and duration. Preset timing applies through the persisted in-place edit path, preserving report identity and content. Refresh is aligned to wall-clock second boundaries, with actual timestamp differences rather than accumulated ticks.
- **Dropdown clicks:** panel drag detection previously treated popup items as blank drag surfaces. Combo/list items and scroll surfaces are now excluded. The shared editable ComboBox template also has its required editable text part. First-open behavior selection is initialized after seasonal mapping so it reflects the actual world mode. Wheel handling accumulates trackpad deltas and limits discrete steps; inactive controls yield to page scrolling.
- **Population/visibility:** selecting a species no longer leaves the ecosystem cap occupied by incompatible species. Lower density trims the maintained population. Widget-edge routing keeps creatures outside opaque panels; unchanged clipping/projection geometry is reused while world coordinates remain independent of scroll offsets.
- **Session navigation:** the purple Session toolbar button opens scheduling, Notes & History, saving and tips. The duplicate global Session tab and global weekday/summary controls are removed. Timeline owns its persisted summary interval. Tip settings navigation scrolls and highlights the bottom control.
- **Settings:** Appearance and Effects are consolidated; Sounds is separate. A native pumpkin opens Nexaween settings while an independent accessible red/green switch toggles the season. Locked Appearance is explanatory/clickable, with ghost artwork and an underlined disable link. Normal preferences are preserved.
- **Saving/history:** existing debounced/event-driven saving, all seven checkpoint options, manual Save Now, stale-write protection, readable reports, review checkpoint, reset postponement and save-before-reset remain. The continuity route includes an actual 30-second continuous-edit test. Autosave transitions remain tied to persistence; notices are left-aligned and tips right-aligned.
- **Score/streak:** a direct squash starts a two-minute 1.5× streak; subsequent valid squashes add 0.5 and refresh expiration up to the configured cap. Timestamps survive restart and expire across resume. Delayed historical events cannot overwrite newer streak timestamps. Duplicate/toggle farming remains bounded. Explicitly configured distraction/alcohol deductions persist, including signed lifetime totals.

## Nexaween and ecosystem

- Seasonal vector branding is derived from the same `AccentBrush` as the checked Task checkbox, **#A855F7** in the existing seasonal palette. Original normal assets remain available. The icon is rendered at seven native sizes rather than enlarging a small bitmap.
- Home retains the existing 55 ms typewriter cadence, hold/fade/restart timing. Only “haunts” is orange. Splash uses static wording. Normal wording and branding restore when the season is disabled.
- Startup no longer has a 2.4-second fake loading delay. Actual settings/data validation, appearance preparation, Home construction and content readiness drive a hair-thin stage progress indicator. Disk work runs on a worker and WPF work is explicitly marshaled to its dispatcher. 100% follows Home rendering, with a failure timeout.
- Four modes—Classic, Cozy, Creepy and Combative—drive both behavior and appearance. Legacy override fields remain readable for compatibility but no longer create competing UI settings. Fresh normal mode is Classic; fresh seasonal mode is Cozy/Adorable. Separate seasonal/nonseasonal choices persist.
- Six species retain native appearance families and seasonal costumes. Insects have six articulated legs, spiders eight, and the modeled stone-centipede form has 15 leg pairs with traveling waves. Fly/ladybug flight uses appropriate wings, visible lift, hovering/short-flight/rest behavior and persisted altitude. Natural eyes do not receive cartoon pupil tracking.
- Spider-generated webs remain outside the decoration system. They develop progressively, use irregular geometry, retain owner/construction state and follow persisted widget anchors. Prebuilt background/habitat webs are removed.

## Minimal attachment API

`NexaweenDecorations.Update(..., IReadOnlyList<Rect>? widgetTargets = null)` is backward compatible. Existing callers may omit the new argument.

- `occupied` is collision-only and is copied internally; the caller's list is not mutated.
- Only `widgetTargets` identifies actual widget attachment locations. Reserved regions are never inferred as widgets.
- Missing/empty targets skip contextual art and clear previous attachments gracefully.
- Target rectangles participate in placement invalidation independently of occupied rectangles.
- Actual transformed widget rectangles are supplied by the existing canvas layout. Scroll offsets do not accumulate into attachment positions; drag/resize and placement completion refresh the existing rectangle path.
- Pumpkins and curved vines attach at suitable widget corners, stay behind card controls, and never intercept input. Collision checks include reserved UI. Background placement uses ten candy kinds; spiders own their separate webs.
- No widget-attachment metadata class or configuration subsystem was introduced.

## Animations and feedback

The prominent lightning score and centered multiplier use subtle streak pulses and surrounding witchfire with orange accents. Confirmed persisted positive/negative scoring events drive a coalesced green/red perimeter glow. Task/timer/note/milestone feedback, animated saving arrows, tip bulbs and bounded seasonal effects reuse the existing services. Reduced-motion options, lifecycle cleanup, offscreen throttling and effect/population budgets remain.

## Principal files changed

- Timing/editing: `PixelTiming.cs`, `MainWindow.ClassSchedule.cs`, `MainWindow.PresetTiming.cs`, `MainWindow.Session.cs`, `MainWindow.TimeWidgets.cs`, `MainWindow.Widgets.cs`, `MainWindow.xaml.cs`, `NexaSelection.cs`, `DarkControls.xaml`.
- Settings/navigation: `SettingsDialog.cs`, `SettingsDialog.Navigation.cs`, `SettingsDialog.Session.cs`, `SettingsDialog.Reliability.cs`, `SettingsDialog.Bugs.cs`, `MainWindow.Settings.cs`, `MainWindow.Navigation.cs`, `NexaPanels.cs`, `NexaChrome.cs`.
- History/scoring: `NexaReliability.cs`, `DashboardContinuity.cs`, `Models.cs`, `Widgets.cs`, `MainWindow.Reliability.cs`, `MainWindow.Feedback.cs`, `NexaFeedback.cs`, `NexaNotifications.cs`, `WidgetActivity.cs`.
- Ecosystem/layout: `DashboardBugs.cs`, `BugNavigation.cs`, `BugOverlay.cs`, `NexaEcosystem.Models.cs`, `NexaEcosystem.Simulation.cs`, `NexaEcosystem.Store.cs`, `NexaWebGeometry.cs`, `BugAnimation.cs`, `BugVisuals.Animation.cs`, `BugHabitatVisuals.cs`, `MainWindow.Bugs.cs`, `MainWindow.WidgetPlacement.cs`, `Nexaween.cs`, `Nexaween.Placement.cs`, `Nexaween.Animation.cs`.
- Branding/startup: `NexaBranding.cs`, `NexaLogo.cs`, `Theme.cs`, `SplashWindow.cs`, `HomeWindow.cs`, `HomeWindow.About.cs`, `App.xaml.cs`, native assets and application metadata.
- Verification: current `tests/`, including `NexaFollowupChecks.cs` and `NexaFollowupDesignChecks.cs`; `tools/Verify.ps1`.

## Repository cleanup and GitHub

The canonical repository is [scrmarsh/nexa](https://github.com/scrmarsh/nexa). Active updater metadata/download allowlists use that canonical path, with synthetic tests rejecting unrelated repositories. The Windows x64 executable is distributed through the [0.1.24 Alpha GitHub release](https://github.com/scrmarsh/nexa/releases/tag/v0.1.24-alpha), with a SHA-256 checksum. Repository visibility is unchanged. See the [root audit](ROOT-AUDIT.md) for the subsequent cleanup and retained compatibility files. Its existing main history is retained. `LEGACY/` preserves obsolete embedded-game code, old checks, notes and one-time maintenance drafts and is excluded from compilation. Active compatibility migration code stays active. The separate Aevoria project was not modified.

Build outputs, local executable bundles, recordings, credentials, personal profiles, diagnostic captures and recovery backups are ignored. Active source, required assets, tests, publishing configuration and documentation are the repository deliverable. See `LEGACY/README.md` and its manifest for the historical organization.

## Remaining manual acceptance and limits

- Physical hibernation/power interruption, mixed-DPI/multi-monitor taskbar/fullscreen behavior, actual mouse/trackpad interaction across devices, and extended memory/GPU sessions require manual/system verification. The Windows UI capture helper was not reliable enough to count direct physical-input checks as passed; routed control/selection/persistence tests passed.
- Hedy/account integrations, microphone permissions/capture/transcription and real provider calls were not tested with personal credentials. Confirmed provider-completion scoring remains wired after successful actions; synthetic history tests cover local behavior.
- All autosave intervals are checked for configuration/application/persistence; only the 30-second interval was exercised for its full continuous-typing duration.
- Locomotion is stylized planar procedural animation, not a full biological/3D physics model. Detailed cooperative lifting, surface-conforming foot locking and further multi-creature choreography remain polish work.
- Offline simulation/render timings are local microbenchmarks, not proven interactive FPS or extended GPU/memory results. No exhaustive hardware-performance claim is made.

## Release root audit follow-up

The final package pass reproduced an intermittent shared-layout regression: reopening preserved the live widgets, but a subsequent `StateStore.Load()` during a background checkpoint returned defaults with an unreadable-file warning. Workspace reads now serialize with the existing write gate. The shared restart check captures one committed snapshot and writes widget-identity diagnostics instead of failing with an uninformative LINQ exception. Retired story source is excluded from MSBuild; its legacy save fields remain readable. The Pixel Timer catalog description now refers to Pixel Swarm rather than the retired story.
