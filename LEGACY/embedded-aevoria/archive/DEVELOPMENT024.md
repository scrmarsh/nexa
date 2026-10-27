# Age of Kingdoms: geography/rendering milestone (unreleased)

**Current Phase 3 development:** Functional culture/government, leader/dynasty, settlement/economy/construction and cultural rendering/event foundations are implemented for newly enabled worlds. Read [CIVILIZATIONS024.md](CIVILIZATIONS024.md) first for the checkpoint, save/backup requirements, acceptance evidence and limits. Existing worlds keep their old mode. Metadata stays 0.1.23 and the root executable is unchanged. Phases 4–5 and final release sign-off remain pending. Earlier milestone notes below are historical records.

**2026-10-20 Phase 2 update:** Nations, persistent land-clipped territories and interactive map selection/focus are now implemented. Read [NATIONS024.md](NATIONS024.md) for the current milestone, file list, save compatibility, measurements and remaining limits. Build, 63 nation checks, 47 geography checks, 81 legacy/shared-view checks, 138 Colony Wars checks and 94 catalog checks pass. Final war021 fails its unchanged software-render timing limit; full sign-off and phases 3–5 remain pending. The text below preserves the original geography milestone record.

The v0.1.24 request is only partly implemented. Version metadata remains **0.1.23**; this is a development build, not the finished Age of Kingdoms release. Nations, historical buildings/royals, military overhaul, territory focus, and dashboard opacity controls remain pending. No executable package was replaced.

## Implemented

- Generator **2**: coherent continental elevation/rainfall, latitude-based climate, broad biomes, muted colors, slope lighting and mountain highlights. Small shelf islands accompany the main landmasses. Priority-flood depression filling and steepest downhill drainage create connected rivers flowing to oceans. No fluid simulation; new-generator depressions are filled rather than rendered as lakes.
- New worlds use generator 2. Existing generator-1 worlds retain their original terrain and renderer; there is no automatic migration. Preserve both algorithms in future work. Do not change generator 2's output silently after this milestone.
- Square map choices retained; added 1024x576, 1536x864, 2048x1152 logical tiles. These dimensions are independent of monitor resolution. Each tile still spans six simulation units.
- Additive metadata stores map height and terrain quality. Large simulation dimensions require the explicit LivingGeneratorVersion=2 snapshot extension. Colony Wars still has its original dimension limits; its default extension field is omitted from JSON. Existing atomic replacement/backup and identity checks remain in use.
- New-world rendering uses asynchronous, visible chunks at eighth-octave detail bands based on camera scale, viewport DPI and quality. Continuous atlas sampling sharpens coastlines/relief at close zoom without regenerating terrain. The small overview is a temporary loading image, not the final full-world terrain image.
- Performance/Balanced/High terrain choices target approximately 1024x576, 1920x1080, and native viewport detail capped at 4K. Native bug artwork is unchanged. Quality persists with the world.
- New-world chunk residency is capped at **96 MiB per opened world** (legacy remains 8 MiB); two workers and the bounded 64-item queue are retained. Per-view mosaics/bitmaps, drainage data, WPF/GPU storage and object overhead are additional memory. This is not a total-process memory limit. Opened-session retention remains as before.
- Fit World now accounts for rectangular width and height, including zoom below the previous minimum for large worlds. Cursor anchoring/pan remain intact. Following a bug zooms closer, tracks smoothly and clears selection on death; manual camera controls exit following.

Main files: LivingWorld.Continents.cs, LivingWorld.Detail.cs; extensions to LivingWorld.Models/Terrain/Chunks/Store/Dialog/Bugs, BattlefieldWindow, and the existing snapshot validation/capture/restore. No dependencies or graphics engine added. Population budgets, combat and reproductive rules were not changed.

## Verification

Release build: zero warnings/errors. Focused `--living024`: **47 checks passed**, artifacts `test-output/20261019-235949`. Existing `--living023`: **81 passed**, artifacts `test-output/20261019-235621`. Existing `--colony019`: **138 passed**, artifacts `test-output/20261019-235701`. Existing `--war021`: **92 passed**, artifacts `test-output/20261020-000112`. Total: 358 checks across four suites.

Tested seeds produced 3/4/5 major connected landmasses, 31.8-40.2% land, and one ocean containing 100% of sampled water. Drainage branches were checked for downhill routing, connected ocean outlets and cycles. Chunk halos agreed at multiple detail levels. Legacy generator's exact biome census was preserved. Generation measured 94-191 ms for these seeds. Counts are sampled diagnostics, not a guarantee about every possible seed.

Corrected physical-pixel captures at Windows 150% DPI:

| Target | Terrain source | Cached bytes | Software capture p95 |
|---|---|---:|---:|
| 1920x1080 | 1730x1026 | 31,948,000 | 70.04 ms |
| 2560x1440 | 2498x1410 | 50,646,400 | 111.72 ms |
| 3840x2160 | 3458x1986 | 93,609,600 | 245.00 ms |

These are offscreen software captures, **not GPU frame times or presented FPS**. Initial detail loading plus captures took roughly 0.6-2.7 seconds. The new two-view foreground probe measured 15.9 OnRender calls/sec and accepted 21 note edits in 1.5 seconds while terrain streamed. That leaves performance work before release. Legacy isolated software capture measured 34.98 ms with 25.9 foreground OnRender/sec; an earlier run measured 84.91 ms before retaining legacy controls. Do not infer a proven renderer cause from those two runs. Colony Wars CPU/software p95: 0.12/31.23 ms.

Normal executable startup and a **fresh-process saved generator-2 rectangular-world startup** both exited zero, with PerMonitorV2 confirmed. Evidence: `test-output/startup024-phase1` and `test-output/20261019-235949/wide-session/startup.json`. The latter loaded six colonies and 15 bugs.

## Remaining work

Continue with persistent nations, land-clipped territories/borders, connected-region labels and click-to-focus. Then historical settlement/royal identities and architecture, coordinated military/conquest/succession, followed by widget-background opacity and final acceptance/performance validation. Existing terrain movement still uses local shore avoidance; no intercontinental transport/pathfinding is added. The old Colony Wars labels/AI still appear in Living Worlds until those phases are implemented. Mixed-monitor DPI transitions and actual presented GPU performance remain unverified. High-resolution software captures are too costly to claim smooth 4K rendering.

Current development executable: `bin/Release/net10.0-windows/Nexa.exe`. Root `Nexa.exe` remains the old packaged release. Do not bump release metadata or package this as the complete 0.1.24 update yet.

Commands:

```powershell
dotnet build tests/SessionDashboard.Checks.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1 -nodeReuse:false
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --living024
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --living023
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --colony019
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --war021
```
