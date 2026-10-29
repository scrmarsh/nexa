# nexa .35 — storage compatibility

The canonical private root remains `%LocalAppData%\Nexa`. Authoritative state, encrypted credential sidecars, profiles and the dashboard ecosystem sidecar retain their .34 relative paths. The empty organizational `Ecosystem` directory is not a second database. No new ecosystem namespace or compatibility identifier is introduced.

Living Worlds belongs to archived game code excluded from the Nexa build. The running dashboard neither loads nor creates its directories. The .35 migration inventory skips directory components named `LivingWorlds`, `Living Worlds`, `Living-Worlds` and `Aevoria` within the old SessionDashboard source tree. Existing copies from older migrations are not automatically removed. Game originals are not changed.

Nexa-owned files continue through the existing SHA-256-verified copy/inventory process: preserve originals and backups, let an existing destination win conflicts, retry interrupted copies, and retain DPAPI Windows-account ownership. The focused .35 fixture repeats migration, loads the migrated ecosystem and checks exact ecosystem and unrelated-game bytes. The .34 regression routes continue covering conflicts, backup export, cancellation, read failures and reset/import guards.

The new 2,400-DIP widget height limit includes save validation. Current .35 saves with heights above 1,440 DIP require .35 or later; older .34 binaries do not understand those expanded dimensions. Do not roll back an active .35 save to .34 without using a pre-upgrade backup. Installer rollback's existing exact-file/data protections are unchanged.

See [the .34 data/installer policy](NEXA_034_DATA_AND_INSTALLER.md) for credential-free export, explicit deletion consent, verified installer repair and recovery details.
