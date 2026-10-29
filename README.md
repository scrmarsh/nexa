<div align="center">

![nexa · latest — a space for whatever comes next](docs/media/nexa-banner.svg)

![version: 0.0.31 Alpha](https://img.shields.io/badge/version-0.0.31%20Alpha-84CC16?style=flat-square&labelColor=111827)
![Platform: Windows x64](https://img.shields.io/badge/platform-Windows%20x64-185FA5?style=flat-square&labelColor=111827)
![Built with .NET 10 and WPF](https://img.shields.io/badge/built%20with-.NET%2010%20%2B%20WPF-7F77DD?style=flat-square&labelColor=111827)

**a space for whatever comes next**

[Download nexa](https://github.com/scrmarsh/nexa/releases/tag/v0.0.31-alpha) · [What's new](https://github.com/scrmarsh/nexa/releases/tag/v0.0.31-alpha) · [Build from source](#build-from-source) · [Verification report](https://github.com/scrmarsh/nexa/blob/v0.0.31-alpha/docs/NEXA_031_PUBLICATION.md)

</div>

> **Latest Alpha: [nexa v0.0.31 — nexaween: Interface Polish & Release Integrity](https://github.com/scrmarsh/nexa/releases/tag/v0.0.31-alpha).** Lowercase branding, refined Space cards and icon editing, readable timer status, improved update discovery and progressive spider webs. This is an **Alpha prerelease**. See the [release notes](https://github.com/scrmarsh/nexa/releases/tag/v0.0.31-alpha) and [verification report](https://github.com/scrmarsh/nexa/blob/v0.0.31-alpha/docs/NEXA_031_PUBLICATION.md).


nexa is a local Windows productivity dashboard for classes, study, meetings and everyday work. Arrange movable widgets into a Space, follow your recurring sessions, and keep your notes, tasks and progress together.

## Dashboard examples

Real screenshots of the published Windows build, using sample notes and tasks. Select an image to view it at full size.

| Study space · dark | Meeting space · dark |
| --- | --- |
| [![Study dashboard with a Pixel Timer, notes, checklist, clock and water counter](docs/media/screenshots/study-space-dark.png)](docs/media/screenshots/study-space-dark.png) | [![Meeting dashboard with a session timer, meeting notes, action items, clock and decision counter](docs/media/screenshots/meeting-space-dark.png)](docs/media/screenshots/meeting-space-dark.png) |

## Get nexa

1. Open the [v0.0.31 Alpha prerelease](https://github.com/scrmarsh/nexa/releases/tag/v0.0.31-alpha).
2. Download **Nexa.exe** from **Assets** and place it in a folder you control.
3. Run it. The self-contained Windows x64 build includes its .NET runtime; no installer or separate .NET installation is required.

Release assets also include `SHA256SUMS.txt`. To inspect your download's checksum:

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

## What’s new

The [v0.0.31 release notes](https://github.com/scrmarsh/nexa/releases/tag/v0.0.31-alpha) describe the latest interface, updater and seasonal changes. The [publication report](https://github.com/scrmarsh/nexa/blob/v0.0.31-alpha/docs/NEXA_031_PUBLICATION.md) records **21 regression routes / 883 assertions**, **17 packaged-update checks**, the exact executable checksum and remaining Alpha limitations.

## Build from source

Use Windows and the **.NET 10 SDK** with Windows desktop support. To build the published .31 source from an existing checkout, select its release tag first:

```powershell
git fetch origin tag v0.0.31-alpha
git switch --detach v0.0.31-alpha
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

The release-tag suite checks timing, layout, scrolling, saving, history, controls, the ecosystem and animations using isolated data stores. Generated logs and captures stay in the ignored `verification/` directory. See [development notes](docs/DEVELOPMENT.md) and the [root audit](docs/ROOT-AUDIT.md).

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
