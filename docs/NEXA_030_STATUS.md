# Nexa 0.0.30 Alpha — implementation status

October 28, 2026. Local Alpha development on `codex/nexa-030-nexaween-refinement`. Starting HEAD: `095c635f56e9f18d45793d5547518c23ec955360`. Application/test implementation checkpoint: **`1c15c39975c0d74a0e0995e329c7cbe06a6dcc3e`**. This report and captured evidence follow in a documentation-only commit.

The actual repository is `continuation-source`, inside the parent Nexa workspace. The existing .29 prerelease, remote branches/tags, approved binaries and desktop shortcut were not changed. Nothing from .30 has been pushed or published.

Final gate: **20 routes / 849 distinct assertions pass**, with a clean Release build (zero warnings/errors). [Exact results](NEXA_030_TEST_RESULTS.md).

## Implemented behavior

| Brief sections | Result | Implementation / evidence |
|---|---|---|
| 1 audit | COMPLETE | [Pre-implementation audit](NEXA_030_AUDIT.md) records the real .29 starting HEAD and targeted findings before source changes. |
| 2 identity | COMPLETE | Shared lowercase `nexa v0.0.30 alpha`, numeric assembly/file/manifest version, new active release note and local-development README/changelog. Historical release entries retained. |
| 3 splash | COMPLETE automated | Exact Classic and seasonal taglines use the shared typing machinery; Classic text is blue/purple. Seasonal loading track and stage text are hidden; real initialization reporting/readiness remains. Five-second monotonic gate still combines with full tagline and actual startup completion. Version label shifts ten DIP right beneath artwork in both modes. |
| 4 developer preview | COMPLETE automated | Standalone centered 620×350 owned window. Host bounds/fullscreen/session are preserved. Closing cancels pending presentation waits and stops typing/display clocks. Windowed and fullscreen-host tests exercise the real preview method. |
| 5 branding | COMPLETE automated | Custom captions subscribe to seasonal changes and use the separate in-app brand image. Classic executable/taskbar/Alt+Tab icon resources remain unchanged and cached. Existing Home/dashboard/splash logo geometry is retained. |
| 6 captions | COMPLETE automated / physical acceptance open | Shared vector dash/square/overlapping squares/X eliminates font-glyph fallback. Existing button styles, click actions and accessible names retained. Maximize/restore state changes and fullscreen hiding verified. |
| 7 fullscreen | COMPLETE automated / physical acceptance open | Shared coordinator carries restoration bounds/state/topmost through Home → dashboard → Home → dashboard. Home honors F11/Escape preferences; Escape does not close Home. Native monitor-bounds hook now recognizes both hosts. Fresh windows restart windowed. Existing dashboard status indicators, notice, top spacing and twelve-DIP notification inset retained. |
| 8 bug input | COMPLETE automated | Species-shaped oriented ellipses follow actual scaled/projected/airborne position, with 1.4–1.6 configurable expansion and three-DIP minimum padding. Widgets no longer exclude eligible bug hits. Topmost visible eligible bug wins; pointer capture consumes movement/release. Outside hits retain ordinary control routing. Damage/red feedback and temporary actual-health bars retained. |
| 9 death latency | COMPLETE targeted / sustained acceptance open | Fatal state and in-memory recovery event register in the input turn. Snapshot/accounting work starts at dispatcher background priority; disk writes run on workers. Event IDs, write-ahead pending events, session association and serialized writes preserve recovery and prevent duplicate awards. Existing save/recovery entry points retained. See timing scope below. |
| 10–13 pumpkin | COMPLETE implementation / subjective acceptance open | One transparent painterly asset feeds Home, widget-edge, background and pumpkin-shaped candy decorations. Large Home art reaches 520 DIP width, scales down, and clears footer. Stationary body/attached curls, warm low-amplitude independently phased candle overlay, shared aspect ratio and cached image. Pumpkin actors no longer recycle/move/fade with candy. Widget vine extensions use two intentional dark-green curved tendrils with gentle existing animation. [Artwork provenance and exact prompt](NEXA_030_ARTWORK.md). |
| 14–16 Home spider | COMPLETE targeted / subjective acceptance open | Persistent state drives wandering, gradual facing, resting, grooming, looking and physically reached candy pickups. Existing eight-leg gait/eyes renderer reused, no costume overlay. Enlarged 2.7 display scale; bounded white-silk territory excludes logo/text/control footprints with clearance. When a small layout has no safe footprint, the companion waits out of view rather than overlapping controls. Candy is decorative, capped at one, collected once, and never touches global balances/saves. |
| 17 seasonal consistency | COMPLETE targeted / subjective acceptance open | Reference-derived pumpkins replace unrelated geometric pumpkin art; other existing candy/seasonal identities retained. Actual WPF captures inspected. |
| 18–19 Off / resources | COMPLETE targeted / sustained acceptance open | Hidden, minimized, reduced-motion or seasonal-off Home stops relevant timers; Classic Home does not keep an empty tagline timer running. Corner drawings cache by size; image is frozen/shared. Window handlers unsubscribe on unload, preview waits cancel on close. Advanced Animations gates companion behavior; Seasonal Animation and reduced motion gate seasonal movement/light. |
| 20 preservation | COMPLETE regression scope / external acceptance open | Existing lifecycle/history/autosave/widget/settings/provider/ecosystem regression routes retained. Saved world IDs, web strands and progress preserved; no live-data migration/reset. Existing twelve-DIP notification padding and Space name-before-icon header ordering retained. |
| 21 tests | See results | [Exact commands, routes, failures and reruns](NEXA_030_TEST_RESULTS.md). |
| 22 evidence | COMPLETE synthetic WPF / physical acceptance open | Thirteen real WPF captures linked below; synthetic fixtures never load the live user workspace. These are not proof of monitor/taskbar/hardware behavior. |
| 23–25 delivery/restrictions | COMPLETE local scope | Audit, artwork provenance, status, test results, active changelog, source inventory and captures. No public actions, Beta transition, approved build promotion or shortcut changes. |

