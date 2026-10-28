# NEXA — COMPLETE FEATURE AUDIT & AUTOMATIC REPAIR
## v0.1.25 Alpha Development Candidate

**Project:** Nexa / SessionDashboard  
**Repository:** https://github.com/scrmarsh/nexa  
**Local directory:** `M:\Nexa`  
**Previous version:** 0.1.24 Alpha  
**Next planned version:** 0.1.25 Alpha  
**Existing repair branch:** `codex/nexaween-final-024`

# PRIMARY OBJECTIVE

Perform a complete audit of the Nexa application, compare the existing source code against every available feature requirement, and **automatically implement or repair everything missing, broken, or partially completed**.

Do not stop after writing an audit report.

Continue into implementation, testing, and candidate packaging.

The objective is a reliable, polished Nexa desktop application that preserves all existing functionality while completing the outstanding Nexaween features.

Do not unnecessarily rewrite functional systems.

Prioritize working functionality, data safety, and application stability over decorative complexity.

---

# PHASE 1 — REPOSITORY SAFETY AND DISCOVERY

Before making changes:

1. Inspect the current Git working tree and active branch.
2. Identify all uncommitted changes and preserve them.
3. Review the existing `codex/nexaween-final-024` repair branch.
4. Review the recovery branch `codex/recovery-before-final-024`.
5. Preserve all previous repair work.
6. Create a new development branch for the 0.1.25 candidate from the correct latest source.
7. Establish a safe recovery commit or branch before major changes.
8. Inspect existing tests, build procedures, and packaging scripts.
9. Preserve the separate Aevoria project.

The repository is now PUBLIC.

Audit tracked files and relevant Git history for accidentally exposed:
- Passwords.
- API keys.
- OAuth credentials.
- Personal access tokens.
- Connection strings containing secrets.
- Personal data.
- Private session records.
- Authentication credentials.

Never print secret values in logs or reports.

Report possible exposures and recommend revocation and replacement. Do not assume deleting a secret in a later commit removes it from Git history.

Do not change repository visibility.

---

# PHASE 2 — COMPREHENSIVE FEATURE AUDIT

Review the entire application, including:

- Source files.
- README.
- CHANGELOG.
- Development documentation.
- Implementation reports.
- Verification reports.
- Relevant historical requirements.
- Existing Git branches.
- Previous release notes.
- Automated tests.
- Existing application assets.
- Saved-data compatibility code.

Create:

`docs/FEATURE-AUDIT-0.1.25.md`

Audit these categories:

1. Homepage and saved Spaces.
2. Session management.
3. Pixel Timer and Progress Bar.
4. Autosave and session history.
5. Dashboard layout and widgets.
6. Productivity tracking.
7. Settings navigation.
8. Appearance and theme switching.
9. Nexaween branding.
10. Halloween decorations.
11. Bug overlays and behavior.
12. Sounds and audio.
13. Notifications.
14. Integrations.
15. GitHub updates.
16. Splash screen.
17. Window rendering and borders.
18. Performance and accessibility.
19. Application packaging and distribution.
20. All other documented features.

For each requirement, document:

- Required behavior.
- Current behavior.
- Source files.
- Test coverage.
- Actual verification evidence.
- Missing functionality.
- Root cause where identifiable.
- Repair priority.

Use the statuses:

- COMPLETE AND VERIFIED.
- IMPLEMENTED BUT UNVERIFIED.
- PARTIALLY IMPLEMENTED.
- MISSING.
- BROKEN.
- REQUIRES MANUAL TESTING.
- UNKNOWN REQUIREMENT.

A feature is not complete merely because a control, method, or automated test exists.

If original requirements are unavailable, do not invent them.

After the initial audit, immediately begin repairing all documented missing or broken requirements.

---

# PHASE 3 — CRITICAL RELIABILITY REPAIRS

## 1. History and Data Preservation

Verify and repair:

- History opens without crashing.
- Existing session records remain readable.
- Archived notes are preserved.
- Session logs display available data changes.
- History search and filtering work.
- Corrupted files are handled safely.
- Recovery never overwrites unreadable original data.
- Historical records survive application updates.
- Existing Spaces and presets remain intact.

Test against isolated copies of existing user data.

