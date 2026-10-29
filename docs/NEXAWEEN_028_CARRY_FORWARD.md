# nexa v0.0.28 Alpha — NEXAWEEN EVOLVED carry-forward

October 28, 2026. Continue from `codex/nexa-027-nexaween`, not the older published
0.1.26 tree. Read [the .27 status](NEXAWEEN_027_STATUS.md) and
[audit](NEXAWEEN_027_AUDIT.md); do not repeat the full audit without new evidence.
Verify actual HEAD and pending changes before work. .27 is not fully accepted.

## Retain completed work

- Shared version identity/Windows metadata and legacy Alpha updater comparison:
  `App.xaml.cs`, `SessionDashboard.csproj`, `app.manifest`, `ExperimentalUpdates.cs`.
  Next Alpha is 0.0.28; 0.1.0 is reserved for the intended first Beta.
- Adorable/Advanced defaults, independent appearance/costumes, size/persistence:
  `SettingsDialog.Bugs.cs`, `NexaEcosystem.Models.cs`, `NexaEcosystem.Store.cs`,
  `BugOverlay.cs`. Existing animation renderer is opt-in; do not redesign it.
- Shared three-second splash, stage ordering and in-window preview:
  `SplashWindow.cs`, `NexaWordmark.cs`, `NexaLogo.cs`, `App.xaml.cs`.
- User-supplied icon reference is now present under `assets/references/` with its
  original hash. User selected preview B: quadruple, 24-unit black perimeter
  and union-silhouette outlines; wordmark letters remain borderless. The user's
  final correction makes the X's top-left-to-bottom-right stroke white (#FFFFFF)
  and the opposite stroke light orange (#FDBA74); the icon still keeps its lighter
  #FDBA74 diagonal. All seven PNG sizes and six ICO frames pass.
  Rebuild through `tools/Prepare-SeasonalBranding.ps1`; ordinary assets are unchanged.
- Seasonal visibility, Home cards/scroll/expansion, compact gold Productivity,
  cumulative history/export snapshots, candy respawn/dedup, timer drafts/DST,
  time-zone aliases, General tabs, sounds, Quick Start and recaps remain in place.
  Use the 95 handoff checks and existing routes rather than rebuilding them.
- All fourteen artwork categories and all six costume overlays exist in
  `Nexaween.cs`, `SeasonalBackdrop.cs`, `BugVisuals.cs`.
- Keep verified tips at 30-second intervals / five-second display and preserve
  notification controls (`NexaNotifications.cs` and related notification partials).

## Confirmed remaining gaps and bugs

- Branding's initial missing-reference block was resolved by the user's upload.
  No remaining asset implementation gap is known; physical visual acceptance
  is still open. Keep ordinary and seasonal assets separate, preserve the lighter
  orange component and never add black strokes to wordmark letters.
- No additional timer, score, save or theme bug was demonstrated in this audit.
  Costume-off bypass under explicit Halloween appearance was fixed in .27.
- Initial sandbox access to `M:/nexa` failed, but the final read-only scoped check
  succeeded: HEAD a308076, 26 modified / four untracked source files, all 30 blobs
  match inherited checkpoint 99a9c3a. Recheck changes before future work; preserve
  both checkouts and approved packages.

## Verification scope

Use `tools/Verify.ps1` (seventeen isolated routes), or build
`tests/SessionDashboard.Checks.csproj` in Release and run individual switches.
`--nexaween027` covers metadata, mixed Alpha discovery/ordering, empty/assets
feeds, old JSON/new settings, real controls, costume gating and splash duration.
The status verification addendum contains final counts/logs and any reruns.

Final .27 evidence: 656 distinct regression assertions pass (605 inherited-route
assertions including one new startup-minimum assertion, plus 51 .27 assertions),
across one integrity execution and final affected-route reruns. Two live
read-only release-discovery checks also pass. Release build: zero warnings/errors.
Initial preview-thread and duration-boundary failures were repaired and retested;
the full suite was not repeated after those scoped changes. Branding adds twenty
assertions and final handoff/alpha/startup route reruns. See status for logs.

Automated WPF rendering, synthetic persistence and mocked HTTP do not prove live
provider authentication, production updates, physical mixed-DPI behavior, audio
quality or long-session stability. No new public package was published.

## Performance and deferred architecture

Prior handoff measurements remain the evidence; .27 makes no navigation speedup
claim. Warm normal median ~434ms, cold normal ~692ms, Developer median ~2160ms.
Developer visual construction ~81–144ms remains on the UI thread; first complete
render ~1191–1461ms and teardown still affect click-to-interactive time.
Keep phase instrumentation in `MainWindow.SpaceNavigation.cs`, creation and
constructor paths and `tests/Nexa026Profiles.cs`. Profile layout/teardown first;
do not mask latency with a longer black transition. Detailed logs/interpretation
are in `docs/HANDOFF-COMPLETION-0.1.26-alpha.md`.

Defer major history/widget-persistence architecture, crash-safe recovery redesign,
hibernate/restart reconciliation, scheduling/reset orchestration, advanced score
events, bug AI/locomotion/eyes, combat/territories/nests, particle systems and
long-session optimization. Existing implementations are not presumed missing.
Audit a concrete defect or gap before replacing a system.

## Save compatibility and manual acceptance

No .27 destructive schema migration; existing JSON lacking AdvancedAnimations
loads with false. Preserve saved Adorable/size/costume/appearance choices.
Private exports already include cumulative score/recap and initial viewport
metadata from the handoff; strict older readers may reject newer additive fields.
Preserve original exports. Never use live credentials/data as test fixtures.

The comparison fix cannot retrofit already distributed legacy executables. A
legacy binary may need a manual installation of a newer approved build to gain
the new ordering logic. Plan and document that bootstrap when preparing Beta;
do not publish Alpha merely to test it.

The complete runnable physical/live checklist is in the .27 status: art, seasonal
animation/toggles/restart, audio, full guide gestures, About/Settings, DPI and
monitor movement, resize/fullscreen, sleep/hibernate/crash, live Hedy microphone,
Hevy and Todoist auth/revocation, genuinely newer-package updates, and sustained
memory/GPU/FPS. These remain unaccepted until observed and recorded.

## Recommended sequence

1. Complete physical .27 branding acceptance using the preserved reference and previews.
2. Review .27 execution evidence; run focused regressions for actual .28 changes.
3. Profile the Developer layout/teardown bottleneck on the same fixture.
4. Prioritize demonstrated data-loss risks over architecture expansion; test
   disposable save/export fixtures and failure paths before migrations.
5. Complete physical/live acceptance and Beta readiness separately. Do not
   publish Alpha, auto-transition to Beta or clean up releases/tags. Public
   v0.1.0 Beta and later destructive history cleanup require separate approval.
