# Nexa Beta 0.1.2 — implementation and verification

Local development on `codex/nexa-012-beta`, based on Beta 0.1.1 `bdb9084` (application source `75a2558`). The initial working tree was clean. No push, upload or publication is authorized. Public Beta 0.1.0 and local Beta 0.1.1 artifacts remain intact.

## Application changes

- Removed the hardcoded reddish DWM frame color; native and WPF window corners now follow one rounded frame, with square edges for maximized/fullscreen windows. Shared outer frames and caption dividers use a neutral frame resource in Classic and Nexaween; internal validation and seasonal control borders remain intact. Home Show More / Hide More controls and logo geometry were not changed.
- Darkened the shared Space icon brush in Light mode. Profile keeps the large name card and inline editing but removes the smaller name-button border.
- Primary buttons use accent-aware text contrast and centered content. Create Space disables unbounded horizontal scroll measurement so template and widget rows wrap within the dialog.
- Replaced the Space icon ComboBox with a compact five-column, rounded, anchored popup. Icon IDs and accessibility names are retained; keyboard arrows, Enter/Space, Escape and outside-click dismissal work through the existing button/popup system.
- First-time startup now opens onboarding before creating any dashboard or splash window. Returning users retain normal splash/Home startup. Rerun Setup reuses the same flow and restores live theme resources on cancellation.
- Onboarding removes the unused use-case page. Widget choices come from the real registry, with the seven requested featured tools first and no duplicates. Light/Dark previews immediately. The sound-preview UI and Nexaween card are removed from appearance setup. The final page offers Nexaween confirmation, applies dark seasonal resources immediately on acceptance, and reuses existing decoration artwork.
- ClockPicker previously emitted partially updated hour/minute/AM-PM values during a programmatic change. It now changes all selectors together and emits one final event, with identical assignments ignored. This prevents event-driven schedule drafts from being captured mid-update.
- Session drafts remain separate for Manual and Schedule modes. Mode restoration suppresses intermediate draft writes. The preview includes AM/PM, duration, time zone and midnight crossing. Applying a schedule uses the existing detached workspace commit and immediately refreshes the active timer. Guided Spaces initialize their draft explicitly so their selected name is not replaced by the first preset during the initial dashboard render.
- Settings failures retain pending changes and expose the storage error. Closing after failure offers Retry, Discard and Cancel. Explicit Discard restores persisted settings/drafts rather than silently saving the failed edits on the next dashboard save.
- Added cached, pixelated Triangle, Octagon and Hearts cell geometry to the existing Pixel Timer progress renderer, preferences and immediate preview. Existing Square and Circle remain supported.

## Installer changes

- Maintenance no longer calls Inno's native SetProgress while rendering a second custom bar. Only one blue maintenance bar is shown, anchored beneath status text. The native payload gauge is kept hidden while its custom blue track follows the actual installation progress and layout.
- Scanning stays read-only and does not require closing Nexa. Closure is requested only for verified application processes relevant to removal or workspace backup. Setup and its headless helpers are excluded; executable names alone do not establish identity.
- The running-app dialog names verified paths and offers Close Nexa or Cancel. Closure sends a normal window-close request, verifies PID/start time/path identity and waits for exit. It never kills a process. Locked files and workspace contention remain separate failures with recovery guidance.
- Retained approved hashes, rehash-before-delete, independent metadata recovery copies, nonrecursive removal, private-file preservation, shortcut/registry checks, mandatory clean-install cleanup, and separate repair/update/cleanup-only workflows. Verification/build/source-control directories are excluded from bounded discovery.
- Rerun Installer verifies the local cache first. A missing/unusable installer prompts before downloading from the existing trusted release source. Exact-version discovery does not accidentally choose a newer unrelated repair installer. Downloads retain SHA256, source/redirect, size and binary checks, show real progress, and ask before launch.

## Components

`App.xaml.cs`, `App.xaml`, `NexaChrome`, `Theme`, `SpaceIcons`, `DashboardCreation`, `OnboardingWindow`, `SettingsDialog.Profile/Autosave/Updates/Widgets`, `ClockPicker`, `MainWindow.ClassSchedule`, `MainWindow.xaml.cs`, `Models`, `PixelShapes`, `NexaChoices`, `NexaProcesses`, `InstallerRepair`, `InstallerMaintenance`, `ExperimentalUpdates`, `LegacyInstallations`, `installer/nexa.iss`, version/build scripts and regression checks.

Application, assembly, installer and compatibility launcher target **0.1.2 Beta**, numeric version **0.1.2.0**. Beta 0.1.0/0.1.1 history remains in release notes. Bug Battle expansions and Nexy are deferred; their existing code is preserved.

## Verification

Results and delivery hashes will be added after the final source/package checks. Test activity uses separate verification folders and the existing TestAppId, never the installed production app or real user saves.
