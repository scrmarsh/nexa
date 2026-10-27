# NEXA v0.1.24 ALPHA — NEXAWEEN
## Complete Codex Implementation & Bug-Fix Prompt

**Application:** Nexa  
**Release:** 0.1.24 Alpha  
**Release name:** NEXAWEEN  
**Seasonal tagline:** "A space for whatever haunts you next."

## CRITICAL INSTRUCTIONS

Inspect the actual Nexa WPF/.NET repository and implement the changes below.

This is an implementation request, not merely a planning exercise.

Preserve existing user data, presets, dashboard layouts, session notes, history, and integrations.

Fix actual root causes. Do not simply change labels or add visual placeholders.

Maintain a buildable application throughout the update.

### STRICT DEVELOPMENT ORDER

**PRIORITY 1 — Functional bugs**
1. Dynamic Pixel Timer behavior and synchronization.
2. Broken Settings controls and selection inputs.
3. Bug visibility, scrolling, and movement.
4. Session navigation and settings consolidation.
5. Autosaving and session history reliability.
6. Productivity Score and streak multiplier logic.

**PRIORITY 2 — Nexaween design**
1. Halloween theme and branding.
2. Redesigned settings.
3. Contextual decorations and candy.
4. Bug appearance and behavior.
5. Productivity widget redesign.
6. Real splash-screen loading progress.
7. Changelog and version metadata.

**PRIORITY 3 — Advanced animations**
1. Articulated bug locomotion.
2. Expressive eyes.
3. Spider web construction.
4. Flames and glow effects.
5. Pumpkins, ghosts, witches, and cauldrons.
6. Final performance optimization.

Do not prioritize elaborate animations over malfunctioning app features.

---

# PART 1 — PIXEL TIMER: HIGHEST PRIORITY

## 1.1 Make the Pixel Timer Fully Dynamic

The Pixel Timer currently fails to respond correctly when its session times or dates are edited.

Fix this at the underlying scheduling and progress-calculation level.

Whenever the user changes:
- Session start time.
- Session end time.
- Session date.
- Recurring weekdays.
- Time zone.
- Session duration.
- Selected preset.

The timer must immediately recalculate its current state.

Do not require restarting Nexa, reloading the widget, or manually resetting the timer.

Example:

If the user changes a session to 9:00 AM–10:00 AM and the current local time is 9:30 AM, the timer must immediately display 50% elapsed session progress.

If the current time is outside the session, show the correct countdown until the next scheduled session.

If the edited schedule includes today's current active time, automatically recognize that the session is already in progress.

## 1.2 Count Up During Sessions

The Pixel Timer must display progress accumulating during an active session.

At session start:
- 0% elapsed.
- No completed pixels.

As the session progresses:
- Add completed pixels.
- Increase displayed progress.
- Reach 100% at session end.

The visual progression must begin at the TOP-LEFT pixel.

Fill pixels:
- Left to right.
- Top to bottom.

The first completed pixel must be in the upper-left corner.

The last completed pixel must be in the bottom-right corner.

## 1.3 Count Down Between Sessions

When the active session ends, automatically switch to the intersession countdown.

During this phase:
- Begin with the grid filled.
- Remove pixels as the next session approaches.
- Remove pixels in reverse order.
- Begin clearing from the BOTTOM-RIGHT.
- Continue right to left and bottom to top.
- Reach an empty grid when the next session begins.

At that moment, automatically switch back to session count-up mode.

The timer must alternate continuously:

SESSION COUNT-UP → INTERSESSION COUNTDOWN → SESSION COUNT-UP.

Use distinct labels so users understand which phase is active.

Example:

"Session Progress — 50.000%"

"Next Session — 22h 15m Remaining"

## 1.4 Recurring Weekly Schedules

Support schedules such as:
- Monday/Wednesday.
- Monday/Wednesday/Friday.
- Tuesday/Thursday.
- Monday–Friday.
- One day each week.
- Any combination of weekdays.

Keep weekday selection inside the Pixel Timer's own editor.

Remove duplicate recurring-day selections from global Session settings.

The timer must correctly calculate irregular intervals between sessions, including entire weekends.

## 1.5 Synchronize All Time-Based Widgets

The Pixel Timer, its internal progress bar, and the separate Progress Bar widget currently display inconsistent information.

Create or reuse one authoritative progress calculation service.

When widgets reference the same session, they must use the same:
- Start timestamp.
- End timestamp.
- Time zone.
- Current clock instant.
- Session phase.
- Normalized progress.

