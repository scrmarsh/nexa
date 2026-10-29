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

`App.xaml.cs`, `App.xaml`, `AutomaticUpdates`, `UserProfile/GuidedSetup`, `NexaChrome`, `Theme`, `SpaceIcons`, `DashboardCreation`, `OnboardingWindow`, `SettingsDialog.Profile/Autosave/Updates/Widgets`, `ClockPicker`, `MainWindow.ClassSchedule`, `MainWindow.xaml.cs`, `Models`, `PixelShapes`, `NexaChoices`, `NexaProcesses`, `InstallerRepair`, `InstallerMaintenance`, `ExperimentalUpdates`, `LegacyInstallations`, `installer/nexa.iss`, version/build scripts and regression checks.

Application, assembly, installer and compatibility launcher target **0.1.2 Beta**, numeric version **0.1.2.0**. Beta 0.1.0/0.1.1 history remains in release notes. Bug Battle expansions and Nexy are deferred; their existing code is preserved.

## Verification

Final source: `d821f2b34a93e33f8a2cf0f6a4fd6eb710a0f5d9`. Logs and identities are retained in [docs/verification/beta012](verification/beta012/verification-receipt.json).

| Check | Result |
| --- | --- |
| Final broad regression | 29 routes, 1,500 assertions passed |
| Beta 0.1.2 focused coverage within that run | 40 assertions passed |
| Beta 0.1.1 regression within that run | 153 assertions passed |
| Final packaged real-process startup/update/rollback | 17 checks passed |
| Official installer cache, tamper rejection and missing-cache path | 3 checks passed |
| Final native installer lifecycle | 45 checks passed |
| Locked obsolete binary blocks new installation | 5 checks passed |
| Final packaged cleanup-only, failure and retry | 15 checks passed |
| Final packaged first launch, verified process detection and graceful close | 11 checks passed |
| App, production Setup, portable and compatibility launcher | Built; numeric version 0.1.2.0 verified |

The final broad run was serialized away from native TestAppId installation. Earlier exploratory runs caught a stale numeric-version assertion and a fixture-registration collision; the corrected clean final run passed all 29 routes. Actual locked-file tests verified retained pending edits, unchanged committed bytes, Cancel, retry after unlock, and explicit Discard. Schedule tests cover 180-minute daytime and 900-minute overnight recurrence. Exact-version repair discovery is tested against an official-shaped feed containing a newer release.

The final native installer uses the same source commit and application SHA256 as production Setup, with a separate TestAppId and verification root. Fresh/repeated installation, explicit repair without shortcut prompts, correct/corrupt/missing payload repair, mandatory obsolete-destination cleanup, shortcut opt-out, save-preserving uninstall and fixture-only doubly confirmed data removal passed. A locked obsolete binary returned a failure before new payload/registration writes; cleanup-only retry succeeded after unlocking. The final packaged first launch again showed only Welcome to Nexa; verified normal closure exited safely. The token was not an administrator. All installation/removal activity used separate verification folders and the TestAppId, never the installed production app or real user saves.

## Native visual acceptance

The isolated packaged candidate opened only Welcome to Nexa. Native review walked through the full widget registry, Light Mode preview, final Nexaween prompt, No Thanks, compact icon-grid keyboard selection, completion, and the resulting saved Space. The saved fixture contained the selected Briefcase icon, Pixel timer/Notes/Clock widgets, completed onboarding and Light appearance. Process detection identified only the requested verified path. Closure refused while a blocking modal was open, then normal window closure succeeded after that modal was dismissed; there was no forced termination.

The isolated welcome-screen scan ran while only Setup was open. Its progress page showed one bottom-anchored track beneath readable status text, with no duplicate bar. Review showed the verified obsolete fixture and returned to Welcome without entering installation or Additional Tasks. The existing update-preference dialog exposed unbounded horizontal measurement during this review; that dialog now wraps its text and shows its save action. Theme palette choices and the sound enable toggle are retained; only the onboarding sound-preview UI was removed.

Native visual acceptance above used the earlier isolated candidate. The final compiled source is separately covered by the 1,500 assertions and final packaged/native results, including rendered [appearance](media/beta012/onboarding-appearance-light.png), [Nexaween ready page](media/beta012/onboarding-ready-nexaween.png), [Create Space action area](media/beta012/create-space-actions-light.png), [Profile](media/beta012/profile-light.png), and [update preferences](media/beta012/update-preference-light.png).

## Local delivery and limits

- Setup: `verification/release012-final/NexaSetup.exe`
- Portable: `verification/release012-final/nexa-portable.exe`
- Compatibility launcher: `verification/release012-final/Nexa.exe`
- [SHA256 values](verification/beta012/SHA256SUMS.txt)

Full physical coverage across multiple mixed-DPI monitors, every Windows scaling setting, and every modal/unsaved-work combination remains manual acceptance. One existing junction fixture could not be created under the restricted token; its unverified result is retained in the junction log. Live external-provider credentials were not used; HEDY behavior and Bug Battle implementation were not changed.

Beta 0.1.2 is not published, so downloading a matching 0.1.2 installer from the live public feed cannot yet complete. The missing-release case reports failure; trusted feed/URL/redirect/size/hash checks and matching-version selection are covered with fixtures. Builds emitted NU1900 when the NuGet vulnerability feed was unreachable; compilation/publishing succeeded, and an online dependency-vulnerability audit is not claimed. Packages are unsigned, as in the existing local workflow.

No commits were pushed, no assets were uploaded, and no GitHub release was created or modified. Beta 0.1.0 and Beta 0.1.1 artifacts remain preserved. Bug Battle expansions and Nexy were not implemented in this release.
