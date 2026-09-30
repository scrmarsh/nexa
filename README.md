# Session Dashboard

A native C# / WPF desktop dashboard inspired by the blue pixel display in ClassDashboard and the forest-green theme in ArgoPantryDashboard. The original HTML projects and the `References` brief were read without modification.

## Run

Open **`publish\win-x64\SessionDashboard.exe`**. This is a self-contained Windows x64 executable with the .NET runtime bundled; no separate .NET installation, web server, account, or internet connection is needed. You may copy the executable to another folder. On first launch, bundled native libraries extract to the normal .NET temporary cache.

## Use

- Choose Lecture, Focused work, Meeting, or Studying; adjust the title, duration, and interval, then **Start now**.
- **Save as new** creates a preset. **Update preset** replaces the selected preset's title, duration, and interval. **Delete selected preset** removes it.
- Expand **Schedule start and end** to enter dates and local times. For an overnight session, set the end date to the following day. Sessions may last up to seven days. Scheduling a session that has already started is supported when its end is still in the future.
- The countdown shows time until the start of a future session, then time remaining. The grid fills as time passes; the current pixel pulses. The final timeline interval may be shorter than the others. Very long timelines group intervals to keep the UI responsive.
- **End session** stops the current timer and preserves its elapsed progress. Starting another session replaces the timer. Notes and checklist items remain available across sessions.
- Scroll the control panel to **Make it yours** to show or hide notes/checklists and change the accent and pixel colors using six-digit hex values.
- **Compact mode** keeps the session title and countdown in a small window. **Always on top** works in both modes.
- **I'm bored** opens *The Last Greenhouse*. Three starting routes branch into six endings, with a saved choice journal. The session timer remains visible and continues. **Back to focus** returns to the pixels. Replaying the story retains discovered endings.

## Local saves

Settings, presets, notes, checklist, current session, and story progress live in:

```text
%LOCALAPPDATA%\SessionDashboard\state.json
```

Timer and story changes save immediately; note/checklist/settings edits save after a short debounce and on exit. Saves replace the JSON file atomically and retain a `.bak` backup. Unreadable settings recover from that backup when available. The unreadable file is preserved before the next save. Save failures appear in the status line; closing with unsaved changes asks before discarding them.

Timers use absolute timestamps, so time spent with the app closed or the computer asleep still counts. Story progress resumes from the most recent choice. Use one app instance at a time to avoid competing edits to the same save file.

## Build and check

Requires the .NET 10 SDK on Windows for development:

```powershell
dotnet build SessionDashboard.csproj
dotnet run --project tests/SessionDashboard.Checks.csproj -c Release
dotnet publish SessionDashboard.csproj -p:PublishProfile=WindowsStandalone
```

The executable is written to `publish\win-x64\`. Publishing may need network access to download the Windows runtime packages on the first build. The app itself stays offline.

The dependency-free check runner tests timer boundaries, persistence and recovery, the story graph, and actual WPF control handlers. It renders dashboard, small-window, compact, and story PNGs into a timestamped `test-output` directory. It uses isolated files and does not touch personal app settings.

To check the published executable's real WPF startup without leaving a window open:

```powershell
Start-Process -FilePath '.\publish\win-x64\SessionDashboard.exe' -ArgumentList '--verify-startup','test-output\packaged-startup' -WindowStyle Hidden -Wait
```

This opt-in diagnostic renders an off-screen window, writes `startup.json` and `startup.png`, and exits. Failures write `startup-error.txt`. Diagnostic state is isolated from normal app settings.

Validation details are in [VALIDATION.md](VALIDATION.md).