During a class:
- Pixel Timer fills upward in elapsed progress.
- Session progress bar fills upward.
- Standalone session-linked Progress Bar shows the same elapsed percentage.

Between classes:
- Pixel Timer clears in remaining-time order.
- Countdown displays actual remaining duration.
- Any linked countdown visualization uses the same remaining-time calculation.

Do not force independently configured progress bars to use a session they are not linked to.

## 1.6 Synchronize Seconds with the Clock

Timer seconds must align with actual wall-clock second boundaries.

Avoid drifting intervals caused by repeatedly adding one second per timer tick.

Use actual timestamp differences.

Derive countdown values from the current authoritative clock instant.

Refresh displays at appropriate second boundaries.

Ensure the displayed seconds, Pixel Timer, session clock, and other linked timer displays do not disagree because of independently drifting schedulers.

Use UTC instants internally and the selected time zone for display and recurring schedule boundaries.

## 1.7 Fix Stray Pixels

Fix the rendering defect that produces isolated illuminated pixels on lower rows.

Requirements:
- Deterministic cell ordering.
- Correct row wrapping.
- No random cells.
- Correct clamping.
- No cumulative tick drift.
- Consistent grid progress at all sizes.

## 1.8 Fix Time Picker Scroll Sensitivity

The time picker dropdown scrolls far too quickly.

Fix scrolling for:
- Hours.
- Minutes.
- Seconds, if supported.
- Session start and end selectors.
- Similar numeric selectors throughout Nexa.

Requirements:
- One controlled increment per standard mouse-wheel notch.
- Smooth trackpad behavior.
- No accidental jumps across several values.
- Clickable options.
- Keyboard navigation.
- Correct focus and selection handling.
- No unintended changes when scrolling the outer Settings panel.

Ensure nested scrolling is handled correctly.

## 1.9 Progress Precision

Display linked progress percentages to exactly three decimal places.

Examples:
- 0.000%.
- 12.345%.
- 50.000%.
- 99.999%.
- 100.000%.

Do not round internal progress calculations prematurely.

Prevent displaying 100.000% before actual completion.

## 1.10 Time Zone Accuracy

Retain the global Time Zone & Clock functionality.

Support:
- Automatic Windows time zone.
- Manual time zone selection.
- Searchable worldwide time zones.
- Daylight saving transitions.
- Correct UTC/local conversion.

Avoid the reported one-hour time discrepancy.

Test Central and Eastern time zones, DST transitions, and application resume.

---

# PART 2 — BROKEN SETTINGS CONTROLS

Inspect all Settings controls for actual functionality.

Do not merely update their appearances.

## 2.1 Bug Behavior Selection

The current behavior selector is stuck on Cozy.

Fix selection so users can click and switch among:

- Classic.
- Cozy.
- Creepy.
- Combative.

All selections must work immediately.

Persist selections correctly.

## 2.2 Bug Density

The density selector is stuck on Low.

Fix all available density options.

Changes must actually affect the number of bugs spawned or maintained.

Validate minimum and maximum population limits.

## 2.3 Bug Type Selection

The user currently cannot reliably click a bug species in the Bug Type selector.

Fix selection for:

- All Bugs.
- Ants.
- Spiders.
- Ladybugs.
- Roaches.
- Centipedes.
- Flies.

Clicking must work, not just mouse-wheel scrolling.

Apply and persist the selection.

## 2.4 Appearance Override

The existing Appearance Override control currently responds to the mouse wheel but not normal clicks.

However, the separate appearance override concept is being retired.

Remove the unnecessary separate appearance-selection control entirely.

Bug appearance should be derived directly from selected behavior mode.

Do not leave a hidden or nonfunctional duplicate setting.

## 2.5 Seasonal Controls

The current seasonal dropdowns cannot reliably select Halloween state or decoration density.

Replace the old seasonal activation dropdown with the new Nexaween toggle.

Keep decoration density configurable inside the Nexaween settings panel.

Ensure all selectors work with mouse clicks, keyboard input, and touchpad interaction.

Test selected-value binding, event handling, disabled states, and settings persistence.

---

# PART 3 — FOUR BUG BEHAVIOR MODES

## 3.1 Classic Mode — Default Outside Nexaween

Add Classic Mode.

Classic is the default when Nexaween is disabled and the user has not explicitly selected another mode.

Classic must preserve the recognizable bug behavior and appearance from the previous working version.

Use the original bug textures and baseline behavioral patterns.

Do not overwrite original assets.

Do not silently transform Classic Mode into Cozy Mode.

## 3.2 Cozy Mode

Default for a fresh Nexaween configuration.

