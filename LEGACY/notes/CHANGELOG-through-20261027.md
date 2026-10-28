# Changelog

## nexa v0.1.24 Alpha — Getting Candy for Trick-or-Treaters

A space for whatever haunts you next.

- **The Dashboard Has Been Haunted:** resource-matched seasonal branding and tagline, candy-only backgrounds, contextual widget corners and curved vines.
- **Time Is Under a Spell:** authoritative elapsed session progress, reverse intersession clearing, linked/independent bars, recurring/date/duration editing, clock alignment and controlled selectors.
- **Creepy Crawlies:** Classic/Cozy/Creepy/Combative, automatic appearance, effective species/density controls, widget avoidance, flight and persistent spider-owned web construction.
- **Productivity Witchcraft:** central signed scoring, prominent score/multiplier, timestamp-based two-minute streaks, bounded half-step multipliers and saved-event perimeter feedback.
- **The Archives Awaken:** retained searchable history, continuous saving, recovery and protected session resets.
- **Spells of Convenience:** consolidated Session/history/tips, unified Appearance, Sounds, pumpkin settings/toggle, genuine splash stages and split floating notifications.

See NEXAWEEN024_IMPLEMENTATION.md for verification evidence and outstanding manual acceptance. Older entries below describe previous dashboard versions.

## nexa 0.1.23 Alpha — Decorating for Halloween (2026-10-19)

- Minor changes.

## nexa 0.1.22 alpha - GPU Performance Fix (2026-10-19)

- Use the existing paced dashboard timer for bug updates; remove the composition-callback update/invalidation loop.
- Correct the hardware diagnostic default to timer pacing; retain legacy-loop and foreground dashboard comparisons.
- Measure sprite rasterization misses without changing artwork quality, GPU support, software fallback, saved data.
- See RELEASE022.md for measured before/after results and remaining limitations.

## nexa 0.1.21 Alpha — Minor changes (2026-10-19)

- Minor changes.

## nexa 0.1.20 Alpha — Minor changes (2026-10-19)

- Minor changes.

## nexa 0.1.19 Alpha — Minor changes (2026-10-15)

- Minor changes.

## nexa 0.1.18 Alpha — Bug fixes (2026-10-15)

- Improved per-monitor DPI scaling, scrolling and widget attachments.
- Minor changes and bug fixes.

## nexa 0.1.17 alpha — Drink Counters & Laps (2026-10-14)

- Water and Beer retain separate widgets with larger glass/bottle artwork and distinct catalog icons. Buttons are smaller; goals and reset live under Options. Saved compact counters adapt the artwork to keep count buttons reachable.
- Beer goals remain selectable from one. Goals above four display “Slow down—consider drinking less.” Every two beers logged show a dismissible eight-second “Take a sip of water.” reminder.
- Pixel Swarm keeps the dashboard pixel color captured when **I'm bored** is clicked.
- Stopwatch adds persisted lap durations and cumulative times, with the list hidden until the first lap. Stopwatch reset clears laps.


## nexa 0.1.16 alpha — Stable Layout, Bugs and Counters (2026-10-05)

Widget drops now preserve unrelated card positions and reject occupied destinations; ordinary remove and resize actions leave open space. Add Bugs tracks dashboard content while scrolling, improves species movement, and raises the Infestation cap. Appearance settings use direct choices. Water gains a gradient and moving fill, while the new Beer Counter has a separate goal (minimum one), amber liquid, foam, and rising bubbles. Home shows the lowercase alpha label and adds safe spacing below its footer controls.

## nexa 0.1.15 Alpha — Add Bugs Settings Fix (2026-10-04)

Bug type and intensity are now visible choices in Settings → Experimental Features → Add Bugs. Ants, Roaches, Flies and All Three, along with Low, Normal and Infestation, can be selected before enabling bugs. Changes apply immediately and persist when Settings is reopened.

## nexa 0.1.14 Alpha — Add Bugs (2026-10-04)