Never perform destructive tests on the live workspace.

## 2. Pixel Timer

Verify and repair:

- Manual Mode.
- Schedule Mode.
- Reset button appears only in Manual Mode.
- Schedule Mode follows recurring weekdays automatically.
- Active sessions count UP.
- Between-session periods count DOWN.
- Pixels fill from the top-left during sessions.
- Pixels clear from the bottom-right between sessions.
- No stray or incorrectly illuminated pixels.
- Schedule edits immediately update the timer.
- Correct behavior when opening during an active class.
- Clock seconds remain synchronized.
- Linked Progress Bars show matching progress.
- Percentages display three decimal places.
- Correct time-zone and daylight-saving behavior.
- Timer recovers after restart and hibernation.
- Time selector scrolling is not overly sensitive.

## 3. Autosave and Session Reliability

Verify:

- Session records begin saving immediately.
- Event-driven saving works.
- Configurable autosave intervals work.
- Manual Save Now works.
- Save failures do not erase user data.
- Session reset warnings appear at the correct time.
- Post-session review remains possible.
- Pre-session resets preserve permanent data.
- Hibernate and restart recovery work.

Prioritize repairs that could cause lost work.

---

# PHASE 4 — SETTINGS AND HOMEPAGE

## 4. Unified Settings

Fix:
- Duplicate Settings windows.
- Broken Appearance navigation.
- Disabled Appearance tab.
- Incorrect sidebar ordering.
- Nonfunctional dropdown selections.
- Missing borders.
- Incorrect window positioning.

Settings should open centered, remain draggable, and preserve their position while switching tabs.

Reopening Settings should recenter the window.

General settings:

1. Appearance.
2. Sounds.
3. Time Zone & Clock.
4. Widgets.
5. Updates, after verification.

Experimental Features:

1. Profile.
2. Integrations.
3. Account & Data.
4. Add Bugs.

## 5. Session Settings

Keep the purple Session toolbar button as the single primary entry to session configuration.

The Session window should contain only session-related settings.

Do not include:
- Main Settings sidebar.
- Time Zone & Clock tab.
- Nexaween pumpkin.
- Duplicate Nexaween toggle.

Keep Notes & History available through the Session interface.

Move Session Timeline summary interval controls into the Session Timeline widget.

Clicking a tip should navigate to the Enable Tips setting in the Session interface.

## 6. Appearance

Show:

**Appearance**

**Nexaween [ON/OFF]**

Use a neon-green Nexaween label.

Keep only one seasonal toggle.

When enabled:
- Show seasonal decorations.
- Apply Nexaween branding.
- Temporarily hide ordinary appearance customization.

When disabled:
- Restore normal styling.
- Remove Halloween decorations.
- Show normal customization controls.

Do not disable access to the Appearance tab itself.

## 7. Homepage

Complete the homepage layout:

- One centered Create New Space button.
- Display Recent Spaces beneath it.
- Show six Spaces in the initial area, arranged in two columns of three rather than three extra-wide cards.
- Each Space card should be approximately half the previous width, with appropriate spacing.
- Provide Show More Spaces when necessary.
- Reveal additional rows in consistent increments.
- Make the homepage scrollable.
- Keep all existing saved Spaces accessible.
- Remove the standalone Changelog button.
- Keep Changelog inside About Nexa.

## 8. Window Borders

Repair missing colored border segments and corners on:

- Settings.
- History.
- Changelog.
- About Nexa.
- Other affected windows.

Borders must remain continuous when moved or resized.

---

# PHASE 5 — BRANDING AND SPLASH SCREEN

## 9. Nexaween Logo

The Halloween logo currently has an incorrect dark-purple section in its top-left area.

Replace that specific section with the existing Nexaween neon green.

Preserve the intended neon-purple elements.

Apply the corrected artwork consistently to:

- Homepage logo.
- Splash screen.
- Top-left window icon.
- Packaged executable icon.
- Other application branding.

Do not approximate or randomly recolor the entire logo.

## 10. Normal Logo

When Nexaween is disabled, restore the exact original blue-and-purple logo from version 0.1.22.

Use the original assets or historical source as the authority.

No green-and-blue hybrid logo should appear in normal mode.

