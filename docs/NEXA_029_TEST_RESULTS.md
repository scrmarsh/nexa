# Nexa 0.0.29 Alpha — test results

October 28, 2026. Starting commit `054fffdc5f0e42f292dcfb28d3a60631b2ba8024`; final application/test commit `34c214507f55ecf87156f6aa7bfe0b9afa9633fc` on `codex/nexa-029-polish`. Reports/captures are recorded in a subsequent documentation-only commit. No live user workspace, credential or provider account was used as a fixture.

## Final gate

Commands from continuation-source:

```powershell
dotnet build tests/SessionDashboard.Checks.csproj -c Release -p:BaseOutputPath=bin/Nexa029/ -p:UseSharedCompilation=false -nodeReuse:false -m:1
& tools/Verify.ps1 -SkipBuild -OutputBase bin/Nexa029/
```

The configurable output root avoids replacing the development executable already open on the user's machine. The default output build was initially blocked by that existing process; it was not killed.

[Final Release build](verification/nexa029/nexa029-final-build.log): **0 warnings / 0 errors**, application and checks, elapsed 20.22 seconds. [Final integrity](verification/nexa029/nexa029-final-integrity.log): **19 routes, 778 distinct passing assertions**, all on the committed application/test source after fixes. Exact execution directory: `verification/acceptance-20261028-160635`. One final rerun of a previously passing assertion is counted once, not added again.

| Route | Assertions | Result |
|---|---:|---|
| `nexa029` | 79 | [PASS](verification/nexa029/nexa029.log) |
| `nexaween028` | 39 | [PASS](verification/nexa029/nexaween028.log) |
| `nexaween027` | 51 | [PASS](verification/nexa029/nexaween027.log) |
| `handoff026` | 95 | [PASS](verification/nexa029/handoff026.log) |
| `pass026` | 36 | [PASS](verification/nexa029/pass026.log) |
| `alpha026` | 29 | [PASS](verification/nexa029/alpha026.log) |
| `arrivals` | 32 | [PASS](verification/nexa029/arrivals.log) |
| `audit025` | 38 | [PASS](verification/nexa029/audit025.log) |
| `final` | 36 | [PASS](verification/nexa029/final.log) |
| `followup-design` | 31 | [PASS](verification/nexa029/followup-design.log) |
| `followup` | 46 | [PASS](verification/nexa029/followup.log) |
| `master` | 23 | [PASS](verification/nexa029/master.log) |
| `reliability` | 19 | [PASS](verification/nexa029/reliability.log) |
| `core` | 64 | [PASS](verification/nexa029/core.log) |
| `continuity` | 43 | [PASS](verification/nexa029/continuity.log) |
| `shared` | 10 | [PASS](verification/nexa029/shared.log) |
| `menus` | 9 | [PASS](verification/nexa029/menus.log) |
| `ecosystem` | 48 | [PASS](verification/nexa029/ecosystem.log) |
| `animation` | 50 | [PASS](verification/nexa029/animation.log) |

## What the new route actually covers

79 assertions: lowercase current metadata and old-option defaults; left label/five-second contract/graduation cap; all distraction phrases; failed save with an exclusive file lock preserving notes/counters; completed archive identity and original values; recording/transcript references; reset counters/water/stopwatch/session squashes while retaining tasks, opt-outs and all-time totals; persisted reset checkpoint/restart and idempotence; work entered after completion; resume/new identities, source immutability, target preferences and score deduplication; native fullscreen geometry/overlay/no data mutation/return dimensions; real Hedy viewport growth; embedded timer controls; repeated themes; missing/damaged/real PNG decoding; archive correctness already on disk during animated completion; duplicate input during animation; transient count/vessel presentation and existing recap; read-only network/default output; direct health damage/full-health resilience and stale combat cancellation; Home/Space fit persistence, two-Space isolation, exact stored widget layout and disabled fullscreen options persistence.

Windows host read-only results: NLM `Wi-Fi / Ethernet · Internet`; Core Audio `Available=True; Level=0.52; Muted=False`. No write to system audio occurred in these tests. These are observed host states, not proof of disconnect/charging/device-event transitions.

