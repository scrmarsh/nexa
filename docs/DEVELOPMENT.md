# Development

Build the root `SessionDashboard.csproj` with the .NET 10 Windows desktop SDK. The historical project filename is retained for compatibility; the executable is Nexa.

Run `tools/Verify.ps1` for the current seventeen-route isolated acceptance suite. Keep `LEGACY/` and `verification/` excluded from compilation. Legacy staging scripts are provenance, not an implementation workflow.

Current development is **Nexa v0.0.27 Alpha — NEXAWEEN**. Read
[the audit](NEXAWEEN_027_AUDIT.md), [status](NEXAWEEN_027_STATUS.md) and
[.28 carry-forward](NEXAWEEN_028_CARRY_FORWARD.md) before changing completed
systems. Alpha now uses 0.0.X; the first planned public Beta is 0.1.0, with
publication requiring explicit approval. Prior specifications remain historical
context. Keep optional rectangle-only `widgetTargets` separate from collision-only
`occupied`; do not infer attachment metadata. Obsolete source stays in LEGACY.

Do not commit build products, recordings, user profiles, secrets or diagnostic recovery copies. The local portable executable can be generated with `Publish-Exe.ps1`. Validate a new package with `--verify-startup <isolated-output-directory>`.

See [root audit](ROOT-AUDIT.md) for active root inputs and archive decisions, [integrations](INTEGRATIONS.md) for account setup, and [release notes](releases/0.1.24-alpha.md) for the downloadable package. The release executable is self-contained Windows x64; generated artifacts stay outside Git.

The .27 continuation branch is `codex/nexa-027-nexaween`, based on the completed
October 28 handoff. Older branches/reports retain provenance. Do not replace
approved packages or publish an Alpha release. Manual acceptance remains open.

Optional existing-data compatibility check: run the checks executable with `--existing-data <isolated-copy-under-verification>`. It refuses live data paths, checks readable Spaces, and verifies the copy is unchanged. Never commit that copy or its private captures.
