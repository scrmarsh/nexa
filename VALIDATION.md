## Nexa 1.0.5 — Focus First

Validation uses isolated stores under ignored `test-output/`; existing user data was not opened or changed.

- Release build: passed with zero warnings and errors (`dotnet build SessionDashboard.csproj -c Release --no-restore`).
- Focus First integration suite: 83 checks passed. Covers all five preset banners, no duplicate timer card, slow and fast multi-cell movement, fixed neighbors, overlap prevention, cancellation, resizing, dragging after resize, saved dimensions and reopening. Also covers the full 45-second breath/look-away flow and local notes recap, nudges off/on and cooldown, synthesized PCM sound, global preferences, notes/history/recovery and import/export safety.
- Pixel Swarm suite: 37 checks passed. Includes exact timer pixel formation, real timer advancement, full-window bounds, retry, resize, maximize/fullscreen, win/loss, return, unmodified widget/history data and both debounced and thirty-second autosave.
- Additional regression: 23 movement/tiling checks and 22 existing settings/notes/clock/background/recording checks passed.
- Class dashboard render inspected visually. Generated captures and test stores remain ignored.
- Interaction input is exercised with routed WPF drag events. Physical slow/fast mouse feel, acoustic quality and moving between real differently scaled 1440p/4K monitors remain manual checks; no such hardware validation is claimed.
- No data format bump or destructive migration. Existing widget instances and their data remain. The primary Pixel Timer is presented outside the card grid. Direct placement freezes current neighbor coordinates using the existing saved-placement fields; responsive collision handling affects display without rewriting saved coordinates.
- Focus nudges are quiet return-to-app messages, not external Windows notifications. Sounds and nudges default off and can be changed under Appearance on Home or in a Space.

# Validation — October 2, 2026 (v1.0.2 development build)

## Built-in Bible, Scripture Pins and full-window Pixel Swarm — October 2, 2026

### Widget sizing — October 2, 2026

Edge resizing follow-up: **91 sizing checks passed** (`test-output/20261002-073423/`). Covers actual WPF pointer hit-testing of all eight resize zones, normal-mode availability, left/right/top/bottom and corner gestures, cumulative movement, cancellation, persistence and viewport sizing. Expanded widgets expose full transparent hit areas instead of a text-glyph-only corner target.

Drag-follow-up: **75 sizing checks passed** (`test-output/20261002-072813/`). Added repeated cumulative drag events, reversing to the grab point and anchored right-edge resizing. The handler now consumes the stationary WPF Thumb's total displacement once, freezes layout/grid calculations during the gesture and previews the actual card size without writing dashboard dimensions until release.

The Release package was updated at `publish/win-x64/Nexa.exe`. Packaged startup passed with exit code 0 and all three Bible indexes loaded (`test-output/widget-sizing-startup/`).

- **72 sizing checks passed**, including all widget definitions at all 20 dimensions, proportional/idempotent legacy migration, content-based defaults, fixed overflow bounds, routed drag increments and cancellation, full viewport sizing, window resize and save/reload. Evidence: `test-output/20261002-072030/`.
- **48 Bible checks**, **28 preview checks** and **165 full regression checks** passed after the layout changes. Evidence: `test-output/20261002-071703/`, `test-output/20261002-072051/` and `test-output/20261002-071945/` respectively. The older full-width pixel default and no-inner-scroll assertions were updated for the requested behavior.
- Full-viewport and half-width renders were visually inspected. Editing shows a subtle grid; content scrolls within custom bounds. Grid columns are versioned per widget so existing saves can migrate without changing their text or other data. No cleanup deletions were performed.

### Bible scrolling crash follow-up

The subsequent 06:51 crash report identified the still-running older `publish/win-x64/Nexa.exe` and the same `GentleMouseWheel` exception. Added translation-switching coverage: NLT/CSB/NKJV with wheel events during and after loading, plus rapid repeated changes that must retain the final edition. All **48 Bible checks passed** (`test-output/20261002-065325/`). The corrected package now replaces the standard `publish/win-x64/Nexa.exe`; its startup diagnostic passed with exit code 0 (`test-output/bible-translation-startup/`).

The corrected Release package was published to `publish/bible-scroll-fix/Nexa.exe` because the running Nexa process locked `publish/win-x64/Nexa.exe`. The separate package passed its isolated startup diagnostic with exit code 0 and all three Bible editions loaded: `test-output/bible-scroll-fix-startup/`.

Windows .NET Runtime events identified an unhandled `InvalidOperationException` in `GentleMouseWheel`: `Run`, `Paragraph` and `FlowDocument` are not visuals. A routed mouse-wheel regression reproduced the reported crash before the fix. The handler now traverses content parents safely and scrolls the owning text editor. All **44 Bible checks passed**, including wheel events from each of those three source types and assertions that the Bible scrolls while the outer dashboard stays still. Evidence: `test-output/20261002-064453/`.

