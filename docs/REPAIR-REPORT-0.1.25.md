# nexa 0.0.25 Alpha repair report

**Latest testing candidate:** follow-up source `0f075afd98d5723dcc402c8852483422c0e230f5`; SHA256 `34AF60183B25A495E191B7975AD72CBA4F4CBDF65053A8932C2581FD4C15B66A`. The executable remains `M:\Nexa\Builds\Testing\Nexa.exe`. See [the current tweak report](TWEAKS-0.1.25.md) for completed follow-up work, 442 distinct passing regression checks, final startup evidence and remaining manual acceptance. This is an unapproved candidate.

The first candidate report below records the earlier implementation and its original evidence. Its initial package hash/version are historical; the current sidecar metadata and linked follow-up report identify the delivered executable.

---

## Initial candidate implementation and evidence

**Unapproved development candidate; the complete feature request is not yet finished.** Core repairs and additional functional/seasonal work are implemented, regression-tested and packaged. Remaining implementation and manual acceptance are explicitly listed below. No production release was published or replaced.

## Delivery and source

| Item | Result |
| --- | --- |
| Candidate | `M:\Nexa\Builds\Testing\Nexa.exe` |
| Product version | `0.1.25+1c48c6519eae912f3937a1f5ad7e38b81a12c85c` |
| Source branch | `codex/nexa-025-audit-repair` |
| Source commit | `1c48c6519eae912f3937a1f5ad7e38b81a12c85c` |
| Recovery point | `codex/recovery-before-025` → `0e71ac7` |
| Package | Self-contained Windows x64 single-file; 145,154,870 bytes |
| Candidate SHA256 | `72EB02137C6D84A27FB1F2D70F453AA4FC88C16CABC8B4B23596AEBF35FE4B38` |
| Approved Latest | Published 0.1.24 Alpha, source `4562d69`; SHA256 `D836DE09B79E873E232ED6E9FAE67F9E1F73D5D6B2A62EFCCE1AB31C2E75B420` |

`candidate.json` and `SHA256SUMS.txt` accompany the ignored local candidate. Documentation-only commits can follow the embedded source commit without changing the executable. The existing root executable, approved Latest, published GitHub release and separate Aevoria project remain unchanged. Duplicate local publish folders are retained in ignored `LEGACY/build-artifacts/publish-before-025`; builds now have one authority described in [BUILDS](BUILDS.md).

## Root causes and repairs

- **Public updates:** asset matching accepted only a versioned Setup.exe although the official public release contains Nexa.exe. Discovery now handles the portable asset, official size/digest, compatible alpha channels, bounded pagination, malformed entries and useful 404/rate-limit errors. An unauthenticated live call found published 0.1.24 Alpha with the expected canonical URL and digest.
- **Safe installation:** replaced the portable-launch-as-installer gap with explicit download/prepare/restart approval, unique partial paths, bounded download/hash checks, PE x64/product/version validation, backups and a separate hidden helper. Atomic replacement is followed by isolated startup health verification; failure rolls the executable back. Interrupted Installing checkpoints resume safely. Exclusive file handles span async work; backup traversal prunes its own updates subtree before recursion. Restart repeats the backup after flushing current changes. Pending/error transaction states remain accessible in experimental Updates. SHA256 is anchored in the official HTTPS GitHub feed; this is not an independently code-signed release.
- **Homepage:** six cards in two columns and three more rows per Show More. New regression coverage exposed a genuine WPF logical-parent crash on Show More; detaching the reused button fixes it. No Space IDs or saved data are discarded.
- **Productivity:** one new combined library entry, shared score/actions/categories/recent gains and compact/expanded controls. Legacy kinds/configurations/positions remain readable. One centered noninteractive popup displays the saved two-minute streak, half-step increments, capped multiplier and visual decay. Actual bonus scoring stops immediately at expiry; visual decay never subtracts points.
- **Squashes:** illustrated per-species counters and persistent session/all-time dictionaries; central direct-action deduplication retained. Earlier totals without species remain explicitly unknown. Historical reports include newly recorded breakdowns without inventing older events.
- **Branding/splash:** corrected seasonal upper-left green/purple and regenerated PNG/multi-resolution ICO. Restored original pre-seasonal normal assets from `9690b91`; stopped monochrome light-mode masking from erasing brand colors. Exact lowercase seasonal tagline, including first-letter orange while typing. Three-second logo-only startup runs alongside real loading; only then is the thin progress track shown.
- **Bug overlays:** added adaptive Infestation, preserving old fixed High preferences via a schema marker. Low/Normal/High keep 12/36/96 caps; Infestation uses frame/memory/serialized-byte pressure instead of a population-count cap. Spatial buckets separate nearby ground bugs; web offsets scale uniformly instead of stretching with world X/Y proportions. Yellow hit rings removed; brief red body damage and small sad/tear effects added. Existing per-species articulated movement/flight is retained.
- **Seasonal scenes:** shared candy-weighted native assets on Home/dashboard/Appearance, per-actor stationary holds/fades/relocation and a separate static home corner scene. Optional rectangle widgetTargets remain independent of occupied collision/reserved UI rectangles; no inference from reserved UI. Two or three seeded pumpkin attachments follow widgets; branching vines reveal over time. Cozy bugs bind to visible candy, briefly pause to eat, consume it and cause delayed replacement, including with seasonal motion disabled; resource leases protect feeding from fades.
- **Notification layout:** visual review caught long text crowding dismiss controls. Explicit grid columns reserve glyph, wrapped message and close-button space; new tests cover two widths.

