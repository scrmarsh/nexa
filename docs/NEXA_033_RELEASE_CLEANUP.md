# nexa .33 historical release audit

Read-only audit of all four published Alpha releases. Every executable was downloaded from its official asset URL, matched the GitHub SHA-256 digest and passed its source-confirmed isolated startup mode. None is demonstrated to be irreparably broken; no deletion is proposed. Legacy identity/comparator limitations are distinct from corrupt executables.

| Release ID | Existing tag | Published UTC | Embedded file version | Startup |
|---|---|---|---|---|
| 427365131 | v0.0.31-alpha | 2026-10-29T01:25:27Z | 0.0.31.0 | PASS |
| 426690043 | v0.1.26-alpha | 2026-10-28T13:21:02Z | 0.1.26.0 | PASS |
| 427233210 | v0.0.29-alpha | 2026-10-28T22:01:16Z | 0.0.29.0 | PASS |
| 425758058 | v0.1.24-alpha | 2026-10-27T17:14:22Z | 0.1.24.0 | PASS |

The API order was .31, .26, .29, .24. It is not canonical version order; .31 is the newest approved source/version until .33 is published. The updater compares canonical versions and filters package eligibility independently. Prereleases cannot be treated as ordinary stable latest releases; the README and release body provide an explicit installer link. No timestamp manipulation is proposed.

## Exact proposed historical operations

These operations are prepared only and require approval of the exact historical edits. The new .33 publication authorization does not require rewriting old assets or tags.

- Release 426690043 (.26): retain tag `v0.1.26-alpha`, asset IDs 685055223/685065709, original download URLs and embedded `0.1.26.0`; clarify that the canonical product identity is .26 Alpha. Its remaining `0.1.26` text identifies the legacy binary/tag; replace confusing prose with an explicit compatibility note, never falsely claim the binary embeds .0.26.
- Release 425758058 (.24): retain tag `v0.1.24-alpha`, asset IDs 682280754/682289562 and embedded `0.1.24.0`; clarify canonical .24 Alpha and preserve legacy download paths.
- Release 427233210 (.29): optional title case cleanup `NEXAWEEN` → `nexaween`; retain tag, assets and genuine Beta milestone references.
- Optional same-commit canonical alias refs `v0.0.24-alpha` and `v0.0.26-alpha` may be created after exact approval. Existing tag objects are not moved; do not create duplicate releases or pretend aliases repair immutable updater comparisons.
- No release or asset deletion, replacement, unpublication, force-push or historical commit rewrite.

## Remaining legacy occurrences

- `v0.0.31-alpha`: - Shared canonical version handling: **0.0.X Alpha**, future **0.1.X Beta**, and **1.X stable**. Confirmed historical Alpha versions are recognized without confusing them with Beta.
- `v0.1.26-alpha`: This release is **nexa v0.0.26 alpha**. The historical tag `v0.1.26-alpha` is retained so existing download links continue to work. Canonical Git tag `v0.0.26-alpha` points to the same original commit. Assets, SHA-256 checksums and original publication time are unchanged. Older numeric-only updaters need the documented one-time verified portable upgrade; this metadata correction cannot patch an already-installed comparator.
- `v0.0.29-alpha`: This Nexaween update focuses on polishing the interface: clearer text, more consistent branding, better fullscreen spacing and more useful widget layouts. It remains an Alpha prerelease; v0.1.0 is reserved for the first approved Beta.
- `v0.1.24-alpha`: [Implementation and verification report](https://github.com/scrmarsh/nexa/blob/main/docs/IMPLEMENTATION-0.1.24.md) · [Root audit](https://github.com/scrmarsh/nexa/blob/main/docs/ROOT-AUDIT.md)
- `v0.1.24-alpha`: This release is **nexa v0.0.24 alpha**. The historical tag `v0.1.24-alpha` is retained so existing download links continue to work. Canonical Git tag `v0.0.24-alpha` points to the same original commit. Assets, SHA-256 checksums and original publication time are unchanged. Older numeric-only updaters need the documented one-time verified portable upgrade; this metadata correction cannot patch an already-installed comparator.

The .24/.26 raw versions compare above .0.33 in their distributed code. Those users need the recommended direct installer once. A new launcher can support .29/.31 helper handoff, but it cannot change an old comparator that rejects the release before download. The new portable executable retains the corrected updater.

Evidence: [release snapshot](verification/nexa033/releases-before.json), [tag refs](verification/nexa033/tag-refs.json), [binary hashes/metadata](verification/nexa033/historical-binaries.json), [native startup](verification/nexa033/historical-startup.json). Original checksum assets and all asset IDs/digests/URLs are recorded in the snapshot. GitHub automatically shows an Assets list and source archives; custom release prose can collapse secondary links, but cannot hide GitHub-owned controls. The recommended Setup button is placed above that section.

References: [GitHub release API](https://docs.github.com/en/rest/releases/releases#list-releases), [GitHub latest release settings](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository).

All four downloadable checksum manifests were also fetched, matched their recorded asset digests and named executable hashes. [Manifest verification](verification/nexa033/historical-checksums.json). Tag-target author/committer dates and the .29 annotated-tag date are [recorded](verification/nexa033/historical-commit-dates.json). These dates do not explain every observed API ordering difference; no unsupported ordering guarantee is inferred. Explicit installer links and canonical updater comparisons determine the newest approved delivery.