Behavior:
- Friendly exploration.
- Gathering resources.
- Ant colony building.
- Spider nest construction.
- Socializing.
- Grooming.
- Sleeping.
- Carrying food.
- Peaceful interaction.
- Extremely rare fighting.

Use dedicated Cozy appearance assets.

## 3.3 Creepy Mode

Behavior:
- Shadow hiding.
- Sudden scuttling.
- More lifelike movement.
- Cautious exploration.
- Occasional hunting.
- Unsettling spider behavior.
- Detailed web construction.

Use dedicated Creepy textures.

Do not return to nonstop combat.

## 3.4 Combative Mode

Behavior:
- Territorial defense.
- Colony competition.
- Resource raids.
- Organized battles.
- Strategic attacks.
- Balanced species combat.

Use the existing Normal/Classic-inspired textures by default.

Preserve any useful battle simulation architecture without forcing combat into other modes.

## 3.5 Automatic Appearance Mapping

Remove "Match Appearance to Behavior."

Remove the redundant standalone appearance dropdown.

The selected behavior mode automatically determines the corresponding visual appearance:

- Classic → Original Classic appearance.
- Cozy → Cozy appearance.
- Creepy → Creepy appearance.
- Combative → Original Normal appearance.

Switching modes should update behavior and visuals together.

Avoid conflicting settings and stale selections.

## 3.6 Nexaween Defaults

When Nexaween is first enabled with no saved seasonal preference:

- Behavior: Cozy.
- Adorable Mode: Enabled.

Users must still be able to select Classic, Creepy, or Combative while Nexaween is enabled.

Do not forcibly switch back to Cozy after every user selection.

Preserve user choices when temporarily enabling or disabling Nexaween.

When Nexaween is disabled, restore the saved nonseasonal mode or Classic as the initial default.

---

# PART 4 — BUG VISIBILITY AND SCROLLING

## 4.1 Bugs Are Hidden Under Widgets

Currently bugs frequently crawl underneath widget surfaces and disappear.

Fix visual stacking and pathing.

Bugs must remain visible during ordinary exploration.

Recommended behavior:
- Crawl around widget edges.
- Travel across safe visible surfaces when appropriate.
- Avoid permanently disappearing behind opaque widget panels.
- Use correct overlay z-order.
- Preserve widget clickability.

Do not solve the problem by placing a fully interactive bug layer above every control.

Use proper hit testing so buttons, notes, and widgets remain accessible.

## 4.2 Scrolling Jitter

Bug movement appears to freeze or jitter briefly when scrolling.

Investigate:
- UI-thread blocking.
- World-to-screen coordinate calculations.
- Layout recalculation.
- Excessive collision processing.
- Animation clock scheduling.
- Scroll offset synchronization.
- Rendering invalidation.

Ensure bugs move smoothly during fast and slow scrolling.

Preserve world coordinates independently of viewport coordinates.

Do not reposition creatures by accumulating scroll deltas incorrectly.

## 4.3 Fly Movement

Flies currently appear not to fly.

Implement meaningful flight behavior:

- Takeoff.
- Hovering.
- Short flights.
- Direction changes.
- Landing.
- Resting.
- Occasional wing grooming.

Use actual airborne movement rather than simply sliding along the ground.

Ensure flight remains visible and does not interfere with UI controls.

Detailed wing animation may be implemented in the final animation phase.

---

# PART 5 — SETTINGS NAVIGATION RESTRUCTURE

## 5.1 Remove Session Tab from Main Settings

There is currently:
- A Session tab inside Settings.
- A separate purple Session button on the main toolbar.

Remove the duplicate Session tab from the main Settings menu.

Make the existing purple Session button the sole primary access point for session configuration.

Move relevant Session tab settings into the interface opened by that button.

Preserve all existing session functionality.

## 5.2 Move Notes & History

Remove Notes & History from the top toolbar.

Place it inside the Session interface opened by the main purple Session button.

Ensure historical reports remain accessible without reopening old sessions.

## 5.3 Move Quick Tips Setting

Place "Enable Tips" near the bottom of the Session interface.

When the user clicks a floating tip:

1. Open the Session interface.
2. Navigate to the Tips setting.
3. Scroll the settings container to the bottom.
4. Bring "Enable Tips" into view.
5. Briefly highlight that setting.

Allow users to disable tips there.

When disabled, dismiss the current tip and prevent future automatic tip popups.

Because the old Session tab is removed, do not leave this control stranded in the deleted navigation tab.

## 5.4 Remove Scheduled Days from Global Session Settings

Recurring weekday selection belongs in the Pixel Timer editor.