## Verification

| Verification | Evidence/result |
| --- | --- |
| Release application/checks build | 0 warnings, 0 errors |
| Full twelve-route regression | 408 checks passed; `verification/acceptance-20261027-195624` |
| After notification fix | 215 affected-route checks passed, including 2 new wrapping checks; `verification/latest025-*.log` |
| Distinct regression coverage | 410 passed across the full run and affected-route reruns; do not add repeated route counts |
| Public GitHub discovery | 2 live unauthenticated checks passed; `verification/20261027-200316` |
| Existing saved-data copy | 2 read-only compatibility checks passed; 8 Spaces, 49 archived reports, input bytes unchanged; `verification/20261027-200317` |
| Packaged update integration | 10 checks passed on disposable copies; rollback, install, idempotence, interruption, tamper, real helper and isolated restart; `verification/20261027-200534` |
| Native packaged startup | Success; x64; PerMonitorV2=True; 759 square pixels; 4 presets; embedded Bible resources checked |
| Packaging | `Publish-Exe.ps1` succeeded to Builds/Testing only |
| Visual inspection | Generated Home and scrolled-dashboard captures inspected; final artwork still requires interactive acceptance |

The distinct total is **424 passed checks**, including the separate live/compatibility/package checks. Expected induced failures (bad checksum, failed health, rate limits, damaged records) are regression cases, not unresolved failing tests. The initial new Show More crash was repaired; an initial overlap between build and runner caused a file-lock build failure, resolved by running them sequentially. A test fixture without spawned creatures was corrected before the passing run. No unresolved automated test failure remains in the checks actually run.

All test writes are isolated. Actual saved data was loaded from a private verification copy and not rewritten. The native helper test uses a verification-only transaction to restart with its own fresh diagnostic store, never the live workspace. Check fixtures, binaries, personal data and logs remain ignored rather than committed.

## Public-history review

Scanned 960 reachable unique text blobs across relevant history without printing matched values. Heuristics found no credential-shaped literals, secret assignments, Windows SIDs or serialized workspace records. **11 historical blobs contained personal local user-folder paths.** Five current archived helper/note files were sanitized; historical copies remain accessible in Git history. No history rewrite, visibility change or credential rotation was performed. Deleting a later copy would not revoke a real secret; any subsequently identified credential must be revoked/replaced at its provider. This heuristic scan does not certify binary asset metadata or every possible secret encoding. Redacted location-only evidence stays in ignored `verification/security-025/history-scan.json`.

## Outstanding implementation and acceptance

