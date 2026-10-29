# GitHub public-page version audit

Audited October 28, 2026 (America/Chicago). This audit made no remote changes. The previous approved release-body corrections and canonical tag aliases are live; they did not update default-branch files or repository settings.

## Scope and evidence

Fetched public `main` at `a308076fba1264f4bb64e53f4610e52caced3eab`. Inspected its complete tracked tree: **459 files, 436 decodable UTF-8 text files**. Searched those text files for `0.1.<number>`; **344 matching lines in 77 files**, including paths, original binary metadata, tests and archived evidence. These are occurrence counts, not 344 incorrect labels. Binary screenshots were not OCR-scanned, and authenticated/private project boards were not audited.

- [Complete path/line/text inventory](verification/nexa031/github-page-version-inventory.json).
- [Live repository/release metadata findings](verification/nexa031/github-page-metadata-audit.json).
- Public release collection: three releases, all Alpha prereleases (.24, .26, .29).
- Tag inventory: canonical .24/.26 aliases and canonical .29; legacy .24/.26 tags retained.
- Issue and pull-request collections: empty, including closed entries.
- No `.github` directory on main, no GitHub Pages site, and discussions disabled.
- Public wiki repository returned repository-not-found; no readable public wiki was available to audit.
- `/releases/latest` returns 404 while all releases are prereleases. Use the explicit newest Alpha release URL rather than treating that endpoint as an Alpha selector.

## Current public defects

| Surface | Verified finding | Correction needed |
|---|---|---|
| [README line 15](https://github.com/scrmarsh/nexa/blob/main/README.md#L15) | Calls `v0.1.26` the latest Alpha and links to .26, despite published .29. | Advertise **v0.0.29 Alpha — NEXAWEEN: Interface Polish**, with the existing canonical .29 release URL. Do not advertise local .31 as published. |
| Repository About website | Points to the historical `v0.1.24-alpha` release. | Point the website to the explicit .29 public release. Repository description and topics contain no old version numbers. |
| [Changelog](https://github.com/scrmarsh/nexa/blob/main/CHANGELOG.md) | Four headings at lines 5, 22, 27 and 38 still label Alpha .26/.25/.24 as `0.1.X`. No .29 entry on main. | Canonicalize Alpha headings to `0.0.X`; add the already-published .29 entry and clearly distinguish unpublished candidates. |
| [Seasonal banner](https://github.com/scrmarsh/nexa/blob/main/docs/media/nexaween-banner.svg) | SVG title line 2 and **visible subtitle line 17** say `v0.1.24 alpha`. The .24 release embeds this live main-branch image, so a corrected release body still displays old numbers inside its banner. | Change both SVG text values to `v0.0.24 alpha`; preserve artwork and path. The general `nexa-banner.svg` says “latest” and has no old numeric label. |
| [.24 repository release notes](https://github.com/scrmarsh/nexa/blob/main/docs/releases/0.1.24-alpha.md) | Image alt text and opening paragraph still say `0.1.24 Alpha`. | Canonicalize visible text. Keep the existing filename as a compatibility path, or add a documented canonical alias without breaking links. |
| [.26 repository release notes](https://github.com/scrmarsh/nexa/blob/main/docs/releases/0.1.26-alpha.md) | Heading still says `v0.1.26 alpha`. | Canonicalize heading to `v0.0.26 alpha`; retain existing links. |
| Linked implementation/testing/readiness reports | Headings in `IMPLEMENTATION-0.1.24.md`, `IMPLEMENTATION-0.1.26-alpha.md`, `TESTING-0.1.26-alpha.md` and `RELEASE-READINESS-0.1.26-alpha.md` still use old labels. README also sends its general verification link to the older .24 repair report. | Use canonical display identities and route current verification to evidence for the actual published build. Preserve exact historical product-version values and checksums, labeling them as original metadata. |
| Other navigable documentation | `BUILDS.md`, `DEVELOPMENT.md`, `INTEGRATIONS.md`, `ROOT-AUDIT.md`, .24 candidate status and .25 audit/repair/manual/tweak reports contain old version labels or historical status presented without enough context. Development guide still says the repository is private and .24 is current. | Update current guidance and visible canonical identities; mark historical reports with their date/scope and canonical identity rather than rewriting their original measurements or user requirements. |

## Corrected release surfaces and intentional old strings

All three public release titles and their primary release-description identities now use canonical `0.0.X Alpha`. The .24/.26 descriptions mention `0.1.X` only in their explicit compatibility explanation. They correctly state that the original tag is retained and the canonical alias points at the same original commit.

GitHub renders the **associated legacy tag name** beside each .24/.26 release, and their release pages, executable/checksum downloads and automatic source archives retain legacy-tag URLs. Adding a canonical Git tag does not change this badge or create a second release page. Removing those visible tag names requires changing release association, which can move asset URLs. That is outside the earlier approved compatibility-preserving corrections and must not be disguised as a simple display rename.

Keep original download paths, existing document paths, archived `LEGACY` material, verbatim requirements, original executable `ProductVersion` observations, test fixtures and commit/tag history intact. Genuine future Beta `0.1.X` numbering is valid. The inventory classifies 195 archive lines, 18 original-requirement lines, 35 source/test lines, 94 documentation lines and two visible-artwork lines; documentation matches include legitimate compatibility paths and original evidence.

## Cause and proposed correction scope

The default branch still ends at the .26 source commit. The .29 release exists on its release commit, while .30/.31 work and many documentation corrections remain on local development branches. Updating release API descriptions cannot merge those repository files. Consequently the GitHub homepage continues rendering the old README, changelog and banner.

A focused documentation/settings publication should update the README, About website, canonical changelog headings, seasonal SVG text and linked current documentation. Keep its application source at the existing main commit unless application publication/merge is separately intended. Do not push the entire local .31 branch merely to repair the public page, because it also contains unreleased application changes. Preserve legacy release associations and assets under the already approved compatibility strategy. Publish .31 separately only when explicitly requested.

This report records an audit, not a claim that the remaining public-page defects have been corrected.
