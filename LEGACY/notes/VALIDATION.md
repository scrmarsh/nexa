## 0.1.21 Alpha ? Infinite Warfare (October 19, 2026)

Clean rebuild and test-project build: zero warnings/errors. Final suites: 92 --war021 checks (test-output/20261019-194158), 138 --colony019 checks (test-output/20261019-194052), and 105 --arcade020 checks (test-output/20261019-193927). See RELEASE021.md for commands, file inventory, detailed status and remaining work.

- New coverage includes exact tiny-menu entry, shared widget-free battlefield, one authoritative controller, same-world transitions, pause/reset cancellation, deterministic saves/replay, corrupt-save preservation and explicit recovery, scroll/pointer coordinates, functional irregular webs, sustained territory capture/defense/reclaim, strategy and migration.
- Six unprotected Infinite runs cover 125 simulated minutes, including a single 75-minute world. Two natural nest ownership changes occurred, with 5?6 factions remaining active across these scenarios. Natural conquest frequency and species balance remain tuning work; separate controlled tests prove capture and recovery rules.
- Latest 180-unit native software probe: CPU p95 0.44 ms and render p95 34.22 ms, 96 terrain cells and one web. Classic live dashboard: 28.9 updates/second with 38 timed text edits. Performance is host-dependent; worst-case multi-web/high-zoom and physical mixed-DPI checks remain outstanding.
- Latest Classic two-hour soak: 52 completed matches, peak 113 entities, two announcements, 18 projectiles. Decorative modes and dashboard editing/counters retain regression coverage.
- Normal and Arcade debug-executable startup probes passed (exit 0, version 0.1.21, PerMonitorV2). Evidence: test-output/startup021-final and test-output/arcade-startup021-final. Arcade initialized six colonies and ten bugs.
- Menu, battlefield and full-world screenshots were inspected. Repackaging subsequently replaced root Nexa.exe and publish/win-x64/Nexa.exe with matching 0.1.21.0 self-contained Windows x64 single-file Release builds. Both packaged startup probes passed (normal and Arcade, PerMonitorV2); Arcade initialized six colonies and eleven bugs. Evidence: test-output/package021.json and test-output/package021-startup / package021-arcade. The older ZIP remains unchanged.

---

## 0.1.19 Alpha - Colony Wars Overhaul (October 15, 2026)

Current release suite: 138 checks passed. Command: dotnet run --no-restore --project tests/SessionDashboard.Checks.csproj -p:UseSharedCompilation=false -- --colony019. Evidence: test-output/20261015-174943/. The 0.1.18 arcade assertions below describe superseded behavior.

- Eighteen independent seeds complete two consecutive autonomous matches each. All six species can attack and retaliate; valid range and friendly-fire rules are checked. Dynamic conquest text supports all thirty species pairings. Final-hit credit, protection, elimination, victory, simultaneous-final-hit draws and automatic restarts pass.
- Two simulated hours complete 74 matches. Peak entities including defeat animations: 83; peak queued announcements: 2; peak projectiles: 13. Effects and colony state remain bounded. Every species records victories. This is accelerated deterministic simulation rather than a physical two-hour observation.
- At 120 live units, simulation CPU p95 is 0.1675 ms and forced software render p95 is 23.8496 ms. The live dashboard runs approximately 29.1 updates/second while accepting 37 timed text edits in the measurement interval, using graceful fallback. The configured target is 60 FPS; the host does not establish a universal 60 FPS guarantee.
- Verified preference round-trips including per-species statistics, immediate six-colony startup, obsolete habitat migration without losing Notes identity/content, catalog removal, settings controls, editing/saving, minimizing/restoring, timer/render-callback cleanup and decorative-mode isolation.
- Beverage/counter migration and all vessel options still pass their existing checks.
- Battle, conquest, victory, scoreboard and dashboard captures were rendered and visually inspected. Royal units stay inset from viewport edges and defeat animations finish during victory. Cache resolution follows monitor DPI rather than reducing the whole overlay.
- Version metadata is 0.1.19 and the visible designation remains Alpha. Self-contained normal and Arcade startup probes pass, reporting 0.1.19 Alpha, PerMonitorV2 and x64. Arcade has six colonies and ten bugs at the initial 700 ms probe. Package evidence: test-output/release019-package/startup.json and test-output/release019-colony-package/startup.json. Executable locations are recorded in RELEASE019.md.
- Wins count across matches in the running simulation; optional lifetime persistence across application restarts is not implemented. Physical mixed-monitor transitions and long subjective gameplay observation remain manual acceptance checks.

