# Nexa v0.0.28 Alpha — NEXAWEEN EVOLVED

October 28, 2026. **Unpublished Alpha; physical/live Beta acceptance remains open.**

## Source and scope

Repository: `C:/Users/Sean Marsh/Documents/ChatGPT/Nexa/continuation-source`.
Starting branch `codex/nexa-027-nexaween`, commit
`b9e68dfcc575e20cfed7a835daacfc0d498e9ca4`, clean. Final branch
`codex/nexa-028-nexaween-evolved`; final application/test source checkpoint is
`235a88e81dec1f8bdf05b29dc8b75f794dc453d8`. A subsequent report-only commit
records this identifier; it does not alter executable source. The .27/handoff ancestry and all prior work are
preserved. No original M: source, approved binary, archived package, live user
data, credential, GitHub release or tag was changed. No history was rewritten.

Read [the targeted audit](NEXAWEEN_028_AUDIT.md) and
[Beta readiness](NEXA_BETA_READINESS.md). Existing score/history/timer/Home/theme
systems were reused. No database, navigation engine, ecosystem manager or
renderer was replaced. All approved assets have zero diff from .27.

## Implemented

- Current assembly/file/manifest `0.0.28.0`, product `0.0.28-alpha` with source
  provenance, shared labels and NEXAWEEN EVOLVED changelog. Updater User-Agent now
  uses the shared current version; legacy Alpha comparison remains unchanged.
- Large Spaces retain every widget model/frame and reuse the existing viewport
  loader for all body types, reducing premature WPF editor construction.
  Scrolling creates real saved editors; partially materialized trees save all
  offscreen data. No new virtualization engine or longer fade was introduced.
- Navigation instrumentation adds layout passes/time, source-close time and
  dispatcher idle after teardown. The profiling fixture waits for Busy to clear;
  intermediate render/input timestamps are not treated as completed navigation.
- Fixed a demonstrated stale-action bug: repeated Complete Postponed Reset could
  archive the just-started session and jump to a future recurrence. Completion
  now requires an actually deferred reset and a later valid occurrence after
  the previous session ends.
- Restored existing Creepy AI to the selector/normalizer alongside Classic,
  Cozy and Combative. Saved .27 Classic remains Classic; existing Creepy enum
  identities are honored again. Appearance/costumes, world IDs/populations,
  seasonal behavior choices and Advanced defaults are retained.
- Added real-control coverage for the restored mode. Automatic appearance now
  refreshes the dropdown without turning that refresh into a manual override;
  an explicit appearance remains independent and persists through later mode
  changes. The stale selector was reproduced before this correction.
- Combative victims receive immediate defensive orders when not already
  targeting another opponent. Species damage values are unchanged. Nearby-bug
  gaze fallback uses a bounded scan only for optional advanced Adorable eyes.
- Direct accepted squashes activate a saved, capped 20-second 2x Bug Frenzy.
  The existing two-minute candy/squash half-step streak is retained. Scoring uses
  the greater eligible multiplier, never a product of bonuses; configured caps
  apply. Duplicate/delayed events and candy cannot incorrectly refresh the
  direct-only window. Existing recorded scores/event identities are not revised.
  The score badge/overlay uses the effective multiplier, with a remaining-time
  tooltip. No simulation outcome is connected to direct-squash input scoring.

## Measured navigation

Same eight-Space fixture, four normal opens and three Developer opens, sequential
processes, same machine, reduced motion. Times below are the complete logical
click path through transition, source teardown and dispatcher idle, including
the fixture's polling overhead. They are conservative interaction bounds, not
physical pointer/keyboard latency certification. `Interactive readiness` remains
an earlier render-ready timestamp. Logs preserve every individual observation.

| Milliseconds | Instrumented .27 baseline | Initial optimization | Final .28 sample |
| --- | ---: | ---: | ---: |
| Normal cold | 492.44 | 528.93 | 495.00 |
| Normal warm median (three) | 405.86 | 375.24 | 312.97 |
| Normal slowest (all four) | 492.44 | 528.93 | 495.00 |
| Developer cold | 1733.05 | 1197.91 | 1092.00 |
| Developer warm median (two) | 2077.92 | 1427.95 | 1221.92 |
| Developer median (all three) | 1912.66 | 1339.19 | 1146.05 |
| Developer slowest | 2243.18 | 1516.71 | 1297.78 |
| Source-close median / slowest (seven) | 4.35 / 6.60 | 3.00 / 8.40 | 2.86 / 5.87 |

The initial paired optimization reduced Developer median ~30%; final sample
reduced it ~40% against that baseline. Normal cold initially regressed ~7.4%,
then measured ~0.5% slower; do not infer a cold-start gain. Sample variability
precludes a universal speedup claim. Five deferred bodies became 26. Final
Developer first-complete-render was 659–778ms versus 1067–1441ms baseline;
layout still ran ten passes (~30–47ms final), leaving WPF construction/render
and queued dispatcher work as remaining bottlenecks. Source close itself was
small in this fixture. Keep instrumentation for future profiling.

Final animation-enabled sample: normal cold 946.53ms, warm median 764.91ms;
Developer median 1469.13ms / slowest 1492.16ms. No paired instrumented animated
baseline was collected, so no animated speedup is claimed. Existing fade lengths
are preserved. Measurements: `verification/nexaween028-performance.csv` and
`nexaween028-{instrumented-baseline,navigation-after,final-profile026,final-profile026-animated}.log`.
The initial uninstrumented .27 reproduction is separately retained in
`verification/nexaween028-baseline-profile.log` (Developer median ~1707ms).