Remove duplicate global weekday controls.

Do not break existing saved schedules during migration.

## 5.5 Remove Global Summary Interval

Remove the Summary Interval control from global Session settings.

Its value belongs in the Session Timeline widget.

Move this functionality into the widget's own configuration.

Preserve existing user values where possible.

## 5.6 Merge Appearance Tabs

Currently two separate tabs exist:

- Appearance.
- Appearance & Effects.

Merge them into one unified Appearance tab.

Consolidate:
- Theme preferences.
- Colors.
- Visual effects.
- Animation preferences.
- Relevant interface styling controls.

Do not create duplicate controls.

## 5.7 Create Separate Sounds Tab

Move Interface Sounds from Appearance into a new Sounds tab.

Include:
- Enable interface sounds.
- Sound volume.
- Relevant notification sounds.
- Timer completion sounds.
- Seasonal sounds where appropriate.

Sounds must remain accessible while Nexaween is enabled.

---

# PART 6 — NEXAWEEN SETTINGS ACCESS

## 6.1 Remove Seasonal Events Tab

Remove the standalone Seasonal Events tab.

Replace it with a special Nexaween control in the bottom-right corner of the Settings window.

## 6.2 Pumpkin Nexaween Button

Create a pumpkin-shaped button at the bottom-right of Settings.

Appearance:
- Orange pumpkin.
- Subtle shading.
- Decorative green vines.
- Vines wrapping around the lower-right corner.
- Very faint purple text reading "Nexaween" on the pumpkin.

The pumpkin must be clickable.

Clicking it opens the Nexaween settings panel.

It must not merely enable or disable the theme.

## 6.3 Red/Green Toggle Beside Pumpkin

Immediately to the LEFT of the pumpkin button, add an on/off switch.

OFF:
- Switch positioned left.
- Red visual state.
- Nexaween disabled.

ON:
- Switch positioned right.
- Green visual state.
- Nexaween enabled.

The switch must:
- Work with mouse clicks.
- Support keyboard navigation.
- Persist its state.
- Apply theme changes immediately.
- Reflect the actual active state.

Keep the switch and pumpkin button separate actions.

## 6.4 Nexaween Settings Panel

The pumpkin button opens settings for:

- Nexaween enable/disable.
- Decoration density.
- Halloween costumes.
- Spooky effects.
- Seasonal sounds.
- Surprise animations.
- Optional seasonal visual settings.

All controls must work with direct clicks and keyboard navigation.

---

# PART 7 — APPEARANCE LOCK DURING NEXAWEEN

## 7.1 Lock Appearance Tab

When Nexaween is enabled:

- Grey out the Appearance tab.
- Prevent entry into ordinary color customization.
- Keep Sounds accessible.
- Keep Nexaween-specific settings accessible.

When Nexaween is disabled:

- Unlock Appearance.
- Restore ordinary theme and color controls.

Preserve saved nonseasonal appearance settings.

## 7.2 Clicking the Locked Appearance Tab

The greyed-out Appearance tab must remain clickable as an explanation trigger.

Clicking it should open a small Halloween-themed dialog.

Suggested message:

"Turn off Nexaween to customize your normal appearance."

Include:
- A red/green toggle switch.
- Cute ghost illustrations.
- A clear explanation.
- Close action.

The popup must be fully accessible and must not trap keyboard focus unnecessarily.

Use a guarded clickable control with a disabled visual style, not a truly disabled control that cannot receive clicks.

## 7.3 Linked Disable Action

Where appearance restrictions are explained, include the phrase:

"switch Nexaween off"

Style it:
- Blue.
- Underlined.
- Clearly clickable.

Clicking the phrase must disable Nexaween immediately and restore ordinary appearance customization.

Do not require users to navigate through multiple settings pages.

## 7.4 App Theme Control

The existing App Theme selector currently does not function correctly.

Do not leave a nonfunctional active control.

While Nexaween is enabled:
- Disable its editing capability.
- Display the linked Nexaween explanation wherever this control is exposed.

When Nexaween is disabled:
- Restore usable theme options or safely retire the obsolete control if it duplicates another functional selector.

---

# PART 8 — NEXAWEEN BRANDING

## 8.1 Match the Existing Task Checkbox Purple

The current Nexa branding purple is too light.

Find the exact purple used in the checked Task List checkbox.

Use that same color as the authoritative dark-purple brand accent for Nexaween.

Do not approximate it with an unrelated purple.