- **41 Bible checks passed**: `dotnet run --project tests/SessionDashboard.Checks.csproj -c Release -- --bible`. Artifacts: `test-output/20261002-011118/`. Covers all three embedded editions, 66 books / 1,189 chapters each, unique addresses, reference/range/word search, exact selected text, multi-verse ranges, reader navigation, translation retention, actual drop-event handling, persistence, configuration/private exports, reset archival, read-only history and deduplicated history merge.
- **37 focused swarm checks passed**: `dotnet run --project tests/SessionDashboard.Checks.csproj -c Release -- --swarm`. Artifacts: `test-output/20261002-010301/`. Covers captured screen positions and rectangular pixel dimensions, stage thresholds, click/hold shooting, projectile hits, fleeing power effect and expiry, all win prerequisites, loss, retry, cleanup, real window boundaries at normal/small/maximized/fullscreen sizes, live session countdown and both debounce and real 30-second safety autosave. Widget data/layout, notes, history and absolute timer timestamps are compared before and after play. A model-only timing sample processed 180 frames with 270 initial pixels in 26 ms; this is not an end-to-end rendering benchmark.
- **28 preview checks passed**: artifacts `test-output/20261002-010437/`. The Church preview now includes a real source-backed NLT Scripture Pin and the built-in reader.
- **165 full regression checks passed**: `dotnet run --project tests/SessionDashboard.Checks.csproj -c Release`. Artifacts: `test-output/20261002-010942/`. Updated the older navigation tests to use Dashboard setup, the current overflow menu and fade-close behavior. The historical navigation-test mismatch documented below no longer blocks the full suite.

Reader, Scripture Pins, small-window layout, full-window swarm and win/loss screens were rendered and visually inspected. The HUD was corrected to show the initialized enemy count and refresh the session countdown while the game is paused.

Source extraction is reproducible with `tools/extract_bibles.py`. The indexes contain **31,062 NLT**, **31,086 CSB** and **31,102 NKJV** addressed passages, with SHA-256 fingerprints of their supplied PDFs and per-verse source pages. No external Bible text was used. Source PDF samples were compared with extracted Genesis, Psalm, John, Acts and Revelation passages; this review caught and removed CSB appendix material and NKJV Psalm section headings. Coverage assertions reject duplicate addresses and missing chapters. The numbering differences between editions are retained; see `assets/Bible/README.md`. The original PDFs are unchanged.

The Release build passed with **zero warnings and zero errors**. The self-contained Windows package was updated at `publish/win-x64/Nexa.exe`. Its isolated startup diagnostic passed with exit code 0 and `Success: true`, loading all three embedded Bible editions with the passage counts above. Evidence: `test-output/bible-swarm-packaged-startup/`.

## Home dashboard previews — October 2, 2026

The focused preview runner passed **28 checks**:

```powershell
dotnet run --project tests/SessionDashboard.Checks.csproj -c Release -- --preview
```

Artifacts: `test-output/20261001-235946/`. All six scenario layouts, Home and a small-window preview were visually inspected. The pantry sample displays populated records before its collapsible entry form. Checks cover relevant notes and tasks, varied widgets, live timers, populated historical sessions, nonrepeating scenario generation, the Home button and preview navigation, credential isolation, debounced preview saving and return to the untouched personal workspace. Personal dashboard and preference files remain unchanged.

The Release build passed with zero warnings and zero errors. The self-contained Windows package was updated, and its isolated startup diagnostic passed with exit code 0 and `Success: true`; evidence is in `test-output/preview-packaged-startup/`. The broader suite's existing navigation-label mismatch is documented below; this result refers to the focused preview checks.

## Enlarged timer-seeded swarm

The updated focused runner passed **28 checks**:

```powershell
dotnet run --project tests/SessionDashboard.Checks.csproj -c Release -- --swarm
```

Artifacts: `test-output/20261001-230556/`. The small-window and game-over renders were visually inspected. The game fills the dashboard space with a near-square outlined arena; half a timer grid initializes 270 persistent chasing enemies. Checks verify captured-pixel counts, chase movement, edge reach, matching visible/model bounds after resize, dashboard restoration, Retry, collision, score, fullscreen/maximized sizing, timer continuity, restart and a real 30-second autosave run. The expanded view is temporary and does not change saved widget layout. Empty timer grids retain an edge-spawn fallback.

The updated Release build and self-contained Windows publication succeeded. The packaged startup diagnostic passed; evidence is in `test-output/expanded-swarm-startup/`.

## Earlier Pixel Swarm update

The focused runner passed **21 checks** using isolated files and suppressing normal application startup:

```powershell
dotnet run --project tests/SessionDashboard.Checks.csproj -c SwarmCheck -- --swarm
```