## 0.1.18 habitat and population update - October 15, 2026

- Current release suite passes 64 checks, including a 30-minute simulated arcade run and a full 240-ant population. Evidence: test-output/20261015-165953/.
- Arcade now caps at 240 bugs, spawning one every five seconds (ten seconds under load). Centipedes occur in the opening group and account for about 15% of subsequent mixed spawns.
- Spiders can maintain up to 18 organic webs at once, with 125-240 DIP radial reach and three inner strand rings. Webs last up to four minutes; cutting and widget-anchor breaking remain supported.
- Six large labeled habitat widgets use the existing free-slot placement, drag, resize and persistence system. Existing widgets keep their positions. Tests verify no overlaps, moving the ant return destination, saved positions and hiding/restoring habitats outside/inside Arcade without duplicate cards.
- Visual inspection confirms large species-specific illustrations and labels. Remaining physical mixed-monitor and hands-on extended-play checks described below still apply.
- Version remains 0.1.18. Repackaged self-contained Windows x64 executable was copied to Nexa.exe after isolated startup verification. Package evidence: test-output/release018-habitats-package/startup.json; startup succeeded and reports PerMonitorV2 awareness.

## 0.1.18 calm arcade and display revision - October 15, 2026

Current validation supersedes older assertions below about escalating Arcade, low-resolution rendering, universal age growth, royal variants and non-arcade shared webs.

- Current release suite: 51 passed. Command: dotnet run --no-restore --project tests/SessionDashboard.Checks.csproj -p:UseSharedCompilation=false -- --release018.
- Evidence: test-output/20261015-133401/. Includes dashboard renders at 100%, 150%, 200%, arcade artwork and beverage renders. The 200% dashboard and arcade frame were visually inspected.
- Thirty minutes of accelerated deterministic simulation stays within 24 bugs, three webs and bounded remains/growth. This verifies state bounds, not thirty minutes of human observation.
- Verified decorative mode isolation, fifty unique names and approximate 2% frequency over 300 seeded populations, ant delivery, fly carrying/release, centipede feeding growth, ladybug attacks and roach resistance.
- Verified live ScrollViewer updates without web loss, actual Thumb drag routing breaking anchors, editor operation and segment-crossing cuts. Blank-space sweeps defer Add Widget until a stationary click is released; widget gestures retain normal routing.
- Self-contained x64 package: publish/win-x64/Nexa.exe. Isolated --verify-startup confirms PerMonitorV2 awareness, resources, initialized controls and x64 architecture. Available test display reports scale 1.0.
- Remaining physical checks: launch on 2K and 4K displays at their configured scaling, move both directions and reopen on 4K; inspect text, buttons, graphics and bugs. Render-target scaling is not a physical monitor transition test.
- Remaining hands-on acceptance: scroll with attached strands, drag a widget away, sweep from blank space and directly from a strand, edit controls beside a web, and observe extended play. Automated checks passed; subjective and hardware checks are not claimed complete.
- Tests and package probes use isolated stores under test-output; personal workspace state was not modified.

## Nexa 0.1.15 Recovery — Group 1 only

