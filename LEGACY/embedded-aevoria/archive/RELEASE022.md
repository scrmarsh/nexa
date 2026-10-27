# Nexa 0.1.22 Alpha - targeted GPU performance fix

The reproduced slowdown comes from the composition-driven update/invalidation loop on this host. Independent timer pacing removes the feedback relationship and the approximately 6 callbacks/sec result. This is evidence for a scheduling correction; the underlying WPF/driver synchronization mechanism has not been traced.

## Changes

- **MainWindow.Bugs.cs:** use the existing paced dispatcher timer for updates in all bug modes. Remove the CompositionTarget.Rendering update subscription and the guard that disabled timer updates while subscribed. Existing frame-rate gating, dt clamp, pause/minimize/disable behavior and single shared-world advancement remain. No new timer or rendering engine.
- **ColonyWars.Rendering.cs:** add an internal sprite-rasterization counter for diagnostics. No cache policy, artwork resolution or sampling-quality change.
- **tests/HardwareAudit.cs / tests/Program.cs:** default to timer pacing; keep --legacy-render-loop to reproduce the old loop. Add foreground real-dashboard, Classic, and linear-sampling comparisons; record independent callback/update counts, cache misses and other Nexa processes. Test saves are isolated. --software-only remains per diagnostic window.
- Release metadata and in-app notes target **0.1.22 Alpha**. Version assertions and short README/CHANGELOG entries updated. Save schema, gameplay rules, GPU support, software fallback and dependencies are unchanged.

## Release before/after measurements

Same machine, 100% DPI, 1200x800 test windows, 180 protected units, high-quality sprites, seeded established worlds, five-second samples. Standalone diagnostic and real dashboard are separate fixtures; compare before/after within each fixture. Dynamic positions evolve with their update cadence, so this is a matched workload, not identical replay frames.

| Probe | Composition callbacks/sec | CPU OnRender calls/sec | Dashboard update ticks/sec |
| --- | ---: | ---: | ---: |
| Legacy GPU diagnostic | 6.19 | 6.19 | - |
| Timer GPU diagnostic | 41.78 | 37.78 | - |
| Timer software diagnostic | 62.45 | 31.13 | - |
| Classic dashboard before | 4.99 | 4.99 | 4.99 |
| Classic dashboard after | 60.46 | 59.86 | 59.86 |
| Infinite dashboard before | 43.65 | 71.49 | 31.84 |
| Infinite dashboard after | 79.33 | 94.31 | 56.15 |

These are CPU-side event/draw-call counts, **not presented GPU FPS**. OnRender may run more than once per composition callback; callbacks can also occur without an overlay redraw. In particular, the new software diagnostic's 62 callbacks/sec represents only 31 OnRender calls/sec. The timer GPU diagnostic records approximately 38 OnRender calls/sec. The real Infinite dashboard already performed much better than the legacy diagnostic before this change because its shared controller supplies independent timer updates.

Linear sampling left the legacy GPU probe at 6.00 callbacks/sec, so that change was rejected and HighQuality retained. Sprite misses during the final five-second samples were 5 for the timer GPU diagnostic, 4 for software, 15 for Classic and 6 for Infinite; this does not support sustained per-frame cache thrashing as the primary cause.

The same production Nexa process (PID 7192) remained present with no main-window handle. It used only about 0.27 CPU seconds between starts of the four final probes, and was neither stopped nor reconfigured. Its presence does not explain why timer pacing fixes the comparison, but uncontrolled GPU/desktop activity remains a limitation. No globally forced software mode or registry/driver settings were introduced.

Evidence: **test-output/gpu022-summary.json**, with all nine source JSON artifact paths and recorded settings. Raw logs: test-output/gpu022-*.log.

## Validation and remaining limits

Release application and test builds passed with zero warnings/errors. **92 Infinite Warfare checks** passed (test-output/20261019-222336), and **138 Classic checks** passed (test-output/20261019-222402). The Classic suite includes its two-hour simulated soak and live editing/minimize checks. Normal and Arcade executable startup probes both exited 0 and reported 0.1.22 Alpha (test-output/startup022 and test-output/arcade-startup022). Relevant checks cover continuous-save replay/recovery, shared views, scrolling, Classic combat/lifecycle, editing responsiveness, minimize/restore and timer cleanup. CPU/GPU power, presentation latency, mixed-DPI/4K behavior and other adapters remain unmeasured. The prior OS profiling-policy restriction prevented a GPU ETW trace; exact compositor/driver wait behavior remains unresolved. No claim of universal 60 FPS.

Reproduce the matched diagnostic comparison from the repository root:

~~~powershell
dotnet build tests/SessionDashboard.Checks.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --hardware-audit --foreground --legacy-render-loop
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --hardware-audit --foreground
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --hardware-audit --foreground --software-only
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --hardware-audit --foreground --dashboard --classic
~~~

Use --dashboard without --classic for the persistent Infinite dashboard fixture. High-quality sampling remains the default. No procedural maps, new graphics backend or unrelated gameplay changes were added. Existing root Nexa.exe and packaged ZIPs were not replaced; the newly built executable is bin/Release/net10.0-windows/Nexa.exe.
