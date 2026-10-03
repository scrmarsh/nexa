## Nexa 0.1.9 Alpha — Versioning & Session Stability

- Final isolated Release build: `dotnet build ".\SessionDashboard.csproj" -c Release --no-restore -m:1 -p:OutputPath=bin/Alpha019/` **PASS**, zero warnings/errors. Output: `bin/Alpha019/Nexa.exe`. Check-runner build also passes with zero warnings/errors.
- Exact requested `dotnet build ".\SessionDashboard.csproj"` restored successfully, but the default Debug executable was locked by running Nexa PID 195656. Only that blocked build was canceled; the existing app was preserved. Close the running app before rebuilding its default output, or use the isolated output above.
- **185 passing assertions across four targeted suites**, including repeated shared model checks: `--alpha019` **47** (`test-output/20261003-135620`), `--continuityaudit` **42** (`134335`), `--polish104` **22** (`134743`), `--onboarding106` **74** (`134938`). Older numeric test switches retain their identifiers; they test current code. An initial WPF generated-markup cache failure recovered with a single-worker Rebuild.
- Alpha checks verify numeric 0.1.9 metadata, centralized `Nexa 0.1.9 Alpha` labels in Home/About/splash, all nine release-history entries, restored 0.1.1 Bug Fixes, descriptions and future 0.1.10 formatting. Changelog, session Settings and splash renders were inspected.
- Settings tests use actual WPF controls and synthetic stores: recognition from Home/dashboard, exact session identity and timestamp precision, countdown advancement while open, title/time edits, custom intervals, invalid input, stopped-session preservation, text/slider debounce, sound/nudge/theme/widget/profile persistence, Hedy region-only credential preservation, immediate close flush, Space switching and restart. Untimed Space appearance does not create a session.
- Actual locked-file failure verifies pending edits survive, failed animated dismissal reaches Closing and remains visible, and the same animated close gesture retries successfully after storage recovers. The check waits for the close callback; a fixed 300 ms animation assertion was too short in this environment.
- Home navigation tests verify pending edits save to the outgoing Space before selecting another one and Settings refreshes saved state without replacing newer history. Existing Space count, layouts, notes, Hedy transcript/interval notes, history and onboarding/profile preferences remain intact.
- Continuity regressions include import/export, ownership checks, recovery, background writes, real thirty-second autosave, close/reopen and save failures. Timer/stopwatch tests cover pause/resume/reset and saved state. Profile/integration tests include legacy preference migration, existing-user onboarding bypass, Windows DPAPI credential isolation/export exclusion and mocked task/calendar requests.
- The integration suite first encountered unavailable DPAPI in the sandbox; it passed when rerun in the normal Windows context with synthetic credentials and mocked HTTP. **No personal stores or live service accounts were used.** No storage/schema migration, data reset, external dependency or widget/menu/game redesign was introduced.
- Manual limitations: physical desktop use and live-account authorization were not tested in this pass. The test windows exercise WPF behavior; this is not a claim that the new app was launched on the user's desktop. Earlier validation evidence below retains its original build numbering; release history now maps 1.0.1–1.0.8 to 0.1.1–0.1.8 Alpha.

## Nexa 1.0.8 — Widget + Dashboard Stability

