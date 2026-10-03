## Nexa 1.0.8 — Widget + Dashboard Stability (2026-10-03)

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
## Nexa 1.0.7 — Lightweight UI & Interaction Polish (2026-10-03)

- Stationary Settings button with a recognizable centered vector gear that alone rotates on hover.
- Physical widget pickup: the actual card follows the pointer with its grab offset and floats above its neighbors; layout changes wait until release.
- Intentional X/Y grid drops reserve the chosen column and row, reflow occupied slots, compact upward and save the final positions. Wide three-column layouts remain reachable; narrower canvases wrap responsively.
- Short native WPF settle animations for the moved and affected cards, including safe cancellation and pickup during a previous animation.
- Square, centered Pixel Timer cells with one device-pixel-aligned size and proportional spacing; no timing-state change.
- Remove only the top Focus Reset button; its widget, library entry and local guide remain.
- Expanded Bible widgets allocate additional height to the passage while preserving normal controls and scrolling.
- No schema migration, user-data reset, integration changes or new dependencies.

## Nexa 1.0.6 — Onboarding & Integrations (2026-10-02)

- Six-step welcome flow with optional name, multiple use cases, timed-session and focus preferences, simple space choices, curated tools, visual theme choices, opt-in sound preview and a ready-to-use first space.
- Existing installations bypass onboarding. Profile settings can rerun setup to append a space while preserving current dashboards and history.
- User-scoped local profiles, preferred widgets and default session behavior; legacy appearance/sound preferences copy safely without deleting original files.
- Optional Todoist and Google Tasks widgets share a small task-provider interface and list selection, date views, completion and creation controls.
- Read-only Google Calendar widget with calendar selection, today's schedule, current/next timed events and time until the next event.
- Dedicated integration settings, browser Google authorization code/PKCE, token refresh, DPAPI credential protection and clean disconnected/offline states. Google requires a configured desktop OAuth client; real-account connection testing was not performed.
- Credentials remain separate from profile/dashboard exports. No personal integration data is cached to disk. No workspace schema change or data reset.
- Verification repairs: clear stale task/calendar/account lists after failures or disconnects, reject results from replaced accounts, bind requests to the selected connection, and handle malformed authorization/configuration/provider responses safely.
- Preserve master sound, volume and focus-reminder preferences when switching Spaces in the same window.

## Nexa 1.0.5 — Focus First (2026-10-02)

- Full-width Pixel Timer banner using the existing session clock, with readable countdown, progress, duration and next interval. Pixel Swarm keeps its timer pixels and full-window game area.
- Simpler Class, Study/Focus, Church and Meeting presets; existing dashboards and the full widget catalog remain available.
- Grid placement preview, stable neighbors, collision-safe dropping, consistent resize snapping and saved dimensions.
- Direct countdown editing, a quick clock settings menu, editable titles and widget actions revealed on hover/focus.
- Offline Focus Reset: five breaths, a look-away prompt and local notes/time recap, with an immediate return button.
- Optional return-to-session nudges with three-minute absence threshold and ten-minute cooldown.
- Global optional original soft interface sounds with a master switch and volume.
- Reusable vector icons for common widget actions, settings, navigation and history; Per-Monitor V2 DPI manifest and layout rounding.
- No data reset or format version change. Original layout metadata remains until a direct placement needs absolute positions to keep neighboring cards stationary.

# Changelog

## [1.0.4] - 2026-10-02

### Polish and bug fixes

- Unified the project, Home and splash version at 1.0.4 and updated the splash tagline.
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
- Added full app themes, themed checkboxes, improved light-mode contrast and a theme-aware Nexa wordmark across Home and About.
- Added per-Space default, solid-color and local-image backgrounds with blur strength and dimming controls.

### Spaces, sessions and widgets

- Updated user-facing dashboard language to Spaces while retaining compatible internal storage identifiers.
- Improved responsive reflow so minimized cards release space and useful widgets can grow across the 20-column grid.
- Added a responsive analog face to the Clock widget and kept Bible reading internally scrollable.
- Redesigned Session Notes as a chronological interval feed with completed summaries, current-interval progress, Hedy deep links and a compact footer.
- Added explicit-permission local recording with pause, resume, stop, status, elapsed time, local WAV storage and deletion. Transcription remains clearly unavailable and no recording is uploaded.
- Added local Flashcards decks with card CRUD, flipping, navigation, shuffle, progress, Know it/Review state and CSV/JSON import.
- Updated Pixel Swarm and session surfaces to inherit the active theme and replaced the DEV reset system prompt with a common Nexa dialog.

## [1.0.3] - 2026-10-02

A more direct, polished session workspace for classes, study, church meetings, team discussions and service shifts.

### Home and release information

- Redesigned Home around the Nexa logo, with the version immediately below it and a Changelog button beneath the version.
- Added “a space for whatever comes next” to the right of the logo. The tagline types out, pauses and fades without shifting the layout, then repeats so it does not remain invisible.
- Replaced the system About message box with a themed, resizable menu containing the updated description and use cases.
- Moved Preview Nexa inside About and pinned View history at the bottom of Home.
- Added versioned changelog cards with readable, aligned bullet points.
- Set application, splash, Home and About version metadata to 1.0.3.

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

- Placed the session name beside the Nexa logo and centered Home and Session in the top bar.
- Moved the countdown and EDIT control into the pixel timer widget. Time edits preserve the active session identity, notes and transcript.
- Organized Session settings into Timing, Presets, Colors, Display, Hedy and Dashboard pages with a left menu and evenly proportioned fields.
- Kept Save color visible in the picker and made it apply and persist choices immediately.
- Fixed Nexa app and menu colors independently of customizable dashboard main and accent/pixel colors, including menus opened from widgets.
- Preserved window dimensions when switching Home/dashboard or choosing Another preview.
- Standardized themed minimize, maximize/fullscreen, restore and close icons throughout the app.
- Combined Hedy tools with the session timeline and moved API key/region setup into settings. Existing standalone Hedy cards migrate without losing notes.
- Moved searchable session history to Home, retaining read-only previews, continue, merge, export and delete actions.
- Reworked Home’s bottom utility row in the order History, About Nexa, Dev, Changelog and the Settings cog; it remains in the natural scroll flow on maximized windows.
- Rebuilt Settings as a dismissible two-pane Appearance/Hedy menu with an animated cog, full app themes, light modes and DPAPI-protected Hedy credentials.
- Added multi-day session scheduling and persistence, plus a confirmed Pixel Timer reset that restores its configured duration without deleting dashboard data or history.
- Removed fixed green status colors so completion and active states follow the selected app or dashboard theme.

## [1.0.2] - 2026-10-01

### Desktop foundation

- Local dashboard creation, templates and presets for classes, pantry shifts, church meetings, study, meetings and focus.
- Session timers, pixel progress, notes, checklists, counters, trackers and session continuity.
- Autosave, backup recovery, dated history, dashboard import/export and local Windows-account workspace ownership.
- Offline NLT, CSB and NKJV Bible reading, passage search and Scripture Pins.
- Editable sample dashboards, developer preview tools and Pixel Swarm.

The Windows desktop build is the current release. A web app remains planned.
