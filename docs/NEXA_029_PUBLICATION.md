# nexa v0.0.29 Alpha — NEXAWEEN: Interface Polish prerelease

Published with the user's explicit authorization on October 28, 2026, 5:01 PM America/Chicago (2026-10-28T22:01:16Z).

- Release: https://github.com/scrmarsh/nexa/releases/tag/v0.0.29-alpha
- GitHub release ID: 427233210; prerelease=true; draft=false. It was explicitly published with make_latest=false.
- Release tag: v0.0.29-alpha, targeting 496dea2db01d4d59832febe5780dacebadd1e88f.
- Source branch: codex/nexa-029-polish. Existing main remains a308076fba1264f4bb64e53f4610e52caced3eab. Historical tags/releases were neither changed nor deleted.
- Release focus: interface polish, typography, branding, Space icon placement, fullscreen spacing/navigation, independent wallpapers and widget readability. Supporting session/history reliability and remaining acceptance items are described in the release notes.

## Download and provenance

Nexa.exe is a self-contained portable Windows x64 executable, 145,433,398 bytes. File version 0.0.29.0; product version 0.0.29-alpha+496dea2db01d4d59832febe5780dacebadd1e88f. No separate .NET runtime installation is needed.

Executable SHA-256:

```text
badbdb24fc0345cc6ed00c27cc873bb92068f27194ca23f25452cba0f57cd9cd
```

Assets: Nexa.exe and SHA256SUMS.txt. Both uploaded to a draft, with GitHub-reported byte count and SHA-256 verified before publishing. An unauthenticated public API request confirms the prerelease flags; the public executable URL returns HTTP 200 and matching Content-Length. The release tag's peeled commit was verified through git ls-remote.

## Verification

Final application/checks Release build: zero warnings/errors. Final self-contained publish: no warnings/errors. All 19 regression routes pass 786 distinct assertions on release commit 496dea2. Logs are retained under docs/verification/nexa029/prerelease/; full execution directory was verification/acceptance-20261028-165210.

Packaged executable --verify-startup passed using a disposable workspace: x64, 759 initialized pixels, four presets, and embedded Bible resources NLT 31,062 / CSB 31,086 / NKJV 31,102 verses. This did not access the live workspace. Packaged startup JSON is included in the evidence directory.

Preparation failures and recovery: the first normal-output build encountered the already running development executable, so validation moved to bin/Nexa029Release without closing it. An initial sandbox publish reported unavailable NuGet vulnerability metadata; an online restore and final clean publish succeeded. A sandboxed packaged-startup probe timed out; the isolated probe outside that restricted sandbox passed. GitHub reset the first metadata connection before any push; a bounded retry succeeded. The final upload and publication completed successfully. These failures are not hidden or counted as passing tests.

Physical mixed-DPI/multi-monitor/focus and hardware transitions, live provider authentication, and long-session performance remain open acceptance items. This is an Alpha prerelease focused on interface polish, not Beta acceptance. The original pre-publication audit/status reports are retained as historical evidence; this user-authorized publication supersedes their earlier no-release restriction.

This publication/evidence commit follows the tagged source without altering the release tag or executable source.