- Release build: `dotnet build SessionDashboard.csproj -c Release --no-restore -m:1 -p:OutputPath=bin/Stability108/` **PASS**, zero warnings/errors. Existing NuGet assets reused. Final executable: `bin/Stability108/Nexa.exe`; the running 1.0.7 app was not overwritten or force-closed.
- **329 passing assertions across six targeted suites**, including repeated shared model checks: `--stability108` **78** (`test-output/20261003-130729`), `--polish107` **67** (`130434`), `--focus105` **83** (`125453`), `--swarm` **37** (`125602`), `--polish104` **22** (`125551`), `--continuityaudit` **42** (`125820`).
- New suite covers responsive edge-aligned square/circle pixels at several widths, calm completion, matching game palette/shared timing, profile greeting including clearing it, Hedy routing, Widgets settings/persisted preferences, custom image and missing-file fallback, save spinner/success/failure timestamp, local Timeline/Hedy separation, Calculator operations/error/history/button input, matching millisecond/analog stopwatch, optional finite distraction messages, focus backdrop isolation, anchored resize, clicked Add, removal Undo/Redo shortcuts and native text undo, Recommended, fullscreen spacing, saved layouts/backgrounds and all preview types.
- Recorder checks use **mocked MCI commands and synthetic local files**, not a real microphone: pause/resume/stop, save retry while keeping the buffer, unique captures, source-neutral entries and outgoing-dashboard isolation during switching. Developer sound test respects master Off. Prior shared sound-generation coverage also passes in Focus First.
- Placement suite uses routed WPF drag input: slow/fast movement, original grab position, held-neighbor stability, occupied drops, cancellation, pickup during settling, resize/scroll, two/one-column layouts, saved reload and Bible growth/shrink/scroll. Historical expectations were updated where this release intentionally changed behavior; this is not a claim that every older test mode was run.
- Focus First completes the actual five-breath/look-away/local recap guide, nudge disable/cooldown, game return, local data and preferences. Swarm exercises window/physics bounds, retry, fullscreen and a real thirty-second autosave while playing.
- Continuity covers history/reset/continue/merge, import/export secret exclusion and owner checks, snapshots/recovery, real autosave, edits during background writes, close/reopen and actual filesystem failure. Failed writes leave the successful timestamp unchanged and stop the spinner.
- Final isolated `--verify-startup` exits **0**, reports Success true, **1,134 responsive pixels**, four presets and all three packaged Bible editions (`test-output/stability108-final-startup`). Dashboard and startup captures inspected. No real user store was opened by checks.
- Initial incremental WPF builds encountered missing generated markup/cache files; rebuilding and single-worker builds recovered. Final build and startup are clean. No generated cache files are committed.
- **Manual checks remaining:** physical captured-mouse feel, microphone permission/device/capture/playback, audible sound quality and mixed-DPI physical monitor moves. Live Hedy/account calls were not performed. Transcription and summary services are intentionally deferred; only provider interfaces/local recording are implemented.
- Data changes are additive JSON fields with defaults. Legacy Hedy widgets are renamed in place; layouts, custom titles and old interval data remain. No database/workspace format reset or onboarding/Create Space redesign. Generated previews alone compact automatically; normal saved layouts change through editing.
## Nexa 1.0.7 — Lightweight UI & Interaction Polish

- Final Release build: `dotnet build SessionDashboard.csproj -c Release --no-restore -p:OutputPath=bin/Polish107/` **PASS**, zero warnings/errors. Existing restored assets avoid the sandbox's inaccessible user NuGet configuration. Output: `bin/Polish107/Nexa.exe`; the older running release was not overwritten.
- **67 polish checks** (`--polish107`, `test-output/20261003-004636`) verify 1.0.7 metadata, stationary Settings button and centered graphic animation, actual-card pickup and unsnapped slow movement, stationary neighbors while held, deferred renders, physical-release settle, pickup during animation, occupied top-right placement, upward and sideways gap filling, cancellation, resize and subsequent dragging, responsive three/two/one columns, final saved/reloaded bounds, notes/tasks preservation, and safe micro-widget hover during capture.
- The same suite verifies 540 shared-size square pixels fitting and centering at current display scaling, unchanged session timestamps/countdown, absence of only the header Focus Reset action, working widget guide/return, and Bible expansion/shrink. Adding 240 DIP to the Bible gives 240 DIP to its passage; headers/footer remain unchanged and Psalm 119 still scrolls. Expanded Bible and dashboard captures were inspected.
- Relevant regressions: **23 tiling** (`20261003-003929`), **22 Timer/Stopwatch and existing-user polish** (`20261003-004030`), **37 Pixel Swarm** (`20261003-004050`, including real timer advancement and thirty-second autosave), **83 Focus First** (`20261003-004137`, including five preset banners, full breath/recap flow, preferences/nudges and data isolation/export models), and **48 Bible** (`20261003-004249`, including document wheel routing, navigation, search, pins and history). Total: **280 passing assertions across six suites**, including repeated model coverage.
- Old stationary-after-drop assertions now expect the intentionally requested reflow while retaining stationary-during-drag and collision checks. Tests use isolated synthetic stores under ignored `test-output/`; personal data was not opened or reset. Existing normalized placement fields are reused, with no schema migration. Integration/storage code is unchanged; the earlier integration audit remains historical evidence below.
- Release diagnostic startup exited 0, reported `Success: true`, 540 pixels, four presets and all three embedded Bible editions (`test-output/polish107-final-startup`).
- **Manual checks remaining:** physical captured-mouse feel/fast motion, audio quality and moving between differently scaled physical monitors. Drag checks use routed WPF input, not physical mouse automation. Narrow-layout checks deliberately lower the test window minimum width to exercise the existing responsive canvas; production window minimums are unchanged. No requested feature was deferred for architectural reasons.

