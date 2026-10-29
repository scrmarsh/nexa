# nexa v0.0.31 Alpha publication

The user explicitly authorized publishing .31 on October 28, 2026. Target: [v0.0.31-alpha](https://github.com/scrmarsh/nexa/releases/tag/v0.0.31-alpha), marked **prerelease**, titled **nexa v0.0.31 Alpha — nexaween: Interface Polish & Release Integrity**. **Published and independently verified:** release ID `427365131`, tag commit `8f1767dba9dee3ee6c5feb0ef906f5a2d7afa0a7`. [Publication receipt](verification/nexa031/publication-receipt.json). Google/Microsoft expansion remains deferred by user request.

## Exact executable

- Application/test source commit: `938f5185f6a752ab6a8ee68bfdf5d97256d4f267`.
- `Nexa.exe`: 72,660,057 bytes, file version `0.0.31.0`.
- Informational version: `0.0.31-alpha+938f5185f6a752ab6a8ee68bfdf5d97256d4f267`.
- SHA-256: `a4af062cfba9ab946568e6f823b0bc8cf06ff68ceb9a95e9b2cc909ca60681f8`.
- [Machine-readable identity](verification/nexa031/publication-package-identity.json), [checksum](verification/nexa031/publication/SHA256SUMS.txt), [release notes](NEXA_031_PRERELEASE_NOTES.md).

The release tag includes subsequent documentation/evidence updates. Those updates do not modify application source or change the validated executable. Existing historical release associations, assets, digests and download URLs must remain intact. Published assets are the portable executable and checksum; no installer, user profile or credentials are uploaded. Local approved-build locations and shortcuts are not promoted/replaced.

## Final verification

Release application/checks build: **zero warnings/errors**. Self-contained compressed Windows x64 publication completed cleanly. **All 21 regression routes / 883 assertions passed**, plus **17 native packaged assertions passed** against this exact executable.

The first full run passed 19 routes and found two outdated UI selectors: `About Nexa` and `Nexa · Notes & History`. They were changed to the requested lowercase labels, without removing or weakening their assertions. `nexa030` then passed 63 checks and `core` passed 64 checks. The other 19 routes were unchanged, and application source is identical between the two builds (`f110f8e` → `938f518` changes only these two test selectors). Each route is counted once; reruns are not added. The initial failure summary remains available for audit.

- [Final route ledger](verification/nexa031/publication/regression-ledger.json).
- [Initial full-run summary](verification/nexa031/publication/initial-suite-summary.log).
- [Corrected nexa030 route](verification/nexa031/publication/nexa030.log), [corrected core route](verification/nexa031/publication/core.log).
- [Build](verification/nexa031/publication/build.log), [publish](verification/nexa031/publication/publish.log), [17 native package checks](verification/nexa031/publication/packaged.log), [native startup health](verification/nexa031/publication/startup.json).

Native checks cover a verified real .29 executable upgrading to .31, exact rollback after failed health, preservation of isolated workspace/notes/permanent score, idempotence, interrupted install recovery, tamper rejection, separate helper installation and separate-process restart/health. Test data is disposable; no live user workspace is used.

Second-computer updating, physical mixed-DPI/multi-monitor/focus behavior, live provider authorization, listening and sustained performance remain unverified. Old .24/.26 executables require a one-time closed-executable replacement to bootstrap the corrected comparator; their code cannot be patched by release metadata.

## Public-page cleanup

The release includes the lowercase branding changes and canonical Alpha display headings. README download/version links target .31. The shared .24 seasonal banner now displays `v0.0.24 alpha`. Historical filenames, legacy tag/download paths, immutable evidence, original product-version observations and verbatim requirements retain original compatibility identities. The release tag reconciles the documentation-only public main changes. Automatic approval review rejected advancing main and the development branch as broader than the release authorization. Publication therefore pushes only the new release tag and assets; main and development branch refs remain unchanged. Repository About points directly to the new release. No history or old tags are rewritten.

Publication follow-up: both GitHub asset digests match the local files, the repository About website points to .31, and an independent connector read plus normalized comparison confirms all three historical releases and six assets are preserved. The publisher’s final historical comparison initially flagged a mismatch after successful publication; independent exact field comparisons passed. Default main remains `ec5e4351008c0ce946fb76506df3b18101ad8702`, pending separate branch-update approval.
