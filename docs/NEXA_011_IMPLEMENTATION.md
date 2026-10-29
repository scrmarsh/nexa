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

Eligibility requires an obsolete canonical version plus an approved released binary hash, or a registered Nexa identity with matching AppId marker and payload-integrity manifest. Candidate bytes are rehashed immediately before removal; changed files are refused. No candidate executable or legacy uninstaller is run. Recovery copies are hash verified. Installation metadata is backed up by its own content digest so identical application versions with different markers cannot collide. Only the identified binary and explicitly validated installation metadata/uninstaller/cache files are disposable. Adjacent saves, recordings, profiles and unknown files remain in place; application directories are never recursively removed. Shortcut targets and uninstall locations are revalidated before reference cleanup. Running/locked/access-denied files produce failures, and removal is checked before success is reported.

- Clean install: mandatory scan/removal before new payload files. No skip/opt-out. A cleanup failure blocks installation with recovery guidance. Preparation must produce a valid migration plan before any payload writes; repeated stages reuse the already extracted helper instead of overwriting a file Windows may still hold open. Post-install validation failures return a nonzero status and show recovery guidance.
- Update: preserve the active registered/target installation; eligible obsolete copies elsewhere can be cleaned.
- Repair Files: validate registry and marker, skip destination/discovery/Additional Tasks/Ready pages, preserve shortcut state, and repair through the trusted embedded payload without legacy removal of the active installation. `/MAINTENANCE=repair` exercises this same selection for native verification.
- Cleanup only: the welcome-screen **Scan PC for Old Nexa Versions** action scans, displays scrollable results, offers one **Remove eligible** action, then displays verified results and returns to Welcome. It never enters normal installation or Additional Tasks.

Blue progress fills use actual scanner/remover counts or Inno's actual payload progress, with meaningful stage text. Scanning supports cancellation in the shared service; the UI allows cancellation before removal and safely finishes an in-flight removal operation.

## Main components

SettingsDialog/Profile/Navigation/Data/Session, NexaPanels, OnboardingWindow/SetupNavigation, Developer toolbar, NexaWindowBounds.Safety, NexaChrome, Integration UI/hints/Profile services, SpiderAnatomy, LegacyInstallations, InstallerMaintenance, installer/nexa.iss, version/build scripts, regression fixtures and new `tests/Nexa011Checks.cs`.

Application, Setup and compatibility launcher identify as **0.1.1 Beta**, numeric file/assembly identity **0.1.1.0**. Beta ordering is tested against 0.1.0. The public README downloads still point to the existing 0.1.0 release.

## Verification

Completed on 2026-10-29. Final application and installer source: `75a2558e1e53ae8fdc9a14d457dacd06c8e514a2`. Build and verification receipts are in [docs/verification/beta011](verification/beta011/verification-receipt.json); [Profile preview](media/beta011/profile.png).

| Verification | Result |
| --- | --- |
| Broad regression | 28 routes, 1,454 assertions passed |
| Final focused Beta 0.1.1 suite | 153 assertions passed |
| Final packaged real-process startup/update/rollback | 17 checks passed |
| Final native installer lifecycle | 45 checks passed |
| Mandatory cleanup blocked by a locked obsolete binary | 5 checks passed: nonzero exit, no new payload/registration, old file and save bytes preserved |
| Final packaged cleanup-only | 15 checks passed, including multiple/nondefault copies, private-file preservation, repeat cleanup, locked failure and retry |
| Application, production Setup, compatibility bridge, test Setup | Built successfully; all numeric file versions 0.1.1.0 |

The broad run preceded the final cleanup alias and recovery-metadata hardening. The final 153 focused checks and all 82 native checks cover the completion code/binaries. The native installer uses a separate TestAppId and verification folders, with the same source commit and application payload digest as production Setup. Its token was not an administrator. Normal install/update, explicit repair with shortcut switches, corrupt/missing payload repair, obsolete destination replacement, shortcut opt-out, save-preserving uninstall and fixture-only doubly confirmed data removal passed.

Manual native review confirmed the first-screen scan action, readable multiline results, blue live progress and Return to Welcome without install/Additional Tasks. The native review caught an early `{app}` initialization issue and UTF-8/newline formatting; both were corrected. Lifecycle testing then caught repeated helper extraction under a transient Windows file lock; preparation now reuses the immutable extracted helper and fails closed if incomplete. Final review also covered distinct metadata recovery for two copies of the same version.

Builds emitted NU1900 because the NuGet vulnerability feed was unreachable; compilation/publishing succeeded. An online dependency-vulnerability audit is not claimed. One existing junction fixture could not be created under the restricted test token; physical junction acceptance remains open alongside the device/provider limitations below.

## Local delivery

- Installer: `verification/release011-final/NexaSetup.exe`
- Portable application: `verification/release011-final/nexa-portable.exe`
- Compatibility launcher: `verification/release011-final/Nexa.exe`
- [Package SHA256 values](verification/beta011/SHA256SUMS.txt) and [build identity](verification/beta011/build-identity.json)

No assets were uploaded, no commits were pushed, and no GitHub release was created or edited. Published Beta 0.1.0 and its local reference application bytes are preserved. All native installation and deletion activity used disposable verification fixtures; the installed production application and real user data were not targets.

Limitations to distinguish from automated coverage: physical dragging over every edge on multiple mixed-DPI monitors and live HEDY credentials require real-device/provider acceptance. Ambiguous legacy copies are deliberately retained with an explanation. Machine-wide uninstall records or protected files can require running Setup with appropriate existing Windows permissions; the per-user installer does not silently elevate or weaken permissions.