- Initial and final standard `dotnet build ".\SessionDashboard.csproj"` pass with zero warnings/errors; the previous executable lock is no longer a blocker. Release version remains 0.1.14 pending Group 7.
- New `--appearance015`: **50 PASS** (`test-output/20261004-122820`). Covers all 12 System/Light/Dark × Nexa/Violet/Aurora/Ember combinations, live Home/Space/Settings surfaces, detached Profile page, autosave/persistence, startup restoration and preservation of notes, Space colors and the live Add Bugs manager.
- Existing `--bugs014`: **43 PASS** (`test-output/20261004-122849`). No production repair or Add Bugs redesign was needed. Tests use isolated stores and programmatic WPF events. Actual OS theme-toggle events and mixed-DPI hardware movement were not tested.
- Group 1 complete; Groups 2–7 are not complete. See `CODEX_SESSION_STATE.md` → `Resume Here` for the exact Group 2 handoff.

## Nexa 0.1.14 Alpha — Add Bugs

- Isolated Debug and Release builds pass with zero warnings/errors. The requested default-output build compiled but could not replace `bin/Debug/net10.0-windows/Nexa.exe` because a running process holds it open; the existing process was preserved. Release startup probe succeeded (`test-output/alpha014-startup/startup.json`).
- `--bugs014`: **43 passing assertions**, including the existing shared-widget resize/save/reopen suite with bugs enabled. Covers default-off and per-user persistence, preservation of appearance settings, all species and density caps, natural fly landing/takeoff, moved/resized/deleted surfaces, transformed drag geometry, squash duplicate protection, four-second splat removal, actual WPF hit testing, live Settings changes, repeated toggling, minimized and game-overlay suspension, restore, shutdown, and Space content preservation.
- Final run: `test-output/20261004-012258`. A 60-bug, 18,000-frame simulation took 110 ms. Drawing 120 WPF overlay frames to a bitmap took 1,567 ms (about 13 ms/frame). These are local synthetic measurements, not a guarantee of live frame rate on other hardware. Settings and Light/Dark infestation renders were inspected. Repeat clicks during the squash compression are consumed without creating another splat or reaching a control underneath.
- Physical mouse play and mixed-DPI hardware movement remain manual checks. Tests use isolated stores; no real user preferences, notes or layouts were changed.

## Nexa 0.1.13 Alpha — Pixel Swarm & Appearance

- Debug and Release builds and the `--swarm013` focused suite pass: 35 checks covering timer-pixel seeds, input/aim/fire, pause, health, stage progression, relief and weapons, upgrades, bounded enemies, local per-user stats, routed WPF menu actions, real session continuation, autosave, return without note loss, and immediate/persisted Appearance changes. Light and Dark checks now assert the visible Space, header and Settings window brushes; both Space renders were inspected alongside start, play and pause. The isolated Release startup probe succeeded with 759 pixels, four presets and three packaged Bible translations.
- Previous `--shared012` suite passes 10 checks for neighboring Pixel Timer/widget resize and live theme selection. Previous `--dashboard012` suite passes 24 checks for saved dashboard sizing, calendar, notes and water counter; its synthetic Windows credential check requires the normal Windows profile.
- Real keyboard/mouse play, mixed-DPI monitor movement and live long-session performance still need manual testing on the target hardware. Test stores are isolated under ignored `test-output`; no existing user data was reset.

## Shared-edge and appearance follow-up (0.1.12 Alpha)

- Isolated Debug and Release builds pass with zero warnings/errors. Release startup probe succeeds with four presets, all three packaged Bible editions and a rendered Pixel Timer (`test-output/shared012-startup`). `--shared012` passes 10 WPF checks: Pixel Timer card edge and adjacency, live width trade with Clock, normal widget pair resizing and cancellation, saved/reopened geometry, and immediate/persisted Space Settings palette selection.
- Updated `--dashboard012` passes 24 checks, including Pixel/Clock width exchange, Pixel/Notes height exchange, widget content and saved layout reload. `--tiling` passes 23 checks. `--swarm` passes 37 checks for timer/data continuity and autosave.
- Tests use synthetic Space data and routed drag events. Physical pointer feel, every monitor scaling combination, and every possible widget arrangement remain manual checks. Existing user stores were not modified; no schema migration is required.

