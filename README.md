## Nexa 0.1.10 Alpha — Interface & Navigation Cleanup

Settings, Add Widgets, History, Changelog and About feel like internal panels with a persistent bottom-left **Close** button. **Esc** closes the topmost panel or its open dropdown. Click outside to dismiss a panel; that click is consumed so the widget catalog does not reopen or begin a dashboard gesture. Settings still autosaves before dismissal.

Use **Add Widget**, immediately beside **Session**, or click empty dashboard space to open the full widget catalog. Empty space stays clear of plus bars. Centered bottom tips rotate gently without changing widget layout or receiving mouse input. Scrollbar controls are hidden; mouse wheel, trackpad and keyboard scrolling remain available.

Home keeps Changelog in its bottom utility row, removes greetings, and shows the four most recent Spaces as compact cards. **More** reveals the remaining Spaces in responsive columns; **Less** restores four. Profile names and saved data remain in their existing local stores. No widget consolidation or Pixel Swarm redesign is included.

## Nexa 0.1.9 Alpha — Versioning & Session Stability

Nexa remains in Alpha. Version labels share the project version, and release history now uses 0.1.x Alpha with the early Bug Fixes entry restored. Settings recognizes the existing session from Home or the dashboard without restarting it. Ordinary Settings changes save automatically with a short debounce and a flush on close; no general Save/Apply button is needed. Invalid values retain the last valid setting. Failed writes keep edits for retry. Existing Space and preference formats are unchanged.

## Nexa 0.1.8 Alpha — Widget + Dashboard Stability

Pixel Timer now fills its right-hand canvas with square pixels that adapt in count to available width. Duration/progress and I'm bored sit on the left; completion remains calm and theme-colored. Settings → Widgets switches Square/Circle, distraction messages and the optional five-second focus backdrop pulse.

Drag the grip to move a card. Resize edges keep the active card anchored while neighbors move out of its way. Empty-space Add anchors the new card there; Recommended offers Session Timeline, Clock and Notes. Remove offers Undo for about ten seconds, with Ctrl+Z / Ctrl+Shift+Z; text editors keep native undo. Double-click a title, press F2 or use its context menu for advanced options. Choose Focus this widget in that menu to dim its surroundings; click the backdrop or Escape to return.

Stopwatch has milliseconds and an analog dial sharing one elapsed-time state. Calculator accepts arithmetic and decimals, with postfix percent (50% means 0.5), clear/backspace, Enter to evaluate, copy and recent history. Settings → Session supports persisted image backgrounds even before starting a timer. Home shows an optional profile greeting; save feedback appears only in dashboards.

Session Timeline works locally with manual notes and source-labeled entries. Hedy Notes stays separate and optional; Hedy credentials live under Settings → Integrations. Add Audio Recorder to capture local session audio with explicit microphone consent, Pause/Resume/Stop and elapsed status. Start a timed session first. Recordings use unique files beside the local state store under an owner-scoped Recordings directory; Open recording launches the saved audio. Switching or closing finalizes capture. A failed audio save keeps the buffer in memory for retry while the app stays open. **Transcription and summaries are not connected yet**; provider interfaces exist without uploading audio or choosing a paid service. Dashboard exports carry recording references, not embedded audio.

Existing spaces, names, interval data and history remain intact. Legacy Hedy widgets become Hedy Notes in place; earlier releases that already combined Hedy content into a timeline keep that content. New JSON fields have safe defaults; no workspace format migration is needed. Onboarding and Create Space remain unchanged. See [validation](VALIDATION.md).

Targeted checks: `dotnet run --project tests/SessionDashboard.Checks.csproj -- --stability108`.
## Nexa 0.1.7 Alpha — Lightweight UI & Interaction Polish

Drag a widget grip to lift the actual card and move it freely. On release, Nexa uses both drop coordinates, reserves the intended grid slot, reflows occupied cards, and fills gaps upward with a short settle animation. Wide canvases support three columns; narrower canvases wrap to two or one. Resize edges retain their grid preview and saved dimensions.

The Settings gear rotates inside a stationary button. Pixel Timer cells use a shared square size, center in the banner and fit display scaling. The Bible passage grows with its widget height. Focus Reset remains available as a widget; the redundant header button is removed.

Existing layouts are preserved until you move a widget. Drops save through the existing position fields; no format migration or data reset is needed. See [validation evidence and manual checks](VALIDATION.md).

## Nexa 0.1.6 Alpha — Onboarding & Integrations

New installations start with six short welcome screens: welcome/name, use cases and session preferences, space type, a curated tool selection, theme and sounds, then a summary and **Create my space**. Back and skip are available. Existing installations with saved data or preferences continue to Home.

**Settings → Profile** stores your local display name, use cases, preferred space/tools and default session behavior. Run the welcome flow again there to create an additional space without replacing current work. Themes, master sound volume and focus reminders remain under Appearance. New space creation uses these preferences.