Settings → Experimental Features → Add Bugs optionally adds tiny animated ants, roaches, flies or all three to Spaces. Bugs follow widget geometry, react to movement and can be clicked to squash them, leaving a small splat that fades away. Low, Normal and Infestation densities share bounded populations. The feature defaults off, saves per user in existing preferences, passes clicks through empty overlay space, and pauses while the dashboard is hidden or minimized. No Space or widget data migration is required.

## nexa 0.1.13 Alpha — Pixel Swarm & Appearance (2026-10-04)

Pixel Swarm now opens a start menu before a run. Move with WASD or arrow keys, aim with the mouse and click to fire the Pixel Blaster. Survive three escalating stages: drifting timer pixels, pursuing enemies and white relief orbs, then mixed enemies, limited weapon pickups, bosses and experience upgrades. Health, score and a results screen make runs replayable; pause, restart and return remain available. Best score and run statistics save locally per nexa user, separate from Space data. The existing session timer and autosave continue while the game is open.

Appearance changes to Light/Dark mode and the app accent now update live Home, Settings and Space surfaces and persist across restarts. A Space's own accent still applies to its Pixel Timer and selected Space accents. Existing dashboards and preference files need no migration.

## nexa 0.1.12 Alpha — Dashboard & Widget UX Update (2026-10-03)

The anchored Pixel Timer now resizes from its right edge and leaves usable top-row space for other widgets. Its target menu has Date, Time and Use Today's Date; the latter proposes one hour from now without starting a session. Dashboard cards compact after editing, use their rendered bounds for placement, show a blue landing preview and scroll near the viewport edges during a drag.

- Follow-up: the Pixel Timer now uses the same rounded card border as other widgets, with no colored resize line. Adjacent cards meet at the normal 8-pixel gap. Dragging a shared horizontal or vertical boundary grows one widget while shrinking its neighbor, stopping at the neighbor's minimum size. Saved positions snap to the dashboard grid; global theme choices apply immediately from Space Settings and persist.

- Follow-up: the Pixel Timer defaults to the 360-pixel expanded Clock height and can be resized vertically from its bottom edge. Its height saves per Space without changing the timer or Pixel Swarm behavior.

Clock, Stopwatch and Calendar have saved Compact/Expanded modes. Session Timeline can open Audio Recorder or Hedy Notes without starting capture. Session settings are visible only in a Space, alongside a separate global Settings action; theme changes apply immediately from Home or a Space. Home/splash branding and the Home footer have been refined.

Calendar now supports Google and Outlook as optional read-only sources, with the selected provider/calendar saved per widget. OAuth authorization uses PKCE and owner-scoped Windows protected storage; no account is needed for local tools. Water Bottle Counter has a filling glass; Notes uses a larger ruled writing surface. Existing Task List/Notes/Counter consolidation, Focus Reset category and verified Experimental Updates remain available.

- Follow-up repair: clicking outside a menu closes the topmost panel even when Windows consumes the first click to reactivate its owner. An older Developer Space now migrates retired widget kinds before DEV fills in missing widgets, preventing a duplicate linked Task List and preserving existing content.

The Add Widget library now presents Task List, Notes and Counter in place of overlapping tools. Existing Agenda, Main Passage, Prayer Requests and other compatible widgets retain titles, positions and saved content when opened. Focus Reset is under Mindfulness. Clock and Timer offer Compact and Expanded controls with appropriate minimum sizes.

Settings → Experimental Features → Updates shows the installed version, release notes and a manual release check. Optional startup checks are off by default. Install is offered only for a published GitHub Releases setup asset matching the version and a published SHA-256 digest; the download is verified before an external installer is launched after confirmation. The updater does not modify nexa's local user store or overwrite its running executable.

The update flow requires a separately published setup installer named `Nexa-{version}-{Alpha|Beta|Stable}-Setup.exe` with a GitHub asset SHA-256 digest. This repository does not yet produce or publish that installer automatically, so live installation awaits such a release asset. Pixel Swarm and the planned 0.1.13 redesign are unchanged.