## Nexa 0.1.12 Alpha — Dashboard & Widget UX Update

- Pixel Timer height follow-up: isolated Debug and Release builds pass with zero warnings/errors. Startup probe passed and the taller banner render was inspected. `--dashboard012` **24 checks** pass, including default height equal to expanded Clock, bottom-edge resize without width movement, save/reopen, and the existing dashboard/calendar checks. `--swarm` **37 checks** pass with the taller timer pixels. The running Debug executable locked only the default output; it was left open and builds used separate output folders.
- The exact `dotnet build '.\SessionDashboard.csproj'` passes with zero warnings/errors. An isolated Release build to `bin/Alpha012Ux/` also passes. Its `--verify-startup` probe succeeds using a synthetic store; the rendered dashboard was inspected.
- `--dashboard012` **19 checks** pass. The suite exercises a live WPF Space with a Pixel Timer sharing the top row with Clock, anchored horizontal resizing and persistence, Clock/Stopwatch/Calendar mode transitions, disconnected Calendar and saved Outlook source selection, the date/time target editor (including midnight rollover), preserved active-session identity, ruled Notes surface, Water glass, upward compaction and non-overlapping cards. Synthetic Graph responses validate Outlook calendar/event mapping; no real Microsoft account was connected.
- Existing `--alpha012` **24 checks** pass, covering idempotent legacy widget migration, saved content, numeric release comparison, verified installer download, rejected untrusted assets, optional startup preference and the Settings update page. `--continuityaudit` **42 checks** pass for account/data isolation, save/reopen and autosave; `--swarm` **37 checks** pass for game/timer/data preservation; `--menurepair` **7 checks** pass for outside dismissal and the Developer Space. Tests use isolated stores.
- Manual hardware checks remain for physical mouse capture/edge scrolling, audible sounds, mixed-DPI monitor moves, and live Google/Microsoft OAuth. A published setup asset and SHA-256 digest are still required for a real update installation.

- Follow-up `--menurepair`: **7 checks** (`test-output/20261003-183237`) pass for outside pointer dismissal, first-click owner activation, nested topmost-only dismissal, and opening a legacy Developer Space with its task content intact. `--alpha012`: **24 checks** rerun successfully (`test-output/20261003-183250`). These use routed WPF events and an isolated store; physical pointer input remains a manual check.

- Isolated Debug, Release and check-runner builds pass with zero warnings/errors. The Release startup probe succeeds with 1,134 Pixel Timer cells, four presets and three packaged Bible editions. `--alpha012`: **24 checks** (`test-output/20261003-182012`), including actual WPF Clock/Timer mode controls and the Settings Updates page, old-kind migration, saved title/content/count/placement, linked Notes/Tasks, save/reopen, numeric release/channel comparison, exact asset selection, synthetic verified download, rejected untrusted URL and hash mismatch cleanup, and opt-in preference persistence.
- `--continuityaudit`: **42 checks** (`test-output/20261003-181209`), including account isolation, credential-free exports, history, autosave and retry. `--swarm`: **37 checks** (`test-output/20261003-181254`), including the live session timer, dashboard preservation and autosave during the game. These suites used isolated test stores and did not open real user data.
- Live GitHub release lookup, a real published setup asset, UAC handoff, physical drag/resize feel and mixed-DPI monitor behavior were not tested. The repository has no setup-installer production pipeline yet. Startup checks remain off unless explicitly enabled.

## Nexa 0.1.11 Alpha — Bug Fixes, UI Refinements & Pixel Timer Performance

