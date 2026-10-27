# nexa v0.1.24 alpha — NEXAWEEN

A space for whatever haunts you next.

Nexa is a local Windows productivity dashboard built with .NET 10 and WPF. It provides movable widgets, recurring class schedules, notes and session history, protected autosaving, productivity statistics and an optional seasonal bug ecosystem.

## Build and run

Install the .NET 10 SDK with Windows desktop support, then run:

```powershell
dotnet build .\SessionDashboard.csproj -c Release
.\bin\Release\net10.0-windows\Nexa.exe
```

`run.bat` performs the same build-and-launch workflow. To publish a self-contained Windows x64 application:

```powershell
.\Publish-Exe.ps1
.\publish\win-x64\Nexa.exe
```

Generated executables and output directories are excluded from source control. `CreateDesktopShortcut.ps1` uses an available root, published or Release executable.

## Verify

```powershell
.\tools\Verify.ps1
```

The script builds the focused WPF checks and runs all ten acceptance routes. Logs and isolated screenshots go into the ignored `verification/` directory. These checks use isolated stores, not personal dashboard data.

## Project layout

- Root `.cs` / `.xaml` files: active WPF application and services. `SessionDashboard.csproj` retains its compatible historical filename; its assembly/product is Nexa.
- `assets/`: required native artwork, embedded offline Bible indexes and their supplied source PDFs.
- `tests/`: current functional, layout, persistence, ecosystem and animation checks.
- `tools/`: current verification/publishing configuration and Bible-index development tools.
- `docs/`: requirements, implementation/verification report and animation notes.
- `LEGACY/`: obsolete source, tests, planning notes and maintenance drafts, excluded from compilation.

## Current update

The Pixel Timer fills from the top-left during class and clears from the bottom-right between sessions. Linked progress bars share the same timestamps and phase; independent bars retain their own ranges. Recurring weekdays and date/duration editing live in the Pixel Timer editor.

Session opens history, saving and tips. Main Settings has unified Appearance, separate Sounds and a pumpkin control with a separate Nexaween switch. Classic, Cozy, Creepy and Combative modes determine bug appearance. Scoring uses timestamp-based two-minute squash streaks, bounded half-step multipliers and optional negative categories.

Contextual decoration placement accepts optional rectangle targets; collision/reserved regions never become inferred attachment locations. Missing targets produce candy-only backgrounds. Spider-owned webs are handled independently by the ecosystem.

See [implementation and verification](docs/IMPLEMENTATION-0.1.24.md), [requirements](docs/NEXAWEEN-0.1.24-requirements.md) and [species animation notes](docs/ANIMATION_NOTES.md). Physical hibernation, live account/microphone integrations and extended multi-monitor/GPU acceptance remain manual checks; details are in the report.

User saves continue to use the established Windows LocalAppData location. Presets, layouts and history are preserved. Never commit recordings, credentials, profiles, verification backups or generated application bundles.
