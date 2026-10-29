# nexa v0.0.31 Alpha — nexaween: Interface Polish & Release Integrity

**nexaween Alpha prerelease · Windows x64 · portable**

This release focuses on interface polish, dependable update discovery and a more natural seasonal ecosystem. It includes the .30 refinements and preserves existing Spaces, notes, tasks, recordings, history, preferences and account connections.

## Interface polish

- Consistent lowercase **nexa** branding throughout visible app labels and release notes.
- Independently centered Recent Space names with larger icons, improved Home header alignment and the approved pumpkin artwork with Home-only candle flicker.
- Edit a Space icon in Session Settings using a preview, selected highlight, Save and Cancel; includes a cap-and-gown choice.
- Readable, wrapped Pixel Timer status and Test Sound directly beneath Sound Style, using the selected style and volume.
- Retained fullscreen notification spacing, Space-name/icon ordering, wallpaper controls and session/history safeguards from earlier refinements.

## Updates and release integrity

- Shared canonical version handling: **0.0.X Alpha**, future **0.1.X Beta**, and **1.X stable**. Confirmed historical Alpha versions are recognized without confusing them with Beta.
- Newest eligible release selection across paginated feeds, saved channel preference and clearer network, rate-limit and incomplete-package errors.
- Permanent Updates page with installed/latest versions and optional startup checks.
- Verified download checks, backup, separate update helper, restart health checks and rollback remain in place.

**Upgrading from .24/.26:** those old executables use a numeric comparator that can reject canonical `0.0.X` releases as older. Save your work, close nexa, keep the old executable as a backup and manually replace only `Nexa.exe` with this release once. Keep the existing LocalAppData `SessionDashboard` data folder. The corrected updater handles later eligible releases; GitHub metadata changes cannot repair code inside an old installed executable.

## Seasonal ecosystem

- Rounded spiders with eight front-body leg roots and slower turning.
- Orb webs grow strand by strand as the spider travels, with cached curved silk and construction stopping if the builder is missing.
- Explicit session loads start fresh live creature/web/candy activity. Permanent totals, preferences, pending awards and ordinary restart/crash recovery are preserved.

## Download and validation

Download **Nexa.exe** and **SHA256SUMS.txt** from Assets. This is a self-contained Windows x64 executable; no installer or separate .NET runtime is required. Compare `Get-FileHash .\Nexa.exe -Algorithm SHA256` with the supplied checksum. The executable is not code-signed.

- **21 regression routes / 883 assertions passed**, plus **17 native packaged-update checks** covering genuine upgrade, data preservation, restart and rollback.
- Release build: **zero warnings, zero errors**.
- Executable source: `938f5185f6a752ab6a8ee68bfdf5d97256d4f267`. Release tag also includes documentation/evidence updates; application source is unchanged.
- Executable SHA-256: `a4af062cfba9ab946568e6f823b0bc8cf06ff68ceb9a95e9b2cc909ca60681f8`.
- [Publication evidence](https://github.com/scrmarsh/nexa/blob/v0.0.31-alpha/docs/NEXA_031_PUBLICATION.md).

## Remaining Alpha limitations

Google/Microsoft account, calendar and task integration expansion was deferred by user request. Existing connections remain available. Physical mixed-DPI/multi-monitor/focus behavior, second-computer updating, live-provider authorization, listening tests and sustained CPU/GPU/memory acceptance remain open. The reported physical maximize-only pumpkin disappearance did not reproduce in the isolated current-source fixture; it is not claimed as physically certified.