- Follow-up UI repairs: updated isolated Debug and Release builds pass with zero warnings/errors; Release executable is `bin/Alpha011Final/Nexa.exe`. Updated `--alpha011` **61 PASS** (`test-output/20261003-174807`), `--alpha010` **72 PASS** (`174854`), and `--swarm` **37 PASS** (`174914`). Checks cover the single maximize control, maximized header clearance, fixed Home utility footer at a shorter window height, blank-area drag eligibility without button interception, and Water Bottle Counter add/count/goal/save/reopen. Maximized dashboard, compact Home footer and bottle widget captures were inspected.
- Updated Release `--verify-startup` succeeds (`test-output/alpha011-final-followup/startup.json`): 1,134 timer pixels, four presets and three packaged Bible editions. Blank-area menu dragging was checked through WPF target classification; physical mouse movement remains a manual check.
- Isolated Debug, Release and check-runner builds pass with zero warnings/errors. The generated WPF cache under the default `obj` directory became inconsistent during incremental builds, so the successful builds use `obj/Alpha011b/` with existing restored assets and `bin/Checks011/` / `bin/Alpha011/` output; no user store was touched.
- `--alpha011`: **52 passing checks** (`test-output/20261003-171307`). Actual WPF checks cover fixed, evenly spaced timer pixels, resize bursts without needless grid rebuilds, visual reuse after regrowth, exact session timestamps/countdown, fullscreen return from normal/maximized, blue drag preview and three-across insertion, saved placement reload, Home Recent Space columns, Settings navigation/empty session/autosave footer, Create Space Back/Close, History clear/cancel, Developer reset window-state preservation and live session-end refresh.
- `--polish107`: **67 passing checks** (`test-output/20261003-171219`). Slow and fast routed drag input, occupied drops, cancellation, resizing then dragging from a scrolled canvas, responsive one/two/three-column layouts, save/reopen and Bible passage resize/scroll pass. Historical assertions were updated where stable placement intentionally leaves unrelated rows in place.
- Earlier `--alpha010`: **72** (`test-output/20261003-170445`) and `--alpha019`: **48** (`test-output/20261003-170615`) pass against current code, including panel/navigation and session/settings autosave continuity. `--alpha019` now expects the Session empty state after a saved session has ended.
- `--continuityaudit`: **42** (`test-output/20261003-171413`) passes, including credential-free exports, user isolation, resets/merges, real thirty-second autosave and failed-write retry. `--swarm`: **37** (`test-output/20261003-171517`) passes, including exact timer continuation, unchanged widget/session data, fullscreen/window bounds and autosave during the game.
- `--polish104`: **22** (`test-output/20261003-171845`) passes for version labels, existing dashboard data, timer/stopwatch persistence and developer splash access. Seven suites total **340 passing assertions**; some cover the same underlying behavior.
- Isolated Release `--verify-startup` succeeds (`test-output/alpha011-final-startup/startup.json`): 1,134 square timer pixels, four presets and all three packaged Bible editions. Its dashboard render was inspected; generated PNG and state remain under ignored test output.
- Checks use synthetic isolated state stores, routed WPF events and in-process windows. They do not verify a physical mouse, high-DPI monitor move, audible sound, or the user's desktop window. Existing real dashboards and credentials were not opened. No layout schema or data migration was needed.

## Nexa 0.1.10 Alpha — Interface & Navigation Cleanup

