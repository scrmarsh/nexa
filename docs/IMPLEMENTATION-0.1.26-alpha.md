# nexa v0.1.26 alpha — implementation status

This is a **partial testing candidate**, not the complete master-prompt update, a beta, or a public release.

## Recovery and compatibility

- Development branch: `codex/nexa-026-alpha`.
- Recovery branch: `codex/recovery-before-026`, created from clean `c9947e4` before implementation.
- No separate Aevoria project, live workspace, credentials, approved Latest, root executable, or old Testing executable is intentionally modified by this work.
- `DashboardData.SpaceIcon` is an additive string. Old data defaults to an empty identifier and resolves a preset-specific vector fallback. Export/import carries it through existing allowlisted dashboard data. No destructive migration is introduced.

## Scope against the master prompt

| Area | Status | Actual delivery and remaining work |
| --- | --- | --- |
| Safety, branch, recovery | COMPLETE AND VERIFIED | Clean starting source, recovery branch, isolated fixtures. |
| Current version | COMPLETE AND VERIFIED | Assembly-driven 0.1.26 and lowercase alpha label; candidate metadata is alpha. Historical labels retained. |
| Navigation / performance | PARTIALLY IMPLEMENTED | File/model preparation moves off the dispatcher; already-prepared state is reused; DEV retains existing session/layout instead of resetting them; frozen vector logos are cached. Full long-session profiling remains. |
| Shared motion | PARTIALLY IMPLEMENTED | Black-backed view transitions, fades, staggered entrances, generation-based stale-fade protection, reduced-motion handling and navigation failure restoration. No complete color/border/crossfade library yet. |
| Homepage | PARTIALLY IMPLEMENTED | Paired Create/Import actions, six cards in two columns, all/six toggle and original six control identities retained. Card removal and height/scroll collapse are not animated. |
| Space icons | PARTIALLY IMPLEMENTED | Eleven vector identities, preset fallbacks, creation/setup picker, header picker, Home/header display and export/restart persistence. No searchable grid or recap integration yet. |
| Onboarding | PARTIALLY IMPLEMENTED | Redundant activity page skipped, use-case recommendations retained, rounded large name fields, final tool cards, icon selection, seasonal toggle, new-install On default and saved-preference preservation. Full redesigned selectable tool cards, focus glow and new sound design remain. |
| Interactive Quick Start | MISSING | No spotlight tutorial or new tutorial state has been added. |
| Settings | PARTIALLY IMPLEMENTED | Lazy page construction, requested general category order, bottom Experimental expander and removal of redundant Profile shortcut. Hedy/Hevy provider distinction remains unresolved; existing working provider code/credentials retained. Future providers have not been fully separated from the existing Integrations page. |
| Pixel Timer polish | PARTIALLY IMPLEMENTED | Irrelevant date/day controls collapse by mode instead of merely disabling them; existing timing regression retained. Separate remembered Manual/Schedule drafts and a complete editor transition are not delivered. |
| Productivity widget | PARTIALLY IMPLEMENTED | Compact horizontal, flame-free streak popup uses current score and multiplier; existing consolidated Productivity entry retained. New activity ring/counting layout remains. |
| Cumulative per-Space scoring | MISSING | Existing session/event scoring remains; no new cumulative baseline migration has been introduced. |
| User candy collection / scoring | MISSING | Existing bug/candy consumption remains; no new collectible user hit targets or shared candy scoring. |
| Bug initialization / separation | REQUIRES MANUAL TESTING | Existing paced arrival system retained and regression tested. No new population-distribution or web-proportion overhaul in this pass. |
| Theme / drinks | PARTIALLY IMPLEMENTED | Normal wine/beer/spirits colors now restore when seasonal mode is off; water retains its existing blue/orange switch. Global animated palette changes and synchronized richer-purple assets remain. |
| Pumpkins / vines / home spider | MISSING | No new v0.1.26 artwork or moving home spider; previous artwork remains. |
| Splash | PARTIALLY IMPLEMENTED | Visible version and progress track, no deliberate logo hold, animated tagline, monotonic completed-work milestones and interpolation. Initial workspace appearance is loaded before showing the logo to avoid wrong-color flashing; time to first splash render still needs profiling. |
| Tips | IMPLEMENTED BUT UNVERIFIED | 30-second rotation, five-second lifetime and close-button gap; clickable existing settings route retained. Timing/input still needs physical UI acceptance. |
| End Session / persisted recap | MISSING | Existing end-session controls remain unchanged. No new toolbar finalizer, visual pixel completion, cached recap or recap navigation. |
| Changelog | PARTIALLY IMPLEMENTED | New factual development entries and requested 0.1.24 display title. No incomplete tutorial/recap/candy features advertised as delivered. |
| Reliability | REQUIRES MANUAL TESTING | Existing history/autosave/reset/update suites retained; no live hibernation or credential-provider validation performed. |

## Testing and delivery

See [testing](TESTING-0.1.26-alpha.md) and [release readiness](RELEASE-READINESS-0.1.26-alpha.md). The master prompt is **not complete**. Missing tutorial, scoring and recap subsystems are release blockers against its acceptance criteria.

## Changed files

```
 M App.xaml.cs
 M CHANGELOG.md
 M DashboardCreation.cs
 M DrinkArtwork.cs
 M HomeWindow.About.cs
 M HomeWindow.Recent.cs
 M HomeWindow.cs
 M MainWindow.ClassSchedule.cs
 M MainWindow.Continuity.cs
 M MainWindow.Navigation.cs
 M MainWindow.Widgets.cs
 M MainWindow.xaml
 M MainWindow.xaml.cs
 M Models.cs
 M NexaChrome.cs
 M NexaLogo.cs
 M NexaMotion.cs
 M NexaNotifications.cs
 M OnboardingWindow.cs
 M ProductivityStreakOverlay.cs
 M ReleaseNotes.cs
 M SessionDashboard.csproj
 M SettingsDialog.Profile.cs
 M SettingsDialog.cs
 M SplashWindow.cs
 M tests/MenuRepairChecks.cs
 M tests/Nexa024Runner.cs
 M tests/Nexa025Checks.cs
 M tests/NexaFollowupDesignChecks.cs
 M tests/NexaweenMoreChecks.cs
 M tests/SessionDashboard.Checks.csproj
 M tools/Verify.ps1
?? MainWindow.SpaceIcons.cs
?? NexaNavigation.cs
?? SpaceIcons.cs
?? tests/Nexa026Checks.cs
```
