> v0.1.22 follow-up: the diagnostic's self-invalidating composition loop reproduced the slowdown. Timer pacing and a real foreground dashboard comparison clarify the result; see RELEASE022.md. The original measurements below remain historical evidence, and the diagnostic now defaults to timer pacing.

# Nexa hardware, RAM and GPU investigation

Date: October 19, 2026. Scope: investigate the current v0.1.21 renderer and prepare for chunked maps/procedural generation. Production rendering, settings, saves and packaged Nexa.exe were not changed. Added an opt-in diagnostic in tests/HardwareAudit.cs and a --hardware-audit route in tests/Program.cs. No new dependencies or registry settings.

## Findings and recommendation

Nexa already uses WPF's Direct3D-backed composition path when the environment supports it. This machine exposes an NVIDIA GeForce RTX 3060 Ti and WPF Tier 2. Measured GPU allocations support that this diagnostic's default path reaches the GPU. However, acceleration is not synonymous with fast rendering: default composition cadence was unexpectedly much slower than a per-window software comparison in this session. Resolve that discrepancy before choosing a new graphics backend or claiming GPU frame-rate gains.

Keep WPF for Nexa's menus, settings and widgets. Build a renderer-independent chunk/world data layer and bounded asynchronous generation first. Prototype batched GPU rendering for the battlefield if the intended chunk workload exceeds cached WPF drawing performance. Treat a dedicated Direct3D 11 renderer as a candidate to benchmark, not an engine already selected or implemented. Keep a CPU/WPF fallback and one authoritative world owner.

## Hardware observed

- CPU registry description: Intel Core i7-8700K at 3.70 GHz; .NET exposes 12 logical processors.
- GlobalMemoryStatusEx reports about 63.93 GiB physical RAM, with roughly 36-37 GiB available during the probes. This is the environment visible to this session, not a portable assumption for all users.
- DXGI identifies NVIDIA GeForce RTX 3060 Ti; dedicated video memory approximately 7.84 GiB. Its observed local video-memory budget was approximately 7.09 GiB, dynamically assigned by Windows.
- WPF Tier 2, process render mode Default, window mode Default. Exposed maximum hardware texture dimensions: 16,384 ? 16,384. A capability limit is not a sensible chunk texture size.
- Runtime .NET 10.0.8, x64, Windows. Tests at 100% DPI, 1184?761 content viewport. These figures do not establish mixed-DPI/4K behavior.
- WMI inventory calls were denied. Inventory above uses ordinary native memory/DXGI calls and a read-only CPU registry lookup; no machine permissions/settings were changed.

## What the code actually does

| Component | Current behavior | Consequence for the next update |
| --- | --- | --- |
| DashboardBugs.cs / BugOverlay.OnRender and RenderGpu | Reissues WPF DrawingContext commands for visible insects, webs, territory and effects. View culling exists for insects, splats and names. | Hardware composition may accelerate these commands, but C# loops, allocations, scene recording and simulation remain CPU work. Web strands are currently traversed without chunk-level view culling. |
| UseGpuSprites | A Boolean retained from older implementations. OnRender always calls RenderGpu; the flag does not select a GPU backend. | Rename/remove this misleading switch during implementation; it is not an acceleration control. Do not report it as evidence of GPU use. |
| ColonyWars.Rendering.cs / BattleSprite | Rasterizes a pose with RenderTargetBitmap on a cache miss; caches up to 128 entries, clearing on DPI change. Key includes species, pose, hurt state, resolution. | Cache misses are CPU rasterization. Cache keys do not include camera zoom; assess resolution/sharpness at high zoom. Whole-cache clearing can produce bursts. |
| BugArtwork.cs | Shared frozen drawings and bitmap poses, with variant caches. | Useful foundation for reusable atlases; retain image quality and variation identity. |
| BugSpriteAtlas.cs | Older CPU pre-rotation/blitting implementation; no active production instantiation was found. | Its existence does not mean the current renderer batches all sprites into one GPU atlas. Avoid optimizing dead paths. |
| ColonyWars.TerritoryRendering.cs | Cached frozen DrawingGroup, rebuilt by territory revision/style; translucent brushes instead of per-cell opacity groups. | Retain the cache idea, partition by chunk/layer and invalidate only affected content. |
| MainWindow.Bugs.cs / ArcadeWorld.Controller.cs | UI-thread updates and view invalidation; shared controller advances one persistent manager. Stopwatch samples measure CPU work/submission. | Current timings do not include completed GPU execution. Preserve single-world ownership while moving expensive generation to workers. |
| ColonyWars.Simulation.cs | Reuses spatial buckets and scans all active units to rebuild them each step. Buckets use positive-coordinate integer truncation. | Avoid advancing every unit in an unlimited world; introduce active/dormant regions. Negative chunk coordinates need mathematical floor division. |
| ColonyWars.Infinite.cs / Territory.cs | Fixed bounded arena (default 1600?1000, dimensions clamped), 12?8 territory grid. | ?Infinite Warfare? is a continuous timeline today, not a streaming infinite map. Chunking is a new data/persistence feature, not merely increasing Bounds. |
| ArcadeWorld.Controller.cs / Persistence.cs | Captures the complete world on dispatcher, then serializes/writes the captured snapshot on a worker. | File writing is asynchronous; snapshot capture is still synchronous. Large maps require per-chunk dirty snapshots and bounded saving. |

