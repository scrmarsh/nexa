# Nexa v0.0.27 Alpha — NEXAWEEN audit

Audit completed before feature implementation, October 28, 2026. Baseline: clean
`continuation-source`, `b30883737ecf87343c73d0fa6f0ac805827e98ae`, now branched as
`codex/nexa-027-nexaween`. Required source `25391210c751b9059e269bbb9229bc68fafcb74b`
is an ancestor; intervening changes are reports/images only. Outer workspace has
no commits and many pre-existing untracked tools, left untouched. `M:/Nexa` is
inaccessible in this session; no claim is made about newer changes there.

Prior evidence: [October 28 handoff](HANDOFF-COMPLETION-0.1.26-alpha.md), 604
regression checks / 95 handoff checks, zero build warnings/errors. Source wiring
was inspected against that report. Current baseline Release app/checks build also
passes with zero warnings/errors. COMPLETE is limited to the cited tested scope;
physical/live-account acceptance is separate.

| Requirement | Status | Existing evidence | Action needed | Verification |
| --- | --- | --- | --- | --- |
| Correct source / preserve changes | COMPLETE | Clean branch; source ancestor verified; report-only diff | Preserve original repositories, candidates and history | Git status/log/diff |
| Current version, metadata and docs | PARTIAL | Assembly 0.1.26, shared App.ReleaseLabel; stale manifest 0.1.23 | Use 0.0.27 Alpha, retain historical versions; update README/changelog | Metadata and display tests |
| Legacy Alpha updater ordering | BROKEN | IsNewer compares Version numerically: 0.1.26 Alpha outranks 0.0.27 Alpha | Normalize legacy Alpha identities for comparison only; stage ordering | Mock feed/ordering regressions |
| Authoritative icon / border / orange X | BLOCKED | Referenced nexa-halloween-icon-1024.png absent from attachment tree and checkout; existing SVG/PNG disagree and show white N | Obtain exact reference; do not redesign or approximate colors | All requested sizes, ICO frames and Home/splash pending |
| Shared splash renderer / startup ordering | PARTIAL | NexaWordmark and dispatcher-safe milestones implemented; only 180ms post-render delay, no minimum display gate | Add shared three-second minimum to startup, onboarding and preview | Elapsed-time tests; staged ordering retained |
| Seasonal visibility / theme / reduced motion | COMPLETE | HomeWindow seasonal subscription, NexaMotion, Theme, SeasonalBackdrop; handoff live/native toggles | Targeted handoff regression; no rewrite | ON/OFF cycles, no hitboxes, existing captures |
| Fourteen decoration categories | COMPLETE | NexaweenDecorations.DrawItem cases 0–14 include pumpkins/vines, ghosts, witch, web, leaves, skull, all candies, bucket, cauldron, slime | None | Existing core/ecosystem checks; subjective art remains manual |
| Six lightweight costumes | COMPLETE | BugVisuals.Costume has six species cases; cached frozen drawings | Preserve overlays | Ecosystem species tests |
| Add Bugs settings | PARTIAL | Adorable true, .75–4 size and three appearance styles; costumes under Nexaween | Add adjacent Advanced Animations toggle default off and independent costume control; preserve saved options | Real settings save/reopen; old options load |
| Pixel Timer drafts / schedule / rendering | COMPLETE | MainWindow timer paths, handoff and core routes cover drafts, identity, DST/overnight and sequential pixels | Focused regression only | Existing timer/core checks |
| Productivity / scoring / recaps / candy | COMPLETE | Handoff, pass026 and ecosystem tests; shared ring, event dedup, cumulative/export metadata | Preserve | Reuse relevant routes |
| Home six cards / expand / scroll / navigation | COMPLETE | HomeWindow and handoff native captures + behavioral checks | Preserve | Handoff route |
| Dashboard workspace / floating notifications | COMPLETE | Nexaween.RefreshSeasonalWorkspace separate from Home; contextual Add Widget; NexaNotifications clipping; saved widget positions | Preserve | Existing final/followup tests; DPI manual |
| Time zones / onboarding / audio / Quick Start | COMPLETE | TimeZoneCatalog, onboarding/guide and sound styles; handoff tests | Preserve | Targeted handoff tests; gestures/audio subjective manual |
| Eight General tabs / Experimental / providers | COMPLETE | SettingsDialog.Navigation; separate provider/vault handlers | Preserve | Existing tab/provider fixture tests; live auth manual |
| Save Now / autosave / history / timeline / export | COMPLETE | SettingsDialog.Reliability, MainWindow.Reliability, HistoryArchive, handoff private export fixtures | Preserve; no database migration | Continuity and round-trip checks |
| Updater empty feed/assets/HTTP validation | COMPLETE | ExperimentalUpdates validates feed/assets/digests and gives HTTP feedback; audit025 fixtures; prior two live checks | Reuse fixtures; legacy ordering fix above | Mock empty/missing/error tests; real newer install manual |
| Navigation performance | PARTIAL | Handoff warm normal ~434ms, cold ~692ms, Developer median ~2160ms; instrumentation retained | No speculative .27 optimization | Deeper layout/teardown deferred |
| Manual acceptance | UNVERIFIED | Handoff explicitly leaves physical, live provider and sustained-load acceptance open | Record executable checklist and results honestly | New .27 status report |
| .28 architectural/performance work | DEFERRED | Existing history/scheduling/recovery systems present; user scope excludes rewrites | Carry forward evidence and unresolved limits | NEXAWEEN_028_CARRY_FORWARD.md |

