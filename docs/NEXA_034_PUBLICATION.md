# nexa v0.0.34 alpha publication

Published as an **Alpha prerelease for installer testing**: [nexa v0.0.34 alpha](https://github.com/scrmarsh/nexa/releases/tag/v0.0.34-alpha). Release ID `427935037`, source tag `v0.0.34-alpha` at `92b2beef20f0c5aeea2439162b6bfadfcfac7595`, publication timestamp `2026-10-29T14:04:19Z`.

**[Download the recommended Windows installer](https://github.com/scrmarsh/nexa/releases/download/v0.0.34-alpha/NexaSetup.exe)**. Close nexa before running Setup. The release and README make the installer the main choice; portable, legacy updater, checksum and source links are secondary. GitHub's own Assets/source controls cannot be hidden by Markdown.

Application source is `66f6bbceb9ce2218ff3bc36bb47aeb2fadd2fd9d`. The tag also contains final documentation, evidence and release preparation. No rebuild was introduced during publication. The installer and portable application payloads are byte-identical and match the audited hashes.

Verification passed: 26 routes / 1,202 assertions, 75 native data/UI checks, 40 native installer lifecycle checks, 32 native upgrade/migration checks and 17 packaged checks. This includes real .33 payload rollback, unmodified .29/.31 helper migration and corrected immediate retry after a failed first installer conversion. Publication did not change the developer's .33 installation or live data.

All four assets were uploaded to a draft and their GitHub SHA-256 digests/sizes verified before publication. All four public download URLs then returned HTTP 200 with exact expected lengths without authentication. Existing five releases, their descriptions, names, publication state and asset IDs/digests/URLs/labels were compared before and after and preserved. Main was fast-forwarded and a new .34 tag created. The repository About URL now opens this release. An independent GitHub connector read confirmed the published prerelease and asset digests.

[Publication receipt](verification/nexa034/publication-receipt.json) · [Public downloads](verification/nexa034/public-downloads.json) · [Asset identities](verification/nexa034/release-identity.json) · [SHA256SUMS](verification/nexa034/SHA256SUMS.txt) · [Implementation status](NEXA_034_STATUS.md) · [Test results](NEXA_034_TEST_RESULTS.md) · [Data/installer guide](NEXA_034_DATA_AND_INSTALLER.md).

Physical flicker, mixed-DPI/multiple-monitor, precision-trackpad and interactive wizard/repair acceptance remain open. This is not Beta. Packages are unsigned; the NuGet vulnerability feed was unavailable during packaging. Private-data migration preserves verified legacy originals. Uninstall preserves data by default; removal requires explicit confirmation. Backup ZIPs exclude managed credentials. Older .24/.26 updaters may require a direct installer download once.
