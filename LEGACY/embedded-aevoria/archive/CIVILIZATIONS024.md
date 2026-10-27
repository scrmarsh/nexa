# Age of Kingdoms — Historical Civilizations, Royals & Settlements

Unreleased Phase 3 development, 2026-10-20. Actual release metadata remains **0.1.23 Alpha**. The root packaged executable is unchanged. Phase 4 military redesign, Phase 5 History Book UI and the complete Age of Kingdoms release are not implemented.

## Checkpoint and prerequisites

Rollback checkpoint: **649aed1b10e32a171da2ce5e62a225d062d5ee34**, retained at `refs/checkpoints/nexa-pre-phase3-20261020-081544`. It includes pre-existing modified/untracked source, assets, documents and packaged releases. Ignored build/test data and the generated `%SystemDrive%` cache were excluded. The branch, original index and working files were not reset, stashed or replaced; branch HEAD remains `6aee3301d87b719268c2126aaf22dcd239c3d08a`.

Initial inspection confirmed Phase 2 functionality and its unresolved timing history. Work began with isolated culture/government profiles. Fresh Phase 2 checks then passed: 63 nation checks and all five requested suites, including 92 war021 checks before and after the isolated increment. That permitted functional integration. Later timing failures are retained and reported below; an earlier pass is not proof of a resolved performance cause.

The original atlas still has **714 identities**, 19 eras, 55 fictional polities and incomplete historical fields. No chronology, historical biographies or verified architecture was invented to fill gaps. Four broad scenario culture vocabulary records were added to the same catalog, reproducibly through its existing importer. Every original entity, relationship, era, government, source record and original culture remains semantically unchanged (`test-output/phase3/catalog-integrity.json`).

## Implemented behavior

**3A — Cultures/governments.** Six configurable profiles reference the existing atlas: Roman ants, Greek spiders, French ladybugs, Mongol cockroaches, Japanese centipedes and Egyptian flies. They provide personal/settlement naming pools, government offices, architectural vocabulary and construction rules. Roman republican office definitions coexist with the imperial default. Profiles are a fictional mixed-era design scenario, not a historical chronology. Unverified dated naming plans fail explicitly. Other catalog entries remain reference-only. The atlas has a read-only theme category and original source excerpts.

**3B — Leaders/dynasties.** Actual founding leaders receive stable individual IDs, personal names, political offices, biography creation/accession/death times, supported death causes, achievements and succession links. Dynasty identity is separate from nation/culture identity. Battle deaths are observed from committed core events; unknown causes remain unknown. A missing visible sprite is never a death source. Dead leaders can leave offices vacant while surviving populations keep their nation alive.

Core biological recovery alone may supply a successor. Civilization logic never grants `CanReproduce` or promotes an arbitrary soldier into a queen. Without documented descent, a legitimate recovered leader founds a new house and the previous dynasty ends. Known generations remain one rather than inventing ancestry. Existing species biology, faction/unit identities, combat randomness and population caps remain authoritative. Government conventions support future extensions; elections, hereditary genealogy and natural aging are not simulated.

**3C — Settlements.** Persistent named settlements have stable IDs, actual political owners, cultural building styles, dry-land positions, assigned live population/workforce, stage, resources, construction and capital references. Initial capitals begin as Outposts. Village/Town/City/Major Capital advancement requires population, completed buildings, production, materials, security and infrastructure/workshop/government prerequisites. Capital designation is independent of development stage.

Actual core settlements are integrated when location and founding conditions permit. Cultural colonization reserves materials and administrative capacity before claiming one neighboring neutral land cell; it never claims a continent. Capture preserves surviving architectural style, transfers ownership and cancels old construction. Lost capitals relocate to a valid owned settlement or leave a vacancy. Capital loss alone does not extinguish a nation.

**3D — Economy/construction.** Existing local, nondeployed insects do useful food/material gathering, construction and maintenance work. An idle local adult can fill the first worker slot when randomized military role slots provide no home guards; this fixes a tested Mongol startup deadlock without changing reproductive or military identity. Resource updates run at most once per simulation second, not per rendered frame.

Seven functional building purposes cover housing, food, workshops, defense, government, infrastructure and monuments. Jobs reserve materials, progress through visible construction states and only confer benefits after completion. Housing/infrastructure limit future births while retaining existing hard/species caps. Food constrains reproduction; workshops support construction; infrastructure/government enable advancement; repairs consume materials; defended settlements consume supplies to slow real occupation. Buildings can be damaged/destroyed under contested conditions; destroyed structures provide no benefit. Loss of housing stops excess new admissions rather than deleting existing insects. Monuments require a developed, populated capital.

