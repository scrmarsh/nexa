# nexa .34 verification

Local candidate only. The final audited application source is `66f6bbceb9ce2218ff3bc36bb47aeb2fadd2fd9d`. No .34 release, remote push or Beta promotion was performed. See the [31-section implementation status](NEXA_034_STATUS.md) and [data/installer policy](NEXA_034_DATA_AND_INSTALLER.md).

## Results

**Final corrected build passed: 26 routes / 1,202 assertions, 75 native data/UI assertions, 40 native installer lifecycle checks, 32 native upgrade/migration checks and 17 packaged-executable checks.** The 75-check native route repeats the targeted route with actual Windows DPAPI and junction support; these counts are test invocations, not a claim of distinct requirements. All final evidence points to source `66f6bbceb9ce2218ff3bc36bb47aeb2fadd2fd9d`.

The clean Release application build passed with zero warnings/errors. Self-contained publishing emits NU1900 because the NuGet vulnerability service is unreachable in this environment. No vulnerability clearance is claimed. Packages are unsigned Alpha candidates. Inno Setup is pinned to 7.1.1.

## Coverage and fixture boundaries

The normal suite covers all 26 existing/targeted routes: .34 data/UI, .33/.32, laptop/audio, update automation, .31/.30/.29, .28/.27 seasonal, handoff, fullscreen, menus, core, continuity, animation and ecosystem checks. The verification script records source and SHA-256 fingerprints for runner EXE/DLL and nexa DLL and verifies that those binaries remain unchanged throughout the run. Each route is bounded by a deadline and isolated processes can be terminated without touching the user's app.

The separate native data route exercises real Windows DPAPI and NTFS junctions. Migration tests load the migrated actual workspace, preserve original bytes, relink recording paths, preserve credentials/connection identity, handle conflicts and repeat safely. Export tests exclude credentials, validate manifest/file hashes, cancellation, failure cleanup, no overwrite and link rejection. Deletion tests cover ownership, exact confirmation, deadline/Now/Cancel, frozen late writes and durable prevention of legacy reimport. The raw backup fixture contains test-only values and stays under verification; only its sanitized validation receipt is committed.

Native installers use a separate test AppId, a marker-validated fixture root, and sibling app/data folders. The lifecycle covers fresh/repeated installation, real startup, Windows registration, correct shortcut targets, independent opt-out, digest cache, keeping intact payload timestamps, replacing missing/corrupt payloads, unregistered portable refusal, keep-data uninstall and double-confirmed fixture-data removal. A standard non-administrator token runs these checks; it does not install or uninstall the developer's .33 app.

Registered upgrade/rollback uses the real approved .33 application payload SHA-256 `5B2FB16B603A4CBA7D0FC4B1C6CF7A8EFC4631DDA87648A5F46F4AAD27A1B6D4` in a test-only wrapper built with the .34 installer recipe and a separate .34 preparation helper. This validates exact .33 executable rollback, .33 registration restoration and save preservation without acquiring the production app lock. The wrapper is a controlled fixture, not the historical shipped .33 installer. Production builds reject the separate-helper option.

The route also exercises successful .34 registration/health, verified repair-cache resolution and corrupt-cache refusal, idempotence, unmodified published .29/.31 helper handoffs through the migration bridge, portable preservation, failed first-conversion rollback and successful current-portable conversion. Final fixture uninstall removes the test registration and retains saves.

The inherited `--packaged031` route uses the final .34 portable asset despite historical .31 assertion text. Its health JSON establishes the actual .34 identity. It tests genuine .29 replacement/rollback, exact workspace/score preservation, interrupted/idempotent/tampered transactions, separate helper application and separate-process startup. The approved older binary remains unchanged.

## Defects found and corrected

The audit corrected helper-count serialization truncation, cached ecosystem reuse when disabled, stale Space preset controls, duplicate completed-session statistics, a menu dismissal/owner-drag hang, unsafe reset-marker ordering and credential sanitization/error handling. Tests cover these behaviors.

Native testing exposed an immediate-retry race after rolling back a first installer migration: Inno's self-cleanup process briefly retained the installer mutex after the launcher exited. Retrying Setup then returned exit code 1. The updater now waits for that actual named mutex to disappear, closes its probe handle on every iteration and has a one-minute actionable timeout. Isolated tests verify timeout, waiting and resumption; the full native migration route passed all 32 checks with the correction. The failed test preserved exact saves and the portable executable.

One verification build was invalidated by overlapping a clean with compilation of the runner. It was rejected on a missing-reference error, never counted as a pass, and rebuilt sequentially. Final executable fingerprints establish which runner was actually tested.

## Package identity and evidence

[Automated/physical gate](verification/nexa034/release-gate.json) · [route ledger](verification/nexa034/route-ledger.json) · [runner fingerprints](verification/nexa034/binary-identity.json) · [clean Release build](verification/nexa034/clean-build.log) · [checks build](verification/nexa034/checks-build.log) · [publish log](verification/nexa034/publish.log) · [source inventory](verification/nexa034/source-inventory.txt).

