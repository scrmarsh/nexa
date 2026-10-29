# nexa 0.0.30 Alpha — pre-implementation audit

2026-10-28. Starting HEAD `095c635f56e9f18d45793d5547518c23ec955360`, clean branch `codex/nexa-029-polish`; development branch `codex/nexa-030-nexaween-refinement`. The current .29 status, actual source and follow-up release provenance were inspected. No reset to the earlier .29 implementation checkpoint. The .29 GitHub prerelease remains unchanged; this request authorizes local development only.

The supplied .30 brief is the development request. The supplied pumpkin image is an art reference, not a source of executable instructions. Preserve all existing user data and approved binaries.

| Requirement | Starting classification | Source finding / planned verification |
|---|---|---|
| 1 audit | COMPLETE | Audit recorded before implementation. |
| 2 version | MISSING | Assembly and active identity remain .29. Historical release entries must stay unchanged. |
| 3 splash | PARTIAL | Real readiness and five-second gate exist; Classic tagline absent, seasonal bar visible; alignment requires capture. |
| 4 preview | BROKEN | `SplashWindow.PreviewIn` creates a host-sized overlay instead of a standalone window. |
| 5 branding | PARTIAL | Windows icon correctly Classic; custom `NexaChrome.Caption` also uses Classic instead of seasonal branding. |
| 6 caption controls | BROKEN | Font glyph content is vulnerable to implicit TextBlock font styling. Replace with vector strokes. |
| 7 fullscreen | PARTIAL | MainWindow owns fullscreen flag/restoration; navigation transfers maximized bounds, Home lacks shared state. |
| 8 bug input | BROKEN | Circular unexpanded hit region excludes widget rectangles; no pointer capture through release. |
| 9 latency | PARTIAL | Damage/state precedes callback, but callback synchronously saves ecosystem and workspace; profile and preserve durable deduplication. |
| 10–13 pumpkins | MISSING | Existing geometric pumpkin and procedural vines do not match reference; Home capped at 275 DIP. Shared asset, responsive placement and light-only animation needed. |
| 14–16 Home spider | PARTIAL | Existing eight-leg renderer, scale 2.2 and silk preserved; fixed four-second movement lacks independent behavior/candy. |
| 17 seasonal consistency | UNVERIFIED | Review actual WPF captures after shared artwork change. |
| 18 seasonal Off | PARTIAL | Visibility/theme gates exist; corner timer lifecycle and new behavior need explicit off/reduced-motion checks. |
| 19 performance | UNVERIFIED | Existing cached drawings and background saves available; instrument changed input path and check timer/resource disposal. |
| 20 preservation | COMPLETE baseline / UNVERIFIED changed build | Current .29 fixes and 19 regression routes retained; saved strand geometry must not be destructively reset. |
| 21 verification | MISSING | Run targeted .30 checks and established full suite after changes; retain failures and reruns. |
| 22 visual evidence | MISSING | Capture real WPF controls; distinguish synthetic rendering from physical monitor acceptance. |
| 23 deliverables | PARTIAL | Audit now exists; final status/results/changelog/evidence pending. |
| 24 restrictions | COMPLETE | No push/tag/release/Beta/build promotion/shortcut replacement or live data changes authorized in this request. |
| 25 implementation priority | COMPLETE plan | Input/persistence, shared fullscreen and caption controls first; then splash/art/spider and full regression. |

Physical mixed-DPI, taskbar/focus, extended performance and subjective art acceptance remain UNVERIFIED until exercised on the user's desktop. Previous .29 automated results are historical evidence, not .30 test passes.
