# nexa 0.1.25 Alpha — dashboard, bugs and seasonal follow-up

These requested tweaks are implemented and packaged as an **unapproved testing candidate**. The broader acceptance gaps in the original audit still apply; this is not a declaration that every prior master-prompt requirement is complete.

## Implemented

- Gradual arrivals: an initial quiet period, then individually paced arrivals with density-dependent jitter. Ordinary densities also stop spawning under frame pressure. Restored creatures enter simulation/rendering gradually while every dormant identity remains in authoritative saves. New arrivals spread throughout the visible area and explore for 16–28 seconds before Cozy gathering. Existing active jobs, loads and owned webs are retained.
- Compact tips: narrower responsive cards, reduced padding/type, no separate settings button. Clicking the card or using Enter/Space opens tip settings. Dismiss controls do not open settings. Important non-tip action buttons remain.
- Productivity: bottom-centered popup within the monitor work area, above the taskbar; existing saved scoring/expiry rules retained. Explicitly centered text and a vector lightning mark avoid the incomplete glyphs seen during scrolling previews.
- Squash gallery: six illustrated species at triple their previous natural drawing size, responsive three-column/two-row layout, enlarged minimum widget height and counts above the illustrations. Existing counters and historical unknown-species totals remain intact.
- Shared silk: widget-sized irregular strand networks replace the small radial web renderer. Spiders walk to attachment points and lay actual persistent strands as they move, with the visible filament trailing from the rear abdomen. Helpers work on independent strands of one web. Collaborators and strand progress persist; owners/helpers favor completed home webs while retaining exploration.
- Species behavior: ants march along actual widget edges while exploring. Only roaches may hide behind widgets. Flies seek landing surfaces and rest briefly before taking off. Existing ladybug/centipede movement and productive-action scoring remain.
- Seasonal water: orange fill and cap when Nexaween is enabled; normal blue water returns when disabled.
- Jack-o’-lanterns: a few larger corner attachments chosen from real widget rectangles, including the Pixel Timer. Placement tries free alternate corners rather than omitting all decorations when one corner is blocked. Reserved Add Widget space is protected independently. Shared pumpkin artwork now has a thick curved stem and jack-o’-lantern face, including settings artwork. Permanent home vines reach the bottom-left and top-right edges with looping tendrils and leaves.

## Safety and verification

| Check | Result |
| --- | --- |
| Release build | Passed, 0 warnings / 0 errors |
| Full thirteen-route regression | 439 passed; `verification/acceptance-20261027-204942` |
| Final focused/visual checks | 32 passed, including 3 additional gallery assertions; `verification/20261027-210651` |
| Distinct regression coverage | 442 passed across full run and focused followups; repeated checks counted once |
| Existing saved-data copy | 2 checks passed; 8 Spaces and 49 archived reports load without rewriting input; `verification/20261027-210258` |
| Final packaged startup | Passed; x64, PerMonitorV2, 759 square pixels, four presets and three embedded Bible resources; `verification/tweaks025-final-packaged` |
| Visual QA | Home vine scene, shared web and full/scrolled dashboard captures inspected. The final refreshed popup capture shows complete score and multiplier text. |

The first pacing test exposed newcomers leaving the viewport too soon; initial exploration targets are now constrained to the visible area. Test setup errors involving unassigned web jobs and pixel rather than normalized placement coordinates were corrected before the passing runs. No unresolved automated failure remains in the checks run.

Saved web fields are additive and bounded. Old web records remain readable; strand topology initializes from their recorded construction fraction. Pending arrivals stay in saves even if the application closes before they appear. No session reset, credential migration or live-user-data rewrite was performed by verification.

Physical long-duration frame-rate/memory behavior, mixed-DPI/taskbar layouts and real-person interaction acceptance remain manual. This report does not certify that every startup delay on the user's machine has been eliminated. Pumpkin/vine dedicated crawl surfaces, strict consolidation of dual legacy Productivity placements, the Hedy/Hevy provider ambiguity and the fixed executable shell-icon limitation remain outside this follow-up.

## Candidate

- Executable: `M:\Nexa\Builds\Testing\Nexa.exe`.
- Product version: `0.1.25+0f075afd98d5723dcc402c8852483422c0e230f5`.
- Source branch: `codex/nexa-025-audit-repair`.
- Source SHA: `0f075afd98d5723dcc402c8852483422c0e230f5`.
- SHA256: `34AF60183B25A495E191B7975AD72CBA4F4CBDF65053A8932C2581FD4C15B66A`.
- Size: 145,171,254 bytes; self-contained Windows x64 single-file application.
- Recovery source: `codex/recovery-before-025-tweaks`.
- Previous test executable and metadata retained under ignored `Builds/Testing/.previous`.

The approved Latest executable, root executable, public release, repository description and separate Aevoria project are unchanged. Candidate publication remains restricted to Testing; the publish script can stage beneath that directory to avoid overwriting a running app. This update does not publish a GitHub release.

## Changed files

- `BugOverlay.cs`
- `DashboardBugs.cs`
- `DrinkArtwork.cs`
- `MainWindow.SquashStats.cs`
- `NexaEcosystem.Arrivals.cs`
- `NexaEcosystem.Models.cs`
- `NexaEcosystem.Simulation.cs`
- `NexaEcosystem.Store.cs`
- `NexaNotifications.cs`
- `NexaSilk.cs`
- `Nexaween.Animation.cs`
- `Nexaween.Placement.cs`
- `Nexaween.cs`
- `ProductivityStreakOverlay.cs`
- `Publish-Exe.ps1`
- `SeasonalBackdrop.cs`
- `SettingsDialog.Navigation.cs`
- `Widgets.cs`
- `tests/Nexa024Runner.cs`
- `tests/NexaArrivalChecks.cs`
- `tests/NexaFollowupDesignChecks.cs`
- `tests/SessionDashboard.Checks.csproj`
- `tools/Verify.ps1`