Check:
- Homepage.
- Splash screen.
- Window title icon.
- Executable resource.
- Relevant shortcuts.

Handle the distinction between runtime theme switching and Windows executable icon resources. If Windows icon caching or packaging imposes limits, document and address them appropriately rather than pretending the shell icon changes instantly.

## 11. Tagline

The Nexaween tagline must be exactly:

**a space for whatever haunts you next**

Requirements:
- All lowercase.
- No "Nexa" prefix.
- No dash.
- No period.
- Correct spacing.

The word **haunts** must type in orange from its first letter.

Do not type it white and recolor it afterward.

Preserve the existing homepage typing animation.

## 12. Splash Screen

Fix flickering, incorrect colors, and inconsistent startup behavior.

Required sequence:

1. Splash screen displays the correct logo immediately.
2. Hold the logo-only splash for three seconds.
3. During those three seconds, do not display the loading track.
4. After the hold, reveal the thin white progress track.
5. Fill the track using the correct theme accent and actual initialization progress.
6. Open Nexa once required initialization completes.

Do not generate fake loading percentages.

Do not unnecessarily delay actual startup work beyond the requested splash hold.

Use reliable monotonic timing and verify the delay within normal rendering tolerances.

---

# PHASE 6 — NEXAWEEN DECORATIONS

## 13. Seasonal Background

Apply the Halloween decoration system consistently to:

- Homepage.
- Dashboard.
- Appearance Settings.

Use these ten decoration types:

1. Candy corn.
2. Pumpkins.
3. Chocolate bars.
4. Trick-or-treat baskets.
5. Gummy worms.
6. Wrapped hard candies.
7. Witch cauldrons.
8. Witch hats.
9. Ghosts.
10. Realistic veiny eyeballs.

Candy should be most common.

Remove generic green stars, balls, and unrelated decorative shapes.

Decorations should:
- Fade in.
- Remain for approximately 5–10 seconds.
- Fade out.
- Reappear in new positions.
- Avoid synchronized transitions.
- Stay behind interactive controls.

## 14. Pumpkins and Vines

Implement large, realistically shaded pumpkins.

Use:
- Deep orange shading.
- Natural grooves.
- Thick stems.
- Leaves.
- Organic vines.
- Curling tendrils.

Do not use jack-o'-lantern faces for the main decorative pumpkins.

Place pumpkins on approximately two or three selected dashboard widgets.

Anchor them to widget corners.

Vines should:
- Begin thick and substantial.
- Extend around widget edges.
- Gradually grow.
- Become more translucent at their outer extremities.
- Remain aligned when widgets move or scroll.

Bugs should be able to crawl on the pumpkin surfaces where supported.

Reuse matching pumpkin artwork in appropriate menu corners.

## 15. Permanent Decorations

Homepage:
- Realistic static upper-left spider web.
- Static Halloween-costumed spider.
- Large bottom-right shaded pumpkin.
- Extensive decorative vines.

These should remain separate from the randomly fading decorations.

Do not obstruct controls.

---

# PHASE 7 — BUG SYSTEM

## 16. Remove Standalone Ecosystem

Retire the separate Living Worlds environment.

Keep bugs exclusively as dashboard/widget overlays.

Preserve reusable creature rendering, behavior, web systems, and squash statistics.

Do not erase historical data.

## 17. Bug Controls

Bug Type:
- All Bugs.
- Spiders.
- Ants.
- Ladybugs.
- Roaches.
- Centipedes.
- Flies.

Density:
- Low.
- Normal.
- High.
- Infestation.

Behavior:
- Classic.
- Cozy.
- Combative.

Appearance:
- Classic.
- Creepy.
- Halloween.

Adorable Mode:
- Independent ON/OFF toggle.

Allow every behavior and appearance combination.

## 18. Infestation

Implement Infestation without a fixed species-count cap, while monitoring resource use.

Target at least approximately 20 FPS.

Apply adaptive spawn throttling and simulation simplification to prevent crashes, excessive memory consumption, or complete UI unresponsiveness.

## 19. Bug Distribution and Collisions

Fix bugs overlapping excessively.

Implement:
- Personal-space collision boundaries.
- Soft avoidance.
- Species-appropriate clustering.
- Several distributed colonies or groups.
- Multiple exploration destinations.
- Movement throughout the dashboard.