Native evidence: [75 data/UI checks](verification/nexa034/native-data.log), [40 installer lifecycle checks](verification/nexa034/native-lifecycle.log), [non-administrator Windows receipt](verification/nexa034/native-lifecycle-result.json), [32 upgrade/migration checks](verification/nexa034/native-upgrade-migration.log), [17 packaged checks](verification/nexa034/packaged.log), [installed upgrade startup](verification/nexa034/installed-upgrade-startup.json), [portable conversion startup](verification/nexa034/portable-migration-startup.json), [packaged startup](verification/nexa034/packaged-startup.json), [backup inspection](verification/nexa034/backup-inspection.json), [live-installation preservation/test cleanup](verification/nexa034/native-scope.json).

Exact identities: [production recipe/payload](verification/nexa034/build-identity.json), [all release assets](verification/nexa034/release-identity.json), [SHA256SUMS](verification/nexa034/SHA256SUMS.txt), [real .33 payload wrapper](verification/nexa034/baseline-fixture-identity.json), [upgrade fixture](verification/nexa034/candidate-fixture-identity.json), [lifecycle fixture](verification/nexa034/lifecycle-fixture-identity.json). User paths/account SID are redacted in committed logs; raw local evidence stays under verification.

| Local asset | Bytes | SHA-256 |
|---|---:|---|
| NexaSetup.exe — official installer | 67,506,973 | `F6AAB11CCBFFA3CC74467D47A646BE35971AF08F864F35BE6E2FFC541254796C` |
| nexa-portable.exe — standalone application | 72,781,913 | `000641ADDE33ED75F3D55EA32703393036A6DA339BFF2AF10E8B7D0142CC08BA` |
| Nexa.exe — legacy migration bridge | 105,075,336 | `12127689A9A1F083B39498100BA0477D55F4E08452F9AF9206F18ABB4C5F8447` |

Assets are under `verification/release034-final/`. The installed application payload and portable file have the same SHA-256 and file version `0.0.34.0`; application informational version is `0.0.34-alpha+66f6bbceb9ce2218ff3bc36bb47aeb2fadd2fd9d`. The bridge has its own role/metadata, recorded exactly in release-identity.json. The existing installed .33 remains version `0.0.33.0`, hash `5B2FB16B603A4CBA7D0FC4B1C6CF7A8EFC4631DDA87648A5F46F4AAD27A1B6D4`. The fixture registration was removed after testing.

Reproduction commands, from the actual repository root (native routes require the normal Windows token, not an administrator account):

```powershell
& tools/Verify.ps1 -SkipBuild -OutputBase bin/Nexa034Checks/
& tests/bin/Nexa034Checks/Release/net10.0-windows/SessionDashboard.Checks.exe --nexa034
& tools/Test-Installer034.ps1 -Setup verification/native034-setup/NexaSetup.exe -FixtureRoot "$PWD/verification/native034"
& tests/bin/Nexa034Checks/Release/net10.0-windows/SessionDashboard.Checks.exe --installer034 verification/installer034-baseline/NexaSetup.exe verification/installer034-upgrade/NexaSetup.exe verification/installer034-upgrade-fixture verification/bridge034-fixture/Nexa.exe
& tests/bin/Nexa034Checks/Release/net10.0-windows/SessionDashboard.Checks.exe --packaged031 verification/release034-final/nexa-portable.exe
```

The production installer is the intended download. `nexa-portable.exe` is the identical application payload. `Nexa.exe` at the release root is the separate legacy migration bridge, not a third app edition. Do not distribute fixture installers or intermediate packages. No signing or publication is implied by creation of local files.

## Physical acceptance still required

- Repeated create/open/switch/Home transitions in Classic and nexaween at normal/maximized/F11 sizes, recorded on the actual screen with no black/white/desktop or partial-content flash. Automated rendering does not establish that flicker is eliminated. Beta remains blocked.
- Mixed-DPI/multiple-monitor moves and maximize/restore/fullscreen on representative laptops, including taskbar/work-area constraints.
- Precision-trackpad, wheel and touch scrolling on all long settings pages; click/drag sliders and small-window layouts.
- Interactive installer theme/layout, second-page Repair Files, discovery progress, independent tasks, install progress, completion and uninstall choices; full Rerun Installer save/exit/relaunch click-through.
- Subjective sound balance, real-provider consent/reconnection and a non-destructive recording-device session. Automated checks use fake providers/isolated fixtures; they do not approve live external accounts.

Screenshots under [media/nexa034](media/nexa034) are WPF RenderTargetBitmap captures. The computer-use capture surface did not expose fixture pixels, so no hardware/compositor acceptance is claimed. External wallpaper bytes are not bundled in ordinary backups, conflicts are retained rather than automatically merged, and Import All Data is intentionally not implemented. These policies are detailed in the data guide.
