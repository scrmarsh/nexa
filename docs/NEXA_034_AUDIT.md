# nexa .34 pre-implementation audit

October 29, 2026. Real root `C:/Users/Sean Marsh/Documents/ChatGPT/Nexa/continuation-source`. Starting clean HEAD `19132d2cbba1e52c59aea964647deb2d48657174`; branch `codex/nexa-034-refinement`; origin `https://github.com/scrmarsh/nexa.git`. .33 is committed and published; reviewed its status, detailed tests, installer guide and publication receipt. Baseline: 25 routes / 1,136 assertions, 31 native lifecycle + 30 upgrade/migration + 17 packaged checks. Earlier laptop/game/audio fixes remain ancestors. No AGENTS.md found. No .34 publication, remote edit, live install or live-data migration authorized.

| Brief | Initial findings / work |
|---|---|
| 1 version | Advance shared csproj/manifest identity to .34; preserve historical Alpha map and Beta transition. |
| 2 flicker | NexaNavigation explicitly paints source and target black, fades source before constructing target, sets target content opacity zero and fades it later. This intentionally exposes blank intermediate states. Keep the fully rendered source visible until a fully laid-out target can replace it; physical acceptance remains a Beta gate. |
| 3–5 Appearance | Four official palettes already exist. Remove .32 restore button/preset tiles; Home Default currently inherits global background brushes. Restore exact logo Blue/Purple from SVG resources. Move the seasonal switch to header; current OFF track is red. |
| 6–9 settings | Appearance hidden-bar physical scrolling exists; Profile/Sounds/Bugs use Auto. New clock default already false (12-hour); preserve saved true. Several sliders omit move-to-point. |
| 10–12 ecosystem | Friendly selector and advanced selector compete; historical Creepy enum must remain readable. Store loads persisted actors/webs; current transient reset is not universal on every entry. Audit combat gates and silk geometry/cooperation before altering simulation. |
| 13 timer | Menu explicitly offers duplicate progress and remove Pixel Timer. Enforce one permanent timer while preserving saved progress data/configuration. |
| 14–15 branding | Classic Splash.CompleteTagline makes loading/stage visible; Home uses empty Classic NexaBranding.Tagline and collapses the right column, shifting logo. Keep layout columns stable and restore Classic typing. |
| 16 Hedy | The repository has a genuine separate Hevy workout integration; do not rename its identifiers/endpoints to Hedy. Keep it separate and remove duplicate Hedy welcome actions where present. |
| 17 data | Canonical root remains LocalAppData/SessionDashboard; Documents/nexa contains readable exports. .34 explicitly changes the prior user choice. Implement verified restart-safe migration and preserve originals until validation; do not run against developer data during tests. |
| 18 export/delete | Missing. Need consistent owned-root ZIP, secret exclusion/manifest/hash verification, typed confirmation/countdown and save shutdown. Tests must use isolated stores only. |
| 19–20 updates | Current UI exposes channel choice; discovery filters by stage rank. Retain consent and safe restart, remove visible channel, add verified installer repair. |
| 21–26 installer | Inno 7.1.1 current-user .33 installer exists with tested repair-by-rerun, data-preserving uninstall, rollback and guarded long paths. Remove optional full-drive crawl/default cleanup, add bounded discovery/report/progress, independent shortcut tasks, integrity manifest and a true repair choice. |
| 27 Profile | Current large preference form exposes onboarding answers. Replace with real recorded activity metrics and concise summaries without deleting saved answers. |
| 28–31 acceptance | Extend established suite, clean Release/publish both artifacts, native isolated installer/export/migration checks and WPF captures. Physical flicker/trackpad/mixed-DPI remain unverified until exercised. Local Alpha only; no pushes/releases/Beta/live uninstall. |

All sections remain tracked through implementation and the final status must explicitly identify partial or physically unverified work. Existing private credentials must never enter an ordinary backup ZIP. Deletion and migration paths must reject traversal/reparse points and avoid binaries/unrelated personal files. Genuine provider names, permanent scores and history are preserved.