Bugs should enjoy clustering without stacking into an indistinguishable pile.

Do not let every bug converge on one location.

Avoid jitter when resolving collisions.

## 20. Spiders and Webs

Fix webs that are stretched vertically.

Webs should have:
- Balanced, approximately circular overall proportions.
- Irregular realistic geometry.
- Visible thin strands.
- Correct scaling.
- Stable appearance during scrolling and resizing.

Spiders should:
- Build their webs visibly.
- Stay near associated nest or web areas when appropriate.
- Occasionally explore.
- Return to their home area.

Webs must not disappear beneath widget surfaces.

## 21. Creature Visual Effects

Remove all remaining distracting yellow targeting circles around bugs.

When damaged:
- Flash the creature red briefly.
- Restore its normal colors afterward.

In Adorable Mode:
- Show sad expressions.
- Display brief animated tears when squashed.

Remove the full-window green glow triggered by bug squashes.

Preserve green feedback for unrelated positive productivity actions.

## 22. Cozy Candy Behavior

Bugs should:
- Explore candy.
- Occasionally approach it.
- Perform short eating interactions.
- Consume the candy.
- Trigger replacement candy spawning.

Coordinate consumption with the background decoration lifecycle.

## 23. Squash Statistics

Redesign Bugs Squashed.

Show a small illustration of each species, with its squash count above its head.

Track:
- Spiders.
- Ants.
- Ladybugs.
- Roaches.
- Centipedes.
- Flies.
- Overall total.

Preserve all historical squash counts.

---

# PHASE 8 — PRODUCTIVITY WIDGET CONSOLIDATION

## 24. Combine Productivity Meter and Productivity Score

Remove the need for two separate productivity widgets.

Create one unified widget called:

**Productivity**

Combine:
- Productivity Score.
- Productivity Meter.
- Meaningful action tracking.
- Recent productivity events.
- Category breakdown.
- Score history.
- Bug streak multiplier.
- Productivity milestones.

Provide compact and expanded layouts.

The main score and lightning bolt should remain visually prominent.

When upgrading existing dashboards:
- Preserve old Productivity Meter data.
- Preserve Productivity Score data.
- Migrate existing widgets safely.
- Prevent duplicate widgets and duplicated scoring.
- Preserve historical reports.

## 25. Streak Multiplier

Direct bug squashes activate a two-minute streak.

Additional squashes:
- Increase multiplier by 0.5.
- Refresh the active duration.

Use an appropriate maximum multiplier.

Display a centered temporary productivity popup during active streaks.

Show:
- Current score.
- Active multiplier.
- Recent point gains.
- Fire animation.

After the streak expires:
- Stop applying bonus points immediately.
- Visually decrease the multiplier in 0.5 steps.
- Fade out the popup.
- Preserve all earned points.

Do not create duplicate popups.

---

# PHASE 9 — SOUNDS AND INTEGRATIONS

## 26. Sounds

Verify:
- Dedicated Sounds tab.
- Sound explanations.
- Test Sound button above volume.
- Volume controls.
- Pop effect.
- Positive angelic cue.
- Negative eerie cue.
- Sound preview controls.
- Proper mute behavior.

## 27. Integrations

Use this order:

Working integrations:
1. Todoist.
2. Hevy.

Then a Coming Soon section:

3. Google Tasks.
4. Google Calendar.
5. Outlook Calendar.

Preserve credentials.

Verify whether the existing integration is actually named Hevy or Hedy in code and documentation. Do not conflate two distinct services; report any ambiguity and retain working functionality.

---

# PHASE 10 — GITHUB UPDATE SYSTEM

## 28. Public GitHub Releases

Repository:

https://github.com/scrmarsh/nexa

The repository is PUBLIC.

Fix the existing 404 update-check problem.

The updater must support:
- Public repositories.
- Alpha prereleases.
- Release discovery.
- Version comparison.
- Asset identification.
- Download verification.
- Installation.
- Backup.
- Rollback.
- Restart.

Do not use `/releases/latest` as the only discovery mechanism, because it excludes prereleases.

Do not require GitHub authentication for public release assets.

Handle GitHub errors and rate limits appropriately.

## 29. Safe Update Installation