Development releases formerly numbered 1.0.1–1.0.8 are listed here as 0.1.1–0.1.8 Alpha. Alpha continues with 0.1.10, 0.1.11 and later patches; 0.2.x is reserved for an intentional Beta, and the eventual first stable release is 1.0.0. Original Git history and tags are unchanged.

## nexa 0.1.11 Alpha — Bug Fixes, UI Refinements & Pixel Timer Performance (2026-10-03)

Keeps the Pixel Timer responsive during window changes and makes everyday Space, Settings and History interactions more predictable without requiring a saved-data migration.

- Pixel Timer cells retain a fixed, device-aligned square size and even spacing. Resize updates are debounced, unchanged grids are left intact, and pixel visuals are reused. The session countdown remains tied to the existing timestamps.
- Fullscreen uses a gentle content transition and returns to the previous normal bounds or maximized state. Internal Space/Developer actions preserve the current window size and state.
- Widget dragging shows a blue landing preview; row insertion supports three cards when minimum widths permit. Drops keep unrelated cards in place, while resized cards can still land at the left edge. Existing placement fields continue to save and reload.
- Home Recent Spaces displays four cards per vertical column, expanding horizontally with a small chevron. Settings has clearer primary/Experimental navigation, a live empty Session state, and its autosave status in the fixed footer.
- Developer Spaces expose Play Sound beside Preview Splash. Home and dashboard History add a themed, cancel-safe red Clear History action that preserves Spaces and current content.
- Create Space uses the shared internal panel, scrollable content and fixed Back/Close footer; Argo Pantry is omitted from new choices without deleting old Spaces. The top-bar wordmark is lowercase, and caption controls are lighter.
- The duplicate fullscreen/maximize icon is removed from the dashboard bar; F11 still toggles fullscreen. Home, Session and Add Widget have top clearance when maximized, and Home's Settings, History, Changelog, About and DEV actions remain at the bottom of the window.
- Menus and secondary windows can be dragged from blank space without taking clicks from buttons and fields. The new Water Bottle Counter stores its count and adjustable goal with its Space.
- No schema migration, account/integration change, data reset, widget-library consolidation or Pixel Swarm redesign.

## nexa 0.1.10 Alpha — Interface & Navigation Cleanup (2026-10-03)

Makes nexa's internal panels, Home and dashboard navigation cleaner and more consistent while preserving active sessions and saved data.

- Shared secondary-panel chrome for Settings, Add Widgets, History, Changelog and About, with a persistent bottom-left Close button outside scrolling content. Main Home/dashboard window controls remain.
- Esc dismisses the topmost panel; open dropdowns receive Esc first. Outside clicks are consumed before dashboard handlers, preventing Add Widgets from immediately reopening.
- Dedicated Add Widget beside Session. Empty dashboard clicks still open the catalog; canvas plus shortcuts and their extra placement allowance are removed.
- Global scrollbar controls are hidden while ScrollViewer wheel, trackpad and keyboard input remain available.
- Home removes both greetings and the duplicate upper Changelog link. Recent Spaces defaults to four compact cards; More expands into additional columns where width permits, and Less restores four.
- Subtle centered tips rotate every 45 seconds outside widget layout and pointer input. No widget consolidation, Pixel Swarm overhaul, new dependency, saved schema change or data reset.

## nexa 0.1.9 Alpha — Versioning & Session Stability (2026-10-03)

Introduces nexa's Alpha version numbering system, improves release history and version labeling, and fixes active-session state handling and settings persistence.

- Centralized Alpha labels in Home, About, splash and release history; restored the early Bug Fixes release.
- Settings binds to the current Space/session and recognizes saved sessions from Home. Opening or closing Settings preserves session identity, timer timestamps, notes and history.
- Ordinary Settings changes save automatically. Typing and sliders are debounced; closing Settings and changing Spaces flush pending edits. Explicit connection and file-selection actions remain.
- No data reset or storage/schema change. Existing Spaces, profiles, preferences and integration credentials are preserved.

