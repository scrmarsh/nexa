# NEXAWEEN: Interface Polish — v0.0.29 Alpha

**Nexaween prerelease · Windows x64 · Alpha**

This Nexaween update focuses on polishing the interface: clearer text, more consistent branding, better fullscreen spacing and more useful widget layouts. It remains an Alpha prerelease; v0.1.0 is reserved for the first approved Beta.

## Interface polish highlights

- **Cleaner typography and Space labels.** Larger, more readable Home titles; icons appear before names on Home and after Space names in the dashboard top bar, with theme-aware contrast.
- **A more cohesive fullscreen workspace.** Compact navigation at the top edge, a temporary overlay hint, optional Windows battery/network/volume controls, and matching 12-DIP side/bottom padding for notifications, tips and the productivity banner.
- **Refined Nexaween branding.** Lowercase version labels below the logo, a complete five-second splash, compact previews and seasonal in-app artwork. The Windows executable keeps the Classic nexa icon.
- **Independent wallpapers.** Home and individual Spaces have separate previews, fit/fill controls and restore-default actions. Space wallpaper lives in Session Settings.
- **More readable widgets.** Growing Hedy/timeline text regions, expandable interval cards based on actual saved content, a compact recording-source selector, live productivity breakdowns and playful distraction messages.
- **Consolidated Session Settings.** Manual/Scheduled timer configuration appears directly in the existing Session Settings interface.

## Supporting reliability improvements

- Manual End Session saves the original History snapshot before resetting session values, presents coordinated closing visuals and reuses the existing recap.
- History offers explicit Resume Previous Session and Load as New Session choices, preserving the source record and preventing repeated score credit or automatic recording.
- Tasks, layouts, Space preferences, all-time statistics, recordings and account connections are retained. Scheduled review/postponement behavior remains available.

## Download and run

Download **Nexa.exe** from this release. It is a self-contained portable Windows x64 build; a separate .NET installation is not required. The executable uses the existing Windows LocalAppData save location.

`SHA256SUMS.txt` records the executable's checksum. To compare it locally:

```powershell
Get-FileHash .\Nexa.exe -Algorithm SHA256
```

## Validation and prerelease status

Release application/checks build and self-contained publish are verified before publication. The complete regression gate covers **19 routes / 786 assertions**. Packaged startup uses a disposable workspace and checks native WPF initialization and embedded Bible resources.

Physical multi-monitor/mixed-DPI/focus behavior, charging/device transitions, live Hedy/Hevy/Todoist authentication and extended-session performance remain open acceptance items. Local-audio transcription and semantic interval summaries require an available connected capability; nexa does not invent summaries when source content or that capability is unavailable.

This is explicitly a **Nexaween Alpha prerelease focused on interface polish**, not a Beta or stable release.