**3E — Presentation.** Cached native WPF settlement geometry provides six palettes/silhouettes, Roman road strips, Greek columns, French fortifications, Mongol yurts, Japanese tiered roofs and Egyptian monuments. Construction scaffolds/progress precede completed silhouettes. Far zoom uses town/capital markers; closer zoom shows buildings. Offscreen settlements are culled, unchanged geometry is reused, names use stored values and labels avoid political labels/map edges. Political borders remain a separate layer.

Cities/labels can be selected for actual population, resource, defense and construction summaries. Focus Settlement, Focus Nation, capital focus, Return to World and close selection are available. Native species letters retain nation selection priority. Camera interpolation accepts ordinary dashboard editing; manual camera/follow behavior remains. Culture/government/dynasty/accession details also appear in nation summaries.

**3F — Events/history.** Structured records contain stable event ID, sequence, simulation time, type, nation, optional ruler/settlement references and factual detail. Events cover cultural government/settlement founding, upgrades, capital establishment, construction/destruction, capture/abandonment, accession/death/vacancy/succession and dynasty founding/ending. No appearance-based, assassination or invented biological ancestry events are generated.

The existing bounded core history remains; civilization facts extend it with per-world durable storage under `civilization-history`. Content-addressed JSON segments contain a previous-segment hash. The snapshot holds one head hash and 64 recent events, so disk history can grow without an unlimited RAM history list. Writes are atomic/idempotent, and restored backups can branch safely without rewriting older segments. Tests traverse the chain to verify chronological continuity and all committed facts. Archive failures pause civilization development with a warning; failed saves preserve the prior saved world.

**3G — Acceptance.** Clean Release build, focused checks, all established suites, native captures, integrity checks and executable startup were run. Current results and timing caveats follow. This is a functional lightweight foundation, not a complete historical simulation or final 0.1.24 release.

## Enabling and saves

New worlds generated through the chooser default to **Age of Kingdoms civilizations (fictional mixed era)** enabled; the checkbox can retain territory-only behavior. Existing worlds remain in their original mode. The programmatic `LivingWorlds.Create` default stays territory-only to preserve legacy callers/fixtures. Generate a new enabled world to use Phase 3; automatic conversion of existing worlds is not implemented.

Base world and metadata schemas remain 1. Optional metadata `CivilizationMode` is omitted when false. Optional `LivingCivilization` uses its own **schema 1** and persists cultural/government assignments, biographies/dynasties, settlements, resources, buildings/jobs, tasks, current capital references, simulation clock, recent facts and durable history head. Rendering caches are not serialized. Null civilization extensions are omitted from Colony Wars and older worlds.

**Back up/restore the entire Living World directory, including `civilization-history`, with its snapshot.** A missing, corrupt or incompatible optional section/history head produces a warning; the base world remains available and raw unsupported JSON is preserved on saving. No world is silently deleted or silently rebuilt. Ruler/clock references are checked against authoritative units. No incompatible migration or terrain redesign was introduced.

## Changed files

New runtime: `LivingWorld.Cultures.cs`, `LivingWorld.CivilizationModels.cs`, `LivingWorld.Civilization.cs`, `LivingWorld.Development.cs`, `LivingWorld.SettlementRendering.cs`, `assets/history/age-of-kingdoms.json`.

Existing integration: `SessionDashboard.csproj`, `HistoricalAtlas.Window.cs`, `assets/history/civilizations.json`, `tools/Build-HistoricalAtlas.mjs`, `LivingWorld.Store.cs`, `LivingWorld.Models.cs`, `LivingWorld.Dialog.cs`, `LivingWorld.Nations.cs`, `LivingWorld.NationRendering.cs`, `LivingWorld.Chunks.cs`, `BattlefieldWindow.Nations.cs`, `ArcadeWorld.Models.cs`, `ColonyWars.Persistence.cs`, `ColonyWars.Civilizations.cs`, `ColonyWars.Simulation.cs`, `ColonyWars.Rendering.cs`, `ArcadeWorld.Controller.cs`.

Tests: new `tests/Cultures024Checks.cs`, `tests/Civilizations024Checks.cs`; routes in `tests/Program.cs`. Documentation: this report, `CHANGELOG.md`, `DEVELOPMENT024.md`, `NATIONS024.md`, `CODEX_SESSION_STATE.md`. Existing dirty work was preserved; this list is relative to the rollback checkpoint, not the old branch HEAD.

## Results and performance

The latest complete verification is recorded in **`test-output/phase3/complete-results.json`**, with `complete-*.log` files. Build evidence: `build-complete.log` (0 warnings/errors). Earlier failures/runs remain for review.