Microsoft documents Tier 2 as supporting hardware acceleration for most WPF graphics when resources are available. It explicitly lists RenderTargetBitmap.Render as software rendered. Consequently the earlier approximately 34 ms forced screenshot result is a CPU/software baseline, not a measurement of GPU rendering speed. [Rendering tiers](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/graphics-rendering-tiers).

## Measurements and limits

The diagnostic creates an established 180-unit world with campaigns disabled, 96 territory regions and one web. It opens an ordinary test window briefly and observes five seconds after warm-up. Default and forced-software runs use the same dimensions and seed but evolve for different numbers of callbacks; this is a practical comparison, not identical replay frames. No production world is loaded or saved.

A rendering callback is not a displayed frame. The probe measures CompositionTarget.Rendering cadence, simulation CPU work, drawing submission CPU time, allocations, process/managed memory, cached sprite pixels and snapshot capture. It does not measure GPU execution time, presentation latency, power, sustained leak behavior or a user's foreground fullscreen session. Window occlusion, desktop/session composition, driver behavior and scheduling remain possible influences. Tier 2 and VRAM allocations alone cannot establish the cause of slow cadence.

Measured results and artifact paths follow below. A separate performance-counter attempt could not correlate GPU-engine instances to the diagnostic PID; test-output/hardware-gpu-counters.json contains zero matching samples. Do not interpret that as zero GPU utilization. WPR and WPA are installed. Starting the GPU+CPU trace was blocked by Windows with ?Failed to enable the policy to profile system performance,? error 0xc5585011. No trace was collected; WPR status confirmed it was not recording. No profiling policy or registry setting was changed.

## Recorded results

The latest Release runs had an active visible foreground window; Windows reported an interactive, non-Remote-Desktop session. They reproduced the earlier Debug result. This excludes simple lack of activation and Debug optimization as sufficient explanations, but does not identify a driver or GPU scheduling root cause.

| Release probe, 180 units | Default WPF | Per-window SoftwareOnly |
| --- | ---: | ---: |
| Composition callbacks/second | 6.00 | 46.76 |
| Callback interval p95 | 213.25 ms | 25.03 ms |
| Simulation CPU p95 | 0.206 ms | 0.155 ms |
| Drawing submission CPU p95 | 1.58 ms | 1.27 ms |
| Process private memory | 120.1 MiB | 115.5 MiB |
| Working set | 144.0 MiB | 139.9 MiB |
| Managed memory at sample end | 4.63 MiB | 2.67 MiB |
| DXGI local GPU usage | 19.73 MiB | 0.00 MiB |
| Managed allocation rate across diagnostic | 1.02 MiB/s | 7.77 MiB/s |
| First snapshot capture | 9.37 ms | 9.78 ms |
| Warm snapshot capture p95, 100 samples | 0.122 ms | 0.098 ms |

Default rendering used approximately 6.77 MiB nonlocal GPU memory as well as the local allocation. The software comparison reported no local allocation on that adapter. That supports actual GPU resource use in default mode; it is not an instruction-level trace of each draw operation.