- Final isolated Release build: `dotnet build ".\SessionDashboard.csproj" -c Release --no-restore -m:1 -p:OutputPath=bin/Alpha010/` **PASS**, zero warnings/errors. Output: `bin/Alpha010/Nexa.exe`; isolated Debug build also passes.
- Check-runner build **PASS**, zero warnings/errors. Five targeted suites pass **250 assertions**, including repeated shared model checks: `--alpha010` **72** (`test-output/20261003-145906`), `--alpha019` **47** (`144658`), `--polish104` **22** (`144907`), `--polish107` **67** (`145014`), `--continuityaudit` **42** (`150016`). Older test switch names retain their identifiers and exercise current code.
- Actual WPF panel checks cover Settings, Add Widgets, History, Changelog and About without secondary title bars; persistent bottom-left Close before/after scrolling; backdrop restoration; topmost Escape with stacked panels; native dropdown Escape; and consumed outside clicks, including clicking the same blank area without reopening the picker. Primary Home/dashboard window controls remain.
- Add Widget is immediately right of Session and opens the full library. Canvas plus shortcuts are absent; blank-space adding remains. Centered rotating tips are noninteractive and leave widget layouts unchanged. Home retains one footer Changelog link, removes greetings while preserving the display name, defaults to four newest non-developer Spaces and expands/collapses compact cards into additional columns.
- Hidden scrollbar controls have zero rendered size. Keyboard Page Down, mouse-wheel input and small wheel deltas still scroll the widget catalog; long panel content retains its Close footer. Physical trackpad gestures were not tested.
- Session/settings regressions verify exact identity/timestamps, countdown advancement while open, debounced autosave, close flush, stopped/untimed sessions, Space switching, locked-file failure and animated retry. Existing notes, Hedy content, profile preferences, layouts, history and saved Space counts survive reopening.
- Continuity regressions additionally verify import/export and credential exclusion, user ownership, reset/continue/merge, recovery snapshots, real thirty-second autosave during continuous typing, edits during background writes, close/reopen and actual I/O failure/retry. Dashboard History now uses shared outside-click dismissal without blocking its owner.
- Timer/stopwatch and placement regressions cover pause/resume/reset, autosave/reload, slow/fast routed drag input, resize/scroll, occupied drops, unchanged neighbors while held, responsive one/two/three-column layouts and Bible passage scrolling with hidden scrollbar chrome.
- Home, dashboard, all five panel types and expanded Recent Spaces captures were visually inspected. Checks use isolated synthetic stores and routed WPF input; no personal store or live account was used. No schema migration, data reset, integration change, widget consolidation or Pixel Swarm overhaul.
- Physical mouse/trackpad feel and desktop interaction remain manual checks. Test windows and captures do not mean the new app was launched on the user's desktop. The existing running Debug app was preserved; isolated output is `bin/Alpha010/`.

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
## Nexa 0.1.16 alpha — 2026-10-05

The exact `dotnet build ".\SessionDashboard.csproj"` and standard Release build both passed with zero warnings or errors. The final Release executable opened and responded; its Windows file version is 0.1.16.0.

Focused WPF checks passed: `--appearance015` (50), `--bugs014` (63), and `--release016` (17). These cover live appearance/persistence, bug movement and scroll coordinates, bounded infestation, shared-edge resizing, Beer Counter goal/count/persistence, occupied drop rejection, adding and removing widgets without shifting another card, Home footer bounds and the lowercase alpha label. The Beer Counter render capture was inspected after its liquid fill transition.

Physical tests at 100%, 125% and 150% DPI, mixed monitors and several fullscreen resolutions remain manual release QA; no such hardware was available to the automated WPF run. Existing pre-0.1.16 working-tree changes were preserved. No commit or push was made.


## Nexa 0.1.17 alpha — October 14, 2026

- Release build passed with zero warnings/errors. `--release017`: 47 passing checks (`test-output/20261014-212150`). Covers independent drink controls, larger distinct artwork, compact saved height, menu icons, beer goal one and warning threshold, dismiss/expiry/every-two reminder behavior and persistence, lap capture/pause/resume/save/reopen/reset, legacy JSON defaults, exact pixel-color handoff, burst geometry/state transitions/probability/count endpoints, spider webs/trapping/escape/expiry and bounded chained bursts. Seeded trials: 498 bursts in 2,000 squashes (24.9%), all thirteen counts 4–16 observed.
- Existing `--release016`: 17 passing layout, scroll, counter save, Home footer and release-history checks. Version assertions now use 0.1.17 and preserve the historical 0.1.16 entry.
- Existing `--bugs014`: 68 passing checks, including original All Three behavior, new type settings, preference isolation, input passthrough, scrolling, lifecycle and shared-edge layout regression.
- Dark/light counter renderings visually inspected. Session timer reset implementation was not changed. Tests use isolated stores; physical mixed-DPI hardware checks remain manual.
- Packaged self-contained Windows x64 `Nexa.exe` reports file version `0.1.17.0`. `--verify-startup test-output/exe-package-017` passed, including all three embedded Bible translations. Root launcher and published executable have identical SHA-256 hashes.