Optional **Todoist**, **Google Tasks**, and **Calendar** widgets use the dedicated Integrations settings. Todoist accepts a personal API token. Google providers require a one-time desktop OAuth client configuration and connect through your browser. Tasks support list/project selection, Today/Upcoming/All views, completion and creation. Calendar is read-only and shows today's schedule and current/next timed events. See [setup and privacy notes](INTEGRATIONS.md).

Credentials are encrypted with Windows DPAPI, scoped to the local user, and excluded from dashboard exports. Integration data stays in memory; offline failures never disable local notes, timers, Bible, Focus Reset or autosave. No online Nexa account is required.

## Nexa 0.1.5 Alpha — Focus First

Nexa helps you stay present in class, study, church or meetings. The existing session clock now powers a full-width Pixel Timer banner with remaining time, total duration, progress and the next interval. **I'm bored** still opens Pixel Swarm across the window; timing and autosave keep running.

New defaults are calm: Class has Notes and Clock; Study/Focus have Notes and Focus Reset; Church has Bible, Notes and Scripture Pins; Meeting has Notes and Agenda. All include the Pixel Timer. Existing Spaces retain their widgets and saved configuration, and the complete widget catalog remains available.

Drag a grip to move a card directly; cards settle into a compact grid when released. Resize with any card edge or corner. Click the countdown to edit timing, the clock to toggle seconds, or a widget title to rename it. Secondary widget controls appear on hover or keyboard focus.

**Focus Reset** is available in the widget catalog. It gently dims the dashboard, guides five slow breaths for 40 seconds, asks you to look away for five seconds, and shows remaining time and a local note/transcript reminder. Return at any point; no online service is used.

Under **Settings → Appearance**, enable optional focus nudges or interface sounds and adjust volume. These preferences apply across Spaces and default to off. Nudges appear quietly on returning after three minutes away during Class, Study or Focus, with a ten-minute cooldown. Original soft chimes accompany task completion, adding widgets, completing Focus Reset and session completion.

Common controls use reusable vector icons. A Per-Monitor V2 manifest and WPF layout rounding improve sharpness between differently scaled monitors. Physical mixed-DPI monitor validation remains a manual release check.

# Nexa

Current release: **Nexa 0.1.10 Alpha** · Windows desktop · October 3, 2026

**Nexa is a local desktop app for building flexible Spaces with sessions, movable widgets, notes, Scripture, flashcards, recordings and saved history.** Build a Space around the information you need, edit it directly, and continue your saved work later.

Your workspace belongs to your signed-in Windows account and stays on this PC. There is no separate login or server. Timers, notes, records, history and Bible reading work offline; Hedy and online links use a network connection. A web app is planned; the Windows desktop app is the current release.

## Use cases

| Session | What Nexa helps you keep together |
| --- | --- |
| Classroom lessons | A lesson timer, interval notes, tasks and optional Hedy transcripts. |
| Study and focus | A session goal, countdown, assignments, research notes and distraction counters. |
| Church meetings | Bible passages, Scripture Pins, prayer requests, meeting notes and timing. |
| Team discussions | An agenda, decisions, action items and a saved session record. |
| Pantry or service shifts | Configurable records, student/customer fields, categories, totals and shift notes. |

Start with Class, Argo Pantry, Church, Study, Meeting or Focus, or build a blank Space. Widgets and presets can be customized for your work.

## Home

Home places the Nexa logo first, with the version immediately underneath and a **Changelog** button below it. To the right of the logo, **a space for whatever comes next** types out, pauses and fades away, then repeats after a short pause. The layout stays in place as the text fades.

The labeled icon actions **Create Space**, **Resume** and **Import Space** have no surrounding boxes and brighten and scale on hover. Recent Spaces offer direct access to saved work. Home scrolls naturally at every window size. Its footer follows the page content, with the animated Settings gear and autosave state on the left and **View History**, **Changelog**, **About Nexa** and **DEV** on the right.

**About Nexa** opens a themed menu with the current version, description, use cases and **Preview Nexa**. Preview opens a separate sample workspace with populated widgets, notes, tasks, a live timer and saved history. It does not load personal Hedy credentials. **Another preview** changes the sample without changing the window size; **Home** returns to your own workspace.

**Changelog** opens polished release cards with dated versions and aligned bullet points. The full release history is also in [CHANGELOG.md](CHANGELOG.md).

## Spaces

The top bar places the editable session name beside the logo, centers **Home** and **Session**, and keeps themed window icons on the right. The countdown and **Edit** control are in the full-width Pixel Timer banner. Switching between Home and a Space preserves window dimensions.

Click empty Space area or its faint asset plus to open **Add widget**. The chosen widget appears at that location without jumping to the bottom. The picker has icons beside left-aligned titles and descriptions.

Drag a widget's grip to lift the card with a shadow. Nearby cards animate and resize as you insert it between or beside them; open-space drops retain the chosen position. Cancel restores the previous layout. Titles, configure, duplicate and remove controls are available directly on screen. Resize from an edge or corner, or use Configure.

Notes and readers start at half width; the primary Pixel Timer occupies its own full-width banner. Clocks start small and reveal a themed analog face when enlarged. Cards have rounded corners, an 8-pixel gap and useful minimum dimensions; minimized cards release their unused grid space and narrow windows reflow the layout. Bible keeps its internal reader scroll, and long Session Notes feeds may scroll inside their card.

