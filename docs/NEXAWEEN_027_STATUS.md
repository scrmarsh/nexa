# nexa v0.0.27 Alpha — NEXAWEEN status

October 28, 2026. **Unpublished development; automated verification passed,
manual acceptance remains open. This is not an accepted release or Beta.**

## Baseline and preservation

- Repository: `C:/Users/Sean Marsh/Documents/ChatGPT/Nexa/continuation-source`.
- Branch: `codex/nexa-027-nexaween`.
- Starting HEAD: `b30883737ecf87343c73d0fa6f0ac805827e98ae`.
- Application/test baseline: `25391210c751b9059e269bbb9229bc68fafcb74b`;
  later inherited changes are documentation/images only.
- Final source checkpoint: see the .27 commit on this branch (`git log -1`).
- Original continuation had no uncommitted changes. Outer workspace tools,
  approved binaries, archived builds, live data, credentials and release history
  were not changed. Initial sandbox access to `M:/nexa` failed; a subsequent
  read-only check with command-scoped Git ownership handling succeeded. Its HEAD
  remains `a308076` on `codex/nexa-026-alpha`; all 26 modified tracked / four
  untracked source files match inherited recovery checkpoint `99a9c3a` by Git
  blob hash (30 compared, zero mismatches). No newer original work was found.

## Completed changes

- Current shared label is **nexa v0.0.27 Alpha**. Assembly/file versions are
  `0.0.27.0`; informational version is `0.0.27-alpha` plus Git provenance.
  Manifest, Home/dashboard/splash titles, About/Settings shared displays and
  current documentation agree. No separate version service was introduced.
  No separate installer/update manifests or .github workflows exist in this
  checkout; the publish script consumes project metadata. Historical release
  reports, version labels/tags and approved packages retain their provenance.
- Updater normalizes only `0.1.X Alpha` for comparison with `0.0.X Alpha`.
  Original descriptors, tags and URLs are retained. Beta `0.1.X` identities are
  not remapped. Empty feeds, missing assets and HTTP errors use existing handling.
- Add Bugs exposes adjacent Adorable Mode / Advanced Animations controls.
  Advanced rendering defaults off and reuses the existing renderer when enabled;
  reduced motion still gates it. Size remains 75–400%. Saved unrelated choices,
  including automatic appearance matching, survive toggle changes.
- Costume choice is independent of appearance. Explicit Halloween appearance
  now honors costume-off. All six existing lightweight overlays are retained.
- Startup, onboarding handoff and in-window Splash Preview use the existing
  staged renderer with a minimum three-second display. Timer rounding is guarded
  by a monotonic elapsed-time recheck. Existing tagline text is unchanged.
- New NEXAWEEN changelog entry uses the existing seasonal panel treatment and
  requested purple/green/orange palette. Historical **Spaces in Motion** remains
  0.1.26; it is no longer decorated as a new NEXAWEEN entry.
- Tests add a focused .27 route and update current-version expectations.
  Existing Home, timer, scoring, saving, history, notifications and seasonal
  visibility implementations were preserved.

## Completed reference branding

The reference was initially absent; the user subsequently supplied
`C:/Users/Sean Marsh/Downloads/nexa-halloween-icon-1024.png`. Its untouched source
is retained at `assets/references/nexa-halloween-icon-1024.png`, SHA-256
`F08A21EC47C7814BC0A5112A480C002094DB3BE1753B32E8404026C8365BD6BF`.

