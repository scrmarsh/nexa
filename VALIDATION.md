# Validation — September 30, 2026

## Build and package

- `dotnet build SessionDashboard.csproj -c Release --no-restore`: passed with **0 warnings and 0 errors**.
- `dotnet publish SessionDashboard.csproj -p:PublishProfile=WindowsStandalone --no-restore`: passed.
- Output: `publish/win-x64/SessionDashboard.exe`, **64,847,502 bytes**, Windows x64, single-file, self-contained.
- The generated runtime configuration includes `Microsoft.NETCore.App` and `Microsoft.WindowsDesktop.App` version 10.0.12.
- SHA-256: `E2D3C419C71A607C93CD6A1B5AB8AA77ACA5ECC3325FB565E5D877DEFFCB9CEF`.

The first runtime-package restore failed under the restricted network environment. The approved retry downloaded the required packages and publishing succeeded.

## Automated checks

`dotnet run --project tests/SessionDashboard.Checks.csproj -c Release --no-restore` passed **51 checks**. The runner exercises real WPF controls and their event handlers, as well as application models and file persistence.

Coverage includes:

- Future, running, overnight, completed, and stopped sessions; elapsed and remaining time; clamped progress after a simulated late restart; countdown rounding and durations longer than 24 hours.
- Title/duration/color validation; invalid input preserving the active session; explicit overnight scheduling; partial final intervals.
- Creating, editing, selecting, and deleting presets through WPF buttons, with disk readback.
- Notes and completed checklist items surviving saves; optional notes visibility; custom colors; topmost property; compact mode and compact restoration.
- Saved session timestamps; settings round trips; invalid JSON fallback; backup recovery; preservation of unreadable source files.
- Active pixel animation, stopping animation for future sessions, and clearing it at completion.
- Every story scene reachable, all six endings reachable, no broken destinations or cycles, choice-dependent outcomes, saved journals, and replay preserving discovered endings.
- Opening story mode with the countdown visible; pumping the WPF dispatcher for 1.3 seconds to verify that countdown text changes during the story; immediate choice persistence; closing and recreating the window to restore the scene; reaching an ending through UI buttons.

Final automated-test artifacts: `test-output/20260930-111945/`. PNGs include dashboard, small dashboard, compact view, story scene, and story ending. Test files are isolated from normal app settings.

## Published executable startup

The final **published EXE**, not just a development build, was launched with `--verify-startup test-output\packaged-startup`.

Its real WPF `ContentRendered` event produced `test-output/packaged-startup/startup.json` with `Success: true`, `Pixels: 540`, `Presets: 4`, and `ProcessArchitecture: X64`. The diagnostic rendered `startup.png` and then shut the app down. It did not create or modify normal user settings.

An earlier check based on `Process.MainWindowHandle` could not identify the window of a process launched hidden. It was replaced with the explicit, opt-in WPF startup diagnostic above; the earlier check is not counted as a pass.

Dashboard, small-window, compact, story, and packaged-startup renders were visually inspected. A cropped diagnostic screenshot was corrected by including the root element's layout margins in the image dimensions.

## Limits

Validation ran on this Windows x64 machine with .NET SDK 10.0.401. A separate clean machine without .NET installed was not available; self-contained deployment was verified through the publish configuration, bundled runtime metadata, and successful launch of the resulting EXE. Physical sleep/wake, a full seven-day real-time session, screen-reader behavior, and every monitor/DPI combination were not manually exercised. Sleep/restart time behavior was checked using timestamps. The app should be used in one instance at a time because instances share the same local save file.