## nexa 0.1.8 Alpha — Widget + Dashboard Stability (2026-10-03)

Improves widget placement, responsive session pixels and local recording while preserving saved Spaces.

- Responsive Pixel Timer canvas reaches both usable edges with more/fewer square pixels; optional Circle shape uses the same session clock. Duration and progress sit left, pixels right, and I'm bored bottom left. Interval data remains; permanent interval UI is removed. Completion stays in the theme color without white glow or flashing.
- Pixel Swarm uses the dashboard palette while session timing and autosave continue.
- Active resize anchors the selected widget and pushes neighbors; drops, clicked-location additions and generated previews share grid packing. Existing layouts are preserved until edited; no placement schema change.
- Remove-widget toast provides ten-second Undo, Ctrl+Z and Ctrl+Shift+Z redo. Text editing retains its own undo. Removed notes/tasks remain in local data and history.
- Dashboard-only save spinner and successful local-time stamp; hidden scrollbar chrome, full-screen top spacing, secondary Home profile greeting and functional persisted image backgrounds.
- Settings / Widgets includes pixel shape, optional slow focus backdrop pulse and distraction escalation. Advanced card options remain available by double-click, F2 or right-click on the editable title; dedicated Configure buttons are gone. Developer settings can play the existing completion chime with master sound preferences respected.
- Stopwatch adds milliseconds and a matching analog display using shared timestamp-based elapsed time. Calculator supports arithmetic, decimals, postfix percent, keyboard input, copy and eight recent calculations.
- Standalone Session Timeline defaults to its correct name, supports local manual entries and retains interval access. Recommended shortcuts lead Add Widget. Hedy Notes is separate; legacy Hedy widgets are renamed in place without merging or deleting content. Hedy settings and old links route to Integrations.
- Audio Recorder surfaces the existing Windows recorder with consent, pause/resume/stop, elapsed/status, owner-scoped unique local files and Timeline entries. Failed capture saves can be retried; close/switch finalizes audio in the outgoing session. No audio is uploaded. Transcription/summary interfaces are foundation only; no provider or credentials are invented.
- Preferences and new entry/history fields load additively. No onboarding/Create Space redesign, database reset, new dependency or integration-account migration.
## nexa 0.1.7 Alpha — Lightweight UI & Interaction Polish (2026-10-03)

Polishes lightweight navigation, widget movement and responsive dashboard presentation.

- Stationary Settings button with a recognizable centered vector gear that alone rotates on hover.
- Physical widget pickup: the actual card follows the pointer with its grab offset and floats above its neighbors; layout changes wait until release.
- Intentional X/Y grid drops reserve the chosen column and row, reflow occupied slots, compact upward and save the final positions. Wide three-column layouts remain reachable; narrower canvases wrap responsively.
- Short native WPF settle animations for the moved and affected cards, including safe cancellation and pickup during a previous animation.
- Square, centered Pixel Timer cells with one device-pixel-aligned size and proportional spacing; no timing-state change.
- Remove only the top Focus Reset button; its widget, library entry and local guide remain.
- Expanded Bible widgets allocate additional height to the passage while preserving normal controls and scrolling.
- No schema migration, user-data reset, integration changes or new dependencies.

## nexa 0.1.6 Alpha — Onboarding & Integrations (2026-10-02)

Helps new users personalize local Spaces and adds optional task and calendar connections.