## Reliability and verification

Final Release application/checks build: **zero warnings, zero errors**.
`tools/Verify.ps1 -SkipBuild` passed **all eighteen routes, 695 distinct assertions**
on the final application source. Logs: `verification/acceptance-20261028-141603`
and `verification/nexaween028-final-integrity.log`; build log:
`verification/nexaween028-final-build.log`. Counts: .28 39, .27 51, handoff 95,
pass026 36, alpha026 28, arrivals 32, audit025 38, final 36, followup-design 31,
followup 46, master 23, reliability 19, core 64, continuity 43, shared 10, menus 9,
ecosystem 48, animation 47. Reruns are not added to that total.

Separate diagnostics passed eight close/view-release checks at
`verification/20261028-141056`, log `nexaween028-diagnostics-final.log`. Two final
live read-only GitHub discovery checks passed at `verification/20261028-141117`,
log `nexaween028-public-updates.log`; no download/install occurred. Final reduced
and animated profiling each completed their sixteen diagnostic assertions,
recorded at `verification/20261028-140327` and `20261028-140336`, separately from
the regression total. Prior .27 live evidence is preserved as prior evidence.

Development failures were resolved before the final gate: the new repeated-reset
scenario caught the stale action, and later real-control coverage caught a stale
appearance selector. An earlier integrity run also stopped on an obsolete test
that required Creepy to remain retired; it was updated to the current requirement.
The complete suite was rerun after these corrections, unlike the final .27 scope.

The diagnostic workload used thirty-widget windows, alternating Advanced on/off,
eight open/close cycles and thirty ordered duels. In 10.96 wall seconds / 10.89
process CPU seconds, all eight closed windows became unreachable in the targeted
WeakReference probe, with zero ecosystem views left attached. Refreshed working
set samples rose from 151.2MB to 186.5MB; managed memory warmed from 8.58MB to
9.22MB and then changed little. These are short, offscreen construction workloads,
not an idle CPU, FPS/GPU, several-hour trend or leak-free certification. Separate
final profile simulation advanced ninety simulated seconds with 59 creatures in
~93ms CPU wall time; that is not ninety seconds of observed display performance.

The .28 Home capture was inspected at
`verification/20261028-141608/nexaween027-home.png`; current labels and the exact
approved icon/X are present. This does not close physical visual acceptance.

New tests exercise a disposable child writer terminated during repeated atomic
saves, orphan partial files, corrupt-primary fallback, preservation of valid
backup/unreadable evidence, exclusive-lock failure/retry, and old ledger JSON
without the new boost fields. This is a process-interruption workload, not proof
of power loss at a particular filesystem instruction. Existing continuity/core
fixtures cover notes, permanent tasks/pins, water/counters, presets/layouts,
timer drafts, history and ecosystem snapshots. No destructive migration was added.
Strict older export readers may reject newer additive boost fields; retain
original exports. Readable .27 data and recorded awards retain their meaning.

Schedule tests cover the exact Mon/Wed 13:00–14:15 example: Monday end and +5m
checkpoint, Wednesday -10m warning/-5m save-first reset, failed save protection,
persisted postponement, permanent tasks and duplicate-action prevention. Existing
midnight/DST/time-zone/overnight routes remain. The repeated reset failed before
the guard and passes afterward. No save architecture change was justified.

## Ecosystem findings and remaining work

Cozy resources/carrying/nests/social/rest and persistent silk construction remain.
Creepy now executes actual hiding/hunting rather than silently becoming Classic.
Combative local raids/defense work; defense was repaired without a damage rebalance.
Thirty ordered pair fixtures (same seed, both attacker roles, equal initial health)
produced wins: ants 3, roaches 6, flies 0, spiders 9, ladybugs 3, centipedes 9;
each species participated ten times, all duels ended. Defender orders and damage
were observed. These are constrained duels, not natural survival rates or army
balance. Flight roles/resources/leader protection were not represented. Spiders
and centipedes remain strong in this scenario; do not declare strategic balance
complete or force equal damage based on this one fixture.

Active territorial ownership, colony leaders, strategic army victory/defeat
announcements and View Ecosystem UI are absent from the active build; related
models/UI exist under excluded LEGACY. No leader survival or territory-capture
results can be claimed. Persistent visible webs are built from actual spider
motion, but an active combat-web slow/trap effect was not established. These
optional advanced systems need a staged implementation/compatibility plan;
ordinary deaths must not be announced as colony defeat. No replacement engine
or invented leader types were added in this update.

## Acceptance and release policy

Physical art/audio/guide/About/Settings, mixed DPI/multiple monitors, resize/F11/
taskbar, sleep/hibernate/power loss, live Hedy microphone/auth/history, Hevy and
Todoist authorization/revocation, clean Windows install, real newer-package
update and extended memory/GPU/FPS remain **MANUAL TEST REQUIRED**. Use the
practical matrix in [the .27 status](NEXAWEEN_027_STATUS.md#manual-acceptance-checklist),
then add .28 reset and lazy-editor checks on representative saved workspaces.
No real account was used without authorization; no stored credentials were tested.

No Alpha or Beta was published, no distribution promoted, no historical release
or tag deleted. Already distributed Alpha binaries may need a manual installation
of a newer approved build to bootstrap comparison logic; current source cannot
retrofit old executables. First intended retained public release remains
v0.1.0 Beta, requiring separate approval and verified clean install/update.
