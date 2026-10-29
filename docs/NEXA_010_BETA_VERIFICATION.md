# Nexa Beta 0.1.0 — release verification

Release tag: `v0.1.0-beta`. Stage: Beta, GitHub prerelease. Windows x64 self-contained packages.

This release includes the .35 refinements and Home Light/installer appearance fixes previously recorded in `NEXA_035_THEME_REFINEMENT.md`. Splash screen, application icon and title-bar icon assets remain unchanged. The release banner is the exact PNG supplied by the user.

Beta identity: application and compatibility launcher use 0.1.0.0 assembly/file versions and 0.1.0-beta informational versions; installer uses 0.1.0.0 file version and displays Beta. Historical Alpha identities remain historical.

Verification is recorded under `verification/beta010/`; final results are added before publication. No production install or private user-data migration is performed by these tests. Native lifecycle tests use a separate test AppId and a verification-only directory.

Acceptance limits: broad physical-device/DPI/display testing and live third-party-provider acceptance remain ongoing Beta work. The installer reads Windows light/dark preference at launch; high contrast remains controlled by Windows. The native wizard was manually reviewed in this PC's dark setting. The supplied light artwork and dynamic light configuration are included; the user's Windows preference was not changed for a second visual pass. Packages are unsigned.