Apply consistently to:
- Nexa logo.
- Stylized X in the Nexa wordmark.
- App title branding.
- Top-left window icon.
- Home page icon.
- Startup splash screen.
- Application icon resources.
- Other existing Nexa logos.

Preserve transparent backgrounds and icon quality.

Rebuild appropriate icon sizes rather than enlarging small raster images.

Keep branding readable on both dark and orange Halloween surfaces.

## 8.2 Halloween Tagline

Change the seasonal tagline to:

"Nexa — A space for whatever haunts you next."

Use it on:
- Home page.
- Startup splash screen.

Only the word "haunts" should be orange.

Use the Halloween orange already defined in Nexa's theme.

## 8.3 Preserve Home Page Tagline Animation

The home page already has an animated tagline.

Preserve its existing animation and timing.

Only update the displayed words and styling.

Do not rewrite or replace a working animation unnecessarily.

## 8.4 Splash Screen Tagline

The splash screen tagline should remain static.

Use the same wording with orange "haunts."

Restore the ordinary branding/tagline when Nexaween is disabled, while preserving the user's nonseasonal appearance settings.

---

# PART 9 — REAL SPLASH SCREEN LOADING BAR

## 9.1 Hair-Thin Loading Bar

Add a very thin progress bar to the startup splash screen.

Appearance:
- White track.
- Halloween green progress fill.
- Minimal height.
- Clean horizontal layout.
- No bulky loading container.

Use the actual Nexaween green theme value.

## 9.2 Real Loading Progress

The progress bar must report genuine application initialization progress.

Do not implement:
- Fake timer-based percentages.
- Arbitrary animation from 0 to 100.
- Artificial delays.
- A simulated loading process.

Instead, instrument actual startup operations.

Examples:
- Configuration initialization.
- Settings loading.
- Local data loading.
- Save migration.
- Session recovery.
- Widget initialization.
- Required service initialization.
- Main window readiness.

Use real completed startup stages.

If exact byte-level progress is unavailable, use transparent stage-based progress weighted by actual completed initialization work.

Do not present estimated stages as exact byte progress.

## 9.3 Startup Completion

When required startup operations are complete:

- Progress reaches 100%.
- Splash screen closes.
- Nexa home page becomes available.

Do not display 100% while the application is still initializing required features.

Ensure initialization failures display useful errors rather than leaving the splash screen indefinitely.

Avoid blocking the WPF UI thread during expensive startup work.

---

# PART 10 — PRODUCTIVITY SCORE WIDGET REDESIGN

## 10.1 Visual Overhaul

The current Productivity Score widget looks too plain.

Redesign it as a polished, visually appealing score display.

Requirements:
- Prominent score.
- Main lightning bolt icon.
- Clear metric hierarchy.
- Attractive spacing.
- Consistent Nexa styling.
- Seasonal color support.
- Compact enough for dashboard use.

## 10.2 Remove Bottom-Left Fire Icon

Remove the small separate fire icon currently displayed in the bottom-left corner.

Keep the existing widget border glow when clicked.

The primary lightning bolt should become the visual focus.

## 10.3 Centered Streak Multiplier

Place the current multiplier in the center of the score display.

The score and multiplier should form one cohesive visual group.

Examples:

1.0× — no active streak.
1.5× — initial active streak.
2.0× — increased streak.
2.5× — continued streak.
3.0× — further streak.

Increase the streak multiplier in 0.5 increments when the user successfully squashes another bug during the active streak.

Use a configurable upper limit to prevent unlimited point farming.

## 10.4 Two-Minute Streak Window

A direct user squash starts a streak window lasting two minutes.

If another bug is personally squashed before expiration:
- Increase multiplier by 0.5.
- Refresh the two-minute expiration window.

If the window expires:
- End the streak.
- Return to normal multiplier.
- Remove the active fire effects.

Calculate duration using authoritative timestamps.

Ensure application hibernation does not artificially extend the streak.

Do not apply streak points for automated bug-versus-bug combat deaths.

## 10.5 Flame Placement

Whenever multiplier mode is active:

- Surround the main score and multiplier with animated flames.
- Integrate flames into the main score area.
- Avoid detached corner icons.
- Keep text legible.

During Nexaween, use themed green witchfire with subtle orange accents.

Use an orange/fire style outside Nexaween if the ordinary effects theme supports it.

Flame animation is a Priority 3 implementation, but multiplier calculations and layout belong to Priority 1.

## 10.6 Score Reliability

Preserve centralized scoring and historical reporting.

Prevent:
- Duplicate awards.
- Repeated-toggle farming.
- Incorrect multiplier persistence.
- Stale UI score values.

Ensure the score widget reflects actual scoring events.

