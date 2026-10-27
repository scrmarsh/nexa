# Nexa 0.1.24 Alpha — final-prompt repair candidate

**Status: critical-repair candidate verified for its implemented scope; final-release acceptance remains open.**

Version remains **0.1.24 Alpha**. The working branch is `codex/nexaween-final-024`; the source recovery point is `codex/recovery-before-final-024` at published commit `4562d69`. The published GitHub release and running root executable are unchanged. The separate Aevoria project was not modified.

## Root causes and functional repairs

- History schema validation could throw `InvalidDataException` outside the loader's catch filter and terminate the History action. Recovery now handles each archived record independently, retaining malformed original fields additively as `PreservedHistory` instead of discarding the readable Space. The original file is copied to `.recovered-*` before a subsequent recovery save.
- Completely unreadable primary/backup files now block writes over fallback defaults. They remain available for manual recovery, and History provides useful error/empty states. Ownership checks remain enforced.
- Home History and dashboard Notes & History use one shared, read-only projection of the existing workspace. They include current and archived reports, original damaged-record exports, and all available known notes/tasks/counters/timers/transcripts/pins/scores. Nothing invents unrecorded past events.
- Archive loading, searching and report generation run in the background. UI updates explicitly marshal to the owning dispatcher, including failures and closing cancellation. A 1,201-record fixture checks responsiveness and search. Lists are virtualized. Editing, confirmed deletion/clear, exports and explicit continue/merge actions remain available for readable archives; mutations are disabled for damaged/recovery views.
- The Pixel Timer editor exposes **Manual Mode** and **Schedule Mode**, reusing the existing persisted `AutomaticScheduling` field on sessions/presets. Schedule mode hides Reset and the handler also rejects it. Manual duration can be inferred from its start/end if omitted. Committed edits reuse timestamp-derived progress, existing weekly/time-zone scheduling and linked displays.
- The old Appearance tab explicitly opened a lockout window while seasonal mode was active. It now stays accessible in the same Settings window with a single neon-green-labeled top toggle. Ordinary customization is hidden during seasonal mode and restored afterward. The footer no longer contains a duplicate seasonal switch/pumpkin.
- Session settings have no sidebar/global clock tab or seasonal controls. Global settings restore General/Experimental grouping, with Updates still experimental and Add Bugs at the bottom. Saving feedback starts hidden, appears after successful persistence, and fades after about two seconds.
- Widget rectangles formerly clipped all bug rendering, hiding creatures over cards. They now protect hit testing while the viewport bounds protect rendering, keeping legs below navigation. Independent behavior/appearance selectors apply to the actual manager. Retired Creepy behavior maps to Classic while retaining its Creepy appearance; old numeric values remain readable.
- The separate ecosystem-map UI and its window-bounds path are retired. Overlay persistence, reusable movement/rendering, nests/resources/webs and squash statistics remain. Switching a full single-species population to All Bugs frees excess slots and preferentially adds underrepresented species without squash credit.

## Interface, sounds and scoring

- Home has one centered **+ Create New Space** action; import remains in the footer, saved Spaces remain accessible, and Show More reveals another three rows. Changelog remains inside About.
- The workspace Add Widget control is centered, faint, borderless, and uses text above a large plus. Existing layout/scroll-extent behavior is retained.
- Shared popup frames retain rounded clips, a continuous accent border and themed background.
- Sounds has explanations, Test Sound directly above the volume slider and separate positive/negative previews. Short generated pop/chime/eerie WAV cues respect volume; preview/productivity/seasonal playback shares a gate. Saved-event IDs prevent history replay from producing sound storms. Physical audio quality/device acceptance remains manual.
- Autosave stays left and tips right, lower in the client area. Notification margins follow the actual monitor work area in client DIPs and update on movement/state/DPI changes. Fullscreen remains topmost when settings are applied, fixing a path that could expose the taskbar. Exact autosave states use one set of message constants.
- The centralized score, two-minute capped half-step streak, deduplication and history remain. Direct bug squashes no longer trigger full-window green feedback. The new centered streak popup and visual decay are not implemented yet.
- In-app **0.1.23 Alpha — Decorating for Halloween** replaces the Living Worlds title/claims. Unrelated older release entries remain. 0.1.24 is not marked final.