The diagnostic allocates about 170 KiB per composition callback when dividing total sample allocations by callback count; that includes simulation, WPF and diagnostic overhead. This is a future optimization target, not an allocation profile identifying a particular source. The software run has a higher allocation rate primarily because it processes many more callbacks.

Warm full-world snapshot captures allocated about 81.6 KiB each; serialized saves were about 166.2-166.9 KiB. Initial probes mixed the first capture into only 20 samples and therefore overstated typical snapshot cost. The final diagnostic separates cold and warm values. Capture is currently inexpensive when warm at 180 units; streaming still requires a different snapshot design before scaling world data.

Debug repetitions: default 5.80 and 6.00 callbacks/sec; software 45.15 and 44.60. Foreground Debug runs were 5.62 versus 46.41. Disabling ownership overlays and removing every web after each simulation step yielded 6.41 callbacks/sec with zero webs. This did not remove the slow default cadence, so webs/territory overlays alone are not an adequate explanation. Settlement glyphs and normal sprite/combat decorations remained in that ablation.

Artifacts:

- Release default: test-output/20261019-220138/hardware-default.json.
- Release software: test-output/20261019-220146/hardware-software.json.
- Consolidated nine-probe results, including source paths: test-output/hardware-audit-summary.json.
- Corrected no-web/territory ablation: test-output/20261019-195552/hardware-default.json.

A separate running production Nexa instance (PID 7192, root Nexa.exe) was present during this investigation and was left untouched. A later uncontrolled idle sample recorded approximately 309.2 MiB working set and 628.2 MiB private memory, with 0.078 CPU seconds over 7.43 seconds and zero sampled GPU-engine activity in that interval. Earlier global counters showed activity attributed to that instance. Its workload was not controlled during the comparisons, so concurrent application/driver effects remain a confounder. This full-session memory footprint is distinct from the small diagnostic-world process. Evidence: test-output/running-nexa-hardware.json. These observations do not establish a leak or sustained GPU utilization.

These are diagnostic-world measurements, not a packaged-app FPS guarantee. A controlled foreground run of the packaged app with the same workload and a valid GPU trace is still required. Neither RAM exhaustion nor the measured simulation CPU cost explains the large cadence difference.

## Architecture proposed for chunks and procedural generation

1. **Separate persistent world data from rendering.** Define ChunkKey with signed 64-bit coordinates, WorldSeed, GeneratorVersion and layer/domain identifiers. Generate each chunk from a stable coordinate hash; never consume the combat PRNG for terrain generation. Store generated baseline plus explicit player/simulation edits. Pin generator version on existing saves; changing a generator must not silently regenerate explored land.
2. **Make boundaries deterministic.** Use floor division for negative coordinates and globally addressed samples or a neighbor halo for borders, rivers and terrain normals. Test negative locations, generation order, worker-count changes and shared chunk edges. Keep camera-relative coordinates near the viewport to avoid precision loss at large world distances. Integer world coordinates and small local floating-point offsets are preferable to ever-growing render positions.
3. **Use bounded background jobs.** Pure CPU generation on a limited worker queue; prioritize visible chunks, then a small prefetch ring. Cancel obsolete camera requests and reject stale world/version results. Never launch an unbounded Task.Run per chunk. Start by measuring 2?4 generation workers on this 12-thread host, with smaller limits on low-core machines. Keep expensive work and file reads off the dispatcher.
4. **Preserve one state owner.** Workers return immutable generated arrays. Apply results at a controlled simulation boundary; render published snapshots. Do not let workers mutate the live BugManager or WPF objects. WPF resources should be created on the owning thread, or be safely frozen before cross-thread use. Start with the existing simulation owner; moving simulation to a dedicated fixed-step worker is a later measured change with explicit input/output queues.
5. **Stream, cull and cache.** Separate static terrain, occasionally dirty ownership/structures, dynamic entities, and screen-space labels. Reuse a frozen drawing or GPU mesh/texture per chunk/layer. Draw only visible chunks; keep a small warm ring; evict by byte budget with LRU policy. Save dirty chunk edits before eviction. Minimap uses a coarser representation, not a full-resolution world-sized bitmap.
6. **Separate simulation distance from visibility.** Nearby chunks get full AI/collision; farther resident chunks use lower-frequency aggregate economy/war progression; unloaded regions retain compact state. Specify how migrations and battles crossing the active boundary work before implementation. Persistent world progress must not depend unpredictably on camera position.
7. **Bound resource upload and shutdown.** Publish generated results through a bounded queue, upload within a measured per-frame budget and dispose evicted textures/drawings. Handle device loss, minimize/restore, DPI changes, save failures, cancellation and world reset without accepting stale chunks.