Inherited routes include actual child-process termination during repeated atomic saves, corrupt/backup/lock recovery, schedule/DST/postponement, score deduplication, strict exports, task continuity, staged startup, exact approved branding, ecosystem persistence/construction and optional animation. Their passing assertions retain their original scope; a synthetic clock or process interruption is not physical sleep/power-loss acceptance.

## Development failures and reruns

All listed final failures were repaired; none is omitted from the final gate. Local full-run outputs and available build/targeted logs are preserved under docs/verification/nexa029; the original disposable fixtures remain under ignored verification/.

- Initial default-output baseline build: MSB3027/MSB3021, 10 retries because the existing Nexa.exe was running. Switched to isolated bin/Nexa029 output. This was an environment/output conflict, not a baseline test pass.
- Build first: MC3088, resource dictionary property/content ordering. Corrected merged dictionary position.
- Build second: CS0136, new phase variables shadowed interval-loop names. Renamed phase variables.
- Build third: CS1061, nonexistent StopwatchRunning property. Removed it; paused state uses existing StopwatchStartedAt.
- Build seventh: CS1061, nonexistent BeerVessel property. Reused existing BeverageVessel.
- Builds fourth, fifth, sixth, eighth, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth and final passed with zero warnings/errors. Original per-build logs are preserved. Failed route traces from the first three executions are retained in the development-first, development-second and development-third evidence subdirectories.
- Authoring tooling interruptions: unavailable Windows Store Python alias, an encoding error during a generated edit and a script string-literal error. Used the bundled UTF-8 runtime, restored the affected timeline file from HEAD before reapplying edits, and repaired the script. No live data was involved.
- First full inherited run: 11 failed routes. A reset marker was accidentally inserted under a null-only portable JSON annotation; corrected to keep that marker only in DashboardSession. Remaining failures included superseded expectations for labels, task clearing, timer editor window, elapsed main timer and oversized webs. They were updated to the explicitly requested behavior, with safety checks retained.
- Second full run: 5 failed routes (nexaween027, handoff026, master, core, continuity). Updated the remaining current metadata/minimum-size/editor/task expectations and investigated lifecycle expectations.
- Third full run: 4 failed routes. Fixed real startup null-workspace access and a WPF dispatcher violation after the new Home collapse await. Restored the prior scheduled review/postponement contract instead of changing its tests to permit early data reset. Continuity/master expectations reflect preserved tasks and embedded editor.
- Fourth full run: **all 19 routes / 771 assertions passed** before the seven additional wallpaper assertions and final defensive checks. This is prior evidence, not the final count.
- Targeted first/second: 60 passed. Targeted third stopped on an invalid synthetic animation Session without a required title; fixed fixture. Targeted fourth stopped on an unpopulated synthetic ecosystem fixture; inserted explicit creatures. Targeted fifth: 72 passed. Targeted sixth: 79 passed. Final full run: **778 passed**.
- Final implementation was committed before the final Release rebuild. The final suite reran all routes after the dispatcher, lifecycle, presentation, wallpaper and defensive fixes. No executable/test source changed afterward.

## Native rendering and remaining acceptance

Captured controls are native WPF RenderTargetBitmap renders, not HTML approximations. Final captures: Home/fullscreen/closing from verification/20261028-160636; compact seasonal splash/Home from 160649; Productivity from 160658. Curated images are linked in [status](NEXA_029_STATUS.md#captured-native-wpf-rendering). The initial arbitrary-width fullscreen crop was replaced by actual render-size capture. Inspected Home, actual-size fullscreen, closing sequence and compact seasonal preview. Pixel/title/asset integrity and more captures are exercised by inherited routes.

**Manual acceptance not yet performed:** actual taskbar coverage and focus/Alt-Tab, physical multiple monitors/mixed DPI, charging/no-battery/device changes, volume writes/external volume changes, microphone quality/permissions, live Hedy/Hevy/Todoist auth/sync, abrupt physical interruption at a precise finalization instruction, actual sleep/hibernate and extended CPU/GPU/FPS/memory. Live integrations were not verified. No clean-install/newer-package update or Beta acceptance is claimed. See the status carry-forward for runnable acceptance scope.

Persistence compatibility: format 2 retained, additive defaults only, source History immutable for restore, no marker in strict portable exports, no migration or deletion of user data. Original artwork has zero diff. No push, release, tag, approved-build promotion or Beta transition.