No missing decoration or costume implementation was found. No verified timer,
theme, navigation, score or save defect justifies rewriting those systems.
Final execution results and any revised classifications follow in the status
report and the verification addendum below.

## Final verification addendum

The table above preserves the audit performed before implementation. Final
classifications for changed areas:

| Requirement | Status | Existing evidence | Action needed | Verification |
| --- | --- | --- | --- | --- |
| Current version, metadata and docs | COMPLETE | Shared 0.0.27 Alpha label, 0.0.27.0 file/assembly/manifest, historical titles preserved | None within automated scope | .27 metadata/UI assertions and inspected WPF captures |
| Legacy Alpha comparison | COMPLETE | Comparison-only normalization; original identities retained | Real newer-package update remains manual | Mixed feeds, equal revisions, downgrade prevention and Beta ordering |
| Splash minimum / renderer | COMPLETE | Three-second monotonic display gate; existing stages preserved | Physical flicker acceptance remains manual | Reduced-motion preview and startup checks; handoff typing checks |
| Add Bugs / costume choice | COMPLETE | Adjacent toggles, advanced default off, independent costume choice; automatic appearance preserved | Physical interaction acceptance remains manual | Save/reopen and legacy JSON checks; costume-off bypass fixed |
| Original checkout preservation | COMPLETE | Read-only scoped access succeeded after initial denial; M:/Nexa at a308076; 30 pending blobs match inherited 99a9c3a, zero mismatches | None | Git status/history and blob hashes; no original writes |
| Reference branding | COMPLETE | User supplied exact reference and selected B: quadruple (24-unit) black borders; hash-preserved original; existing geometry/colors matched; standalone/combined icon outlined and wordmark borderless | Physical visual acceptance remains manual | All seven PNG sizes, six ICO frames, selected silhouette-only edges, exact X colors and WPF previews inspected/tested |
| Manual acceptance / deeper performance | UNVERIFIED / DEFERRED | Existing handoff measurements retained | Follow status checklist and .28 carry-forward | No physical/live-account/sustained-load pass claimed |

Final build: zero warnings/errors. 605 inherited-route assertions plus 51 new
.27 assertions = **656 distinct passing regression checks**, across the full
integrity execution and affected-route reruns; **two live read-only discovery
checks** separately passed. The new duration boundary failed during the initial
integrity run and was fixed/retested; see the exact execution breakdown in
[status](NEXAWEEN_027_STATUS.md). Existing saves remain readable in synthetic
continuity/export/old-option fixtures; live data was not migrated. No newly
missing artwork or costume implementation was identified. No Alpha was published.