Existing native vector geometry exactly matches the supplied 1024px composition.
The sampled colors are green **#84CC16**, purple **#9333EA**, orange **#F97316**,
light orange **#FDBA74**. Existing vector paths were reused with these exact
colors; the input raster was not redesigned. The user selected preview B:
24-unit black outlines, quadruple the six-unit border shown before comparison,
surround
the union of the orange strokes and the inset rounded-square perimeter. One union
outline avoids internal seams across the two orange layers. Wordmark letters
retain their geometry/spacing without black outlines. Per the user's final
correction, the X's top-left-to-bottom-right stroke is white (#FFFFFF), and its
opposite stroke is light orange (#FDBA74), following the classic logo's contrast.
This supersedes the original two-orange wordmark requirement; the icon retains
both original orange components. Combined Home/splash branding uses the same
icon contours. The generator now takes the whole wordmark group from the retained
classic asset and changes only its purple X path to light orange. Tests compare
the complete group against the classic source with that single substitution,
preserving all other geometry, colors and layering in both dark/light variants.
Existing light-theme contrast adaptation remains in the renderer.

`tools/Prepare-SeasonalBranding.ps1` reconstructs these deterministic vector
changes from the existing geometry and hash-checked reference.
`tools/Export-SeasonalIcon.ps1` exports 16/32/48/64/128/256/1024px PNGs and a six-frame
16–256px ICO. All sizes were visually inspected; dimensions, transparency, exact
colors, black edges, seam-free crossing, ICO frames and actual shared WPF X colors
are tested. Native SVG rendering now respects rectangle x/y offsets for the inset
perimeter. Ordinary icons/wordmarks have zero diff against baseline. No image
generation or nonseasonal asset replacement was used. Subjective/physical visual
acceptance remains on the checklist.

## Verification

The baseline Release application/checks build passed with zero warnings/errors;
the unmodified 95-check handoff route also passed before implementation.

Final Release application/checks build: **0 warnings, 0 errors**, log
`verification/nexaween027-final-build.log`.

**656 distinct regression checks pass across the integrity run and final focused
reruns**, plus **two live read-only GitHub discovery checks**. Repeated executions
are not added to these totals. Exact run scope:

| Execution | Result | Evidence |
| --- | --- | --- |
| Pre-implementation handoff route | 95 passed | `verification/baseline-027-handoff.log` |
| Full sixteen inherited routes | 605 passed (604 inherited assertions + one splash-minimum assertion) | `verification/acceptance-20261028-130800/*.log` |
| Final .27 route including selected quadruple borders and classic-wordmark comparison | 51 passed | `verification/nexaween027-classic-logo-final.log`, captures `verification/20261028-133457` |
| Final affected-route reruns | followup-design 31, ecosystem 48, handoff026 95 passed | `verification/nexaween027-final-<route>.log` |
| Post-branding affected-route reruns | handoff026 95, alpha026 28, followup-design 31 passed | `verification/nexaween027-branding-<route>.log` |
| Live discovery, final retry | 2 passed, no package download/install | `verification/nexaween027-public-updates-retry.log`, `verification/20261028-131305` |

The seventeen-route integrity invocation initially exited nonzero because the
new .27 route caught fractional-millisecond truncation before the three-second
minimum. Its other sixteen routes all passed. The final code rechecks monotonic
elapsed time and preserves automatic appearance matching; it was rebuilt and all
affected routes above were rerun successfully. A preliminary preview test also
caught a dispatcher-affinity mistake introduced during this change; preview
initialization now runs on the host dispatcher. No failure is hidden or counted
as a passing run. The full suite was not repeated after these scoped repairs.

The first live-discovery attempt failed DNS resolution inside the network
sandbox; the permitted network retry passed both existing checks. This preserves
the prior handoff's live evidence and adds current discovery evidence without
publishing anything. Empty feed, asset validation, HTTP failure and mixed-version
cases pass mocked HTTP regressions; real newer-package installation remains manual.

Final source-rendered WPF captures were inspected: [shared logo](media/nexaween027/nexaween027-logo.png),
[Add Bugs](media/nexaween027/nexaween027-bug-settings.png),
[in-window splash](media/nexaween027/nexaween027-splash-preview.png) and
[Home](media/nexaween027/nexaween027-home.png). These confirm current labels and
control placement, reference icon borders and the requested white/light-orange wordmark X.
Captures are animation snapshots, not
subjective visual acceptance or physical multi-monitor testing.

Only synthetic isolated stores were used. Existing fixture saves, exports,
score/session identities and history remain covered by the inherited continuity,
core, pass026 and handoff routes. No destructive data migration was added.
New `AdvancedAnimations` is an additive ecosystem setting defaulting to false;
old JSON lacking it remains readable. Strict older readers of newer exports
remain a historical compatibility concern: preserve original exports.

## Manual acceptance checklist

All rows below are **NOT RUN for .27 physical/live acceptance**. Automated WPF
fixtures or earlier captures do not certify these scenarios. Use isolated copies
of workspaces; record machine, DPI, build commit, observations and evidence.

| Scenario | Practical procedure and acceptance target |
| --- | --- |
| Icon and wordmark | Review the automated previews on real displays at every requested size; check transparent edges, silhouette-only black borders and borderless letters, plus Home/splash consistency. |
| Pumpkins/vines | Review Home and multiple widget arrangements at narrow/fullscreen sizes; inspect stems, ribs, candlelight and unobstructed controls. |
| Seasonal animation | Cycle ON → OFF → ON → OFF, including during fades; check no flashes, invisible hit targets or lingering art; repeat reduced motion and restart. |
| Audio | Listen to all five positive and five negative cues; exercise mute/volume and independent seasonal sounds. |
| Quick Start gestures | Add, drag, resize and rename a real widget; navigate Back/Continue, Skip, replay and resume after restart. |
| About/changelog scrolling | Reach every section with wheel, keyboard and scrollbar; check .27 and historical titles and seasonal panel contrast. |
| Settings | Exercise each General tab and Experimental Features; change and reopen all bug controls; check saving/error feedback. |
| Mixed DPI | Move live Home/dashboard/settings between 100%, 150% and 200% monitors; inspect clipping, hit targets and sharpness. |
| Multiple monitors | Open dialogs on each monitor, disconnect/reconnect a monitor and restore saved window bounds. |
| Resize | Resize narrow/wide and maximize/restore with widgets near the bottom/right; inspect workspace scrolling and notification anchors. |
| Fullscreen | Enter/exit repeatedly on multiple displays, including with settings and tutorial active; check bounds and input. |
| Sleep/hibernate | Suspend inside a session and across an overnight/DST boundary; resume and compare actual timestamps, identity and saved state. |
| Crash/power interruption | With disposable data, terminate during edits/save/session finalization; restart and inspect backups, score dedup and history. |
| Live Hedy/microphone | Authorize a test account; grant/revoke microphone; record/stop/reopen history; verify disconnect and actual provider state. |
| Live Hevy | Connect a test account, inspect response/status and disconnect/reconnect. |
| Live Todoist | Authorize a test account, read/update within approved scope, revoke externally and disconnect; inspect accurate state. |
| Real package update | On an isolated install with a genuinely newer approved package, verify download/hash, replacement, health/restart, rollback and unchanged data. Do not publish Alpha to manufacture this test. |
| Long session | Use a large disposable workspace for several hours; record working set, memory trend, GPU/FPS and normal/Developer navigation latency. |

## Remaining priorities

1. Complete physical branding and seasonal appearance review with the user.
2. Complete the manual checklist; no live account or hardware results are claimed.
3. Continue .28 profiling and carefully scoped architecture work using
   [the carry-forward report](NEXAWEEN_028_CARRY_FORWARD.md).

No .27 portable candidate was packaged or promoted, no public Alpha was
published, and no tags/releases were deleted or rewritten. The first planned
retained public release remains **v0.1.0 Beta**, requiring explicit approval.
