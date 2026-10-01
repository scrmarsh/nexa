# Nexa

A native C# / WPF desktop dashboard inspired by the blue pixel display in ClassDashboard and the forest-green theme in ArgoPantryDashboard. The original HTML projects and the `References` brief were read without modification.

**Nexa** is a customizable multi-user desktop dashboard app designed to help people manage different types of sessions from one place. It can be used for classes, work shifts, church meetings, study sessions, meetings, focus time, or any other activity that benefits from a live dashboard.

## Current features and usability

Nexa is built for everyday local-first use: each Windows account keeps a private dashboard workspace, autosave protects ongoing work, presets can be customized quickly, and previous sessions remain searchable and recoverable. The app supports multiple dashboard types, live timers, notes/checklists, session continuity, and export/import workflows without needing a server or internet connection.

The current desktop release is intentionally simple to use: open the app, pick a dashboard or create a new one, customize the layout, and continue from the most recent saved state. The interface is meant to feel fast and familiar, with clear controls for starting sessions, viewing history, importing dashboards, and preserving work between sessions.

> Web app version coming soon. The current desktop build remains the primary release while the web experience is being prepared.

Each user logs in with their own email and password and gets a completely private workspace. Their dashboards, notes, presets, history, tracker data, and saved sessions are kept separate from every other user.

Nexa is built around a universal widget system. The included Class, Argo Pantry, Church, Study, Meeting, and Focus dashboards are simply preconfigured presets. Users can fully customize them by adding, removing, moving, resizing, or configuring widgets, and they can create entirely new dashboards from scratch.

Examples include a Class dashboard with a timer, notes, session timeline, and Hedy tools; an Argo Pantry dashboard that tracks students and customizable item categories like freezer, fridge, fruit, or canned goods; and a Church dashboard with Bible tools, Scripture pins, notes, prayer requests, and meeting information. Widgets are reusable across all dashboards, so a Bible widget, tracker, timer, task list, counter, notes panel, or other tool can be placed wherever the user wants.

Nexa automatically saves active dashboard changes every 30 seconds and briefly displays a **“Saved at [time]”** indicator. Dashboards also retain their data after the app closes, including notes, students, counters, categories, tasks, and other session information. By default, reopening a dashboard continues from its most recently saved state.

Sessions are stored by date so users can browse previous work, view old notes and data, continue an earlier session, or use **MERGE TO CURRENT DASHBOARD** to bring information from an older session into the current one. A deliberate **RESET DASHBOARD** action archives the current session and starts a fresh one without destroying the dashboard's layout or customization.

Nexa also includes a lightweight boredom mode built directly into the pixel timer. **Pixel Blast** lets the timer digits fall apart into physics-based blocks that can be destroyed for points. Clearing them unlocks **I AM STILL BORED**, which launches **Pixel Swarm**, a survival game where the player dodges incoming pixels. The actual session timer continues running throughout, and returning to the dashboard reconstructs the timer using the correct current time.

Overall, Nexa is meant to be a **modular personal session workspace**: presets give users a starting point, widgets provide the building blocks, autosave protects their work, and each person can shape their dashboards around exactly what they need.

## Run

Open **`publish\win-x64\SessionDashboard.exe`**. This is a self-contained Windows x64 executable with the .NET runtime bundled; no separate .NET installation, web server, account, or internet connection is needed. You may copy the executable to another folder. On first launch, bundled native libraries extract to the normal .NET temporary cache.

## Use

