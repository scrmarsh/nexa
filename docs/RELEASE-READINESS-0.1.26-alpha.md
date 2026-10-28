# nexa v0.1.26 alpha — release readiness

**PARTIALLY IMPLEMENTED — not ready for public release.**

Intended candidate: `M:/Nexa/Builds/Testing/0.1.26-alpha/Nexa.exe`.

Approved `Builds/Latest`, root `Nexa.exe`, earlier `Builds/Testing/Nexa.exe`, archived builds and public GitHub releases must remain unchanged.

## Blockers

- Interactive tutorial and spotlight persistence are missing.
- Per-Space cumulative scoring, candy scoring and the new Productivity ring are missing.
- Dedicated End Session finalization, saved recap and recap navigation are missing.
- Hedy is implemented separately; functional Hevy authorization is not verified. Do not relabel Hedy as Hevy.
- Remaining animation, art, performance, onboarding and theme requirements are listed in the implementation matrix.
- Full physical interaction, DPI/taskbar, hibernation, integration-provider and long-session performance acceptance remains manual.
- C: was full during implementation. Compilation can use the task-specific M: temporary directory, but normal application storage and Windows still require working system-drive space.

No beta promotion, public release, installation or replacement of the approved build is authorized by this candidate's creation. Packaging/verification status and source commit are recorded after the final checks.

## Source verification

Release build passed with zero warnings/errors using M: temporary files. Final suite passed 471 checks. Native Home rendering was inspected. This does not clear the blockers above or establish real-user migration/hibernation safety. Live-data copy verification was rejected by automatic approval review and was not performed.