---

# PART 11 — APP BORDER FEEDBACK

Create one reusable global edge-glow feedback system.

## 11.1 Positive Actions — Green Glow

When an action awards positive productivity points, briefly illuminate the application window border green.

Examples:
- Complete a task.
- Log water.
- Add a meaningful note.
- Pin scripture.
- Complete a Todoist task.
- Other legitimate positive scoring events.

Integrate with the central productivity event/scoring system.

Only trigger after confirmed successful actions.

## 11.2 Negative Actions — Red Glow

Briefly illuminate the application border red for configured negative events.

Examples:
- Increment Distraction Counter.
- Log an alcoholic drink where the relevant widget explicitly supports negative scoring.

Negative scoring rules should be configurable.

Do not apply a moral judgment to the user or to unrelated activity.

Use the negative-event category defined by the application's scoring preferences.

## 11.3 Animation Behavior

- Glow the perimeter of the application window.
- Do not cover content with a fullscreen color overlay.
- Do not flash rapidly.
- Fade smoothly.
- Avoid blocking input.
- Support reduced-motion preferences.
- Coalesce rapid repeated events.
- Preserve existing click-based widget border glow.

The full app-edge animation is Priority 3.

Event classification and scoring should be implemented first.

---

# PART 12 — HALLOWEEN DECORATION PLACEMENT

## 12.1 Simplify the Dashboard Background

Remove the current mixture of pumpkins, witches, ghosts, leaves, skulls, and other decorations as a generic repeating background.

The ordinary Halloween dashboard background should primarily contain candy.

Use approximately ten visually distinct candy types.

Examples:
- Candy corn.
- Chocolate bars.
- Wrapped orange candy.
- Wrapped purple candy.
- Lollipops.
- Gummy worms.
- Pumpkin-shaped sweets.
- Green magical candy.
- Candy baskets or small candy pieces.
- Other appropriately varied Halloween sweets.

Scatter them across the background with balanced spacing.

Avoid obvious grid repetition and excessive density.

Keep the background subtle enough for readable widgets.

## 12.2 Decorations Attach to Widgets

Move pumpkins and similar environmental decorations into contextual widget decorations.

Examples:
- Pumpkin in a widget corner.
- Vines wrapping around the border.
- Small skull on the corner of a panel.
- Tiny witch perched near a decorative area.
- Ghost peeking beside a widget.
- Candy gathered near widget edges.

Keep all important text and controls unobstructed.

Decorations must follow widget movement and resizing.

## 12.3 Pumpkin Vines

Pumpkin vines should visibly wrap around selected widget corners.

Use curved organic paths.

Do not simply place straight green lines behind the widget.

Keep vine thickness and opacity subtle.

## 12.4 Spider Webs Must Be Built by Spiders

Do not scatter prebuilt decorative spider webs across the dashboard background.

Spiders should construct their own webs.

Webs should:
- Begin from selected anchor points.
- Develop gradually.
- Attach to suitable dashboard edges and surfaces.
- Have irregular, natural geometry.
- Be associated with an actual spider or nest.
- Persist with ecosystem state.
- Respond correctly to scrolling and widget movement.

The functional web-construction system belongs in Priority 2.

The detailed silk-spinning animation belongs in Priority 3.

---

# PART 13 — EXISTING NEXAWEEN FEATURES TO PRESERVE

Preserve and complete the features already specified for v0.1.24 Alpha.

## 13.1 Session History

- Continuous session record creation.
- Widget history.
- Notes archive.
- Task and counter summaries.
- Scripture pin history.
- Timer activity.
- Productivity Score reporting.
- Search, filtering, and export.
- No reopening of historical live sessions.

## 13.2 Configurable Autosaving

Allow:
- 30 seconds.
- 1 minute.
- 3 minutes.
- 5 minutes.
- 10 minutes.
- 15 minutes.
- 30 minutes.

Save important changes independently of periodic checkpoints.

Support manual Save Now.

Preserve hibernation recovery.

## 13.3 Session Reset

Preserve the established lifecycle:

- Session history starts saving immediately.
- Scheduled session ends without clearing widgets.
- Five-minute post-session checkpoint.
- Reset warning before the next session.
- Reset five minutes before the next start.
- Preserve history and permanent data.
- Clear only session-scoped widget content.
- Support postponement.

## 13.4 Productivity Meter

Keep Productivity Meter separate from Session Timeline.

It records:
- Meaningful actions.
- Tasks.
- Water.
- Notes.
- Scripture pins.
- Counters.
- Timers.
- Widget activity.

