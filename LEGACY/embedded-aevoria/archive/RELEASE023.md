# Nexa 0.1.23 Alpha - Living Worlds

Open **Arcade** on Home, or **Arcade: COLONY WARS / LIVING WORLDS** in Add Bugs. Choose Colony Wars, or create/load a Living World using a name, entered/random seed and map size. The existing tiny bug-play link resumes the selected mode.

Completed: deterministic continents/islands, oceans, beaches, rivers/lakes, grassland, forests, deserts, hills, mountains and snow; asynchronous chunk loading/prefetch; cached visible terrain and a coarse overview; widget-friendly scrolling background; shared pan/zoom battlefield; land-constrained ground units with water-crossing flies; separate persistent worlds and camera preferences. Colony Wars combat and saves are retained.

Worlds default to 512x512 logical tiles (3072x3072 world units), with 256/640 tile options. Chunk size defaults to 64; the model supports 32/64/128. Coordinate hashing is independent of generation order, including negative chunk coordinates. The finite map is surrounded by ocean. Chunk generation uses two bounded workers and a short-lived overview task; obsolete requests are ignored. Cache accounting is capped at 8 MiB per session, with additional bounded overview/view buffers. Inactive views release chunk residency. Rendering batches visible chunks into a cached image with halo samples, avoiding texture-edge seams and full-world overdraw. Terrain uses [bilinear sampling](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.bitmapscalingmode); bug artwork retains its existing quality. Living dashboard painting targets 30 Hz, or 10 Hz when the battlefield has priority; simulation advancement remains separate and timer-paced.

New saves: **%LOCALAPPDATA%\SessionDashboard\living-worlds\<world-id>\world.json** plus **arcade-world.json** in that directory. Metadata schema/generator version 1 records identity, name, seed, size, UTC creation, camera and terrain edits. Unmodified terrain is regenerated; tiles are not serialized. Existing Colony Wars arcade-world.json stays in its original location. New generation always creates a new ID/directory. Simulation saves retain atomic replacement, backups and explicit recovery. Unsupported generator versions are rejected. There is no terrain-edit UI yet; edit records already round-trip and apply.

## Validation

- Release application/check builds: **0 warnings, 0 errors**.
- **81 focused checks passed**: test-output/20261019-230949. Covers malformed-file preservation, seeded/order-independent geography, all eleven observed biomes, negative coordinates, matching halos and rendered seams, bounded cache movement/obsolete jobs, async input responsiveness, separate saves/new IDs, terrain constraints, settings safety, shared views, widget edits/scrolling, pan/cursor zoom/overview, camera persistence and 120-step original-versus-restored simulation replay. Includes the inherited 16 shared-view checks.
- **138 Colony Wars checks passed once**: test-output/20261019-225312, including its two-hour simulated soak. Later changes affected only Living rendering/pacing and its test fixture.
- Normal and Colony startup probes passed: test-output/startup023 and arcade-startup023. A **fresh process loaded the saved Living World** successfully with six colonies and 127 units: test-output/living-startup023 (cloned isolated fixture, no production saves touched). All report 0.1.23 Alpha.
- Final fixture: 450 simulation steps took **104 ms** (115 entities at that point). Foreground two-view animation recorded **27.4 OnRender calls/sec** while accepting **26 widget edits in 1.2 seconds**. This measures CPU-side drawing calls, not presented GPU FPS.
- Isolated forced-software terrain/insect render p95: **36.78 ms**, with 46 resident chunks accounting for **1,803,200 bytes**. Earlier full-overview/per-chunk draws and simultaneous background repainting produced 48-94 ms; visible-image batching and background pacing addressed that work. These are host-specific measurements, not a universal frame-rate guarantee. Screenshots in the focused-test directory were inspected.

## Files and limits

Created LivingWorld.Models.cs, .Terrain.cs, .Chunks.cs, .Bugs.cs, .Store.cs, .Dialog.cs and tests/Living023Checks.cs. Updated MainWindow.Bugs.cs/Widgets.cs, DashboardBugs.cs/Camera.cs, BattlefieldWindow.cs, ArcadeWorld.Controller.cs, App.xaml.cs, HomeWindow.cs, SettingsDialog.ArcadeWorld.cs, test routing/version assertions, release metadata and brief handoff/release notes. No dependencies or new graphics engine.

Limits: finite map sizes; stylized noise-based waterways rather than drainage simulation; simple shore avoidance rather than global island/obstacle pathfinding; no naval travel or advanced biome rules. The overview is deliberately coarse. CPU software rendering and dual-view performance still need broader hardware/DPI testing. Future civilization systems listed as out of scope were not added.

Build: dotnet build tests/SessionDashboard.Checks.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1. Focused tests: dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --living023.

Current executable: **bin\Release\net10.0-windows\Nexa.exe**. Root Nexa.exe and older ZIP/publish artifacts were not repackaged.
