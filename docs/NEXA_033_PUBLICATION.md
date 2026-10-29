# nexa v0.0.33 alpha publication

Published as an **Alpha prerelease**: [nexa v0.0.33 alpha](https://github.com/scrmarsh/nexa/releases/tag/v0.0.33-alpha). Release ID `427482731`, source tag `v0.0.33-alpha` at `21d8869f9841885a5b139be78838f31e989d4258`. GitHub publication timestamp `2026-10-29T05:07:55Z`. No historical releases, assets, original binaries or tags were replaced.

**[Recommended Windows installer](https://github.com/scrmarsh/nexa/releases/download/v0.0.33-alpha/NexaSetup.exe)**. Portable, compatibility launcher and source/checksum links are secondary and clearly labeled. The public README points to this installer and the current verification report. GitHub-owned Assets/source controls cannot themselves be hidden by Markdown.

**Verification passed:** 25 routes / 1,136 assertions, 31 native lifecycle checks, 30 native upgrade/migration checks and 17 native portable-package checks. The installer payload and portable download are byte-identical application executables with updates retained in both. The uninstaller keeps private data by default; removing it requires explicit confirmation.

Application source `2f5c5e9bbef49dcfdba509fc10661e5050fc02be`; installer recipe `a46a7cd0fa5f0ae528d71f8aff7d263fa71e74ff`. The tag additionally contains the final harness, docs and native evidence. [Release gate](verification/nexa033/release-gate.json), [detailed results](NEXA_033_TEST_RESULTS.md), [publication receipt](verification/nexa033/publication-receipt.json).

| Asset | Bytes | SHA-256 |
|---|---:|---|
| NexaSetup.exe | 67234759 | `f872aaecd538471b62781ad0d62b1a386c8c78d9707f924f8b050a1363bbc1f4` |
| nexa-portable.exe | 72729689 | `5b2fb16b603a4cba7d0fc4b1c6cf7a8efc4631dda87648a5f46f4aad27a1b6d4` |
| Nexa.exe | 104802952 | `e57de0c2f7b4da1d7b719ae916d185d1a4e81d718fe4c7426527b6e455965c5f` |
| SHA256SUMS.txt | 242 | `40e65cb56447d40c16c94ee05a32781afbd00f092ff41ce07b2c563b13c7a432` |

All four upload digests and sizes were verified through GitHub before publication. Historical asset IDs/digests/URLs and publication timestamps were compared before making the new release public. The source push fast-forwarded main without rewriting its prior commits.

Known limits: real precision-trackpad feel and the complete monitor/DPI hardware matrix remain unverified. Windows packages are unsigned. Old .24/.26 executables need one direct installer download because their immutable comparison logic can reject canonical .33. Genuine published .29/.31 helpers and current portable conversion/rollback were exercised with isolated data. Historical metadata/alias cleanup remains a separate prepared operation; no destructive historical edits were performed.

The later main-branch documentation checkpoint records this receipt and corrects all secondary README download/build/test instructions. The audited source tag remains unchanged.

Unauthenticated public HEAD requests succeeded for all four download URLs with HTTP 200 and exact expected lengths. [Public-link verification](verification/nexa033/public-downloads.json). Independent connector reads confirmed the published prerelease, four asset digests and the repository About download URL.
