# Nexa Beta 0.1.1 — implementation and verification

Local branch `codex/nexa-011-beta`, based on published Beta 0.1.0 commit `e37fdb1`. The workspace was clean before work. No push, asset upload, or GitHub release is authorized for this task; Beta 0.1.0 remains intact.

## Changes

- Profile: styled name card, centered inline editor, Save/Cancel, Enter/Escape, validation and current-profile persistence. Re-entering the same display name is an idempotent edit in this single-profile-per-Windows-account model. Rerun Setup is visible near the top of Profile, and Developer Mode offers the same existing onboarding implementation. Cancellation retains the profile and Spaces.
- Settings: constrain every selected page to the content host's measured width, disable horizontal scroll measurement, wrap text and action rows, and retain vertical scrolling. No global font shrink or removed settings controls.
- Windows: the old safety handler was subscribed to LocationChanged and SizeChanged and repeatedly called SetWindowPos. Primary windows now request initial fitting only for a missing primary save/backup; the fit is consumed once after layout. DPI/work-area updates maintain sizing constraints without moving ordinary windows. The redundant general-window DragMove path no longer competes with native caption movement. Owned panels retain their existing drag interaction.
- Integrations: HEVY UI, API textbox and connection implementation removed. HEDY code/authentication/endpoints unchanged. Legacy encrypted provider files remain inert for preservation; unrelated credentials are not deleted. Current About, welcome hints, Profile connections and integration documentation no longer advertise the retired provider. Historical verification evidence remains historical.
- Spiders: shared procedural roots are evenly spaced on the rear ellipse, four on each side. Existing body proportions, artwork, animation timing, collision and combat logic are retained.

## Installer architecture

`LegacyInstallations.cs` supplies one scanner/remover/report model used by welcome-screen cleanup and mandatory installation preparation. It inspects bounded known directories, HKCU/HKLM uninstall references in both registry views, and bounded Desktop/Start Menu shortcut targets, including registered/nondefault locations. It does not crawl every drive. Unknown or inaccessible copies are reported, never deleted based only on a name.

Eligibility requires an obsolete canonical version plus an approved released binary hash, or a registered Nexa identity with matching AppId marker and payload-integrity manifest. Candidate bytes are rehashed immediately before removal; changed files are refused. No candidate executable or legacy uninstaller is run. Recovery copies are hash verified. Only the identified binary and explicitly validated installation metadata/uninstaller/cache files are disposable. Adjacent saves, recordings, profiles and unknown files remain in place; application directories are never recursively removed. Shortcut targets and uninstall locations are revalidated before reference cleanup. Running/locked/access-denied files produce failures, and removal is checked before success is reported.

- Clean install: mandatory scan/removal before new payload files. No skip/opt-out. A cleanup failure blocks installation with recovery guidance.
- Update: preserve the active registered/target installation; eligible obsolete copies elsewhere can be cleaned.
- Repair Files: validate registry and marker, skip destination/discovery/Additional Tasks/Ready pages, preserve shortcut state, and repair through the trusted embedded payload without legacy removal of the active installation. `/MAINTENANCE=repair` exercises this same selection for native verification.
- Cleanup only: the welcome-screen **Scan PC for Old Nexa Versions** action scans, displays scrollable results, offers one **Remove eligible** action, then displays verified results and returns to Welcome. It never enters normal installation or Additional Tasks.

Blue progress fills use actual scanner/remover counts or Inno's actual payload progress, with meaningful stage text. Scanning supports cancellation in the shared service; the UI allows cancellation before removal and safely finishes an in-flight removal operation.

## Main components

SettingsDialog/Profile/Navigation/Data/Session, NexaPanels, OnboardingWindow/SetupNavigation, Developer toolbar, NexaWindowBounds.Safety, NexaChrome, Integration UI/hints/Profile services, SpiderAnatomy, LegacyInstallations, InstallerMaintenance, installer/nexa.iss, version/build scripts, regression fixtures and new `tests/Nexa011Checks.cs`.

Application, Setup and compatibility launcher identify as **0.1.1 Beta**, numeric file/assembly identity **0.1.1.0**. Beta ordering is tested against 0.1.0. The public README downloads still point to the existing 0.1.0 release.

## Verification

Final results are recorded below after the final local build and native fixture checks. Logs live under `verification/beta011/`; screenshots under `docs/media/beta011/`.

Limitations to distinguish from automated coverage: physical dragging over every edge on multiple mixed-DPI monitors and live HEDY credentials require real-device/provider acceptance. Ambiguous legacy copies are deliberately retained with an explanation. Machine-wide uninstall records or protected files can require running Setup with appropriate existing Windows permissions; the per-user installer does not silently elevate or weaken permissions.