**Final run: all 633 checks passed, no failed suites.** cultures024 **60**, civilizations024 **58**, nations024 **63**, living024 **47**, living023 **81**, colony019 **138**, war021 **92**, historical-atlas **94**. Final war021 CPU/software p95: **0.18/35.42 ms**. Timing variability remains unresolved; this does not erase the earlier failure below. Final functional artifacts: `test-output/20261020-090435`; geography: `test-output/20261020-090454`.

Focused coverage includes identity/catalog isolation, naming, malformed data, real construction/resource use, every cultural style, all development stages, population limits, pause/cadence, destruction, capture/job cancellation, actual battle death, biological recovery/succession, vacancy without extinction, exact restoration, history continuity, native selection/caching/culling and dashboard input.

The preceding full run passed cultures024 **60**, civilizations024 **58**, nations024 **63**, living024 **47**, living023 **81**, colony019 **138**, historical-atlas **94**. Its war021 run failed after **74** assertions: CPU p95 **0.19 ms** + software rendering p95 **40.79 ms** exceeded the unchanged combined **40 ms** gate; later checks were not reached. Earlier full runs passed all **92**, including a pre-change rendering sample of **34.20 ms**. This intermittent benchmark does not attach civilization or terrain layers. No cause has been established and no performance threshold was relaxed. Do not infer a renderer fix from a passing rerun.

Final functional sample (`test-output/20261020-090435/civilizations024.json`): **694 simulated seconds**, **151 insects**, **13 settlements**, **65 building records**, **7 royal biographies**, **134 events** across **4 segments**, **64 recent events** in RAM. Development simulation: **1,430.6 ms** for the measured 643-second growth interval. Civilization update p95 **0.210 ms**, max **7.02 ms**, including rare archive work; snapshot **55,199 bytes**. Focus accepted **7 edits/0.5 s**. These are single-machine samples, not guaranteed budgets.

That sample's full 1200×800 window software capture p95: **50.40 ms at 100%**, **107.17 ms at 150% (1800×1200)**, **176.28 ms at 200% (2400×1600)**. These are offscreen scaled captures, not actual monitor DPI transitions or presented GPU FPS. Native settlement/overview and theme-preview captures were inspected. GPU support/software fallback remain; smooth high-resolution rendering is not established.

Normal executable startup and fresh-process saved civilization-world startup exited **0** with the final binary, with six colonies, 151 insects and PerMonitorV2 enabled. Evidence: `test-output/phase3/startup-complete-results.json` and `test-output/20261020-090435/civilizations/startup.json`.

Generator 1/2 source hashes, three geography golden PNGs, root executable and all eight production world/selection/backup files remain unchanged; no production file-set additions. Evidence: `integrity-final.json`, `integrity-summary.json`, `golden-integrity.json` under `test-output/phase3`.

## Remaining limitations

- Six current species/factions remain authoritative. Separate same-species political identities still lack independent army spawning. No Phase 4 coordinated-command overhaul, new animals or intercontinental transport/pathfinding.
- Governments have office conventions and recovery-based succession; no elections, verified royal genealogy, biological family trees, natural aging or comprehensive dated name pools. Fictional names do not claim to represent historical individuals. Royal-pair biology is not invented.
- Economy/work are lightweight aggregate assignments using actual local insects; no separate civilian pathfinding, animated job interactions, trade market or industrial progression. Core tactical nests and civic development settlements coexist; new civic outposts are future military integration points.
- Limits: 512 cultural nations, 1,024 important biographies/dynasties, 128 settlements, 2,048 building records, 240 worker assignments. Capacity exhaustion can halt additional civic records/development; base simulation remains. Future history work must archive older biographies/ruins as well as events.
- Event detail is factual structured metadata, not generated narrative. The full History Book interface, searchable biographies and historical scenario chronology remain Phase 5 work. Disk history is unbounded in principle, subject to storage availability; it requires whole-world backups. Only the current head is checked eagerly on load; full chain validation is tested/offline work.
- Rendering storage is additional to the terrain chunk budget. High-DPI captures are not a mixed-monitor test; GPU presentation and the intermittent war021 timing failure remain unresolved.

Continue with targeted validation/performance investigation before declaring release readiness or starting the Phase 4 military rewrite. No current save incompatibility was observed in the tested enabled/legacy/Colony Wars paths.

Development executable: **`M:\Session Dashboard\bin\Release\net10.0-windows\Nexa.exe`**. Root `Nexa.exe` remains 0.1.21.0, SHA256 **17E90F770F5C883CCFD46EFD9AFFFCDAD517E467D3F7B92A185FC797295613B6**. No release package, branch commit or push was made.
