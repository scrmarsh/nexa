# Local build authority

Current source development is **Nexa v0.0.28 Alpha — NEXAWEEN EVOLVED**, unpublished.
Use a fresh `Builds/Testing/0.0.28-alpha-*` output directory when explicitly
packaging it. No .28 portable package has been accepted. Existing executable
paths and hashes below are historical records, not verification of current bytes.

- `Builds/Testing`: unapproved candidates. `Publish-Exe.ps1` writes only within this tree; select a fresh subdirectory to preserve earlier candidates.
- `Builds/Latest/Nexa.exe`: approved published 0.1.24 Alpha, commit `4562d69`. SHA256 `D836DE09B79E873E232ED6E9FAE67F9E1F73D5D6B2A62EFCCE1AB31C2E75B420`.
- `Builds/Archive`: earlier approved packages when available. No approval is inferred from a local compilation.

The existing root executable is preserved for compatibility. Old duplicate publish folders were retained under ignored `LEGACY/build-artifacts/publish-before-025`; they are not an additional build authority. `bin`/`obj` remain compiler intermediates.

`CreateDesktopShortcut.ps1` selects only the approved Latest executable. Testing a candidate is an explicit launch of `Builds/Testing/Nexa.exe`. No script here promotes a candidate or publishes a release.
