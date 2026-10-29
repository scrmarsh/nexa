# Nexa 0.0.31 Alpha — test results

October 28, 2026. Starting HEAD `328dfacdc37647b71441f76b892122016c11d026`; final application/test checkpoint **`c1145c8fa3e1f37470712f8646ea76e91444216f`**, branch `codex/nexa-031-integrity-ecosystem`. Reports/captures are recorded separately. No live workspace, provider account, recording or historical user ecosystem was used as a fixture.

## Commands

From `C:/Users/Sean Marsh/Documents/ChatGPT/Nexa/continuation-source`:

```powershell
dotnet restore SessionDashboard.csproj -r win-x64 --configfile tools/Publish.NuGet.Config
dotnet build tests/SessionDashboard.Checks.csproj -c Release --no-restore -p:BaseOutputPath=bin/Nexa031/ -p:UseSharedCompilation=false -nodeReuse:false -m:1
& tools/Verify.ps1 -SkipBuild -OutputBase bin/Nexa031/
dotnet publish SessionDashboard.csproj -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:BaseOutputPath=bin/Nexa031Package/ -p:UseSharedCompilation=false -nodeReuse:false -m:1 -o verification/package031
& tests/bin/Nexa031/Release/net10.0-windows/SessionDashboard.Checks.exe --packaged031 verification/package031/Nexa.exe
```

The final restore and packaged route used normal Windows/network access; the initial restricted-runner limitation is documented below. Source and package outputs are separate from the running .30 candle build and approved binaries. No process owning a live user workspace was killed or replaced.

The final committed-source application/checks build took **14.26 seconds, zero warnings and zero errors**. The final publish completed without warnings/errors after the successful normal-network restore. Its informational version is `0.0.31-alpha+c1145c8fa3e1f37470712f8646ea76e91444216f`; this rebuild ensures package provenance identifies the saved implementation checkpoint rather than the starting .30 commit. [Build](verification/nexa031/nexa031-build-final.log), [publish](verification/nexa031/nexa031-package-final-build.log), [restore](verification/nexa031/nexa031-final-restore.log).

## Scope of added checks

The `.31` route verifies canonical current/tag identity, confirmed versus unknown Alpha maps, genuine/unsuffixed Beta identity, real captured unsorted release selection, incomplete-package errors, normal/maximized/restored/F11 Home, centered title centroid and larger icon, Session Settings icon action, grid preview/Cancel/Save/persistence, cap-and-gown rendering, Updates promotion, Sound Style/Test Sound adjacency and selected cue, wrapped Pixel Timer status, ordinary restart world recovery, explicit same-session reset, preserved notes/session/world identity, pending awards through reset, persisted generation/no stale webs, rounded eight-leg roots, empty orb topology, physical progress, bounded travel and stopped growth without a builder.

The original twenty routes remain. Current identity assertions now expect .31, and the historical .26 note is looked up under its corrected canonical number. No lifecycle/provider/save assertions were removed. The old `.27` fixture's hypothetical `0.1.27-alpha` is now described as unconfirmed rather than being silently canonicalized. Assetless/foreign newer packages now assert explicit `InvalidDataException` instead of accepting `null` as a possible false up-to-date result. Adding the requested icon also exercises one additional existing Alpha icon assertion.

## Native package route

`--packaged031` uses the approved cached .29 executable only after matching its live published SHA-256. A disposable copy is upgraded to the real self-contained .31 package. It tests successful replacement, deliberate failed-health rollback to exact original bytes, unchanged workspace bytes and permanent totals, durable state, unchanged approved source file, plus the existing real-helper/idempotent/interrupted/tampered/staged/restart/health route. **17 assertions**, counted once.

Final package size: **72,660,057 bytes**. SHA-256: `47C3B56503AC185E5F6E33642E8CBB4D1097376C4C8605D4DAE704520A06C24B`. Public upload did not occur. Native health confirms .31 Alpha, x64, PerMonitorV2, 759 square pixels, four presets and full packaged Bible counts. Details: [updater validation](NEXA_031_UPDATER_VALIDATION.md), [package identity](verification/nexa031/package-identity.json), [health report](verification/nexa031/startup.json).

## Failure and rerun journal

