# Development

Build the root `SessionDashboard.csproj` with the .NET 10 Windows desktop SDK. The historical project filename is retained for compatibility; the executable is Nexa.

Run `tools/Verify.ps1` for the current eleven-route isolated acceptance suite. Keep `LEGACY/` and `verification/` excluded from compilation. Legacy staging scripts are provenance, not an implementation workflow.

The current specification is `NEXAWEEN-0.1.24-requirements.md`, with these later decisions: optional rectangle-only `widgetTargets` separate from collision-only `occupied`; no inferred attachment metadata; project source belongs in the private scrmarsh/nexa repository, with obsolete files preserved in LEGACY. Version remains 0.1.24.

Do not commit build products, recordings, user profiles, secrets or diagnostic recovery copies. The local portable executable can be generated with `Publish-Exe.ps1`. Validate a new package with `--verify-startup <isolated-output-directory>`.

See [root audit](ROOT-AUDIT.md) for active root inputs and archive decisions, [integrations](INTEGRATIONS.md) for account setup, and [release notes](releases/0.1.24-alpha.md) for the downloadable package. The release executable is self-contained Windows x64; generated artifacts stay outside Git.

The final-prompt repair candidate lives on `codex/nexaween-final-024`, with a recovery ref at `codex/recovery-before-final-024`. Read [current candidate status](FINAL-0.1.24-STATUS.md) before changing release metadata. Do not mark the final release or replace the published release until the remaining acceptance is complete and publication is authorized.

Optional existing-data compatibility check: run the checks executable with `--existing-data <isolated-copy-under-verification>`. It refuses live data paths, checks readable Spaces, and verifies the copy is unchanged. Never commit that copy or its private captures.
