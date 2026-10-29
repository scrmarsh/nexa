# Canonical Alpha identities and published-release migration

October 28, 2026. Read-only live GitHub audit; **no remote changes performed**. See [captured metadata](NEXA_031_RELEASE_SNAPSHOT.json) and [exact proposed API payloads](NEXA_031_REMOTE_CHANGE_PLAN.json).

| Published release ID | Existing / legacy tag | Canonical tag to add | Original commit | Current title |
|---|---|---|---|---|
| 425758058 | `v0.1.24-alpha` | `v0.0.24-alpha` | `4562d696f39e181e180d1b3b93fc4d060b941dce` | `nexa v0.0.24 alpha — NEXAWEEN` |
| 426690043 | `v0.1.26-alpha` | `v0.0.26-alpha` | `a308076fba1264f4bb64e53f4610e52caced3eab` | `nexa v0.0.26 alpha — Spaces in Motion` |
| 427233210 | `v0.0.29-alpha` | Already canonical | `496dea2db01d4d59832febe5780dacebadd1e88f` | `nexa v0.0.29 Alpha — NEXAWEEN: Interface Polish` |

The live .24/.26 titles were already corrected. Their associated tags/descriptions still contain legacy identities. Remote matching-refs confirmed canonical .24/.26 aliases were absent. .29 is an annotated tag (`28f237e13101043855997660a20c03df5e8510e6`) resolving to the commit above. Preserve the annotated object and all old refs.

## Approval-ready, compatibility-preserving remote plan

1. Re-read releases/assets/refs and compare them with the prepared `expectedBefore` values. Stop if anything changed.
2. Create the two **new** canonical Git refs at their original commits. Never update/delete/force-push existing refs.
3. PATCH only the .24/.26 release bodies with the exact prepared descriptions. Their names already match the requested canonical titles. Preserve release IDs, associated legacy tags, publication times, assets and checksums.
4. Keep the published releases associated with the legacy tags so old binary-download URLs remain valid. A Git ref alias does not create a duplicate public release or guarantee a `/releases/tag/<canonical-alias>` release page. Do not advertise such a page as a working canonical download.
5. Verify IDs, all original URLs, original publication timestamps and asset digests after the approved mutation. Record results before considering any further migration.

Changing the existing release's `tag_name` would move its asset URLs. No legacy redirect guarantee was established, so that potentially disruptive operation is **not** part of this plan. Any future retagging needs a separately verified compatibility strategy and approval. The prepared JSON includes complete bodies and original asset metadata for concrete review. The brief's sections 3 and 29 require explicit approval before these published-release/tag operations.

## Asset identities that must remain unchanged

| Release | Nexa.exe asset ID | Bytes | SHA-256 |
|---|---:|---:|---|
| .24 | 682280754 | 145085238 | `d836de09b79e873e232ed6e9fae67f9e1f73d5d6b2a62efcce1ab31c2e75b420` |
| .26 | 685055223 | 145244982 | `1d08b896653932f0470bf09eef5464887bc9abb0c539e8e91ac549b227e0868c` |
| .29 | 686467578 | 145433398 | `badbdb24fc0345cc6ed00c27cc873bb92068f27194ca23f25452cba0f57cd9cd` |

The snapshot also records checksum-file asset IDs, digests and original created/published times. No old executable was modified or uploaded.

## Local version and link inventory

`NexaReleaseIdentity` centralizes assembly-derived current identity, stage, labels, canonical tags, channel ranking, explicit confirmed Alpha mappings and release parsing. Historical Alpha revisions 1–26 are mapped explicitly from `0.1.N` to `0.0.N`; unknown legacy-looking numbers are not silently rewritten. Genuine `0.1.N Beta` remains unchanged. `v0.1.0-alpha` is invalid; `v0.1.0-beta` remains reserved for the approved Beta.

The project/assembly/file/manifest target .31. Active README, changelog headings and in-app historical release notes use canonical Alpha identities. Release descriptors retain original raw package versions and original asset URLs for binary validation; visible updater labels use canonical versions.

Active public links deliberately remain:

- [.24 published release](https://github.com/scrmarsh/nexa/releases/tag/v0.1.24-alpha), [.24 existing executable](https://github.com/scrmarsh/nexa/releases/download/v0.1.24-alpha/Nexa.exe).
- [.26 published release](https://github.com/scrmarsh/nexa/releases/tag/v0.1.26-alpha), [.26 existing executable](https://github.com/scrmarsh/nexa/releases/download/v0.1.26-alpha/Nexa.exe).
- [.29 published release](https://github.com/scrmarsh/nexa/releases/tag/v0.0.29-alpha), [.29 existing executable](https://github.com/scrmarsh/nexa/releases/download/v0.0.29-alpha/Nexa.exe).

Those exact asset URLs are returned by the live release API. The cached .29 executable is separately verified against the published digest for packaged upgrade tests. Binary URL retrieval on a second computer remains manual acceptance.

The default remote README still advertises `v0.1.26`; local .31 README is corrected but not pushed. Old documentation filenames, original provenance/audit reports, test feeds and download paths retain legacy strings intentionally. Renaming them or blindly replacing every `0.1` would break links or alter historical evidence. The canonical mapping here supplies their corrected identity. The actual repository has no local `.github/workflows`; a live GET for that directory returned 404. No workflow publishing configuration was invented or changed.

No .31 release/tag, Beta, duplicate release, rewritten history, overwritten asset, approved build promotion or desktop shortcut replacement was performed.