WPF drawing objects are lighter than one UI Shape/FrameworkElement per map tile, and static content should be shared and frozen where practical. Avoid a Canvas child per tile or per bug. This is consistent with Microsoft's [2D graphics guidance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging). Bounded worker counts matter because parallel loops have partitioning/synchronization overhead and shared-state risks; measure speedups rather than assuming every spare core helps. [Parallelism pitfalls](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/potential-pitfalls-in-data-and-task-parallelism).

## RAM and GPU budgets to prototype

Use spare RAM to avoid repeated generation, decoding and disk I/O, with explicit limits. Deliberately filling RAM or allocating most VRAM is not an optimization. Current process memory and GPU allocations are small relative to this host; that does not identify memory capacity as the current bottleneck.

Illustrative starting targets, not implemented defaults or supported minimum system requirements:

| Resource | Initial experiment | Rationale |
| --- | --- | --- |
| CPU chunk/data/cache budget | 256?512 MiB with a smaller low-memory profile | Leaves room for WPF, snapshots, generation scratch space, UI and other applications. Size caches by bytes, not only chunk count. |
| GPU terrain/sprite cache | 128?256 MiB initially, subject to current DXGI budget | Enough for meaningful tests without assuming an 8 GB dedicated card. Integrated graphics shares physical memory; GPU and CPU budgets are not independent. |
| Chunk dimensions | Compare 32?32 and 64?64 logical tiles | Measure generation, draw calls, evictions and edit invalidation rather than hard-coding a size from RAM capacity. |
| Resident neighborhood | Start with visible coverage plus one prefetch ring; e.g. 25?49 chunks for a chosen zoom/tile scale | Bound residency; this count is an experiment, not a guarantee that it covers every viewport. |
| Allocations | Reusable arrays, pooled temporary generation buffers, reusable command/entity snapshots | Avoid creating large arrays every frame or on every camera movement; retain ownership until consumers finish. |

Example arithmetic: a 64?64 chunk at 16 bytes/tile occupies 64 KiB of raw tile data; 49 such chunks take about 3.06 MiB before objects, navigation, entities and scratch space. A 512?512 RGBA8 terrain texture uses 1 MiB at its base level; 49 use 49 MiB, or about 65.3 MiB with a full mip chain. Doubling both texture dimensions quadruples memory. GPU render targets, upload buffers and CPU copies add further costs. These examples are memory calculations, not generated-map benchmarks.

Windows exposes an OS-assigned per-process video memory budget and current usage through IDXGIAdapter3.QueryVideoMemoryInfo. Track both local and nonlocal segments and trim caches before memory pressure causes stuttering. Dedicated GPU capacity and shared-memory limits are not amounts to reserve automatically. [DXGI video-memory budgeting](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_4/ns-dxgi1_4-dxgi_query_video_memory_info).

Large .NET arrays can reside on the large-object heap. Retain/reuse suitable buffers and measure allocation/GC behavior; do not add forced GC.Collect to the frame loop. [Large-object heap](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap).

## GPU backend options

| Option | Good fit | Tradeoffs / decision |
| --- | --- | --- |
| Current WPF, cached chunk drawings/images | First chunk prototype and compatibility fallback | Lowest integration cost. Must investigate the observed default-cadence problem and benchmark foreground behavior with actual maps. WPF does not expose a general compute pipeline for the existing C# simulation. |
| Dedicated Direct3D 11 world renderer composited through shared surfaces | Batched terrain, atlases, entity instances and future shader work | Candidate if measured requirements exceed WPF. WPF D3DImage interop involves Direct3D9-compatible surfaces; a D3D11-to-9Ex bridge must be proven. Handle surface sharing, synchronization and device recovery. Use a small number of presentation surfaces, not one D3DImage per chunk. |
| Native HWND-hosted Direct3D viewport | Standalone battlefield prototype | Direct presentation can be simpler, but WPF/child-HWND airspace complicates dashboard widgets layered over the world. It does not automatically satisfy the current dashboard overlay requirement. |
| GPU compute for noise/terrain masks | Later, large independent workloads that remain GPU-resident | Optional acceleration with CPU fallback and explicit determinism rules. Compute is not a free speedup for small chunks or branch-heavy combat; CPU?GPU transfers and readbacks can erase gains. |