## Persistence and performance

The original fatal-click callback synchronously called `FlushScores` → `MainWindow.RecordEcosystemSquash` → `SaveNow`, followed by `Interaction` → ecosystem `Save`. Those paths include serialized state preparation, atomic file replacement and durable file flushes on the dispatcher. Damage itself was already immediate; persistence could delay the visible next frame.

The revised interaction registers a pending event in memory before navigation/shutdown can run. After the input turn, a frozen ecosystem document containing that event is written on a worker. The matching session ledger is updated on the dispatcher and its prepared workspace payload written on a worker; only then is the pending event removed and the ecosystem checkpoint written again. Repeated IDs are harmless. Rapid writes serialize. Failed workspace saves retain the event; retries preserve an already-applied ledger entry. Closed source windows leave the event for saved-session recovery; an active owner can credit its current or historical session without redirecting the award to another Space. UI models and controls stay on their dispatcher.

The focused fixture measured a **7.257 ms fatal pointer handler**, with durable snapshots deferred, and **34.118 ms background persistence protocol** for its last successful rapid-hit operation. These are single synthetic samples from the final committed-source suite, not a before/after benchmark or a hardware latency guarantee. Earlier cold runs were approximately 7–8 ms input / 190–205 ms protocol. File-system writes no longer occupy the pointer handler; snapshot preparation still does bounded CPU work on the dispatcher. Existing explicit close/manual recovery paths may synchronously checkpoint for durability. The twenty-minute deterministic companion simulation collected 48 decorative candies with no invalid positions in its obstacle fixture.

## Captures

- [Seasonal Home](media/nexa030/home-seasonal.png), [small seasonal Home](media/nexa030/home-seasonal-small.png), [Classic Home](media/nexa030/home-classic.png).
- [Classic compact splash](media/nexa030/splash-classic.png), [seasonal compact splash](media/nexa030/splash-seasonal.png).
- [Shared widget pumpkins](media/nexa030/widget-pumpkin.png), [bug near a widget](media/nexa030/bug-near-widget.png).
- [Spider in territory](media/nexa030/home-spider-center.png), [boundary](media/nexa030/home-spider-boundary.png), [candy present](media/nexa030/home-spider-candy.png).
- [Home fullscreen](media/nexa030/home-fullscreen.png), [dashboard fullscreen](media/nexa030/dashboard-fullscreen.png), [maximized caption controls](media/nexa030/caption-controls-maximized.png).

RenderTargetBitmap records WPF content at synthetic fixture geometry. Window background outside the captured content can appear transparent/black; no desktop screenshot or native monitor acceptance is implied. Image generation was used only for the reusable PNG asset.

## Compatibility and remaining acceptance

Workspace format remains version 2. `EcosystemOptions.HitboxExpansion` is additive, defaults to 1.5 and clamps safely; the existing ecosystem event ledger/recovery format remains. No original history, recordings, credentials, user content, world IDs or saved strand geometry was reset. Home candy is transient decorative state and does not add a persistence format.

Still open: physical mixed-DPI/multi-monitor fullscreen and restoration, taskbar/focus/Alt+Tab appearance, actual mouse feel on the user's hardware, extended CPU/GPU/memory sessions, live recording/provider behavior and subjective visual approval. Prior provider/hardware acceptance limitations from .29 remain; automated checks do not close them. Existing saved web shapes are intentionally preserved, including older geometry; this change does not regenerate user webs.

The implementation file inventory is the exact output of `git show --name-only 1c15c39975c0d74a0e0995e329c7cbe06a6dcc3e`, recorded in [source inventory](verification/nexa030/source-files.txt). Reports/captures/logs are added separately.
