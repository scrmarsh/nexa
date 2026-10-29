# nexa v0.0.32 alpha â€” verification

Final application/test source: `3262017b0d164058c22359f8c071e952757f276b`. Starting source: `6f729f4633c64b70a804cbf103e54adea0b3d3a0`. Evidence is local and separate from the approved public .31 package. See the [status and capture index](NEXA_032_STATUS.md) and [source inventory](verification/nexa032/source-inventory.txt).

**Final results: 24 routes / 1,107 assertions passed, plus 17 separate native packaged assertions.** Application/checks build: zero warnings/errors. Compressed self-contained Windows x64 publish succeeded. [Route ledger](verification/nexa032/route-ledger.json), [suite summary](verification/nexa032/suite-summary.log), [build](verification/nexa032/build.log), [publish](verification/nexa032/publish.log), [native package checks](verification/nexa032/packaged.log), [startup health](verification/nexa032/startup.json), [package identity and SHA-256](verification/nexa032/package-identity.json).

The inherited `--packaged031` route name and a few historical assertion descriptions still say .31; this run uses the exact .32 executable and its health report explicitly records `0.0.32`. It verifies a genuine cached .29 upgrade, exact failed-health rollback, notes/scores/workspace preservation, idempotence, interrupted installation, tamper rejection, separate helper installation/restart and native startup. The approved .29 cache remains unchanged. Native startup reports PerMonitorV2 and current-host DPI scale 1.5; that is not full Appearance acceptance across physical DPI configurations.

## Commands

From `continuation-source`:

```powershell
dotnet build tests/SessionDashboard.Checks.csproj -c Release --no-restore -p:BaseOutputPath=bin/Nexa032Final/ -p:UseSharedCompilation=false -nodeReuse:false -m:1
& tools/Verify.ps1 -SkipBuild -OutputBase bin/Nexa032Final/
dotnet publish SessionDashboard.csproj -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:BaseOutputPath=bin/Nexa032FinalPackage/ -p:UseSharedCompilation=false -nodeReuse:false -m:1 -o verification/package032-final
& tests/bin/Nexa032Final/Release/net10.0-windows/SessionDashboard.Checks.exe --packaged031 verification/package032-final/Nexa.exe
```

## Acceptance coverage

The .32 route covers the shared release identity, complete Appearance bottom/footer reachability, one content scroll owner, dynamic expand/collapse extent updates, saved global themes, contrast selection, all recovered colors, immediate selection/persistence/defaults, seasonal transitions and independent wallpapers. Normal, small, maximized and fullscreen-host configurations run on the current monitor. 100%/125%/150% LayoutTransform checks are **synthetic layout stress tests**, not actual Windows DPI acceptance.

Image checks use synthetic PNG/JPEG/BMP, a valid WebP fixture, corrupt and missing files, cancel, repeated frozen decode caching, fit/fill, removal/default, restart persistence, other-Space separation and history restoration. Native WIC WebP support on this host was available. Injected native selection verifies Settings ownership/topmost transfer in normal/fullscreen hosts. The separately driven native Windows dialog opened over the fullscreen host, accepted the synthetic PNG, updated its preview and preserved fullscreen and selected settings on close: [result](verification/nexa032/native-picker-result.json). That physical UI fixture predates the final contrast/horizontal-template refinements; native picker source did not change afterward. Unrelated native-dialog folder contents were not retained as repository screenshots.

Scrolling checks cover proportional small deltas, normal wheel equivalence, Windows page/zero-line settings, rapid-event bounds, same-frame accumulation, reversal, nested child handling exactly once, boundary handoff, visible draggable native vertical thumbs, Page Down, horizontal track commands, horizontal thumb fit and zero input. Existing menu, selector, editor, dashboard and ecosystem routes remain in the full suite. New capture `nested-scrolling.png` is a controlled WPF nested-scroll fixture; `session-background.png` shows the actual scrollable Session Settings.

No test assertions were removed to obtain a pass. Current-version expectations were advanced to .32 and the existing shared-theme test now locates the same Ember identity by its tagged swatch. A development-only horizontal test initially raised Click directly, which bypassed ButtonBase's command execution; it now executes the actual routed command. The earlier thumb fixture selected the nested child's scrollbar; it now selects the outer scrollbar by TemplatedParent. Both checks assert actual viewport movement.

## Remaining manual acceptance

Physical precision-trackpad gestures, hardware jitter/lag, horizontal/Shift gestures, actual Windows 125%/150% DPI, mixed monitors and second-laptop appearance are **UNVERIFIED**. The shared service does not identify device models or replace OS acceleration. A synthetic delta test cannot certify physical feel. Native picker foreground was physically checked in fullscreen on the current monitor; further normal/maximized hardware sessions remain open. No live account operation, live user-data upgrade or approved binary replacement was performed.