## 13.5 Bugs Squashed Widget

Keep Bugs Squashed optional and exclusive to Settings → Add Bugs.

Display current-session and all-time direct squash counts.

## 13.6 Session Timeline

Keep it focused on:
- Hedy notes.
- Recorded notes.
- Transcript intervals.
- Manual lecture notes.
- Audio recording integration.

Interval settings belong inside the widget.

## 13.7 Floating Notifications

Preserve:
- Tips on the right.
- Autosave popups on the left.
- No fixed bottom status bar.
- No clipped notifications.
- Faster tip cycling.
- Tips enabled/disabled from the Session interface.

Autosave notification states:

"Autosaving..." with spinning circular arrows.

"Autosave complete" with green checkmark.

"Autosave failed — retrying..." with warning indicator.

Autosave status and configuration must remain accessible from the Session interface.

## 13.8 Expanded Dashboard Workspace

Keep:
- Scrolling below the last widget.
- Extra empty workspace.
- Add Widget shortcut.
- Drag-to-scroll support.
- Persistent widget positions.

---

# PART 14 — ADVANCED ANIMATIONS LAST

Only begin after the preceding fixes and feature systems function correctly.

## 14.1 Bugs

Implement:
- Larger rendered bugs.
- Detailed body shading.
- Articulated legs.
- Correct six-legged insect movement.
- Eight-legged spider movement.
- Centipede leg-wave locomotion.
- Fly wing movement.
- Realistic landings.
- Resting and grooming.
- Eye blinking.
- Pupil tracking.
- Curious facial expressions.

Spiders should move in a particularly creepy and lifelike way while remaining adorable when Adorable Mode is on.

## 14.2 Halloween

Animate:
- Pumpkin illumination.
- Ghost floating.
- Witch flight.
- Cauldron bubbling.
- Green goop.
- Candy collection.
- Vines subtly growing or swaying where appropriate.
- Spider web construction.
- Seasonal magical particles.

## 14.3 Productivity

Animate:
- Lightning bolt.
- Centered multiplier.
- Score flames.
- Positive green app-border glow.
- Negative red app-border glow.
- Task completion pops.
- Note creation feedback.
- Progress milestones.

## 14.4 Notification Effects

Animate:
- Yellow lightbulb glowing.
- Circular autosave icon spinning.
- Green checkmark confirmation.
- Smooth notification transitions.

Respect reduced-motion settings and performance constraints.

---

# PART 15 — VERSION AND CHANGELOG

## 15.1 Official Release

Set the application release to:

**Nexa v0.1.24 Alpha — NEXAWEEN**

Update actual version metadata consistently.

Follow valid .NET assembly version formatting.

## 15.2 Themed Changelog

The v0.1.24 Alpha changelog entry must use:

- Dark purple background.
- Neon green text.
- Bright orange border.
- Halloween decorative accents.

Use the exact existing dark task-checkbox purple for branding where required.

The special changelog style must remain identifiable even after Nexaween is disabled.

Preserve older changelog entries.

## 15.3 Changelog Sections

**The Dashboard Has Been Haunted**
- Nexaween theme and branding.
- Seasonal tagline.
- Halloween decorations.
- Pumpkin settings control.

**Time Is Under a Spell**
- Dynamic Pixel Timer.
- Session count-up.
- Intersession countdown.
- Progress synchronization.
- Accurate seconds.
- Time picker fixes.

**Creepy Crawlies**
- Classic, Cozy, Creepy, and Combative modes.
- Bug selection and density fixes.
- Appearance mapping.
- Bug visibility and movement.
- Flight and web construction.

**Productivity Witchcraft**
- Score widget redesign.
- Lightning bolt and flames.
- Two-minute streak multiplier.
- Positive and negative edge feedback.

**The Archives Awaken**
- Session history.
- Autosave.
- Notes persistence.
- Hibernation recovery.
- Session reset.

**Spells of Convenience**
- Settings consolidation.
- Separate Sounds tab.
- Nexaween pumpkin button.
- Real splash loading indicator.
- Tips and notifications.
- Dashboard scrolling improvements.

Only list features as shipped once verified implemented.

---

# PART 16 — REQUIRED TESTING

## Pixel Timer

Verify:
- Editing 9:00–10:00 immediately updates the timer.
- Selecting an active session automatically starts count-up.
- Session pixels fill from top-left.
- Between-session pixels clear from bottom-right.
- Progress percentages agree.
- Clock seconds synchronize.
- No stray pixels.
- Schedule survives restart.
- Correct after hibernation.
- Correct time zone and DST handling.

