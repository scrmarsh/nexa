# Age of Kingdoms — Nations, Territories & Interactive Map Milestone

**Later checkpoint, 2026-10-20:** The Phase 3 request is now implemented as a functional lightweight foundation; see [CIVILIZATIONS024.md](CIVILIZATIONS024.md). Phase 2 suites were freshly rerun and passed before functional integration. The war021 timing failure remains intermittent and must not be considered resolved by those passes. The following text preserves the original Phase 2 report.

Development milestone, 2026-10-20. Functionality implemented; regression sign-off remains blocked by the existing `--war021` software-render timing gate. Release metadata stays **0.1.23**. Phases 3–5 have not started. No packaging, commit, reset or root executable replacement.

## Implemented

- Persistent nation GUIDs, names, distinct colors, species, dry-land capitals, founding time, ruler reference, active/extinct status and optional historical civilization/government references. Historical assignment is opt-in. Separate same-species identities are supported; existing simulation bindings still use six factions.
- Deterministic new-world placement across suitable homelands and connected, nonoverlapping claims without consuming combat RNG. Legacy initialization retains units, positions and populations.
- Authoritative ownership cells spanning 8×8 logical tiles, clipped to land with per-tile masks. Rivers remain visible. Neutral/contested land, extinction and actual committed conquest/occupation/settlement changes are represented. Passing soldiers cannot transfer ownership.
- Sequenced ownership facts with old/new owner, simulation time, reason and optional settlement ID; live population, military and settlement statistics.
- Cached fills/borders with no same-owner interior borders. Exact tile connectivity locates disconnected regions and dry interior label anchors. Species letters/names use native WPF drawing independently of terrain detail.
- Authoritative selection, selected borders, compact summary, smooth camera focus, clicked-component versus largest-component focus, capital focus, Return to World, manual camera takeover and preserved Follow Bug. Both views share one model and simulation.
- Streaming terrain mosaics are coalesced and retained during interpolation. Cached alpha brushes avoid costly WPF opacity groups; glyph drawings and pens are cached. GPU support/software fallback remain.

## Changed files

New: `LivingWorld.NationModels.cs`, `LivingWorld.PoliticalLand.cs`, `LivingWorld.Nations.cs`, `LivingWorld.NationSimulation.cs`, `LivingWorld.NationRendering.cs`, `BattlefieldWindow.Nations.cs`, `tests/Nations024Checks.cs`, `tests/PoliticalRenderAudit.cs` and this report.

Existing integration: `LivingWorld.Store.cs`, `LivingWorld.Models.cs`, `LivingWorld.Chunks.cs`, `LivingWorld.Detail.cs`, `BattlefieldWindow.cs`, `ArcadeWorld.Models.cs`, `ColonyWars.Persistence.cs`, `ColonyWars.Territory.cs`. `ArcadeWorld.Store.cs` adds `InvalidDataException` to its recovery catch: that exception does not derive from `IOException`.

`tests/Program.cs` adds diagnostic routes. `tests/Living023Checks.cs` restores optional politics before its exact snapshot replay comparison, matching session restoration. Existing performance thresholds are unchanged. Documentation: `CHANGELOG.md`, `DEVELOPMENT024.md`, `CODEX_SESSION_STATE.md`.

The repository already contained extensive unfinished changes; this list describes this milestone. `LivingWorld.Bugs.cs` and both terrain generator source files were unchanged here.

## Saves and compatibility

Base snapshot schema remains 1. Optional `LivingPolitics` has its own schema 1, world ID/grid dimensions, nation records, owner slots, conflicts, revisions, sequence, recent facts and sync clock. Optional `SelectedNationId` extends camera metadata. Colony Wars omits the null extension.

Legacy generator-1/2 worlds initialize politics asynchronously from committed territory without reseeding units or consuming RNG. A permanent `arcade-world.json.pre-nations` copy preserves the pre-migration file; atomic saves/rotating backups remain. Repeated load is idempotent. Invalid/future optional politics produces a warning, preserves raw JSON and leaves the base world available; it requires a valid backup to recover politics. Invalid base schemas still use explicit backup recovery.

Initialization, ownership sequencing, component tie-breaking and restored replay are deterministic for the same saved world. New worlds deliberately receive distinct IDs. No cross-runtime floating-point determinism guarantee is made.