- Six-step welcome flow with optional name, multiple use cases, timed-session and focus preferences, simple space choices, curated tools, visual theme choices, opt-in sound preview and a ready-to-use first space.
- Existing installations bypass onboarding. Profile settings can rerun setup to append a space while preserving current dashboards and history.
- User-scoped local profiles, preferred widgets and default session behavior; legacy appearance/sound preferences copy safely without deleting original files.
- Optional Todoist and Google Tasks widgets share a small task-provider interface and list selection, date views, completion and creation controls.
- Read-only Google Calendar widget with calendar selection, today's schedule, current/next timed events and time until the next event.
- Dedicated integration settings, browser Google authorization code/PKCE, token refresh, DPAPI credential protection and clean disconnected/offline states. Google requires a configured desktop OAuth client; real-account connection testing was not performed.
- Credentials remain separate from profile/dashboard exports. No personal integration data is cached to disk. No workspace schema change or data reset.
- Verification repairs: clear stale task/calendar/account lists after failures or disconnects, reject results from replaced accounts, bind requests to the selected connection, and handle malformed authorization/configuration/provider responses safely.
- Preserve master sound, volume and focus-reminder preferences when switching Spaces in the same window.

## nexa 0.1.5 Alpha — Focus First (2026-10-02)

Makes nexa simpler and more focused with a signature Pixel Timer, calmer defaults and optional focus tools.

- Full-width Pixel Timer banner using the existing session clock, with readable countdown, progress, duration and next interval. Pixel Swarm keeps its timer pixels and full-window game area.
- Simpler Class, Study/Focus, Church and Meeting presets; existing dashboards and the full widget catalog remain available.
- Grid placement preview, stable neighbors, collision-safe dropping, consistent resize snapping and saved dimensions.
- Direct countdown editing, a quick clock settings menu, editable titles and widget actions revealed on hover/focus.
- Offline Focus Reset: five breaths, a look-away prompt and local notes/time recap, with an immediate return button.
- Optional return-to-session nudges with three-minute absence threshold and ten-minute cooldown.
- Global optional original soft interface sounds with a master switch and volume.
- Reusable vector icons for common widget actions, settings, navigation and history; Per-Monitor V2 DPI manifest and layout rounding.
- No data reset or format version change. Original layout metadata remains until a direct placement needs absolute positions to keep neighboring cards stationary.


## nexa 0.1.4 Alpha — Polish and Bug Fixes (2026-10-02)

Spaces become more personal, responsive and useful for study and live sessions.

- Unified the project, Home and splash version at 0.1.4 Alpha and updated the splash tagline.
- Hid developer Spaces from normal Recent Spaces and widened Preview Splash.
- Added editable hours/minutes/seconds and Start, Pause, Resume and Reset to independent timers.
- Added a simple Stopwatch; timer and stopwatch state survives layout changes and autosave.
- Kept the widget picker scrollable and added an explicit Add button to every widget.
- Removed disposable generated captures and expanded test-output ignore rules.

A responsive Spaces update with unified settings, personal backgrounds, clearer session notes, local recording and flashcards.

### Home, settings and appearance

- Reworked Home for normal and maximized windows with natural scrolling, a compact recent-Spaces list, labeled primary actions and a footer that follows the content.
- Added a compact autosave state and temporary recovery feedback. The Settings gear sits at the bottom left and animates on hover.
- Replaced separate settings surfaces with one dismissible two-pane window for Appearance, Session, Hedy, Account and Data. Requests for a section reuse the open window.
- Added full app themes, themed checkboxes, improved light-mode contrast and a theme-aware nexa wordmark across Home and About.
- Added per-Space default, solid-color and local-image backgrounds with blur strength and dimming controls.

### Spaces, sessions and widgets

- Updated user-facing dashboard language to Spaces while retaining compatible internal storage identifiers.
- Improved responsive reflow so minimized cards release space and useful widgets can grow across the 20-column grid.
- Added a responsive analog face to the Clock widget and kept Bible reading internally scrollable.
- Redesigned Session Notes as a chronological interval feed with completed summaries, current-interval progress, Hedy deep links and a compact footer.
- Added explicit-permission local recording with pause, resume, stop, status, elapsed time, local WAV storage and deletion. Transcription remains clearly unavailable and no recording is uploaded.
- Added local Flashcards decks with card CRUD, flipping, navigation, shuffle, progress, Know it/Review state and CSV/JSON import.
- Updated Pixel Swarm and session surfaces to inherit the active theme and replaced the DEV reset system prompt with a common nexa dialog.

