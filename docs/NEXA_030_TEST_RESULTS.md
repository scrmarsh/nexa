# Nexa 0.0.30 Alpha — verification

Starting source: `095c635f56e9f18d45793d5547518c23ec955360`. Final application/test source: **`1c15c39975c0d74a0e0995e329c7cbe06a6dcc3e`**, branch `codex/nexa-030-nexaween-refinement`. October 28, 2026. Local only; no GitHub publication or live user-data fixture.

## Commands and scope

Run from `C:/Users/Sean Marsh/Documents/ChatGPT/Nexa/continuation-source`:

```powershell
dotnet build tests/SessionDashboard.Checks.csproj -c Release -p:BaseOutputPath=bin/Nexa030/ -p:UseSharedCompilation=false -nodeReuse:false -m:1
& tests/bin/Nexa030/Release/net10.0-windows/SessionDashboard.Checks.exe --nexa030
& tools/Verify.ps1 -SkipBuild -OutputBase bin/Nexa030/
```

The final [Release build](verification/nexa030/nexa030-build-final.log) completed in **20.48 seconds, zero warnings and zero errors**. The isolated output root avoids replacing the user's open .29 executable. The final suite runs the committed code above; subsequent changes are reports and evidence only.

The focused route covers current identity/history, invalid/legacy hit-tolerance defaults, six species under projection/scroll/orientation, topmost eligible hits, native WPF hit routing over controls, pointer capture/release, immediate fatal state/recovery registration, deferred durable snapshots, repeated/rapid event IDs, failed-save recovery, fullscreen navigation/restoration, F11/Escape preferences, caption vector state/actions/accessibility, preview cancellation in both host modes, fresh windowed restart, twenty simulated minutes of bounded companion behavior, candle amplitude/smoothness/phase, seasonal-off/hidden/reduced-motion timers, shared Classic Windows icon and small-window footer clearance. It captures thirteen actual WPF content trees.

Current-version assertions in older routes now expect .30. Their historical fixtures/behaviors remain; obsolete expectations for an in-host preview, the removed seasonal progress bar and the old fixed spider-path helper were updated to the requested behavior. The independent five-second preview duration gate remains tested.

## Failures and corrections

1. Initial preview fixture did not compile: `NexaOptions` is a class, not a record, and `SeasonalMode` is a string. Corrected the fixture; subsequent builds compile cleanly.
2. The first 19-route run failed `nexaween027` and `handoff026` because the new preview continuation could run without a WPF synchronization context. Fade/close now explicitly dispatch to the owner thread. [Original logs](verification/nexa030/first-suite-failures/nexaween027.log), [handoff log](verification/nexa030/first-suite-failures/handoff026.log).
3. That run also failed `pass026`'s old four-second fixed-path assertion and `alpha026`'s expectation that a seasonal loading track becomes visible. Replaced those obsolete assertions with bounded stateful movement and the theme-specific track visibility contract. [Original pass log](verification/nexa030/first-suite-failures/pass026.log), [alpha log](verification/nexa030/first-suite-failures/alpha026.log).
4. One build was attempted while the regression runner still mapped the test output DLL. It failed copying the locked DLL after retries; no application process was killed. [Locked-output build](verification/nexa030/nexa030-build-2.log). After the runner completed, clean builds succeeded. The final complete run occurred without a concurrent build.
5. The extended focused route caught a few pixels of overlap between the pumpkin's reserved rectangle and the footer at 800×600. Increased the bottom reservation to 112 DIP and regenerated captures. [Failed extended run](verification/nexa030/nexa030-second.log), [passing correction](verification/nexa030/nexa030-third.log).
6. Final review hardened model/UI dispatcher ownership, rapid-hit serialization, failed-worker retry, original-session association, close/navigation recovery registration, minimized/hidden Home timers and stationary background pumpkin actors. The final committed revision is covered by the full suite below.

Repeated passing runs are not added together. Cold/intermediate timing runs are kept as diagnostic evidence, not claimed as extra assertions. [First focused run](verification/nexa030/nexa030-first.log), [later focused run](verification/nexa030/nexa030-fourth.log).

## Timing interpretation

The final focused fixture's measurements are in `nexa030.log` below. These are synthetic local samples, not physical click-to-photon measurements or sustained benchmarks. The costly original path was synchronous `SaveNow` plus ecosystem atomic writes on the input dispatcher. The revised pointer path registers state/recovery in memory; snapshot and durable worker I/O follow the input turn. State snapshots still require bounded dispatcher CPU work. Explicit shutdown/manual recovery retains synchronous durability checkpoints.

The companion test simulates 1,200 seconds against a bounded obstacle fixture; it is not a twenty-minute real-time CPU/GPU endurance run. Its candy is decorative and never credits persistent productivity or ecosystem resources.

## Visual and physical acceptance

See the [capture index](NEXA_030_STATUS.md#captures) and [artwork provenance](NEXA_030_ARTWORK.md). These are RenderTargetBitmap captures of real WPF content on disposable data, not AI-generated UI mockups. Windows backdrop/native taskbar composition is outside those captures.

Physical mixed-DPI/multi-monitor geometry, monitor restoration/taskbar/focus, actual hardware pointer feel, Alt+Tab/executable icon appearance, extended CPU/GPU/memory behavior, live provider/recording behavior and subjective art acceptance remain **UNVERIFIED**. Existing .29 provider limitations carry forward. No live credentials, workspace, world, recordings or History were reset or used as test fixtures.

## Final route results

**20 routes / 849 distinct assertions PASS** on the committed implementation. Final directory: `verification/acceptance-20261028-174017`. [Full suite summary](verification/nexa030/nexa030-final-suite.log).

| Route | Assertions | Result |
|---|---:|---|
| `nexa030` | 63 | [PASS](verification/nexa030/nexa030.log) |
| `nexa029` | 87 | [PASS](verification/nexa030/nexa029.log) |
| `nexaween028` | 39 | [PASS](verification/nexa030/nexaween028.log) |
| `nexaween027` | 51 | [PASS](verification/nexa030/nexaween027.log) |
| `handoff026` | 95 | [PASS](verification/nexa030/handoff026.log) |
| `pass026` | 36 | [PASS](verification/nexa030/pass026.log) |
| `alpha026` | 29 | [PASS](verification/nexa030/alpha026.log) |
| `arrivals` | 32 | [PASS](verification/nexa030/arrivals.log) |
| `audit025` | 38 | [PASS](verification/nexa030/audit025.log) |
| `final` | 36 | [PASS](verification/nexa030/final.log) |
| `followup-design` | 31 | [PASS](verification/nexa030/followup-design.log) |
| `followup` | 46 | [PASS](verification/nexa030/followup.log) |
| `master` | 23 | [PASS](verification/nexa030/master.log) |
| `reliability` | 19 | [PASS](verification/nexa030/reliability.log) |
| `core` | 64 | [PASS](verification/nexa030/core.log) |
| `continuity` | 43 | [PASS](verification/nexa030/continuity.log) |
| `shared` | 10 | [PASS](verification/nexa030/shared.log) |
| `menus` | 9 | [PASS](verification/nexa030/menus.log) |
| `ecosystem` | 48 | [PASS](verification/nexa030/ecosystem.log) |
| `animation` | 50 | [PASS](verification/nexa030/animation.log) |