Microsoft warns that D3DImage interop has surface-copy/command-flush costs. A DirectX bridge must be benchmarked end to end rather than selected solely because it uses a GPU. [Interop performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/performance-considerations-for-direct3d9-and-wpf-interoperability). Direct3D11 compute shaders support parallel general-purpose work through explicit shader dispatch; adopting them would require a new implementation and device capability checks. [Compute shader overview](https://learn.microsoft.com/en-us/windows/win32/direct3d11/direct3d-11-advanced-stages-compute-shader).

CPU-generated, versioned terrain should initially remain authoritative for gameplay/collision/save reconstruction. GPU noise can produce visual details or accelerate proven large batches later; do not assume CPU and GPU floating-point noise will reproduce the same terrain bit-for-bit on every adapter.

## Recommended next implementation and acceptance criteria

First resolve the default-rendering cadence anomaly with a controlled foreground packaged-app run (accounting for other running Nexa instances) and WPR/WPA GPU/CPU trace, including GPU queue time, presentation, WPF render thread and competing applications. Repeat on a second adapter or machine when possible. Keep registry/driver forcing out of the app.

Then implement a small deterministic chunk prototype behind a world-data/renderer boundary, using the existing WPF backend initially. Compare the same generated scene with a dedicated GPU viewport only if profiling warrants it. Do not increase current unit caps or world bounds in isolation.

Acceptance tests before making chunks the default:

- Same seed/version produces identical chunk data across restarts, generation order and worker counts; negative coordinates and all chunk seams match.
- Camera pan/zoom streams chunks without freezing typing/settings or allowing stale jobs from an old/reset world. Pause, minimize, close and same-world transitions remain correct.
- Dirty chunks survive save, reload and eviction. Incompatible generator versions cannot silently overwrite explored terrain. Bound write queues and crash-safe replacement.
- Crossing chunk boundaries preserves units, targeting and ownership; dormant-region progression follows a documented rule independent of render visibility.
- Memory plateaus during sustained exploration, then repeated return trips. Measure CPU managed/native/private/working set, queue depth, resident chunks and local/nonlocal GPU memory; no unbounded caches.
- Measure p50/p95/p99 and worst cases for generation latency, simulation, snapshot capture, UI input, drawing submission, GPU execution, upload and presentation. Use 16.7 ms and 33.3 ms as 60/30 FPS frame budgets, not as promises from CPU-only timers.
- Test 1080p/1440p/4K, 100/150/200% DPI, maximum webs, map edits, high zoom, two views, integrated/discrete GPUs, software fallback and device loss.

## Reproduce the diagnostic

From the repository root:

~~~powershell
dotnet build tests/SessionDashboard.Checks.csproj -c Release --no-restore -p:UseSharedCompilation=false -m:1
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --hardware-audit --foreground
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --hardware-audit --foreground --software-only
dotnet tests/bin/Release/net10.0-windows/SessionDashboard.Checks.dll --hardware-audit --no-webs --no-territory
~~~

The --foreground variant activates its test window for measurement. Each probe briefly opens a window and closes it automatically; output goes to a timestamped test-output directory. SoftwareOnly applies only to the diagnostic window and is never written into production preferences. The no-territory variant suppresses ownership fills, borders, labels and contested marks; settlement glyphs remain. The no-webs variant removes webs after each simulation step.

## Change and validation scope

Created tests/HardwareAudit.cs and this report; modified tests/Program.cs to add only opt-in diagnostic routing. Updated CODEX_SESSION_STATE.md with the investigation handoff. Debug and Release check-project builds succeeded with zero warnings/errors; Release also rebuilt application source successfully. All diagnostic runs completed successfully. The existing full 335-check gameplay regression was not rerun for this diagnostics-only change. Production source, version metadata, packaged executable, saves and preferences remain unchanged.
