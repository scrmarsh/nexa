<div align="center">

![nexa Â· latest â€” a space for whatever comes next](docs/media/nexa-banner.svg)

![version: 0.0.30 Alpha](https://img.shields.io/badge/version-0.0.30%20Alpha-84CC16?style=flat-square&labelColor=1C102A)
![Platform: Windows x64](https://img.shields.io/badge/platform-Windows%20x64-185FA5?style=flat-square&labelColor=111827)
![Built with .NET 10 and WPF](https://img.shields.io/badge/built%20with-.NET%2010%20%2B%20WPF-7F77DD?style=flat-square&labelColor=111827)

**a space for whatever comes next**

[Nexaween prerelease](https://github.com/scrmarsh/nexa/releases/tag/v0.0.29-alpha) Â· [What's new](CHANGELOG.md) Â· [Build from source](#build-from-source) Â· [Development status](docs/NEXA_030_STATUS.md)

</div>

> **nexa v0.0.30 alpha — NEXAWEEN: Final Refinement (local development).** Shared reference-inspired pumpkin artwork, refined bug input and persistence, fullscreen navigation across Home and Spaces, vector window controls and standalone compact splash previews. See the [audit](docs/NEXA_030_AUDIT.md), [status](docs/NEXA_030_STATUS.md) and [test results](docs/NEXA_030_TEST_RESULTS.md). The downloadable GitHub prerelease remains v0.0.29; v0.0.30 has not been published.

Alpha development now uses **v0.0.X Alpha**, Beta will use **v0.1.X Beta**, and
the first stable release is planned as **v1.0.0**. This Nexaween build is an
explicitly marked Alpha prerelease; **v0.1.0 Beta** remains reserved for the first
approved Beta. Historical 0.1.X Alpha identities and **0.1.26 â€” Spaces in
Motion** remain in the release history. Updater comparison recognizes legacy
Alpha revisions without changing their tags or download URLs.

Already-distributed older Alpha executables retain their original comparison
logic. Installing a newer approved build manually may be necessary to bootstrap
the corrected updater. The code cannot retrofit an old executable.


nexa is a local Windows productivity dashboard for classes, study, meetings and everyday work. Arrange movable widgets into a Space, follow your recurring sessions, and keep your notes, tasks and progress together.

## Dashboard examples

Real screenshots of the published Windows build, using sample notes and tasks. Select an image to view it at full size.

| Study space Â· dark | Meeting space Â· dark |
| --- | --- |
| [![Study dashboard with a Pixel Timer, notes, checklist, clock and water counter](docs/media/screenshots/study-space-dark.png)](docs/media/screenshots/study-space-dark.png) | [![Meeting dashboard with a session timer, meeting notes, action items, clock and decision counter](docs/media/screenshots/meeting-space-dark.png)](docs/media/screenshots/meeting-space-dark.png) |

## Development builds and historical downloads

Download **Nexa.exe** from the [Nexaween Alpha prerelease](https://github.com/scrmarsh/nexa/releases/tag/v0.0.29-alpha), or build from source below. The release is explicitly marked as a prerelease; physical and live-provider acceptance remains open.
Self-contained Windows x64 packages include their .NET runtime; framework-dependent
builds require the .NET 10 Desktop Runtime.

For a downloaded package with a published `SHA256SUMS.txt`, inspect its checksum:

```powershell
Get-FileHash .\Nexa.exe -Algorithm SHA256
```

Compare the result with `SHA256SUMS.txt`. User data stays in the existing Windows LocalAppData `SessionDashboard` location, so replacing the executable preserves saved Spaces, settings and history. The repository and release downloads are public; no GitHub sign-in is needed.

## Inside your Space

| Make it useful | Make it yours |
| --- | --- |
| Recurring class schedules and a Pixel Timer that fills during class and counts down between sessions | Movable, resizable widgets with scrolling room below your layout |
| Notes, tasks, water tracking, counters, Scripture and recording/timeline tools | Configurable appearance, sounds and optional integrations |
| Session history, automatic checkpoints and save-before-reset protection | Combined Productivity view, score breakdowns and squash streaks |
| Time zone settings and precise linked or independent progress bars | Optional decorative bugs with independent behavior and appearance |

## Whatâ€™s new

Release-specific features, seasonal artwork and update notes live in the [changelog](CHANGELOG.md). The [verification report](docs/IMPLEMENTATION-0.1.24.md) records testing and known limitations.

## Build from source

Use Windows and the **.NET 10 SDK** with Windows desktop support:

```powershell
dotnet build .\SessionDashboard.csproj -c Release
.\bin\Release\net10.0-windows\Nexa.exe
```

`run.bat` provides the same build-and-launch workflow. To create a self-contained Windows x64 executable:

```powershell
.\Publish-Exe.ps1
.\Builds\Testing\Nexa.exe
```

The historical project filename and save-data namespace remain for compatibility; the product and executable are **nexa**.

## Verify and contribute

```powershell
.\tools\Verify.ps1
```

The nineteen-route suite checks version compatibility, settings, splash timing,
layout, scrolling, saving, history, controls, the ecosystem and animations using
isolated data stores. To run the .28 failure/recovery and behavior route after building the checks project:

```powershell
dotnet build .\tests\SessionDashboard.Checks.csproj -c Release
.\tests\bin\Nexa024\Release\net10.0-windows\SessionDashboard.Checks.exe --nexaween028
```

Generated logs and captures stay in the ignored `verification/` directory. See
[development notes](docs/DEVELOPMENT.md) and the [root audit](docs/ROOT-AUDIT.md).

| Location | Purpose |
| --- | --- |
| Root `.cs` / `.xaml` files | Active WPF application and shared services |
| `assets/` | Runtime artwork, embedded offline indexes and retained source assets |
| `Builds/Testing`, `Builds/Latest`, `Builds/Archive` | Unapproved candidate, approved build, and older approved builds respectively; ignored by Git |
| `tests/` | Current functional and regression checks |
| `tools/`, `Properties/` | Verification, publishing configuration and asset-development helpers |
| `docs/` | Development, integrations, requirements, audit and implementation reports |
| [`LEGACY/`](LEGACY/README.md) | Retired source, pre-naming-scheme notes, old tests and historical development logs; excluded from compilation |

Build outputs, executable bundles, personal profiles, recordings, credentials and recovery backups are excluded from Git. Approved downloadable executables belong in **Releases**. Development candidates stay in `Builds/Testing` until approved; publishing a candidate never replaces `Builds/Latest` or the root executable.