- Choose Lecture, Focused work, Meeting, or Studying; adjust the title, duration, and interval, then **Start now**.
- **Save as new** creates a preset. **Update preset** replaces the selected preset's title, duration, and interval. **Delete selected preset** removes it.
- Expand **Schedule start and end** to enter dates and local times. For an overnight session, set the end date to the following day. Sessions may last up to seven days. Scheduling a session that has already started is supported when its end is still in the future.
- The countdown shows time until the start of a future session, then time remaining. The grid fills as time passes; the current pixel pulses. The final timeline interval may be shorter than the others. Very long timelines group intervals to keep the UI responsive.
- **End session** stops the current timer and preserves its elapsed progress. Starting another timer archives the previous session before replacing its timer; notes and checklist items carry forward.
- Use the dashboard picker to switch independent workspaces, or **New dashboard** to create one from a preset. Each dashboard remembers its notes, tasks, timer, interval/Hedy notes, story, presets, colors, panel settings, window size and input drafts. Existing installations migrate into **My dashboard** automatically.
- **PREVIOUS SESSIONS** shows dated, searchable, read-only previews, with date filtering and sorting by creation time, last access or session name. **CONTINUE THIS SESSION** archives current work and restores an editable copy of the selected session. Timer timestamps remain absolute; continuing an expired timer does not restart its clock.
- **MERGE TO CURRENT DASHBOARD** appends notes and source-labelled interval/Hedy content and imports missing incomplete tasks. It preserves the current timer and layout by default; historical configuration can be included explicitly. Identical tasks and repeated merges are skipped. Historical content stays preserved.
- **RESET DASHBOARD** asks for confirmation, archives current work, and starts a blank session while retaining dashboard identity, presets, colors, panel settings and window size. Closing or switching dashboards never resets content automatically.
- **EXPORT DASHBOARD** writes a versioned `.nexa` file. Configuration only is the default; private current-session data requires an explicit checkbox. **IMPORT DASHBOARD** validates the file, asks before importing private session data, and creates a separate dashboard owned by the current Windows account. Credentials and session history are never exported. Historical sessions can also be exported or deleted (with confirmation) from history.
- Scroll the control panel to **Make it yours** to show or hide notes/checklists and change the accent and pixel colors using six-digit hex values.
- **Compact mode** keeps the session title and countdown in a small window. **Always on top** works in both modes.
- **I'm bored** opens *The Last Greenhouse*. Three starting routes branch into six endings, with a saved choice journal. The session timer remains visible and continues. **Back to focus** returns to the pixels. Replaying the story retains discovered endings.

## Local saves

Dashboard records, current sessions, bounded recovery snapshots and archived history live in:

```text
%LOCALAPPDATA%\SessionDashboard\state.json
```

Edits save after a 600 ms debounce, with a **30-second safety net** during continuous editing. Idle dashboards do not cause repeated full writes. Autosave disk writes run in the background using detached state; dirty edits made during a write remain pending. Important transitions save before switching, reset, continue, import or exit. **Saving...**, **Saved at …** and **Save failed** appear below the title in a reserved line. Success fades after three seconds; errors do not trigger repeated popups.

Saves use a flushed temporary file and atomic replacement, with a `.bak` backup. Each current dashboard retains only three recent snapshots; archived sessions retain one final copy. Unreadable saves recover from the backup when available, preserving the unreadable source on the next save. An unclean shutdown restores the latest saved workspace and reports its save time. Failed saves keep dirty edits; closing asks before discarding them. Failed reset/continue/import commits leave the current editor intact.

This WPF project uses the **signed-in Windows account**, not a separate Nexa login. Save files are under that account's local app-data directory, with ownership checked on dashboard/session access. Another Windows account cannot open a copied workspace as its own; use an explicit portable export/import to transfer configuration. The optional DPAPI-protected Hedy key and region live separately in `state.json.credentials`, outside dashboard/history/export payloads. Legacy backups may still contain the old DPAPI-protected key from before migration.

Timers use absolute timestamps, so time spent with the app closed or the computer asleep still counts. Story progress resumes from the most recent choice. A per-user process mutex prevents competing normal app instances from overwriting each other's saves.

Continuity covers the controls implemented in this WPF project. Pantry students/categories, Church panels and a draggable widget system are not part of this codebase.

## Build and check

Requires the .NET 10 SDK on Windows for development:

```powershell
dotnet build SessionDashboard.csproj
dotnet run --project tests/SessionDashboard.Checks.csproj -c Release
dotnet publish SessionDashboard.csproj -p:PublishProfile=WindowsStandalone
```

The executable is written to `publish\win-x64\`. Publishing may need network access to download the Windows runtime packages on the first build. The app itself stays offline.

The dependency-free check runner tests timer boundaries, persistence/recovery, reset/continue/merge, export/import validation, user/dashboard isolation, actual filesystem failure, and WPF controls. It includes a real 31-second continuous-edit run to verify the autosave safety net, plus debounce, fade and close/reopen checks. It renders dashboard, history, small-window, compact and story PNGs into a timestamped `test-output` directory. It uses isolated files and does not touch personal app settings. DPAPI checks require a loaded Windows user profile.

To check the published executable's real WPF startup without leaving a window open:

```powershell
Start-Process -FilePath '.\publish\win-x64\SessionDashboard.exe' -ArgumentList '--verify-startup','test-output\packaged-startup' -WindowStyle Hidden -Wait
```

This opt-in diagnostic renders an off-screen window, writes `startup.json` and `startup.png`, and exits. Failures write `startup-error.txt`. Diagnostic state is isolated from normal app settings.

Validation details are in [VALIDATION.md](VALIDATION.md).
