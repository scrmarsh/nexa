# Nexa v0.0.28 Alpha — targeted audit

October 28, 2026. Completed before implementation. Clean starting branch
`codex/nexa-027-nexaween`, HEAD `b9e68dfcc575e20cfed7a835daacfc0d498e9ca4`;
new branch `codex/nexa-028-nexaween-evolved`. Read .27 audit/status/carry-forward
and the October 28 handoff. Prior 656 assertions and two live checks are prior
evidence across executions, not a claim of one final .27 full-suite run.
No pending source changes were present. Original M: checkout and packages remain
outside the editing scope. Approved branding is preserved without regeneration.

| Requirement | Previous status | Current status | Evidence | Necessary work | Verification |
| --- | --- | --- | --- | --- | --- |
| Shared identity/updater/branding | COMPLETE .27 | COMPLETE | App/shared label, manifest, comparison-only Alpha mapping; .27 exact asset assertions | Advance current version only; preserve all assets/history | Metadata and existing .27 route |
| Navigation | PARTIAL | PARTIAL | Same eight-Space profile: normal cold 508ms, warm 515/314/401ms; Developer 1707/1839/1505ms; first render dominates | Add layout/teardown/dispatcher-idle timing, profile before choosing small optimization | Comparable reduced-motion and animated runs |
| Widget layout/lazy bodies | PARTIAL | PARTIAL | MainWindow.Widgets.ArrangeWidgetFramesCore measures every body with infinite height each pass, then fixes body height; six kinds already defer construction | Measure arrange passes; avoid repeated unconstrained measurement when safe | Scrolling, resizing, lazy content and guide routes |
| Atomic save/recovery | COMPLETE fixtures | UNVERIFIED failure extensions | Models.AtomicWrite uses write-through temp/replace/retry; LoadCommitted tries primary/backup and snapshots | Disposable corruption/locked-write/restart tests; fix demonstrated failure only | New recovery tests plus continuity/core routes |
| Schedule/reset | COMPLETE fixtures | COMPLETE within existing scope | MainWindow.Reliability.ReconcileSchedule implements end/+5m checkpoint, next -10m warning/-5m reset, defer/save-first; WeeklySchedule handles zones/DST | Add exact Mon/Wed transition assertions; no engine replacement | Synthetic timestamps, failure and restart checks; physical power remains manual |
| Shared ecosystem/persistence/resources | COMPLETE fixtures | COMPLETE within tested scope | NexaEcosystem.For/store, document validation, stable IDs/positions/nests/resources/webs and pending squash queue; ecosystem route | Reuse existing save/resource fixtures; stress deterministically | Ecosystem/animation routes and short workload |
| Cozy/Creepy/Combative | Cozy/Combative complete; Creepy intentionally retired | PARTIAL for new .28 request | Creepy AI exists but EcosystemOptions.Safe maps it to Classic; Settings only exposes Classic/Cozy/Combative | Restore requested selectable Creepy using existing AI, retain Classic and stored populations | Actual activities, save/reopen and mode switching |
| Combat retaliation/balance | UNVERIFIED | UNVERIFIED | NexaEcosystem.Simulation.Encounter applies damage, does not immediately order defender retaliation; existing damage values differ by species | Seeded combat scenarios, repair proven absent defense; no blind spider damage rebalance | Per-species attacks/survival with explicit scenario limits |
| Territories/leaders/victory | DEFERRED | MISSING in active ecosystem | Strategic territory/leader models and announcements are under excluded LEGACY/embedded-aevoria; active world has nests and local raids but no colony leader/ownership model | Record staged integration gap; do not claim archived game is active or invent defeat for ordinary deaths | Beta readiness NEEDS WORK for optional strategic subsystem |
| Advanced locomotion/eyes | COMPLETE renderer, optional off | COMPLETE / PARTIAL gaze target | BugAnimation and BugVisuals.Animation provide gaits, wings, bend/curl, antennae, blink/gaze/sleep/hit reactions; BugOverlay gates Advanced+motion and Adorable eyes | Preserve renderer/defaults; add nearby-bug gaze fallback if absent | Animation route, gate and source/render fixtures |
| Seasonal effects | COMPLETE static and paced effects | COMPLETE / DEFERRED optional effects | Nexaween.Animation, SeasonalBackdrop provide reveal/floating/wobble/glow; reduced-motion and seasonal gates exist | Preserve; optional particles/steam need no new engine | Existing handoff/seasonal tests; subjective appearance manual |
| Bug Frenzy | Two-minute shared half-step streak verified | PARTIAL new requirement | Ledger Frenzy fields actually implement preserved candy/squash streak; no independent direct-only 20s 2x window | Add bounded direct-only window without replacing recorded scores/shared streak; no combat activation | Activation/refresh/expiry/dedup/caps/persistence tests |
| Home/timer/scoring/tips/settings | COMPLETE .27 | COMPLETE within prior scope | Existing handoff and core/continuity tests | Preserve; current version expectations only | Final integrity gate |
| Fullscreen/DPI/window recovery | Implemented, physical open | UNVERIFIED physical scope | Native monitor bounds and F11 implementation, prior reliability fixtures | Reuse fixtures; retain physical checklist | No mixed-monitor certification claim |
| Integrations / real updater | Fixtures/discovery pass; live acceptance open | BLOCKED for live auth / UNVERIFIED real install | Separate Hedy/Hevy vaults; no explicit live account authorization or newer approved package | Document manual tests; no credential use or Alpha publication | Mocked routes preserved |
| Sustained memory/CPU/GPU | UNVERIFIED | UNVERIFIED | Prior short working-set figures are not leak/FPS evidence | Repeat bounded large-workspace cycle workload; report duration and limits | Memory/CPU/retained-window sample; extended GPU/manual pending |
| Beta readiness | Not accepted | UNVERIFIED | Physical/live/clean install/production update still open | Evidence-backed subsystem checklist and next milestones | NEXA_BETA_READINESS.md |

Implementation is limited to evidenced gaps above. No replacement database,
navigation engine, updater, ecosystem manager, or renderer is justified.

## Implementation findings

The audit led to reuse of existing lazy loading for all large-Space body types,
with matched before/after profiling. No speculative layout rewrite was retained:
measured arrange CPU was small relative to first rendering/queued work.

New disposable tests demonstrated a stale postponed-reset action that could
archive a newly active session; it now requires an actual pending reset. Recovery
failure tests passed without storage changes. Restored Creepy selection exposed
a stale automatic-appearance dropdown; real-control tests reproduced it, and
refresh now preserves the distinction between automatic and explicit appearance.
Direct-only 20s Frenzy was added alongside the verified shared streak, with caps,
timestamp/dedup checks and old-ledger compatibility. Combative victims now receive
immediate defense orders; no species damage values were changed.

Advanced territory/leader/army victory systems remain MISSING in the active build,
with implementations archived under LEGACY. Functional combat-web effects remain
UNVERIFIED/MISSING in the active simulation; spider motion builds visible silk,
but that does not establish enemy movement effects. These require staged work,
not an invented claim of completion. See the final evidence and readiness tables
in [status](NEXAWEEN_028_STATUS.md) and [Beta readiness](NEXA_BETA_READINESS.md).

Final verification: zero build warnings/errors; all eighteen integrity routes
pass 695 distinct assertions, plus eight separate close/view diagnostics and two
live read-only discovery checks. Final suite was rerun after the reset/selector
repairs and updated Creepy expectation. Navigation baseline/initial/final values,
short-workload limits and manual acceptance remain explicit in the status report.