## Nexa 1.0.4–1.0.6 verification pass

- Phase 1: **22 polish checks passed** (`--polish104`, final `test-output/20261002-234712`). Corrected obsolete version assertions to match 1.0.6. Splash capture inspected; Timer/Stopwatch transitions, layout/autosave/reopen, reset duration after reload, developer-space preservation/access and picker scrolling/adding exercised.
- Phase 2: **83 Focus First checks** (`test-output/20261002-231938`) and **23 layout checks** (`test-output/20261002-232018`) passed, including the full breath sequence, local recap, shared Swarm/session time, neighbors, saved placement and nudge/sound behavior.
- Phase 3–4: baseline **74 onboarding/integration checks** pass after repairs (`test-output/20261002-233232`). New **23 targeted verification checks** pass (`--audit106`, `test-output/20261002-233139`): wizard Back/Skip/tool selection, editable Profile persistence, task date views, cache invalidation, in-flight account replacement/disconnect, new-account lists, rejected old-account mutations, malformed responses/configuration and Calendar current/next countdown.
- Repairs are confined to integration lifecycle/input handling, global preference application and regression tests. No user stores or schema changes. DPAPI tests use synthetic credentials under the loaded Windows profile; HTTP is mocked. Live OAuth/accounts, physical mouse/audio quality and actual mixed-DPI monitor movement remain **Unable to Verify**.
- Final compatibility checks: **37 Swarm** (`test-output/20261002-233456`), **22 Settings/core** (`test-output/20261002-233508`), **10 direct-editing** (`test-output/20261002-234151`) and **41 continuity** (`test-output/20261002-234204`) checks passed. Includes real timer advancement, game autosave, direct countdown/clock/title editing, reset/reopen, global interaction preferences across Space switches, read-only history, continuous-typing autosave, edits during background writes and forced save/reset failure recovery. Corrected continuity fixtures to render the window before testing animation and locate the current vector History label.
- A localized preference fix loads master sounds/volume/nudges when activating a Space. No changes to saved layouts or workspace schema.
- Isolated Release executable startup exited 0 with `Success: true`, 540 timer pixels, four presets and all three Bible editions loaded (`test-output/audit-final-startup`). No real user data was opened. Tracked source/config secret-pattern scan and private/generated-file inventory found no matches; audit commits exclude existing unrelated dirty files.
- Release build into `bin/Audit/` passed with zero warnings/errors. Ongoing phase status and exact resume instructions are recorded in `CODEX_SESSION_STATE.md`.

## Nexa 1.0.6 — Onboarding & Integrations

- Debug application/checks build and Release build passed with zero warnings or errors. Release output is `bin/Release106/Nexa.exe`, leaving the running 1.0.5 executable in place.
- **74 onboarding/integration checks passed** (`--onboarding106`). Fresh profile detection, existing-user bypass, legacy theme copying, profile persistence, distinct user scopes, five space recommendations, use-case-driven recommendations, timed/untimed creation, the six-step modal wizard, first-space opening, repeat creation and disconnected Calendar were exercised.
- Security checks used synthetic credentials: Windows DPAPI encrypted round-trip, ciphertext inspection, rejection of copied credentials under another Nexa user ID, no credentials in exports, provider-host restrictions and imported desktop OAuth configuration. DPAPI validation ran outside the restricted sandbox because its impersonated context cannot access the Windows user protection keys.
- Mocked HTTP verified Todoist connect, projects, dated tasks, pagination, completion, creation, unauthorized/offline/disconnected states; Google Tasks lists/tasks and completion; secure Google refresh; calendar list selection and timed/all-day events. Every Calendar request was GET. OAuth callback decoding and PKCE encoding were checked.
- **23 layout checks and 22 existing feature checks passed**: movement, cancellation, resize/save/reopen, themes/backgrounds, clock, chronological notes, history-related model behavior, recordings/Hedy configuration and settings remained compatible.
- Welcome and theme screenshots were visually inspected. Generated captures, profiles, credentials and personal fixture data remain under ignored `test-output/`.
- **Not tested live:** real Todoist account connection, Google browser consent/authorization with a registered client, provider task mutation against real accounts, and real calendar schedules. Google requires a user-supplied desktop OAuth client with APIs enabled. No real account or token was used.
- No workspace schema bump or destructive data migration. Preferences copy once into user-scoped folders; legacy originals remain. Integration responses are memory-only, and exports contain only ordinary widget configuration, never authorization.

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
