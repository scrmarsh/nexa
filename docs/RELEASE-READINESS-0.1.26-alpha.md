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

## Packaged testing candidate

- Executable: `M:/Nexa/Builds/Testing/0.1.26-alpha/Nexa.exe`.
- Source commit: `49b138123f249bb53dee08a6050b8f1c1fed0f24` on `codex/nexa-026-alpha`.
- Informational version: `0.1.26-alpha+49b138123f249bb53dee08a6050b8f1c1fed0f24`.
- SHA256: `D9D9C1D9CBA7868D2B36B62936C598A692BFA3AC90BD944B6FDF8A97DA325B22`.
- Size: 145,195,830 bytes; self-contained Windows x64 single-file package, with adjacent checksum and candidate metadata files.
- Isolated packaged health run: `verification/packaged026/startup.json`, exit 0. Correct alpha version, x64, PerMonitorV2, 759 square pixels, four presets, three embedded Bible datasets and zero initial bugs.
- Final metadata/string-only rebuild: zero warnings/errors; alpha and menu follow-up checks passed again (27 and 9, repeated checks not added to the 471 distinct total).
- The approved Latest, root executable and earlier Testing executable were checked against their pre-packaging SHA256 values and remain unchanged.
- This is a **partial preview**. No claim of a complete v0.1.26 release, successful physical hibernation testing, live-user-data migration or improved overall Space loading is made.
