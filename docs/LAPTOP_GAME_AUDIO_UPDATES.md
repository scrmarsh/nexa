# Laptop safety, Pixel Timer game, independent audio and automatic updates

October 28, 2026. Local follow-up on `codex/laptop-game-audio`, application/test commit `c926a53e90307159d7fe2d436d9f7068f39313b3`. These changes are not in the published .31 release and do not replace its assets. The development version remains .31 pending a separately authorized new release.

## Window and Home fixes

Every shared-chrome window, including gameplay dialogs, fits the current monitor's usable work area. Initial and restored bounds are clamped in native physical pixels, retaining negative monitor origins and accounting for per-monitor DPI. Oversized minimum dimensions are reduced to the available area so the title bar stays reachable. Display, DPI, position, size and state changes reapply the rule. Explicit fullscreen uses the full monitor; ordinary/maximized windows respect the taskbar work area.

Home uses a stretched header with equal left/right columns and a stable height. The logo scales down within the left column; the tagline types/fades in its own right column. Its changing text width no longer shifts the centered group. Classic mode collapses the tagline region and retains the centered logo.

## Reproduced game crash and tests

A fatal hostile projectile called `DamagePlayer`, which cleared `Shots`, then the projectile loop called `RemoveAt` on that cleared list. The fix returns immediately on game over. It also stops the remaining simulation work after phase transitions, rejects non-finite frame sizes/time/pointer data, and enforces the projectile cap even for multi-shot upgrades. It does not hide arbitrary exceptions behind a generic catch.

Tests reproduce three successive hostile hits through the real collision path, including the fatal hit, and verify one death cue, stopped game-over simulation, firing cooldown, power-up collection, invalid-input guards and forty repeated bounded game runs. Other unexplained hardware-specific lockups cannot be ruled out without the affected laptop and its crash log.

## Kill scoring without a save on every hit

Dashboard kills immediately update the in-memory score, species totals and the current session ledger. They mark changes dirty without starting the edit-save debounce or writing either workspace or ecosystem files per hit. Ordinary configured autosave, manual save, navigation/close and the ecosystem's existing 30-second checkpoint persist batches. Periodic ecosystem checkpoints now use worker I/O. Explicit recovery batches pending awards into one workspace save rather than saving every event separately.

Only award IDs present in the captured, successfully written workspace snapshot are acknowledged. Failed saves retain their pending awards; event IDs prevent duplicate recovery credit. Session-generation resets retain the pending original-session awards. As requested, a kill has no immediate durable write: a forced termination before any checkpoint may lose the newest unsaved activity. Regular save/close and recovery of checkpointed events remain covered.

## Independent sound choices and effects

Changing either positive or negative feedback preserves the other choice. Choosing a style preset does not change either selection; **Apply preset to both feedback sounds** is the explicit bulk action. “Warm wood” is now **Soft tap**, and the old “Wood” feedback choice migrates to **Tap**, retaining that original cue's waveform.

Nine original synthesized PCM cues are generated locally: shooting, power-up, death, quiet crawling, ghost, candy pickup, bat flutter, witch bell and pumpkin pop. Actual successful game actions emit cues; moving visible dashboard bugs emit rate-limited crawling; seasonal surprises rotate Halloween effects; a successfully collected candy emits its candy cue. Master sound enable/volume and independent game/crawl switches are persisted through the existing per-user preference flow. Halloween effects also require active nexaween and Seasonal sounds. Inactive/hidden/minimized views remain silent. Preview buttons intentionally audition selected cues without enabling automatic playback.

Playback uses one background worker, at most one pending effect, category priorities and rate limits. Rapid firing/crawling cannot accumulate unbounded tasks or an audio backlog. Existing short interface previews retain their own bounded player. Audio device quality and physical listening remain manual acceptance.

## Automatic update behavior

- First normal startup asks whether to **keep automatic updates enabled**, checked by default. Explicitly saving the choice persists it; closing the prompt without choosing does not grant automatic-install consent.
- Discovery runs after the usable Home/dashboard opens and every four hours while the app remains open. The old optional startup-check field no longer suppresses notices. Previously offered versions are not silently suppressed on later checks.
- Everyone receives an in-app available-update card. With automatic installation disabled, **Install update** on that card starts the complete process without entering Settings.
- With automatic installation enabled, a newer eligible portable release downloads, validates its official digest and binary identity, and stages the existing transaction/backup protocol automatically.
- Only after successful preparation does a **30-second countdown** start, with **Restart now** and **Later**. Postponing during download cannot produce a hidden countdown. Disabling automatic updates cancels pending automatic restart, including when the download finishes afterward.
- At the deadline or Restart now, editors/settings, the active game score, recordings, workspace and ecosystem are saved/checkpointed, then the data backup is refreshed. A failed save blocks restart and keeps a retry action. Helper launch failure leaves the app open. The existing helper waits for parent exit, installs, checks startup health and rolls back failed startup.
- Startup and Settings use the same coordinator/flow, preventing duplicate operations from those two entry points. The notice follows the visible Home/dashboard through navigation. Errors remain explicit; no eligible metadata or a failed check is not presented as proof that the installed version is current.

Downloads and automatic replacement are supported by the portable `Nexa.exe`, not the test host. Existing .24/.26 installations still need the documented one-time manual replacement to reach corrected updater code. This local follow-up has not changed public releases or exercised an automatic restart against a live user workspace.

## Verification

**Final regression: 23 routes / 1,024 assertions passed** on the committed source. Release application/checks build: **zero warnings, zero errors**. The compressed self-contained x64 package also built cleanly. [Route ledger](verification/laptop-game-audio/route-ledger.json), [suite summary](verification/laptop-game-audio/suite-summary.log), [build](verification/laptop-game-audio/build.log), [publish](verification/laptop-game-audio/publish.log), [local package identity](verification/laptop-game-audio/package-identity.json).

Native captures inspected: [stationary Home logo and right-hand tagline](media/laptop-game-audio/stationary-home.png), [independent sound controls](media/laptop-game-audio/independent-sounds.png), [first-run update choice](media/laptop-game-audio/update-choice.png). The nine original WAV previews are retained under `docs/verification/laptop-game-audio/sound-previews`. New routes `laptop-audio` and `update-automation` are included in `tools/Verify.ps1` alongside all 21 previous routes. Existing tests were updated for the explicit new sound-preset behavior and batched-kill persistence, preserving their scoring/deduplication/recovery checks.

The updater-flow tests use an injectable clock and fake download/restart functions, never replace the user's app or access live accounts. Native packaged tests separately exercise the real helper and isolated data. Physical second-laptop/mixed-DPI/taskbar behavior and listening remain open acceptance items.

**Native packaged verification: 17 assertions passed** against the exact local executable, including genuine .29 replacement, data preservation, idempotence, interrupted-install recovery, tamper rejection, real helper/restart/startup and failed-health rollback. [Packaged log](verification/laptop-game-audio/packaged.log), [native health](verification/laptop-game-audio/startup.json). Package file version `0.0.31.0`, informational version `0.0.31-alpha+c926a53e90307159d7fe2d436d9f7068f39313b3`; SHA-256 `87f908e296214b40f6f42bdd9ae8d0f8510896a7f56491555117fed6d9396bb3`. This is a local development artifact, distinct from the previously published .31 binary.