## nexa 0.1.3 Alpha — Session Workspace Polish (2026-10-02)

A more direct, polished session workspace for classes, study, church meetings, team discussions and service shifts.

### Home and release information

- Redesigned Home around the nexa logo, with the version immediately below it and a Changelog button beneath the version.
- Added “a space for whatever comes next” to the right of the logo. The tagline types out, pauses and fades without shifting the layout, then repeats so it does not remain invisible.
- Replaced the system About message box with a themed, resizable menu containing the updated description and use cases.
- Moved Preview nexa inside About and pinned View history at the bottom of Home.
- Added versioned changelog cards with readable, aligned bullet points.
- Set application, splash, Home and About version metadata to 0.1.3 Alpha.

### Dashboard and widgets

- Widgets added from an empty dashboard click or its asset plus appear at that location; adding no longer scrolls to the bottom.
- Dragging visibly lifts a copy of the card with a shadow. Neighbors animate, squeeze, expand and shrink to make space; drops insert between or beside widgets instead of swapping them. Cancel restores the saved layout.
- Added three-column layouts while keeping readable minimum dimensions, purpose-specific defaults, rounded cards and 8-pixel gaps.
- Clocks default to compact cards, counters and independent timers use smaller sizes, and notes and readers start at half width. Bible remains expanded at a readable height.
- Widget titles and controls are available directly on screen. Removed Dashboard setup and redundant top-bar overflow and dashboard dropdown controls.
- Added icons and aligned title/description rows to the Add widget picker. Dashboard adding uses the faint plus asset in available space.
- Mouse-wheel scrolling advances 48 pixels per notch; widgets grow through direct resizing instead of showing internal scrollbars, and narrow windows stack cards.
- Retired Pomodoro from available widgets and templates. Existing cards migrate to ordinary timers without losing settings or notes.

### Sessions, colors and windows

- Placed the session name beside the nexa logo and centered Home and Session in the top bar.
- Moved the countdown and EDIT control into the pixel timer widget. Time edits preserve the active session identity, notes and transcript.
- Organized Session settings into Timing, Presets, Colors, Display, Hedy and Dashboard pages with a left menu and evenly proportioned fields.
- Kept Save color visible in the picker and made it apply and persist choices immediately.
- Fixed nexa app and menu colors independently of customizable dashboard main and accent/pixel colors, including menus opened from widgets.
- Preserved window dimensions when switching Home/dashboard or choosing Another preview.
- Standardized themed minimize, maximize/fullscreen, restore and close icons throughout the app.
- Combined Hedy tools with the session timeline and moved API key/region setup into settings. Existing standalone Hedy cards migrate without losing notes.
- Moved searchable session history to Home, retaining read-only previews, continue, merge, export and delete actions.
- Reworked Home’s bottom utility row in the order History, About nexa, Dev, Changelog and the Settings cog; it remains in the natural scroll flow on maximized windows.
- Rebuilt Settings as a dismissible two-pane Appearance/Hedy menu with an animated cog, full app themes, light modes and DPAPI-protected Hedy credentials.
- Added multi-day session scheduling and persistence, plus a confirmed Pixel Timer reset that restores its configured duration without deleting dashboard data or history.
- Removed fixed green status colors so completion and active states follow the selected app or dashboard theme.

## nexa 0.1.2 Alpha — Desktop Foundation (2026-10-01)

Establishes nexa’s local Windows desktop foundation.

- Local dashboard creation, templates and presets for classes, pantry shifts, church meetings, study, meetings and focus.
- Session timers, pixel progress, notes, checklists, counters, trackers and session continuity.
- Autosave, backup recovery, dated history, dashboard import/export and local Windows-account workspace ownership.
- Offline NLT, CSB and NKJV Bible reading, passage search and Scripture Pins.
- Editable sample dashboards, developer preview tools and Pixel Swarm.

The Windows desktop build is the current release. A web app remains planned.

## nexa 0.1.1 Alpha — Bug Fixes

General bug fixes and stability improvements during early Alpha development.
