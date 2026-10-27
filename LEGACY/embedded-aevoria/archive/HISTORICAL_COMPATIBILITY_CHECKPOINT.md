# Grand Historical Update compatibility checkpoint

Checkpoint completed 2026-10-20. **PASS: safe to continue isolated, incremental atlas development.** No major gameplay rewrite was started. This does not validate future historical-gameplay integration or mean the Grand Historical Update has shipped.

## Build and regression results

Release build completed with **0 warnings and 0 errors**. All requested suites passed sequentially against that build:

| Suite | Checks | Result | Artifacts |
|---|---:|---|---|
| `--living024` | 47 | PASS | `test-output/20261020-064847` |
| `--living023` | 81 | PASS | `test-output/20261020-064900` |
| `--colony019` | 138 | PASS | `test-output/20261020-064913` |
| `--war021` | 92 | PASS | `test-output/20261020-064939` |
| `--historical-atlas` | 94 | PASS | `test-output/20261020-065006` |

**358 existing regression checks, plus 94 atlas checks; no failed tests in this checkpoint.** Logs, durations and exit codes: `test-output/grand-history/checkpoint/results.json` and adjacent suite logs. Build log: `test-output/grand-history/checkpoint/build.log`. Earlier v0.1.24 performance limitations remain documented in DEVELOPMENT024.md; these passing tests do not establish smooth 4K GPU rendering.

## Compatibility evidence

- **Generator 1:** its sampling function and palette match the pre-v0.1.24 source stored in `test-output/baseline024.json` exactly after excluding the existing generator-2 dispatch line. The legacy exact biome census and terrain/save/shared-view regression checks passed.
- **Generator 2:** the three deterministic geography images (kingdoms, old-world, five-seas) from this checkpoint match the pre-historical-update images in `test-output/20261019-235949` byte for byte. Golden hashes: `test-output/grand-history/checkpoint/generator2-golden-images.json`.
- **Protected implementation:** 31 existing rendering, simulation, persistence and configuration files stayed byte-identical throughout the checkpoint. The historical work has not edited terrain generation, nation/combat/royal/territory implementation or their save models. The project resource entries were already present when the checkpoint snapshot was taken; their change is listed below.
- **Production data:** all eight discovered production world/selection/save/backup files under `%LOCALAPPDATA%/SessionDashboard` retained their hashes. The file set also stayed unchanged. Tests used isolated `test-output` stores. This verifies the discovered production files and tested fixtures, not arbitrary saves stored elsewhere.
- **Root executable:** untouched and not packaged. Root `Nexa.exe` is still version **0.1.21.0**, 145,089,334 bytes, SHA-256 `17E90F770F5C883CCFD46EFD9AFFFCDAD517E467D3F7B92A185FC797295613B6`. Only ordinary build outputs under `bin`/`tests/bin` were rebuilt.
- **Original reference:** the retained DOCX copy matches the supplied Downloads file exactly.

Integrity summary: `test-output/grand-history/checkpoint/integrity-results.json`. Source snapshots and full existing working-tree inventory are adjacent; no reset, cleanup, commit or discarded work occurred.

## Historical-update file inventory

These are the changes made for the historical catalog milestone, distinct from the much larger pre-existing v0.1.24/older working tree:

| File | Change |
|---|---|
| `HistoricalAtlas.Models.cs` | New versioned catalog records; separate culture, dynasty, government, source, emblem and scenario types. |
| `HistoricalAtlas.cs` | New lazy, immutable catalog loader, validation, indexes, search and reference-year formatting. |
| `HistoricalAtlas.Window.cs` | New read-only searchable atlas browser. Completed the pending local list-background styling fix before checkpoint build. |
| `assets/history/civilizations.json` | New imported reference catalog. |
| `docs/Nexa_Grand_Historical_Update_Civilizations_to_4000.docx` | Exact copy of the supplied content specification. |
| `docs/historical-atlas-source.json` | Complete extracted body/table content and source hash. |
| `tools/Import-HistoricalAtlas.ps1` | New local DOCX extraction/import tool. |
| `tools/Build-HistoricalAtlas.mjs` | New deterministic development importer with coverage and identity-retention guards. No runtime Node dependency. |
| `LivingWorld.Dialog.cs` | Added one Civilization Atlas button; opens only the read-only browser. |
| `SessionDashboard.csproj` | Added two embedded JSON resources; release version unchanged. |
| `tests/HistoricalAtlasChecks.cs` | New catalog, provenance, classification, identity, browser and no-save-mutation checks. |
| `tests/Program.cs` | Added opt-in `--historical-atlas` route. Existing suite routes retained. |
| `CODEX_SESSION_STATE.md` | Handoff/checkpoint status only; previous handoff retained. |
| `HISTORICAL_COMPATIBILITY_CHECKPOINT.md` | This report. |

Runtime integration audit: `test-output/grand-history/checkpoint/atlas-references.txt`. Outside the atlas classes, the only application reference is the chooser button. No atlas call is made by the world controller, combat loop, territory logic, reproduction, renderer or save serializer.

## Save boundaries and remaining concerns

No new world-save extension or migration is introduced by this milestone. **SchemaVersion=1 belongs to the separate catalog/source resources**, not to existing world saves. Existing Living Worlds generator versions and the earlier v0.1.24 `LivingGeneratorVersion` snapshot extension are unchanged. Opening the atlas does not select a scenario, rename a faction, advance technology or save historical identities into a world.

Future gameplay integration must introduce an explicit versioned extension, retain stable catalog IDs independently of display names, validate absent/unknown versions, preserve backups, and require an explicit migration before changing old insect civilizations. None of those migrations has been performed here.

The atlas imports 714 identities, 19 reference eras, 55 fictional polities, 13 fictional conflicts, six technology scenarios and eight successor examples. Historical catalog entries remain provisional; source classifications and the document's cautions are retained. Missing dates, origins, architecture and other attributes remain unspecified. Structural checks and selected identity distinctions are validated; this is **not scholarly verification of all 714 records**. Fictional wars are lore templates, never automatically recorded game events. Symbolic animals are not implemented biological units.

Nations, advanced territory controls, historical architecture/royals, political evolution, technology gameplay, new animals, grand warfare and a durable Living History Book remain unfinished. The existing productivity History UI is not that History Book. Ten-world management is also not an established implemented prerequisite in this repository. Do not treat those requested systems as available.

Current release metadata remains **0.1.23**, with the separate unreleased v0.1.24 geography milestone preserved. **0.2.0 Alpha** is the proposed future Grand Historical Update version only; no changelog replacement, release bump or package was made. Continue with isolated data refinement or a separately scoped integration milestone after this checkpoint; do not silently attach atlas state to existing worlds.