## Settings

Verify:
- All four behavior modes are clickable.
- Species selection works with clicks.
- Density changes work.
- Appearance Override is removed.
- Appearance matches behavior automatically.
- Session tab is removed from main Settings.
- Session top button opens all session controls.
- Notes & History is accessible there.
- Tips link scrolls to the enable setting.
- Summary Interval exists only in Session Timeline.
- Appearance tabs are merged.
- Sounds is its own tab.
- Nexaween pumpkin opens seasonal settings.
- Red/green toggle activates/deactivates Nexaween.
- Appearance locks while Nexaween is active.
- Locked Appearance shows ghost popup.
- Linked disable action works.

## Bugs

Verify:
- Classic restores original look and feel.
- Cozy, Creepy, and Combative can be selected.
- Nexaween defaults to Cozy + Adorable for fresh seasonal setup.
- Bugs are visible around widgets.
- Scrolling does not freeze movement.
- Flies visibly fly.
- Density and species selection affect the world.
- Spider webs are constructed by spiders.
- Existing world data persists.

## Productivity

Verify:
- Lightning bolt appears prominently.
- Bottom-left fire icon is gone.
- Border click glow remains.
- Squash streak lasts two minutes.
- Each valid additional squash increases multiplier by 0.5.
- Multiplier is centered.
- Score and multiplier gain flames during a streak.
- Correct scoring and historical reporting.
- Positive events trigger green edge glow.
- Negative configured events trigger red edge glow.
- No duplicate point farming.

## Halloween

Verify:
- Candy-only background treatment.
- Approximately ten distinct candy types.
- Pumpkins attach to widget corners.
- Vines wrap around selected widget edges.
- Settings pumpkin and toggle work.
- Purple branding matches task-checkbox color.
- Orange "haunts" styling appears.
- Home animation is preserved.
- Splash tagline is static.
- Real startup progress updates.
- Halloween decorations do not block controls.

## Saving and History

Verify:
- Autosave intervals function.
- Manual Save Now works.
- Session data survives hibernation.
- Historical notes remain readable.
- Pre-session reset occurs correctly.
- Reset never destroys unsaved data.
- Session History does not reopen old dashboards.
- Autosave notifications appear on the left.
- Tips remain on the right.

## General UI

Verify:
- Dashboard scrolls below the last widget.
- Widget placement works in blank space.
- No fixed bottom status bar.
- Notifications never clip.
- Settings changes persist.
- Startup completes without fake loading progress.
- Normal appearance restores after disabling Nexaween.

---

# PART 17 — IMPLEMENTATION QUALITY AND REPORTING

Before declaring completion:

1. Build Nexa successfully.
2. Run all relevant automated tests.
3. Exercise affected UI controls.
4. Verify persistence after restart.
5. Test scrolling and timer transitions.
6. Verify user data migration.
7. Check animation and simulation performance.
8. Verify actual version metadata.
9. Check changelog accuracy.

Report:

- Files changed.
- Root causes fixed.
- Features completed.
- Tests run and results.
- Remaining bugs.
- Incomplete features.
- Manual testing required.

Do not report success for features that have not been verified.

If work cannot be completed in one pass, preserve a usable, buildable application and identify the next unfinished feature by priority.

---

# FINAL OBJECTIVE

Nexa v0.1.24 Alpha — NEXAWEEN should first fix its dynamic Pixel Timer, synchronization issues, broken Settings controls, bug visibility, scrolling, and productivity logic.

Then consolidate and improve the interface, including Nexaween's pumpkin settings button, seasonal theme, branding, and real splash-screen progress.

Finally, implement the advanced bug, Halloween, productivity, and notification animations.

The completed update should feel polished, responsive, spooky, and fun without compromising reliability.

**FUNCTIONAL FIXES FIRST. NEXAWEEN DESIGN SECOND. ADVANCED ANIMATIONS LAST.**

# END — NEXA v0.1.24 ALPHA: NEXAWEEN

## Accepted follow-up decisions

- Decoration update takes optional `IReadOnlyList<Rect>? widgetTargets = null`.
- `occupied` is collision-only; attachment targets come exclusively from the supplied widget rectangles. Missing targets skip contextual art. No new attachment metadata/configuration system is required for 0.1.24.
- Pumpkins/vines follow actual widget corners through scrolling/resizing and never block controls; backgrounds stay candy-only and spider-owned webs remain separate.
- The complete active project belongs in the private `scrmarsh/nexa` GitHub repository. Preserve obsolete source, notes and helpers in `LEGACY`; generated builds and private user/recovery data remain local.