The application should:
1. Check for available updates.
2. Display the latest compatible version.
3. Show release notes.
4. Offer Download and Install.
5. Download the verified release.
6. Back up important local user data.
7. Use an external updater process if necessary.
8. Replace outdated application files safely.
9. Preserve local settings and session history.
10. Prompt for restart.
11. Launch the updated application when requested.

Do not silently install updates without user approval.

Do not provide a downgrade/version manager.

Do not overwrite the currently running executable directly.

Implement and test recovery from interrupted or failed installation.

Test with a safe isolated installation and a suitable test release. Do not modify or publish production GitHub releases during testing.

---

# PHASE 11 — PERFORMANCE, TESTING, AND VERIFICATION

## 30. Automated Tests

Run existing automated tests and add regression coverage for repaired features.

Test:
- History and recovery.
- Autosaving.
- Pixel Timer.
- Session resets.
- Theme switching.
- Settings.
- Homepage layout.
- Window rendering.
- Bug behavior and population.
- Productivity scoring.
- Update discovery and installation.
- Data migration.

Do not rely exclusively on static code checks.

Exercise real functional paths wherever feasible.

## 31. Manual Acceptance

Prepare a concise manual checklist for:
- Startup.
- Window dragging.
- Actual mouse-wheel behavior.
- Multi-monitor handling.
- DPI scaling.
- Fullscreen/taskbar.
- Audio playback.
- Windows sleep/hibernate.
- Live integrations.
- Extended performance.
- Visual correctness.

Do not claim these tests passed unless they were actually performed.

---

# PHASE 12 — CLEAN BUILD ORGANIZATION

## 32. One Authoritative Builds Folder

The project currently contains numerous Nexa.exe files in different directories.

Standardize user-facing build output under:

`M:\Nexa\Builds`

Structure:

`Builds\Testing`

`Builds\Latest`

`Builds\Archive`

Rules:

**Testing**
- Contains new unapproved development candidates.

**Latest**
- Contains only the newest verified and approved build.

**Archive**
- Contains previous approved builds retained for development recovery.

Do not automatically delete old builds or overwrite existing files before verifying the new build.

Internal compiler-generated files can remain in normal .NET `bin` and `obj` locations; these are not additional user-facing release directories.

## 33. Candidate Build

After repairs and automated verification, publish a self-contained Windows x64 candidate to:

`M:\Nexa\Builds\Testing\Nexa.exe`

Verify:
- Executable exists.
- Executable launches in an isolated test environment.
- Version metadata is correct for the candidate.
- Required assets are present.
- Source commit is recorded.
- No unexpected external runtime dependency is introduced.

Do not replace `Builds\Latest` until the candidate is approved.

Do not replace the existing root executable automatically.

Do not publish a GitHub release.

---

# PHASE 13 — FINAL REPORT

Produce:

`docs/FEATURE-AUDIT-0.1.25.md`

`docs/REPAIR-REPORT-0.1.25.md`

`docs/MANUAL-TESTING-0.1.25.md`

The report must include:

1. All requirements reviewed.
2. Previously completed features.
3. Missing features discovered.
4. Broken features discovered.
5. Root causes identified.
6. Repairs implemented.
7. Remaining limitations.
8. Automated tests performed.
9. Build and packaging results.
10. Manual tests still required.
11. Files changed.
12. Candidate executable path.
13. Git branch and source commit.
14. Recommended next steps.

Do not claim the update is complete if any major requirements remain unfinished.

---

# FINAL INSTRUCTIONS

This task is not complete after producing documentation.

After auditing, automatically implement the outstanding requirements in priority order.

Use separate logical commits and rerun tests after significant changes.

Prioritize:

1. Data safety and crashes.
2. Timer and scheduling correctness.
3. Broken settings and navigation.
4. GitHub updater.
5. Productivity widget consolidation.
6. Branding and splash fixes.
7. Bug behavior and rendering.
8. Halloween decorations.
9. Advanced animations.
10. Final performance optimization.

Preserve all user data and existing working functionality.

Do not finalize the changelog or publish v0.1.25 yet.

Deliver a tested candidate executable for manual approval.

**The goal is to make Nexa genuinely functional and complete—not merely make the automated test suite pass.**

# END OF PROMPT