Minimize is available only for suitable widgets. Bible, Scripture Pins, session widgets, clocks and timers remain expanded. Pomodoro is retired; existing cards become normal timers while retaining their data.

## Settings, sessions and backgrounds

The Settings gear and **Session** button share one two-pane Settings window with these pages:

- **Appearance:** System, Light and Dark modes plus Nexa, Violet, Aurora and Ember app themes.
- **Session:** current session name, times, weekdays, summary intervals and the Space background. Untimed Spaces keep appearance controls without creating a session.
- **Widgets:** pixel shape, focus backdrop and distraction preferences.
- **Profile:** local name, use cases, preferred tools and session defaults.
- **Integrations:** optional task/calendar connections and Hedy API settings with Windows DPAPI protection.
- **Account:** a clear Coming Soon page without placeholder sign-in controls.
- **Data:** a clear Coming Soon page reserved for future export and storage tools.

Settings changes save automatically after a short pause and flush when Settings closes or the Space changes. Invalid values retain the last valid setting; write failures keep edits available for retry. Session changes preserve notes, transcripts and history identity. Name and appearance edits preserve exact timer timestamps, including seconds. Weekday choices such as Monday and Wednesday persist. **Reset** beside the Pixel Timer’s Edit button asks for confirmation and restores the configured timer length without removing notes, widgets or history.

Each Space can keep Nexa's default background, use a solid color, or display a local image. Blur affects only the image layer, while adjustable dimming keeps widget text readable. Background settings persist with that Space. Clicking outside dismisses Settings and other Nexa dialogs.

## Bible and Hedy

The offline Bible reader includes **NLT, CSB and NKJV**. Choose a translation, book, chapter and verse; search references or words; use previous/next chapter controls; and select text to save as Scripture Pins. Pins retain the passage, translation, selected text and an optional note. Selected text can also be dragged to a visible Scripture Pins widget.

**Session Notes** combines chronological interval summaries, Hedy session selection and live transcripts. API key and region setup live in Settings. The source can switch to **Local Recording**, which asks for microphone access and provides Record, Pause/Resume, Stop and Delete controls. WAV files stay on this PC and are never uploaded. Local transcription is not currently available, and Nexa does not fabricate transcript text.

## Flashcards

The local Flashcards widget creates decks and cards without an account. Flip cards, move Previous/Next, shuffle, track progress, and mark each card **Know it** or **Review**. Decks and study state persist in the Space. CSV and JSON deck imports are supported; future export controls belong in Data settings.

## Saving and history

Changes save automatically, with a 30-second safety-net autosave and a brief save indicator. Closing flushes pending edits. Reopening continues the saved session, including widget positions, content and configuration. Backup recovery helps protect local work.

**View History** at the bottom of Home searches saved sessions across Spaces. Filter by date, preview saved content, continue an earlier session, merge its data into a Space, export or delete an entry. Reset archives the current session and starts fresh while preserving Space customization. Import Space and Export Space support portable backups.

## Pixel Swarm and developer tools

**I'm bored** in the pixel timer opens Pixel Swarm. Lit timer pixels become enemies; move with the mouse, click or hold to shoot, collect power dots and survive while clearing the swarm. **Back to timer** or Esc restores the Space. Session timing and autosave continue; the game follows the active app theme and does not change saved session data.

The small **DEV** control on Home opens a Space with all available widgets and a timer starting halfway elapsed, plus splash preview and themed reset tools.

## Launch and build

A standard Release build writes `bin\Release\net10.0-windows\Nexa.exe`. The isolated Nexa 0.1.10 Alpha build is in `bin\Alpha010\Nexa.exe`, keeping any running older release undisturbed. Launch it from the project directory:

```powershell
& '.\bin\Alpha010\Nexa.exe'
```

Build with the .NET 10 SDK:

```powershell
dotnet build .\SessionDashboard.csproj -c Release
```

This build requires the .NET 10 Windows Desktop Runtime. A self-contained build can be created when the matching runtime packages are available:

```powershell
dotnet publish .\SessionDashboard.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\win-x64
```

Run targeted verification with:

```powershell
dotnet run --project .\tests\SessionDashboard.Checks.csproj -- --polish104
dotnet run --project .\tests\SessionDashboard.Checks.csproj -- --focus105
dotnet run --project .\tests\SessionDashboard.Checks.csproj -- --onboarding106
dotnet run --project .\tests\SessionDashboard.Checks.csproj -- --audit106
dotnet run --project .\tests\SessionDashboard.Checks.csproj -- --directaudit
dotnet run --project .\tests\SessionDashboard.Checks.csproj -- --continuityaudit
```

Additional focused modes are `--tiling`, `--swarm` and `--push104`. Credential tests need the normal loaded Windows profile for DPAPI and use synthetic credentials with mocked HTTP. See `VALIDATION.md` for actual results and unverified live-account/hardware checks.

Nexa is implemented in C# and WPF. The original ClassDashboard and ArgoPantryDashboard references remain under `References`.
