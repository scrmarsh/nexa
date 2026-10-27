# Milestone D — F3 diagnostics implemented; performance acceptance pending

2026-10-20 performance repair: F3 now toggles a lightweight Aevoria-only live monitor. Render FPS explicitly means changed-gameplay WPF callbacks; CPU/GPU/RAM are measured process resources, with unavailable indicators and a busiest-engine GPU convention. Sampling suspends when hidden/inactive and releases native resources on close. Fix transient GPU startup no-data handling. Add repeatable rendering/overhead diagnostics, copied-fixture hashes and opt-in layer timing. All 44 diagnostics checks pass; the full run completes 14 routes and fails the unchanged warfare timing gate at 42.35 ms against 40 ms. Keep 0.0.1 Alpha; no package/version advancement or Milestone E. See AEVORIA_PERFORMANCE_REPAIR.md.

Camera-lag follow-up: move landscape chunk construction off the UI thread, release the covered base-terrain pipeline, use scoped linear terrain scaling and avoid drawing a covered overview. Add completed-detail and cold-camera diagnostics. See `AEVORIA_LAG_FIX.md`; no version/package transition.

2026-10-20. Displayed version remains 0.0.1 Alpha, Development Build — Unreleased. No package or public executable was replaced.

- Added the offline numeric Development Chronicle and About page, preserving original source reports and marking retrospective milestone labels.
- Added borderless gameplay, Escape controls, per-monitor work-area handling, center-preserving resize, faster configurable anchored zoom and a cached political minimap.
- Added deterministic biome vegetation, relief and bounded landscape drawing caches without changing generator output.
- Added production-funded fixed-point treasuries, paid construction/upkeep, conserved transfers, six smoothed prosperity tiers and factual economic history/inspection.
- Added explicit backed-up upgrades for old Aevoria worlds; existing worlds keep prior rules until upgraded.
- Added bounded atomic-replace contention retries and regression coverage. D passes 73 checks; remaining legacy software-render timing failures prevent 0.0.4 acceptance. See AEVORIA_MILESTONE_D.md for measured results and limitations.

---
# Aevoria development history

## Alpha 0.0.1 target — unreleased — Milestone C

- New worlds grow connected kingdoms through bounded, deterministic, simulation-time frontier updates funded by existing civic food/materials and settlement administration.
- Respect actual land connectivity, reject mixed coastal cells as ocean bridges, weight terrain and compactness, and fill supported neutral pockets through real ownership events.
- Extend capital/development/infrastructure influence, validate paid support-settlement founding and retain all existing population/record limits.
- Cache actual shared land frontiers, border settlements and neighbor relationships; expose read-only strategic queries and validated future intervention commands.
- Use political schema 2 plus expansion extension 1 for new C worlds. Preserve old schema-1 worlds without automatic conversion and record aggregated growth/first-border facts in durable history.
- Keep political overlays chunked; compute C label/focus components at regional resolution and retain actual owned anchors. Exclude generated test XAML from application builds.
- See AEVORIA_MILESTONE_C.md and test-output/aevoria-c for measured coverage, regression results, rollback checkpoint and limitations. No release/version bump or Milestone D–I work.

## Alpha 0.0.1 target — unreleased — Milestone B

- Show assembly-derived Alpha 0.0.1 and Development Build — Unreleased on the initial menu and settings pages. Nexa versioning remains independent.
- Add generator 3, Continental Kingdoms, Balanced Continents, Archipelago and Custom geography with stored generation settings. Preserve generator 1/2 output.
- Add independently budgeted Standard through Massive logical worlds, retaining Small and marking Massive Experimental. Keep the ten-world library.
- Add staged asynchronous generation, phase progress, cooperative cancellation, persistent random/manual seeds and Copy Seed.
- Add four presentation levels with hysteresis, hidden distant sprites/effects/buildings, connected distant river vectors, native political/settlement labels and read-only military sector markers.
- Add military inspection/focus, smooth anchored wheel zoom and bounded pan. Keep simulation state, unit identities and orders independent of zoom.
- Retain cached chunk/political/settlement rendering; add offscreen label culling. Fix map clicks being interpreted as window drags and bound menu fade closure independently of compositor completion.
- See AEVORIA_MILESTONE_B.md for current measurements, test results, compatibility and limitations. No public alpha release or Milestone C–I implementation is implied.

## Alpha 0.0.1 target — Milestone A foundation

Standalone WPF host, independent menus/storage, ten-world library, full-history backups/import, Nexa Home launcher and separation from traditional bugs. A subsequent explicit user request produced local self-contained Milestone A executables. See AEVORIA_MILESTONE_A.md. This did not declare the complete playable alpha released.
