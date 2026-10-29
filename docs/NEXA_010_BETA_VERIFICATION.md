# Nexa Beta 0.1.0 — release verification

Release tag: `v0.1.0-beta`. Stage: Beta, GitHub prerelease. Windows x64 self-contained packages.

This release includes the .35 refinements and Home Light/installer appearance fixes previously recorded in `NEXA_035_THEME_REFINEMENT.md`. Splash screen, application icon and title-bar icon assets remain unchanged. The release banner is the exact PNG supplied by the user.

Beta identity: application and compatibility launcher use 0.1.0.0 assembly/file versions and 0.1.0-beta informational versions; installer uses 0.1.0.0 file version and displays Beta. Historical Alpha identities remain historical.

Verification is recorded under `verification/beta010/`; final results are added before publication. No production install or private user-data migration is performed by these tests. Native lifecycle tests use a separate test AppId and a verification-only directory.

Acceptance limits: broad physical-device/DPI/display testing and live third-party-provider acceptance remain ongoing Beta work. The installer reads Windows light/dark preference at launch; high contrast remains controlled by Windows. The native wizard was manually reviewed in this PC's dark setting. The supplied light artwork and dynamic light configuration are included; the user's Windows preference was not changed for a second visual pass. Packages are unsigned.

Final candidate results:

- Full regression coverage: 27 routes / 1,308 assertions pass. The first sweep passed 26 routes and stopped the .30 route at an obsolete active-release-title expectation. After updating that test expectation to the Beta headline, the complete .30 route passes all 63 assertions. No application code changed for that correction.
- Additional Home Light rendered verification: 10 assertions pass.
- Native packaged startup/update/rollback: 17 assertions pass against the actual 0.1.0 executable.
- Native installer lifecycle: 40 assertions pass against the 0.1.0 fixture installer, including registry version, fresh/repeat install, missing/corrupt payload repair, save preservation, shortcut opt-out, portable refusal and confirmed fixture-only data cleanup.
- Three final executable SHA-256 digests recorded in `docs/verification/beta010/SHA256SUMS.txt` and checked again before upload. The supplied banner's local and repository copies have identical SHA-256 hashes.
- Application and launcher informational identity: 0.1.0-beta+d6444b9872324468a322d2bfdf2d8fb2cd221c05. Subsequent changes are test expectations and documentation/evidence only. Setup embeds the exact verified app payload; the compatibility launcher embeds the exact Setup digest.
- Build succeeds with NuGet vulnerability-feed availability warnings. The existing isolated NTFS-junction fixture is unverified under the test token; that route reports this explicitly.

Durable logs: `docs/verification/beta010/`. The initial full-sweep log and per-route logs remain under `verification/acceptance-20261029-120525/`; the corrected .30 route is recorded separately. New Home captures: `docs/media/beta010/`.

Publication uses a draft-first GitHub release, verifies uploaded asset sizes/digests, and publishes as an explicitly labeled Beta prerelease after the checks pass. The first image in the release notes is the supplied Nexaween Beta banner.
