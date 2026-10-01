# Validation — October 1, 2026 (v1.2.0)

## Result

The isolated WPF check runner passed **130 checks**:

```powershell
dotnet run --project tests/SessionDashboard.Checks.csproj -c Release --no-restore
```

Artifacts: `test-output/20261001-143559/`. Dashboard, history preview, compact and small-window screenshots were rendered; the dashboard and history preview were visually inspected. All fixtures use isolated files and leave personal app data untouched.

The sandbox could not access DPAPI's user-profile context. Running the checks with the loaded Windows profile passed the existing credential-protection checks. Early runs also caught and fixed history-preview refresh and background-save dispatcher issues.

## Continuity coverage

- Actual 30-second autosave during 31 seconds of continuous text changes, preventing the 600 ms debounce from being the reason for the save.
- Debounced background saving, idle no-op, timestamp indicator and opacity fade after three seconds.
- An edit arriving during background I/O remains dirty after the older capture completes.
- Close/reopen flushes pending notes and retains the same session identity.
- Independent dashboard notes, pending task input, title and interval drafts survive switching.
- Reset archives a deep copy, clears session content, and preserves dashboard identity, theme and panel settings.
- Continue creates an editable copy with provenance and archives displaced work; editing it cannot change history.
- Merge appends notes and labelled interval/Hedy content, deduplicates tasks, preserves layout by default, and ignores a repeated merge of the same source.
- The real history dialog groups sessions by date and shows a read-only preview without changing current content.
- Versioned configuration-only export omits private notes, transcripts, owner IDs and credentials. Import creates a fresh owned dashboard. Private data needs explicit acceptance; invalid versions, unknown credential fields and malformed collections are rejected.
- Simulated distinct owners cannot load, query, continue or merge another owner's records; one dashboard cannot continue another dashboard's history.
- Unclean-shutdown metadata restores the latest saved state with a recovery notice. A damaged current payload recovers from its latest valid snapshot; corrupt primary files recover their backup.
- Recovery snapshots remain capped at three per current dashboard. Archives contain one final state without redundant snapshots.
- Forced filesystem failure leaves edits dirty, displays `Save failed`, and prevents a reset from replacing current work. Retrying after storage recovery succeeds.
- Existing timer, preset, Hedy, story, fullscreen, compact mode and control-handler regression checks pass.

## Windows package

```powershell
dotnet publish SessionDashboard.csproj -p:PublishProfile=WindowsStandalone --ignore-failed-sources
```

Published successfully to `publish/win-x64/SessionDashboard.exe`, a self-contained Windows x64 single-file application (64,885,916 bytes). The first no-restore attempt lacked the runtime-specific assets target; restoring and publishing succeeded using available packages. NuGet vulnerability metadata was unavailable due to an SSL connection error (`NU1900`); no application dependencies were added.

SHA-256: `FB4F04D7C74A302989E0C608E2CE2A2FFAF25080CF9BCEA9AE5B097A0A35B11A`.

The published executable was launched hidden with `--verify-startup test-output\continuity-packaged-startup`. Its real WPF `ContentRendered` check exited with code 0 and reported `Success: true`, `Pixels: 540`, `Presets: 4`, and `ProcessArchitecture: X64`. Diagnostic output uses isolated state.

## Scope and limits

This implementation covers this WPF project's notes, tasks, timers, interval/Hedy data, story, presets, settings and input drafts. This project has no Pantry tracker, Church widgets, draggable widget framework or application login. User isolation uses Windows-account local storage, owner validation and a per-user app-instance mutex.

User isolation and crash recovery were exercised with controlled owner IDs and unclean/corrupted save fixtures, not a real Windows account switch or forced power loss. Windows sign-out saving is wired through WPF session-ending events but was not triggered on the host. Native file-picker interaction and confirmation dialogs were not automated; the import/export, reset and merge operations behind them were tested. A separate clean Windows machine, screen-reader behavior and every DPI/monitor combination were not available.
