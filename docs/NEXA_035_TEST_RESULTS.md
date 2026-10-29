# nexa .35 verification

Verified application source: `6d4b6afef0958c55639126b35de616165c94bbcd`. Branch: `codex/nexa-035-ui-session-refinement`. Label: `nexa v0.0.35 alpha`; Windows version `0.0.35.0`. Local candidates are unpublished. The large detailed Home pumpkin and candlelight are preserved; only small decorations use the flat pumpkin.

## Results

| Run | Result |
|---|---|
| Full Release regression suite | **27 routes / 1,308 assertions passed**, including 108 focused .35 assertions. Runner binary fingerprints remained unchanged throughout. |
| Native Windows data/UI route | **75 checks passed on the preceding .35 source** (`74f30f7`), including DPAPI and NTFS-junction rejection. Final-source in-process route passed 73; the optional native repeat was unavailable. See the limit below. |
| Native installer lifecycle | **40 checks passed** with a non-administrator Windows token. |
| Native registered upgrade, rollback and migration | **32 checks passed** with isolated .34/.35 installers and unmodified older updater helpers. |
| Packaged portable executable | **17 checks passed**, including exact rollback, save/score preservation, interruption/idempotence, tamper rejection, separate helper and separate-process startup. |
| Release check build | Passed, **0 warnings / 0 errors**. |
| Self-contained Release publish and installer/bridge compilation | Passed. Publishing emitted NU1900 because the NuGet vulnerability feed was unreachable; no vulnerability clearance is claimed. |

Counts are assertion invocations across overlapping routes, not a count of distinct product requirements. The initial sandbox prevented native registry access and packaged startup; those attempts were rejected as acceptance evidence. Native portable, installer lifecycle and upgrade runs on the final binaries passed. Earlier regression failures exposed obsolete version/visible-scrollbar assertions and leg roots outside the actual body ellipse; the final suite passed after correction.

## Coverage

Focused checks cover default/custom Home backgrounds, exact tagline and white halo, name-dialog focus/validation/save/cancel, one Hedy and one genuine Hevy entry, unrelated-game exclusion and exact ecosystem bytes, primary navigation, bottom Add Widget, rendered hidden bars, 3/6/10/14 Timeline intervals and persisted dimensions, bug-overlay scrolling, Home cancellation/repeated clicks, finalization/save failure/retry, recap return paths and read-only compact summaries, inline volume UI, repeated Classic/seasonal/windowed/F11 navigation, leg geometry and shared appearance. Header alignment is checked with three icon shapes at 16/20/24-DIP text. Separate assertions protect the large detailed Home pumpkin and flat small pumpkin scopes.

The inherited routes cover autosave/recovery, old saves, history operations, scores, scheduling, widgets, game/audio, accounts, theme independence, high-resolution wheel deltas, nested boundary handoff, Page Down, hidden horizontal commands, ecosystem generation/web construction, menus and update consent/automation. Actual WPF captures are under `docs/media/nexa035`; older release screenshots are preserved.

Installer lifecycle uses a separate fixture AppId and app/data/shortcut folders under `verification`. It covers fresh/repeated install, startup, registration, correct shortcuts, independent opt-out, verified cache, keeping correct payload timestamps, replacing missing/corrupt files, refusal to overwrite an unregistered older portable, data-preserving uninstall and explicitly confirmed fixture-data removal. Upgrade testing restores exact .34 executable bytes and registration on failed health, installs .35 successfully, rejects a corrupt repair cache, checks idempotence, uses genuine .29/.31 updater helpers with the .35 migration bridge, and validates first-conversion rollback/retry and portable preservation. Final test registration is removed. The production installation and live saves were not modified.

## Package identities

Application informational version: `0.0.35-alpha+6d4b6afef0958c55639126b35de616165c94bbcd`. Installer and portable contain identical application bytes. All candidates are unsigned.

| File | SHA-256 |
|---|---|
| `NexaSetup.exe` | `9DF4952B2A1D7C97B334C2AE2BC02011B67E06C877A5828FAED2BE5108D86D5F` |
| `nexa-portable.exe` | `F89F032DA072FE913F0CE869BDD34DFB836A24F1376FCDA866A8D0B8CF8C005A` |
| `Nexa.exe` compatibility installer launcher | `A45BC864DF5A9BF34223A188CD2E2A0CA176DBF12B3BA98D496B8D416F6D9174` |

Local delivery: `verification/release035-final/`. The launcher is a digest-verified installer bridge; it is not the portable application. Use the explicitly named portable file for portable operation.

## Evidence and limits

[Route logs and runner fingerprints](verification/nexa035/binary-identity.json) · [package identity](verification/nexa035/release-identity.json) · [publish log](verification/nexa035/publish.log) · [native data/UI](verification/nexa035/data-ui-native.log) · [native installer lifecycle](verification/nexa035/installer-lifecycle-native.log) · [native upgrade/migration](verification/nexa035/installer-upgrade-native.log) · [packaged executable](verification/nexa035/packaged-native.log).

Physical trackpad/touch, mixed-monitor/DPI transitions, frame-level flicker, wizard click-through, actual audio-device synchronization, wireless hardware/link changes and battery/AC changes remain manual acceptance gates. The startup process reported PerMonitorV2 at 1.5 DPI scale; that does not certify monitor transitions. Companion size is constrained by actual free Home layout and shrinks on crowded laptop screens. Signal strength is explicitly unavailable, not estimated. Beta remains unapproved.

The final safeguard binds an end confirmation to its original session and dismisses it on a successful Space transition. A focused regression checks that a newly active Space is not ended by an older prompt. History compact summaries read the archived session's own timezone without another live-state read.

## Documented native data/UI repeat limit

The 75-check native data/UI route passed on source `74f30f735bb98253397eec43c8088e1e95f6d75b`. The final source adds a stale-confirmation guard and read-only History refinement. Storage, migration, DPAPI, export/delete and ecosystem files are unchanged between these source commits. The final full suite includes the 73-check in-process data/UI route; the two additional native junction checks were not rerun successfully on the final runner because automatic approval review twice exceeded its deadline. No failed data assertion occurred in those attempts: the commands were not launched.

The user explicitly chose to finish with this limit documented. Final-source regression (1,308), portable executable (17), installer lifecycle (40) and upgrade/migration (32) all passed. Native installer and portable receipts refer to the exact final payload hash above. This remaining repeat is not presented as a final-source native pass.

The .34 upgrade baseline uses the approved .34 application payload inside a separate fixture installer wrapper, with the existing fixture-only maintenance helper. It is a controlled rollback fixture, not the historical shipped .34 installer. Production installers reject that separate-helper option. No production installation or live save was changed.
