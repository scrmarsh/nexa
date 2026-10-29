# nexa .33 verification

**Release audit passed: 25 routes / 1,136 assertions, plus 31 native installer lifecycle checks, 30 native installer upgrade/migration checks and 17 native portable-package checks.** Application/checks and final native-harness builds completed with zero warnings and errors. Native lifecycle ran on Windows 11 with a non-administrator token. Installer and portable payloads are byte-identical applications; both retain updates. The production Setup is separate from the test product-ID fixtures.

Application source: `2f5c5e9bbef49dcfdba509fc10661e5050fc02be`; final installer recipe: `a46a7cd0fa5f0ae528d71f8aff7d263fa71e74ff`; native harness: `2ef5010e98189863f6f591c71bf05991bec27500`. Packaging-only and harness changes after the application checkpoint do not change its C# implementation. The binary embeds the application checkpoint; the source tag contains all packaging fixes and evidence.

[Release gate](verification/nexa033/release-gate.json) · [route ledger](verification/nexa033/route-ledger.json) · [build](verification/nexa033/build.log) · [publish](verification/nexa033/publish.log) · [installer compilation](verification/nexa033/installer-build.log) · [migration launcher build](verification/nexa033/bridge-build.log) · [asset hashes/identity](verification/nexa033/release-identity.json) · [modified files](verification/nexa033/source-inventory.txt).

Native evidence: [31 lifecycle checks](verification/nexa033/native-lifecycle.log), [standard-user/Windows result](verification/nexa033/native-lifecycle-result.json), [30 upgrade/migration checks](verification/nexa033/native-upgrade-migration.log), [17 portable helper checks](verification/nexa033/packaged.log), [installed upgrade health](verification/nexa033/installed-upgrade-startup.json), [portable conversion health](verification/nexa033/portable-migration-startup.json), [packaged startup](verification/nexa033/startup.json). Paths and Windows account SID are redacted in published logs; raw local evidence remains under verification.

The native matrix includes fresh/repeated installation, desktop opt-out, Start menu and Installed Apps registration, repair, default keep-data uninstall, doubly confirmed private-data removal including long backup paths, successful registered upgrades, exact failed-health executable/registration rollback, real published .29/.31 helper handoffs, current portable conversion, failed first-conversion rollback, exact data preservation and fixture cleanup. The prior installer is a controlled synthetic .32-metadata fixture, not a previously shipped installer.

The final inherited portable `--packaged031` route retains some historical .31 assertion wording, but runs the exact .33 portable asset; its health JSON records .33. It checks real .29 replacement/rollback, workspace preservation, interruption/idempotence/tamper rejection, separate helper install/restart and genuine separate-process startup.

Commands (from the repository root):

```powershell
& tools/Verify.ps1 -SkipBuild -OutputBase bin/Nexa033ReleaseChecks/
& tools/Test-Installer.ps1 -Setup verification/installer033-upgrade/NexaSetup.exe -FixtureRoot "$PWD/verification/installer033-upgrade-fixture"
& tests/bin/Nexa033ReleaseChecks/Release/net10.0-windows/SessionDashboard.Checks.exe --installer033 verification/installer033-baseline/NexaSetup.exe verification/installer033-upgrade/NexaSetup.exe verification/installer033-upgrade-fixture verification/bridge033-fixture/Nexa.exe
& tests/bin/Nexa033ReleaseChecks/Release/net10.0-windows/SessionDashboard.Checks.exe --packaged031 verification/release033-final/nexa-portable.exe
```

Actual WPF captures: [Appearance top](media/nexa033/appearance-top.png), [bottom](media/nexa033/appearance-bottom.png), [seasonal override](media/nexa033/seasonal-override.png), [theme colors](media/nexa033/theme-colors.png), [presets](media/nexa033/background-presets.png), [image preview](media/nexa033/custom-wallpaper.png), [Classic Home](media/nexa033/classic-home-preset.png), [seasonal Home](media/nexa033/nexaween-home-preset.png), [Space background](media/nexa033/session-background.png), [nested-scroll fixture](media/nexa033/nested-scrolling.png). These are native renders of synthetic data. Approved artwork is unchanged.

The audit extends the established suite with .33 palette/seasonal, asset-ordering, migration, cleanup and readable-history assertions. The existing .32 layout/input route remains. Current-version/title expectations were advanced to .33; historical identity assertions remain. Initial stale .32 title/file-version expectations failed in three older routes and were corrected without deleting assertions. Initial optional data-removal testing found an installer lifecycle bug; the rule was moved from installation-time evaluation to the actual uninstall step.

Native installer tests run with a separate test product ID and verification-only app/data/shortcut directories. The earlier lifecycle run passed 31 checks under a non-administrator Windows 11 token. Final source and package verification, including real Setup rollback and genuine published helper migration, is recorded with the release evidence. Historical .24/.26/.29/.31 executable downloads matched their official digests and passed isolated native startup. Their embedded metadata is preserved accurately.

Hardware limits: physical trackpad behavior, full actual 100/125/150% Appearance DPI matrix, mixed monitors and subjective appearance on other laptops are unverified. Synthetic layout/input checks are labeled as such. A successful Alpha verification does not establish universal device compatibility. No live data or approved install was used in destructive tests.

The native upgrade audit also caught and repaired a WPF dispatcher deadlock in synchronous startup-health completion and padded Inno Setup version-resource text being rejected as a different product. Health completion is now awaited; identity fields trim padding without relaxing digest, product, architecture or numeric-version checks. Final verification uses the repaired source.

The extended native fixture exposed long update-backup paths being mistaken for links by an unprefixed Win32 attribute check. The uninstaller now uses extended-length paths for that check while still refusing actual reparse points. The same fixture is rerun with its accumulated backup tree rather than discarding the condition.
