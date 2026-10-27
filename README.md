<div align="center">

![nexa v0.1.24 alpha — NEXAWEEN. A space for whatever haunts you next.](docs/media/nexaween-banner.svg)

![Version: 0.1.24 Alpha](https://img.shields.io/badge/version-0.1.24%20Alpha-84CC16?style=flat-square&labelColor=1C102A)
![Platform: Windows x64](https://img.shields.io/badge/platform-Windows%20x64-A855F7?style=flat-square&labelColor=1C102A)
![Built with .NET 10 and WPF](https://img.shields.io/badge/built%20with-.NET%2010%20%2B%20WPF-F97316?style=flat-square&labelColor=1C102A)

**Your dashboard. Your Space. A little seasonal mischief.**

[Download Nexa](https://github.com/scrmarsh/nexa/releases/tag/v0.1.24-alpha) · [What's new](CHANGELOG.md) · [Build from source](#build-from-source) · [Verification report](docs/IMPLEMENTATION-0.1.24.md)

</div>

Nexa is a local Windows productivity dashboard for classes, study, meetings and everyday work. Arrange movable widgets into a Space, follow your recurring sessions, and keep your notes, tasks and progress together.

## Get Nexa

1. Open the [0.1.24 Alpha release](https://github.com/scrmarsh/nexa/releases/tag/v0.1.24-alpha).
2. Download **Nexa.exe** from **Assets** and place it in a folder you control.
3. Run it. The self-contained Windows x64 build includes its .NET runtime; no installer or separate .NET installation is required.

Release assets also include `SHA256SUMS.txt`. To inspect your download's checksum:

```powershell
Get-FileHash .\Nexa.exe -Algorithm SHA256
```

Compare the result with `SHA256SUMS.txt`. User data stays in the existing Windows LocalAppData `SessionDashboard` location, so replacing the executable preserves saved Spaces, settings and history. The repository and downloads currently require an authorized GitHub account.

## Inside your Space

| Make it useful | Make it yours |
| --- | --- |
| Recurring class schedules and a Pixel Timer that fills during class and counts down between sessions | Movable, resizable widgets with scrolling room below your layout |
| Notes, tasks, water tracking, counters, Scripture and recording/timeline tools | Configurable appearance, sounds and optional integrations |
| Session history, automatic checkpoints and save-before-reset protection | Productivity Meter, score breakdowns and squash streaks |
| Time zone settings and precise linked or independent progress bars | Classic, Cozy, Creepy and Combative bugs with species-specific movement |

## 🎃 NEXAWEEN

The seasonal update brings **candy-only backgrounds**, pumpkins and vines anchored to real widget corners, six costumed bug species, and spider-owned webs. The app's seasonal changelog palette carries onto this page: deep purple `#1C102A`, neon green `#84CC16`, pumpkin orange `#F97316`, pale green `#D9F99D`, and witch purple `#A855F7`.

Seasonal settings support automatic dates or a manual switch. Your normal appearance settings are retained. Decorations avoid widget controls, and advanced effects respect reduced-motion preferences.

Read the [current changelog](CHANGELOG.md) for the release features and the [implementation report](docs/IMPLEMENTATION-0.1.24.md) for evidence and limitations. Physical hibernation, real account/microphone integrations, and extended mixed-DPI/GPU testing remain manual acceptance work.

## Build from source

Use Windows and the **.NET 10 SDK** with Windows desktop support:

```powershell
dotnet build .\SessionDashboard.csproj -c Release
.\bin\Release\net10.0-windows\Nexa.exe
```

`run.bat` provides the same build-and-launch workflow. To create a self-contained Windows x64 executable:

```powershell
.\Publish-Exe.ps1
.\publish\win-x64\Nexa.exe
```

The historical project filename and save-data namespace remain for compatibility; the product and executable are **Nexa**.

## Verify and contribute

```powershell
.\tools\Verify.ps1
```

The ten-route suite checks timing, layout, scrolling, saving, history, controls, the ecosystem and animations using isolated data stores. Generated logs and captures stay in the ignored `verification/` directory. See [development notes](docs/DEVELOPMENT.md) and the [root audit](docs/ROOT-AUDIT.md).

| Location | Purpose |
| --- | --- |
| Root `.cs` / `.xaml` files | Active WPF application and shared services |
| `assets/` | Runtime artwork, embedded offline indexes and retained source assets |
| `tests/` | Current functional and regression checks |
| `tools/`, `Properties/` | Verification, publishing configuration and asset-development helpers |
| `docs/` | Development, integrations, requirements, audit and implementation reports |
| [`LEGACY/`](LEGACY/README.md) | Retired source, pre-naming-scheme notes, old tests and historical development logs; excluded from compilation |

Build outputs, executable bundles, personal profiles, recordings, credentials and recovery backups are excluded from Git. Downloadable executables belong in **Releases**.