Artifacts: `test-output/20261001-223259/`. Small-window and game-over renders were visually inspected. Coverage includes direct launch, 10 points per second, smooth bounded movement, difficulty progression beyond 60 seconds, safe spawning, collision and fast-movement collision detection, Retry, return cleanup, live countdown accuracy, debounce and the real 30-second autosave safety net, restart, and actual small-window/maximized/fullscreen resizing. Game state is never serialized; optional high scores are not implemented.

The broader runner passed the game, timer and persistence checks, then stopped at a pre-existing navigation expectation in `tests/RevisionChecks.cs`: it expects **Session settings**, while the current UI uses **Dashboard setup**. The full suite is not reported as passing. An initial persistence-recovery attempt encountered a transient filesystem replacement error; the subsequent broad run passed recovery.

The Windows package was republished using `SwarmCheck` configuration to avoid overwriting the running Debug application. README and the Unreleased changelog document the new flow; tagged release history remains unchanged.

The published executable's isolated `--verify-startup` diagnostic passed (`Success: true`, X64, 540 timer pixels, four presets). Evidence: `test-output/swarm-packaged-startup/startup.json` and `startup.png`.

## Previous result

The isolated WPF check runner passed **130 checks**:

```powershell
dotnet run --project tests/SessionDashboard.Checks.csproj -c Release --no-restore
```

Artifacts: `test-output/20261001-143559/`. Dashboard, history preview, compact and small-window screenshots were rendered; the dashboard and history preview were visually inspected. All fixtures use isolated files and leave personal app data untouched.

The sandbox could not access DPAPI's user-profile context. Running the checks with the loaded Windows profile passed the existing credential-protection checks. Early runs also caught and fixed history-preview refresh and background-save dispatcher issues.

## Continuity coverage

- Actual 30-second autosave during 31 seconds of continuous text changes, preventing the 600 ms debounce from being the reason for the save.
- Debounced background saving, idle no-op, timestamp indicator and opacity fade after three seconds.
- An edit arriving during background I/O remains dirty after the older capture completes.
- Close/reopen flushes pending notes and retains the same session identity.
- Independent dashboard notes, pending task input, title and interval drafts survive switching.
- Reset archives a deep copy, clears session content, and preserves dashboard identity, theme and panel settings.
- Continue creates an editable copy with provenance and archives displaced work; editing it cannot change history.
- Merge appends notes and labelled interval/Hedy content, deduplicates tasks, preserves layout by default, and ignores a repeated merge of the same source.
- The real history dialog groups sessions by date and shows a read-only preview without changing current content.
- Versioned configuration-only export omits private notes, transcripts, owner IDs and credentials. Import creates a fresh owned dashboard. Private data needs explicit acceptance; invalid versions, unknown credential fields and malformed collections are rejected.
- Simulated distinct owners cannot load, query, continue or merge another owner's records; one dashboard cannot continue another dashboard's history.
- Unclean-shutdown metadata restores the latest saved state with a recovery notice. A damaged current payload recovers from its latest valid snapshot; corrupt primary files recover their backup.
- Recovery snapshots remain capped at three per current dashboard. Archives contain one final state without redundant snapshots.
- Forced filesystem failure leaves edits dirty, displays `Save failed`, and prevents a reset from replacing current work. Retrying after storage recovery succeeds.
- Existing timer, preset, Hedy, story, fullscreen, compact mode and control-handler regression checks pass.

## Windows package

```powershell
dotnet publish SessionDashboard.csproj -p:PublishProfile=WindowsStandalone --ignore-failed-sources
```

Published successfully to `publish/win-x64/SessionDashboard.exe`, a self-contained Windows x64 single-file application (64,885,916 bytes). The first no-restore attempt lacked the runtime-specific assets target; restoring and publishing succeeded using available packages. NuGet vulnerability metadata was unavailable due to an SSL connection error (`NU1900`); no application dependencies were added.

SHA-256: `FB4F04D7C74A302989E0C608E2CE2A2FFAF25080CF9BCEA9AE5B097A0A35B11A`.

The published executable was launched hidden with `--verify-startup test-output\continuity-packaged-startup`. Its real WPF `ContentRendered` check exited with code 0 and reported `Success: true`, `Pixels: 540`, `Presets: 4`, and `ProcessArchitecture: X64`. Diagnostic output uses isolated state.

## Scope and limits

This implementation covers this WPF project's notes, tasks, timers, interval/Hedy data, story, presets, settings and input drafts. This project has no Pantry tracker, Church widgets, draggable widget framework or application login. User isolation uses Windows-account local storage, owner validation and a per-user app-instance mutex.

User isolation and crash recovery were exercised with controlled owner IDs and unclean/corrupted save fixtures, not a real Windows account switch or forced power loss. Windows sign-out saving is wired through WPF session-ending events but was not triggered on the host. Native file-picker interaction and confirmation dialogs were not automated; the import/export, reset and merge operations behind them were tested. A separate clean Windows machine, screen-reader behavior and every DPI/monitor combination were not available.