1. The initial new fixture had an invalid object initializer after a helper call; compiler CS1002 was fixed. [Initial build](verification/nexa031/nexa031-build-first.log).
2. An intermediate fixture nullable dereference warning was fixed; subsequent builds have zero warnings. [Intermediate build](verification/nexa031/nexa031-build-fourth.log).
3. First full suite: `.27` failed because its hypothetical unconfirmed legacy candidate could still compare as numerically newer. The shared comparator now rejects unsupported Alpha-family candidates. [Original failure](verification/nexa031/first-suite-failures/nexaween027.log).
4. That run's `followup` expected `null` for a newer release pointing at foreign assets. The new explicit incomplete/untrusted-package error is required to avoid false currentness, so that assertion and the analogous assetless fixture now require the error. [Original failure](verification/nexa031/first-suite-failures/followup.log), [first suite](verification/nexa031/nexa031-suite-first.log).
5. Initial restricted publish logged NU1900 because NuGet vulnerability metadata could not be reached. A normal-network restore succeeded and final publish is clean; audit suppression was not used. [Initial package build](verification/nexa031/nexa031-package-build.log).
6. Restricted native startup failed before managed code. Direct stderr reports bundle extraction could not commit files inside the agent sandbox temp directory. Normal Windows access passed real startup and all packaged helper/restart cases. This is an execution-environment distinction, not a requirement for end users to run as administrator. [Restricted package failure](verification/nexa031/nexa031-package-checks.log), [bundle diagnostic](verification/nexa031/nexa031-native-stderr.log), [first normal-access pass](verification/nexa031/nexa031-package-native-checks.log).
7. After saving implementation commit `c1145c8`, application/tests/package were rebuilt so embedded source provenance matches that commit. The full suite and native packaged route were repeated on these final bytes. Earlier passes are retained as evidence and are not added to final assertion totals.

## Visual and external acceptance

Twelve real WPF captures are indexed in [status](NEXA_031_STATUS.md#native-wpf-evidence). They cover Home modes, settings/picker, readable timer, directional spiders and actual partial silk. The completed-web inspection fixture intentionally sets completed strands and is labeled synthetic; movement/progress and missing-builder checks independently test construction.

Physical mixed-DPI, monitor/taskbar/focus, the reported maximize disappearance on the user's actual machine, listening to sound styles, sustained CPU/GPU/memory and the reported updater behavior on a **second computer** remain UNVERIFIED. The current-source synthetic maximize case did not reproduce pumpkin disappearance. Google/Microsoft provider work and live authorization are deferred by direct user request. Existing provider limitations remain; mocks/packaged tests do not close them.

## Final route gate

**21 regression routes / 883 assertions PASS**, plus **17 native packaged assertions PASS**. Counts include each final route once; reruns are not added. Final regression directory `verification/acceptance-20261028-185547`; final packaged directory `verification/20261028-185549`.

[Full regression summary](verification/nexa031/nexa031-final-suite.log), [final packaged route](verification/nexa031/nexa031-package-final-checks.log).

| Route | Assertions | Result |
|---|---:|---|
| `nexa031` | 33 | [PASS](verification/nexa031/nexa031.log) |
| `nexa030` | 63 | [PASS](verification/nexa031/nexa030.log) |
| `nexa029` | 87 | [PASS](verification/nexa031/nexa029.log) |
| `nexaween028` | 39 | [PASS](verification/nexa031/nexaween028.log) |
| `nexaween027` | 51 | [PASS](verification/nexa031/nexaween027.log) |
| `handoff026` | 95 | [PASS](verification/nexa031/handoff026.log) |
| `pass026` | 36 | [PASS](verification/nexa031/pass026.log) |
| `alpha026` | 30 | [PASS](verification/nexa031/alpha026.log) |
| `arrivals` | 32 | [PASS](verification/nexa031/arrivals.log) |
| `audit025` | 38 | [PASS](verification/nexa031/audit025.log) |
| `final` | 36 | [PASS](verification/nexa031/final.log) |
| `followup-design` | 31 | [PASS](verification/nexa031/followup-design.log) |
| `followup` | 46 | [PASS](verification/nexa031/followup.log) |
| `master` | 23 | [PASS](verification/nexa031/master.log) |
| `reliability` | 19 | [PASS](verification/nexa031/reliability.log) |
| `core` | 64 | [PASS](verification/nexa031/core.log) |
| `continuity` | 43 | [PASS](verification/nexa031/continuity.log) |
| `shared` | 10 | [PASS](verification/nexa031/shared.log) |
| `menus` | 9 | [PASS](verification/nexa031/menus.log) |
| `ecosystem` | 48 | [PASS](verification/nexa031/ecosystem.log) |
| `animation` | 50 | [PASS](verification/nexa031/animation.log) |
| `packaged031` | 17 | [PASS](verification/nexa031/nexa031-package-final-checks.log) |