## Verification

Release build: **0 warnings, 0 errors**. The complete **eleven-route suite passed 371 checks**. An additional **two read-only existing-workspace compatibility checks passed**.

Local acceptance evidence: `verification/acceptance-20261027-153534/`. These logs and private fixture copies are intentionally ignored by Git. The package receives an isolated `--verify-startup` check before delivery.

The existing-workspace compatibility check uses a read-only isolated snapshot, not the live application file. It loaded **8 Spaces and 49 archived reports**, required no recovery records, and verified that loading did not rewrite the snapshot. Credentials and recordings were not copied or exercised. No personal data enters Git.

The suite covers configured autosave intervals, event persistence, save failure/reset protection, simulated missed transitions/restart recovery, UTC/DST timing, pixel ordering, three-decimal percentages, linked progress, controls, layout/scrolling, scoring, bug persistence, and existing procedural anatomy/animation. Only the 30-second autosave interval has a full continuous-typing elapsed-time exercise; configuration tests do not imply waiting through every long interval.

## Remaining final-release acceptance

The following are **unfinished or unverified**, and block labeling this the final release:

1. A transactional, authenticated GitHub updater with backup, integrity/authenticity policy, safe external-process replacement, rollback/restart and same-version final-build handling. Current updater checking/download work remains experimental; it has not passed an end-to-end installation test, including this private repository's authorization.
2. Adaptive **Infestation** with no fixed population-count cap and measured resource/FPS safeguards. Only Low/Normal/High are currently exposed and bounded. No extended 20-FPS claim is made.
3. Final Integrations ordering/availability, including the requested Hevy entry. Existing Hedy/account integrations and credentials remain; no unimplemented provider is newly labeled functional.
4. The exact three-second logo-only splash stage before revealing the real initialization track. Existing accurate initialization progress is retained.
5. The new mixed fading background lifecycle shared by Home/dashboard/Appearance: ten requested categories, balanced density, independently timed 5–10-second visibility and coordinated candy consumption. Current background remains the prior candy system.
6. The static upper-left costumed spider/web, large shaded bottom-right pumpkin, extensive branching vines and matching menu artwork. Current widget-target rectangles remain separate from collision/reserved regions.
7. Two or three selected widget pumpkins with growing translucent vines and bug surface interactions. Existing anchored corner decorations are retained, not claimed as this complete behavior.
8. Final damage/sad-face/tear effects, elimination of any remaining yellow targeting visuals, the illustrated per-species Bugs Squashed layout and durable new per-species totals.
9. The centered, reused Productivity popup with live gains, flames and half-step visual decay after the actual multiplier expires. Earned score must remain intact.
10. Further coordinated candy interaction/locomotion polish and reduced-motion/performance acceptance. Existing species-specific articulated animation remains; it is stylized planar rendering, not a 3D biomechanics model.
11. Final splash/tagline/branding acceptance in every normal/seasonal location, including the Windows shell executable/shortcut icon. The existing runtime branding is retained; the packaged executable currently uses its seasonal icon.
12. Final notification expiry fade and full physical window/multi-monitor acceptance. Success already expires after approximately two seconds; exhaustive hardware behavior is not claimed.

Manual checks still required: real sleep/hibernate/power interruption, actual mouse/trackpad interaction, mixed-DPI/multi-monitor/taskbar/fullscreen cases, native dragging/resizing, physical sound output, live account/microphone/transcription flows and long memory/GPU sessions. Synthetic clock/restart tests do not substitute for these.

The source is prepared as a reviewable candidate. Close an older Nexa instance before manually testing a candidate against the same saved workspace; packaging/startup verification uses isolated stores. No public release or production-release replacement is authorized by this pass.

## Files modified or added