1. Existing dual Productivity placements are preserved instead of automatically collapsed; complete lossless placement consolidation if strict single-widget migration is required.
2. Code integrates **Hedy** lecture notes, not **Hevy** fitness. Requested provider identity and ordering/Coming Soon presentation remain unresolved; working connections and credentials were retained.
3. Pumpkins/vines do not yet have dedicated crawl surfaces, all popup pumpkin variants are not synchronized, extensive organic/right-edge vine artwork remains incomplete, and completed-web home-return behavior needs further implementation/verification.
4. The executable's Explorer/shortcut icon is fixed seasonal. Runtime window logos/icons switch normally; exact v0.1.22 asset-version provenance and shell-cache behavior are not certified.
5. Sustained ~20 FPS Infestation, long-session memory, physical mixed-DPI/fullscreen/taskbar behavior, hibernation, real sound/microphone/provider integrations and the real-person updater approval flow remain manual acceptance work. Updates stays experimental.
6. Advanced movement remains stylized procedural illustration; this report does not certify every requested sophisticated animation or realistic interaction as complete.

See [the full 33-requirement audit](FEATURE-AUDIT-0.1.25.md), [retained exact requirements](REQUIREMENTS-0.1.25.md), and [manual checklist](MANUAL-TESTING-0.1.25.md). The next pass should resolve the explicit implementation gaps, then record manual acceptance before any promotion to Latest or public release. No final-release changelog was generated for this candidate.

## Files changed from the 0.1.24 repair source

- `.gitignore`
- `App.xaml.cs`
- `BugOverlay.cs`
- `BugVisuals.Animation.cs`
- `BugVisuals.cs`
- `CreateDesktopShortcut.ps1`
- `DashboardBugs.cs`
- `DashboardContinuity.cs`
- `ExperimentalUpdates.cs`
- `HomeWindow.Recent.cs`
- `HomeWindow.cs`
- `LEGACY/implementation-staging/finalize_docs.py`
- `LEGACY/implementation-staging/organize_legacy.ps1`
- `LEGACY/implementation-staging/stage_followup_branding.py`
- `LEGACY/implementation-staging/start_followup.py`
- `LEGACY/notes/CODEX_SESSION_STATE.md`
- `MainWindow.Bugs.cs`
- `MainWindow.Continuity.cs`
- `MainWindow.Feedback.cs`
- `MainWindow.Productivity.cs`
- `MainWindow.Reliability.cs`
- `MainWindow.SquashStats.cs`
- `MainWindow.Widgets.cs`
- `Models.cs`
- `NexaBranding.cs`
- `NexaEcosystem.Models.cs`
- `NexaEcosystem.Simulation.cs`
- `NexaEcosystem.Store.cs`
- `NexaLogo.cs`
- `NexaNotifications.cs`
- `NexaReliability.cs`
- `Nexaween.Animation.cs`
- `Nexaween.Candy.cs`
- `Nexaween.Placement.cs`
- `Nexaween.cs`
- `ProductivityStreakOverlay.cs`
- `Publish-Exe.ps1`
- `README.md`
- `SeasonalBackdrop.cs`
- `SessionDashboard.csproj`
- `SettingsDialog.Bugs.cs`
- `SettingsDialog.Navigation.cs`
- `SettingsDialog.Updates.cs`
- `SplashWindow.cs`
- `UpdateTransaction.cs`
- `Widgets.cs`
- `assets/nexa-icon-1024.png`
- `assets/nexa-icon-256.png`
- `assets/nexa-icon-seasonal-256.png`
- `assets/nexa-icon-seasonal.ico`
- `assets/nexa-icon-seasonal.svg`
- `assets/nexa-icon.ico`
- `assets/nexa-icon.svg`
- `assets/nexa-logo-full-dark-seasonal.svg`
- `assets/nexa-logo-full-dark.svg`
- `assets/nexa-logo-full-seasonal.svg`
- `assets/nexa-logo-full.svg`
- `docs/FEATURE-AUDIT-0.1.25.md`
- `docs/REQUIREMENTS-0.1.25.md`
- `tests/Nexa024AnimationChecks.cs`
- `tests/Nexa024CoreChecks.cs`
- `tests/Nexa024Runner.cs`
- `tests/Nexa025Checks.cs`
- `tests/NexaFollowupDesignChecks.cs`
- `tests/NexaweenMoreChecks.cs`
- `tests/SessionDashboard.Checks.csproj`
- `tools/Export-SeasonalIcon.ps1`
- `tools/Verify.ps1`

This report and the manual/build-authority documents are included with the delivery. Generated binaries and verification evidence are intentionally excluded from Git.