`test-output/nations024/integrity.json` confirms unchanged generator source hashes, three pre-existing generator-2 golden PNGs, root executable and eight production world/selection/backup files. `production-file-set.json` confirms no additions. Tests used isolated saves.

## Validation

Release build: **0 warnings/errors** (`test-output/nations024/build.log`; final tests-only rebuild `build-tests.log`).

| Suite | Latest result | Log under test-output/nations024 |
|---|---|---|
| nations024 | 63 passed | focused-settled.log |
| living024 | 47 passed | verified-living024.log |
| living023 | 81 passed | verified-living023.log |
| colony019 | 138 passed | verified-colony019.log |
| historical-atlas | 94 passed | verified-historical-atlas.log |
| war021 | **Failed timing gate after 74 passed assertions** | war-standalone.log |

Latest war021: CPU p95 **0.21 ms**, software rendering p95 **42.74 ms**, exceeding the unchanged combined **40 ms** limit. Later checks were not reached. An earlier run passed all 92 checks at 31.45 ms rendering; final runs measured 41–43 ms and failed. This benchmark attaches neither Living Worlds terrain nor politics. A cause is not established; final performance sign-off is incomplete. Earlier logs remain for review.

Earlier failures uncovered expensive opacity groups, optional-error exception handling and missing political restoration in the replay fixture. The final camera probe waits for both map views to settle while retaining its threshold; streaming remains tested separately. Cold-load editing is not covered by that camera measurement.

Latest focused artifacts: `test-output/20261020-075851`; native map and selection screenshots visually inspected. Geography artifacts: `test-output/20261020-075655`. Normal startup and fresh-process saved generator-2 startup exited 0; saved world loaded six colonies/19 bugs, PerMonitorV2 enabled (`startup-results.json` and geography `wide-session/startup.json`).

## Measurements

1024×576 fixture: topology **55.29 ms / 147,456 bytes**; nation initialization **126.53 ms**; capture **0.86 ms**; restore **16.13 ms / 314,336 allocated bytes**; political snapshot **59,978 bytes**; 200 ownership changes **0.065 ms**; 10,000 owner queries **22.98 ms** (not full screen-hit tests). Forty cached border chunks built cumulatively in **51.53 ms**; ten component analyses totaled **44.44 ms**. Camera probe accepted **14 edits/0.5 s**.

Same paused 1920×1080 diagnostic, eight software captures per variant: before alpha-brush fix, all layers averaged **239.67 ms**, terrain-only **62.49 ms**. Final audit: all layers **71.24 ms** (p95 85.54), terrain-only **68.91 ms** (p95 74.19). See `render-audit.log` and `render-audit-final.log`. Ordering/system variance limit precision. These are software capture times, **not presented GPU FPS**.

Legacy foreground: **22.2 OnRender calls/s, 27 edits**, software p95 **39.61 ms**. Generator-2 foreground: **27.7 OnRender calls/s, 34 edits**; software p95 **84.84/154.27/346.53 ms** at 1080p/1440p/4K. Smooth 4K performance is not demonstrated.

## Limits and continuation

- Separate same-species political identities do not spawn independent armies yet: combat retains one faction per species. Existing coarse tactical ownership drives committed changes into finer political ownership.
- At most 4,096 recent facts and 512 nation records; no unlimited History Book archive. Capacity exhaustion reports a diagnostic while base simulation continues.
- Clipping follows logical tiles; fractional coast detail can differ within one tile. Rivers connect land but are excluded from fill ink. No naval/intercontinental pathfinding.
- Political topology/geometry, label caches and mosaics consume memory outside the bounded terrain chunk cache. Mixed-monitor DPI and presented GPU performance remain unverified.
- Historical architecture, royal succession, army redesign, later technologies and phases 3–5 remain pending. No new animals, dependencies or engine.

Work is preserved for review and targeted fixes. **Resolve war021 before declaring full validation or beginning major phases 3–5 rewrites.** Tested migration/restore fixtures showed no save incompatibility; malformed optional sections remain preserved.

Development executable: **`M:\Session Dashboard\bin\Release\net10.0-windows\Nexa.exe`**. Root packaged exe remains 0.1.21.0, SHA256 `17E90F770F5C883CCFD46EFD9AFFFCDAD517E467D3F7B92A185FC797295613B6`.
