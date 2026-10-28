# Nexa 0.0.29 audit — before implementation

2026-10-28. Source: continuation-source, clean HEAD 054fffdc5f0e42f292dcfb28d3a60631b2ba8024, branch codex/nexa-028-nexaween-evolved. Work branch: codex/nexa-029-polish. Parent workspace is an unrelated unborn master with pre-existing untracked material; leave it alone. No AGENTS.md found. Read .27 audit/status, .28 audit/status/carry-forward and existing code/tests. Prior .28 evidence: 695 assertions; this is inherited evidence, not a new execution. The later eight-leg fix is retained.

Classification applies to this request, not historical acceptance. COMPLETE means established prior automated evidence; physical acceptance remains separate. No architectural replacement is justified.

| Request | Baseline | Evidence and necessary action |
|---|---|---|
| 1 audit/provenance | COMPLETE | Clean nested source and reports inspected; this document precedes code edits. |
| 2 identity | PARTIAL | App.xaml.cs shared assembly label and csproj/manifest at .28; advance current metadata only, lowercase label and left alignment. Preserve ExperimentalUpdates comparison. |
| 3 typography | PARTIAL | Segoe UI body and Consolas numerical displays already used; consolidate shared inherited font resources; Home titles currently 16. Wordmark remains artwork. |
| 4 Home tiles | PARTIAL | HomeWindow.Recent.cs has stagger/reverse collapse and animated MaxHeight. Icon is left; title 16; change right/19, ensure contrast and title/timestamp separation. Existing central action gap 12 versus cards 8. |
| 5 fullscreen | PARTIAL | MainWindow.ToggleFullscreen and NexaWindowBounds native monitor bounds exist. Custom caption buttons remain visible; chrome/margins and notification/settings need completion. Monitor/DPI acceptance UNVERIFIED. |
| 6 Windows status | MISSING | No fullscreen battery/network/Core Audio status or display options. Add compact real status and persisted options; actual device/multiple-monitor acceptance UNVERIFIED. |
| 7 splash/branding | BROKEN | SplashWindow minimum 3 seconds, PreviewIn stretches renderer across host; NexaWordmark version right. csproj embeds seasonal executable icon. Theme logo resources exist; preserve approved artwork, fix compact preview, five-second gate and Classic executable icon. |
| 8 wallpapers | PARTIAL | SpaceBackground data and per-Space Session page exist, preserve identity. Missing Home wallpaper, fit, preview/default controls. Session appearance disabled in seasonal mode; remove restriction while retaining overlays. |
| 9 Pixel Timer | PARTIAL | PixelTiming and schedule drafts/DST verified. Main/secondary labeling and countdown units need adjustment; MainWindow.ClassSchedule editor exists but SettingsDialog.Session links externally. Embed existing editor; remove redundant widget Edit entry. |
| 10 timeline | PARTIAL | MainWindow.SessionWidgets uses fixed content height calculated only at construction, large recorder/Hedy shortcuts. Add source menu, explicit recording, dynamic scrolling height. Existing recordings retained. |
| 11 intervals | PARTIAL | Actual IntervalSummaries/HedyIntervalNotes exist; timeline uses Open session intervals button. Replace with visible source-based expandable cards. Offline local transcription unavailable; say so and never generate invented summaries. |
| 12 Hedy | BROKEN | BuildHedyNotes fixes transcript Height from initial widget height; resizing does not resize transcript. Bind useful content area to actual widget body. |
| 13 Productivity | PARTIAL | MainWindow.Reliability ring/gold art/service/details menu exist. Add live compact two-row breakdown and sufficient minimum height. |
| 14 distraction | PARTIAL | MainWindow.WidgetFocus has only threshold phrases 5–30, empty at zero. Replace with requested 0–20 phrases, preserve counter archive. |
| 15 lifecycle | BROKEN | MainWindow.Completion commits recap into Current but does not add History or reset. Completion animates pixels upward, not disappearing. Reuse detached CommitWorkspace and existing recap; explicit finalized snapshot/history/reset checkpoint, idempotence and save failure tests. Scheduled reset already save-first; preserve postponement. Task preservation must include all local tasks for this new request. |
| 16 history/restore | PARTIAL | SessionHistoryWindow and Continuity.Continue/Merge exist; review styling and explicit resume/new options, candidate-first overwrite confirmation, preserve original records and scoring identities. |
| 17 friendly bugs | PARTIAL | Classic/Cozy noncombat already exist, Creepy selector restored in .28 and must retain legacy values but remove from normal presentation. Click damage works. Inspect costume hats and add click-only fading health. |
| 18 webs | PARTIAL | NexaSilk creates spider-owned progressive strand geometry and BugOverlay draws it. Preserve construction; inspect green color/geometry and abdomen tether. Screens/scroll acceptance UNVERIFIED. |
| 19 Home spider | UNVERIFIED | SeasonalBackdrop/Nexaween already have decoration and animation; inspect before refinement, preserve motion gate. |
| 20 Session Settings | PARTIAL | Existing Session page contains duration/reset/save options and Space background; embed existing Pixel Timer controls instead of rebuilding. |
| 21 performance/reliability | COMPLETE / UNVERIFIED | Preserve .28 lazy body loading, atomic persistence, disposals. No architectural optimization justified. Long-run/hardware performance remains UNVERIFIED. |
| 22 verification | UNVERIFIED | Run Release checks and Verify.ps1, add synthetic lifecycle/failure/restore coverage. Record failures/reruns; native captures are not physical acceptance. |
| 23 deliverables | MISSING | Add status/results/changelog and captures where practical; report exact HEAD, modified files and remaining acceptance. |
| 24 restrictions | COMPLETE | Local unpublished Alpha work only; no push/release/tag or user data manipulation. |

## Built-in state/reset inventory (baseline)

Session resettable: Pixel timer/session intervals/timeline/Hedy link and current transcript/local recording reference; Notes and session-scoped text; Counter/distractions; Water Bottle Counter; Alcoholic Beverage; independent Timer/Break timer/Stopwatch; Progress bar; Bugs Squashed session ledger; Productivity session events. Completed source recordings/transcripts must survive in History and files. Respect WidgetInstance.SessionScoped opt-outs.

Space persistent: widget layout/size/visibility, Space name/icon/background, timer drafts/schedules/presets, Task List and legacy Tasks (all tasks must be retained), Scripture permanent pins, Bible state, Flashcards, Calculator history, Distribution Tracker ledger, reading/reference settings, Focus Reset preferences, story progress. Mixed widgets reset only their session fields.

Global persistent: application appearance/options, all-time squash/species and productivity totals, credentials/connections, user profile, shared ecosystem/world saves. External-provider-managed: Todoist/Google Tasks/Calendar/Hevy/Hedy remote data; do not copy remote tasks or start microphone/provider recording during restore/link.

No schema migration or destructive rewrite planned. Additive fields use defaults for old saves. Strict old export readers may reject additive fields; retain original exports. Architectural projects and physical/live acceptance are separate from implemented source, and will be carried forward explicitly.
