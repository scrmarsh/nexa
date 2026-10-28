# nexa v0.1.26 alpha - testing candidate readiness

**IMPLEMENTED BUT UNVERIFIED for public release.**

The three requested implementation passes are represented in working code. A release has not been published and the approved executable has not been overwritten. This remains alpha.

Candidate destination: M:/Nexa/Builds/Testing/0.1.26-alpha-passes-20261028/Nexa.exe.

Source commit: e63ea704090b7299c2e07662a9411601a9ba66c4.

SHA-256: 1D08B896653932F0470BF09EEF5464887BC9ABB0C539E8E91AC549B227E0868C.

Self-contained Windows x64 executable: 145,244,982 bytes; product version 0.1.26-alpha+e63ea704090b7299c2e07662a9411601a9ba66c4. Candidate provenance and checksums are also saved beside the executable in candidate.json and SHA256SUMS.txt.

Release application/test build: zero warnings, zero errors. Full fifteen-route regression: 507 checks passed (verification/acceptance-20261028-080105). Packaged startup passed with PerMonitorV2, 759 square pixels, four presets and all three bundled Bible datasets (verification/packaged026-20261028-confirmed/startup.json).

Native updater integration: 10 checks passed against this exact candidate (verification/20261028-080527), including rollback, preserved synthetic data, idempotence, interruption reconciliation, tamper rejection, external helper install and isolated restart. No production installation was modified.

The approved/root executables and earlier testing candidates were hash-verified unchanged. Only the development branch is updated; no release is published.

[Fullscreen Home screenshot with synthetic Spaces](media/preview026/home-fullscreen.jpg).

## Outstanding acceptance

- Physical interaction/appearance and multi-monitor/DPI testing.
- Genuine hibernation/crash recovery and an approved private saved-data fixture.
- Live integration accounts and a real newer-package updater transaction.
- Extended-session memory and live high-density rendering FPS.
- Remaining Space-load latency outlier seen in the comparison fixture.

No known failing automated functional test is intended to ship. These remaining items prevent declaring the alpha fully accepted or publishing a release without the user's explicit approval.

See the [implementation](IMPLEMENTATION-0.1.26-alpha.md) and [testing evidence](TESTING-0.1.26-alpha.md).