### 0.1.17 spider feeding and beer-bottle refinement — October 15, 2026

- Spider webs now persist until capture and feeding complete. Captives remain fixed in the web, a spider approaches and visibly feeds for 1.35 seconds, then the captive and used web are removed; the spider can create another web at a new random location.
- Beer counters and catalog icons use a dark amber-brown bottle shell with a separate inset yellow liquid area driven by the existing progress fraction.
- Current user-facing version labels are normalized to `Nexa 0.1.17 alpha`; the project and manifest remain 0.1.17/0.1.17.0.
- Focused `--release017`: 50 passing checks. Existing `--arcade017`: 42 passing interaction and performance checks, including 600-bug render p95 below the 33 ms frame budget. Existing `--bugs014`: 76 passing checks. Existing `--release016`: 17 passing layout and counter checks.
- The rebuilt self-contained x64 root `Nexa.exe` reports file version `0.1.17.0`, matches the published executable at SHA-256 `53E5E05C728D01892C18E4DFBEF5FD0A44D34A6FE7F01A31DCCDE13FAC1F3BC2`, and passed isolated startup verification with all three embedded Bible translations.

### 0.1.17 additions — Ladybugs, Centipedes and Arcade

- `--arcade017`: 42 passing behavior, persistence, input and performance checks (`test-output/20261014-214817`). Ladybug first hits never spawn or squash; second hits roll once (498/2,000 bursts), persistent cracked shells and transient hit rings verified. Mixed centipedes: 100 of 6,000 natural spawns (1.67%) across ten seeds, at least five times rarer than each other species. Crawl poses, larger silhouette and sparse species-only populations verified.
- Arcade starts at 24, progresses automatically, accelerates with frequent accepted hits, reaches the 600-bug infestation cap without preference changes, cools click pressure without regressing the reached stage, ignores invalid/repeated squash events, persists settings and restarts run-only progression on reopen.
- Performance report: `test-output/20261014-214817/bug-performance.json`. At 600 bugs: simulation mean 0.054 ms/p95 0.079 ms, 32 allocated bytes/frame, hit test mean 0.0142 ms, full WPF software render mean 24.57 ms/p95 29.94 ms at 1280×720. At the 768-bug burst safety limit: render p95 23.59 ms. Both rendering runs met the 33 ms budget; these runs are separate samples, not evidence that 768 bugs render faster. Live dashboard input callback p95 was 4.35 ms while 600 bugs animated, and note edits saved correctly.
- Initial per-bug vector and image rendering missed the frame budget. The final renderer pre-rotates shared sprite/crawl frames, composites them into one reusable premultiplied-alpha bitmap and updates that bitmap on the animation tick rather than inside OnRender. The pixel surface is bounded to approximately 2.1 million pixels; offscreen bugs are culled. Repeated-late frames or expensive work reduce decorative cadence to 20 fps, with hysteresis for recovery.
- Native-size infestation and enlarged artwork renderings were visually inspected. Numbers are local software-render measurements; physical mixed-DPI and slower-device validation remain hardware-dependent.
- Earlier `--release017` still passes all 47 checks after the additions (`test-output/20261014-214946`), including drink reminders, persisted laps, color transfer, spider webs and burst rules.
- Updated legacy Add Bugs suite: 76 checks passed (`test-output/20261014-215111`), including the larger cap, all new selectable choices, scrolling, input passthrough, theme/preference isolation, suspension, shutdown, and shared-edge layout. Packaged startup probe passed again at `test-output/exe-package-017-arcade/startup.json`.
- Final layout/scroll/counter/Home suite: 17 passing checks (`test-output/20261014-215223`). Final Release build: zero warnings/errors. Total final checks across suites: 182 (42 Arcade/performance + 47 release + 76 Add Bugs + 17 layout). Published executable and delivered launcher have matching SHA-256 hashes.