- [BugOverlay.cs](../BugOverlay.cs)
- [CHANGELOG.md](../CHANGELOG.md)
- [DashboardBugs.cs](../DashboardBugs.cs)
- [DashboardContinuity.cs](../DashboardContinuity.cs)
- [HistoryArchive.cs](../HistoryArchive.cs)
- [HomeWindow.History.cs](../HomeWindow.History.cs)
- [HomeWindow.Recent.cs](../HomeWindow.Recent.cs)
- [HomeWindow.cs](../HomeWindow.cs)
- [InterfaceAudio.cs](../InterfaceAudio.cs)
- [LEGACY/README.md](../LEGACY/README.md)
- [LEGACY/manifest.json](../LEGACY/manifest.json)
- [LEGACY/notes/IMPLEMENTATION-0.1.24-published-4562d69.md](../LEGACY/notes/IMPLEMENTATION-0.1.24-published-4562d69.md)
- [LEGACY/source/NexaEcosystem.Window.cs](../LEGACY/source/NexaEcosystem.Window.cs)
- [LEGACY/tests/Nexa024EcosystemChecks-before-final.cs](../LEGACY/tests/Nexa024EcosystemChecks-before-final.cs)
- [MainWindow.Bugs.cs](../MainWindow.Bugs.cs)
- [MainWindow.ClassSchedule.cs](../MainWindow.ClassSchedule.cs)
- [MainWindow.Continuity.cs](../MainWindow.Continuity.cs)
- [MainWindow.Feedback.cs](../MainWindow.Feedback.cs)
- [MainWindow.NotesHistory.cs](../MainWindow.NotesHistory.cs)
- [MainWindow.Reliability.cs](../MainWindow.Reliability.cs)
- [MainWindow.Session.cs](../MainWindow.Session.cs)
- [MainWindow.xaml.cs](../MainWindow.xaml.cs)
- [Models.cs](../Models.cs)
- [NexaChrome.cs](../NexaChrome.cs)
- [NexaEcosystem.Models.cs](../NexaEcosystem.Models.cs)
- [NexaEcosystem.Simulation.cs](../NexaEcosystem.Simulation.cs)
- [NexaEcosystem.Store.cs](../NexaEcosystem.Store.cs)
- `NexaEcosystem.Window.cs` — retired; preserved in `LEGACY/source/`.
- [NexaFeedback.cs](../NexaFeedback.cs)
- [NexaNotifications.cs](../NexaNotifications.cs)
- [NexaWindowBounds.cs](../NexaWindowBounds.cs)
- [Nexaween.Animation.cs](../Nexaween.Animation.cs)
- [Nexaween.cs](../Nexaween.cs)
- [README.md](../README.md)
- [ReleaseNotes.cs](../ReleaseNotes.cs)
- [SessionHistoryWindow.cs](../SessionHistoryWindow.cs)
- [SettingsDialog.Autosave.cs](../SettingsDialog.Autosave.cs)
- [SettingsDialog.Bugs.cs](../SettingsDialog.Bugs.cs)
- [SettingsDialog.Navigation.cs](../SettingsDialog.Navigation.cs)
- [SettingsDialog.cs](../SettingsDialog.cs)
- [docs/DEVELOPMENT.md](../docs/DEVELOPMENT.md)
- `docs/FINAL-0.1.24-STATUS.md` — retired; preserved in `LEGACY/source/`.
- [docs/IMPLEMENTATION-0.1.24.md](../docs/IMPLEMENTATION-0.1.24.md)
- [docs/ROOT-AUDIT.md](../docs/ROOT-AUDIT.md)
- [tests/Nexa024AnimationChecks.cs](../tests/Nexa024AnimationChecks.cs)
- [tests/Nexa024CoreChecks.cs](../tests/Nexa024CoreChecks.cs)
- [tests/Nexa024EcosystemChecks.cs](../tests/Nexa024EcosystemChecks.cs)
- [tests/Nexa024Runner.cs](../tests/Nexa024Runner.cs)
- [tests/NexaFinalChecks.cs](../tests/NexaFinalChecks.cs)
- [tests/NexaFollowupChecks.cs](../tests/NexaFollowupChecks.cs)
- [tests/NexaFollowupDesignChecks.cs](../tests/NexaFollowupDesignChecks.cs)
- [tests/NexaweenMoreChecks.cs](../tests/NexaweenMoreChecks.cs)
- [tests/SessionDashboard.Checks.csproj](../tests/SessionDashboard.Checks.csproj)
- [tools/Verify.ps1](../tools/Verify.ps1)
