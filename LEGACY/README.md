# LEGACY

Preserved project history. This directory is excluded from Nexa compilation; it is not a second active application.

- `source/`: retired story engine and artwork, duplicate Session UI and unused sprite-atlas implementation.
- `embedded-aevoria/`: archived Aevoria/Colony Wars/Living Worlds source, tests, assets and design notes recovered from the earlier local archive. The separate active Aevoria project was not modified.
- `notes/`: superseded validation reports, pre-naming-scheme 1.0.x notes, handoff/planning documents, the original 0.1.6 integration guide and full historical/mixed development changelog.
- `tests/`: an obsolete duplicate test project. Current checks are in the root `tests/` directory.
- `tools/`, `assets/`, `maintenance/`: obsolete launch helpers, icon and one-time cleanup scripts.
- `implementation-staging/`: historical one-time staging scripts/drafts retained for provenance. Many contain old absolute paths or non-idempotent substitutions; do not run them against current source.

`manifest.json` records this cleanup. Previous source also remains available through Git history. Build outputs, recordings, credentials, personal workspace/recovery data and generated captures are deliberately not uploaded.

The final-candidate pass also archives `source/NexaEcosystem.Window.cs` (the retired separate map), its prior UI checks, and the published `4562d69` implementation report. Reusable overlay simulation/rendering and compatibility data remain active; personal saved world files are not deleted.
