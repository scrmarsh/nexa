## Nexa 0.1.23 alpha - Living Worlds

Open **Arcade** from Home or Add Bugs to choose **COLONY WARS** or **LIVING WORLDS**. Create named worlds with random or entered seeds, load existing worlds, and observe the same terrain and insects on your dashboard or a pan/zoom battlefield. Oceans, islands, forests, deserts, rivers and snowy mountains stream through cached terrain chunks. Colony Wars saves stay separate. See [release notes and validation](RELEASE023.md).

## Nexa 0.1.22 alpha - GPU Performance Fix

Dashboard animation now uses timer-paced updates instead of driving invalidation from the compositor callback. GPU rendering, high-quality sprites and existing saves remain intact. The hardware diagnostic uses application-like pacing by default and keeps a legacy-loop comparison. See [measurements and limitations](RELEASE022.md).

## Nexa 0.1.21 alpha - Infinite Warfare

Arcade now resumes one persistent local civilization world. On the main menu, the tiny **I just wanna play with bugs** link beside Settings opens that same world on a widget-free battlefield. Dashboard and battlefield share armies, territory, webs, history and pause state. Pan with right/middle drag, zoom with the wheel, follow a civilization or selected bug, and return without restarting.

Infinite Warfare adds bounded strategic campaigns, functional territory and nest occupation, subtle territory colors, and irregular collaborative spider webs. Classic Rounds remains available separately. Local atomic saves include a previous-save backup and explicit recovery/reset controls. See [implementation, validation and known limits](RELEASE021.md).

## Nexa 0.1.20 alpha - Armies and Shared Webs

Arcade improves nearby-threat targeting, bounded retreats and surviving armies after leader loss. Spiders cooperate on connected radial webs that grow, slow enemies and can be cut and repaired. Established factions support 24-28 visible individuals with default 150/180 global soft/hard budgets. Resources and surviving reproductive adults govern limited recovery. Named factions, confirmed individual kills and a bounded run-only event history provide foundations for future civilization saves. See [implementation, verification and remaining limits](RELEASE020.md).

## Nexa 0.1.19 alpha - Colony Wars Overhaul

Arcade now fields six autonomous insect armies from invisible edge territories. Protected royal leaders assemble troops almost immediately; coordinated attacks, retaliation, defense, healing, dashes and aerial formations lead to colony conquest, victory or a draw, then an automatic fresh match. Stationary habitat widgets are retired while unrelated Space content is preserved.

The compact scoreboard tracks units, leader health, kills, match number and wins for the running simulation. Add Bugs offers pacing and per-species balance settings. Defaults are an 80-unit soft cap, 120-unit hard cap, four soldiers per wave and a 60 FPS target with a 30 FPS fallback. Spider projectiles apply brief control with immunity between hits; decorative webs are sparse, spaced, harmless and temporary. See [release implementation and validation](RELEASE019.md).

## Nexa 0.1.18 alpha — Bug Fixes

Nexa uses per-monitor DPI scaling and native vector bug artwork without adaptive downsampling. Scroll offsets update immediately; bugs retain widget attachments, and ordinary scrolling preserves web strands. Arcade is a bounded habitat of up to 240 bugs with slower movement and one spawn every five seconds, six large labeled habitat widgets placed in free space, and up to 18 spider-built organic webs roughly 480 pixels across, fly carrying, ladybug attacks, resistant roaches, ant scavenging and feeding-based centipede growth. Mixed spawns include an early centipede and about 15% centipedes thereafter. About 2% receive one of fifty names. Homes, combat, webs and names are exclusive to Arcade. Drag across exposed strands to cut; widget controls retain priority and dragging a widget breaks its anchors. Ants stay tiny while other species have distinct larger silhouettes. Existing startup delay and beverage improvements are retained.

## Nexa 0.1.17 alpha — Drink Counters, Laps & Bug Arcade

Water and Beer keep separate counts and goals, with larger water-glass and yellow beer-bottle visuals. Small count buttons stay below the artwork; expand **Options** for goals, reset and compact/expanded sizes. Beer goals start at one. Goals above four show “Slow down—consider drinking less.” Every two beers logged show “Take a sip of water.” for eight seconds, with a dismiss button.

Stopwatch adds **Lap**, with lap duration and total elapsed time. The list appears after the first lap, saves with the Space, and clears with the stopwatch Reset. **I'm bored** carries the current dashboard pixel color into Pixel Swarm.

Add Bugs offers **Spiders**, **Ladybugs**, rare **Centipedes** and **All Bugs**, while **All Three** keeps ants, roaches and flies. Ladybugs need two taps: the first flashes and cracks the shell; only the second squashes and rolls the 25% chance to burst into 4–16 new bugs. Centipedes have larger segmented bodies and animated crawling legs. In All Bugs, roughly 2% of natural spawns are centipedes; a centipedes-only selection uses a sparse population. Spiders spin small webs that briefly trap other bugs and expire after twelve seconds.

Select **Arcade** under Intensity to begin with normal activity and build automatically toward infestation. Faster successful hits and squashes increase pressure and accelerate progression. Infestation supports **600 bugs**, with a hard limit of **768** including bursts; Low and Normal retain their existing activity and 256-bug burst safety limit. Webs remain capped at 24. A single batched bitmap layer, shared pre-rotated sprites, cached crawl frames, offscreen culling and a frame-time guard keep decorative activity lightweight.

## Nexa 0.1.16 alpha — Stable Layout, Bugs and Counters

Widget placement is less aggressive, bugs follow dashboard content during scrolling, and the new Beer Counter tracks a separate goal with amber liquid and carbonation. Appearance choices are directly selectable.

## Nexa 0.1.15 Alpha — Add Bugs Settings Fix

Bug type and intensity are visible choices in Add Bugs settings. Select a type and intensity before or after enabling bugs; changes apply immediately and save automatically.

## Nexa 0.1.14 Alpha — Add Bugs

Under **Settings → Experimental Features → Add Bugs**, enable tiny ants, roaches, flies or **All Three**. Choose Low, Normal or Infestation density. Bugs crawl along widgets, scuttle through gaps or fly and land; click one to squash it. The small splat fades after four seconds. Empty overlay space leaves your dashboard controls usable. The feature defaults off and saves locally for your Nexa user.

## Nexa 0.1.13 Alpha — Pixel Swarm & Appearance

**I'm bored** opens a Pixel Swarm start menu. Move with WASD or arrow keys, aim with the mouse, and click to fire. Escape pauses. Survive increasingly hostile stages, collect white relief orbs and limited weapons, choose upgrades as you earn XP, and try to beat your best score. Return to Nexa at any time; the Space timer and autosave keep running. Scores and run statistics are local to your Nexa user.

Appearance now applies Light/Dark mode and the selected app color immediately across Home, Settings and Spaces. Both choices save for the next launch; a Space's own timer accent remains independent.

## Nexa 0.1.12 Alpha — Dashboard & Widget UX Update

The Pixel Timer stays anchored at the top-left and uses the same rounded edge as other widgets. It starts at the same height as an expanded digital-and-analog Clock. Drag the gap between it and a neighboring card to trade width or height; both dimensions save with the Space. Compact cards such as Clock can share the top row with an 8-pixel gap. Click the timer to set a target date and time, or choose **Use Today's Date** to propose one hour from now without starting a session. Cards show a blue landing preview and compact upward after moves, removals or smaller resizes. Drag near the viewport's top or bottom to scroll through a long Space.

Clock, Stopwatch and Calendar offer saved **Compact** and **Expanded** views. Session Timeline opens Audio Recorder and Hedy Notes without automatically recording. Global Settings sits immediately left of the per-Space **Session** button; Home Settings no longer shows a Session tab. Notes has a ruled writing surface, and Water Bottle Counter shows a glass that fills with the saved count.

The Calendar widget works disconnected and can show Google or Outlook calendars when connected. Choose a provider and calendar independently in each Space; account connections are managed in **Settings → Integrations**. Read-only calendar authorization is optional, and local Spaces remain usable offline. See [integration setup](INTEGRATIONS.md).

Add **Task List** for independent checklists with add, complete, remove and reorder controls; its title can be edited on the card. Notes absorbs Topics and Custom Fields, while Counter includes an optional Distraction mode in its title menu. Old widget kinds migrate when opened, preserving their content and placement. Focus Reset appears under Mindfulness. Clock and Timer provide Compact and Expanded controls, and expanded cards retain usable minimum sizes.

**Settings → Updates** under Experimental Features can check [the official Nexa releases](https://github.com/scrmarsh/SessionDashboard/releases) manually. Startup checks are optional and default off. A newer release can show its notes and download a published `Nexa-{version}-{Alpha|Beta|Stable}-Setup.exe` only when GitHub reports its SHA-256 digest. Nexa checks the download before asking Windows to start the external installer. No background download or automatic install occurs. A setup asset must be published separately; this source tree currently has no installer packaging pipeline.

This release keeps local Spaces, sessions, notes and integration credentials in their existing stores. The updater keeps its preference and verified downloads in an owner-scoped LocalAppData directory. See [validation](VALIDATION.md).

## Nexa 0.1.11 Alpha — Bug Fixes, UI Refinements & Pixel Timer Performance

The Pixel Timer now keeps even, fixed-size device-aligned pixels while resizing, maximizing and entering fullscreen. Nexa reuses pixel visuals and leaves the running session timestamps alone. Fullscreen returns to the prior window size or maximized state.

Drag a widget to see its landing position before release. A row can hold three cards when their minimum widths fit; unaffected cards stay where they are. Recent Spaces shows four cards in a vertical column and expands to more horizontal columns with the chevron. Settings keeps its autosave status in the footer and updates the Session page when a timer ends.

The dashboard has one square maximize control; press **F11** for fullscreen. Home's Settings, History, Changelog, About Nexa and DEV actions stay at the bottom while its main content scrolls. Drag a menu from blank space to move it. Add **Water Bottle Counter** from the widget library to count bottles in a Space and choose a simple goal; both values save locally.

History now offers a red **Clear History** action with a confirmation panel. It clears archived sessions across Spaces only after confirmation and keeps the Spaces and current content. Create Space has a fixed Back/Close footer, and Developer Spaces put **Play Sound** beside **Preview Splash**. Existing Spaces and saved layouts need no migration.

Targeted checks: `dotnet run --project tests/SessionDashboard.Checks.csproj -- --alpha011`. See [validation](VALIDATION.md) for what was tested and hardware limits.

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

New defaults are calm: Class has Notes and Clock; Study/Focus have Notes and Focus Reset; Church has Bible, Notes and Scripture Pins; Meeting has Notes and Task List. All include the Pixel Timer. Existing Spaces retain their widgets and saved configuration, and the complete widget catalog remains available.

Drag a grip to move a card directly; cards settle into a compact grid when released. Resize with any card edge or corner. Click the countdown to edit timing, the clock to toggle seconds, or a widget title to rename it. Secondary widget controls appear on hover or keyboard focus.

**Focus Reset** is available in the widget catalog. It gently dims the dashboard, guides five slow breaths for 40 seconds, asks you to look away for five seconds, and shows remaining time and a local note/transcript reminder. Return at any point; no online service is used.

Under **Settings → Appearance**, enable optional focus nudges or interface sounds and adjust volume. These preferences apply across Spaces and default to off. Nudges appear quietly on returning after three minutes away during Class, Study or Focus, with a ten-minute cooldown. Original soft chimes accompany task completion, adding widgets, completing Focus Reset and session completion.

Common controls use reusable vector icons. A Per-Monitor V2 manifest and WPF layout rounding improve sharpness between differently scaled monitors. Physical mixed-DPI monitor validation remains a manual release check.

# Nexa

Current release: **Nexa 0.1.18 alpha — Bug Fixes** · Windows desktop · October 15, 2026

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

Notes and readers start at half width; the anchored Pixel Timer acts as a permanent card and leaves room beside it for other widgets. Drag a shared edge to grow one card while its neighbor shrinks in place, subject to readable minimum sizes. Clocks start small and reveal a themed analog face when enlarged. Cards have rounded corners, an 8-pixel gap and useful minimum dimensions; minimized cards release their unused grid space and narrow windows reflow the layout. Bible keeps its internal reader scroll, and long Session Notes feeds may scroll inside their card.

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

**I'm bored** in the Pixel Timer opens Pixel Swarm. Lit timer pixels seed the arena. Choose **Play**, move with WASD or arrow keys, aim with the mouse, and click to shoot; hold to fire when you collect a Machine Gun. White relief orbs slow enemy pressure in Stage 2. Later stages add varied enemies, weapon pickups, XP upgrades and bosses. Escape pauses; **Return to Nexa** restores the Space. Session timing and autosave continue, and game stats are stored separately from dashboard exports.

The small **DEV** control on Home opens a Space with all available widgets and a timer starting halfway elapsed, plus splash preview and themed reset tools.

## Launch and build

A Release build writes `bin\Release\net10.0-windows\Nexa.exe`. Launch it from the project directory after closing any older Nexa instance:

```powershell
& '.\bin\Release\net10.0-windows\Nexa.exe'
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

### Standalone executable

Double-click `Nexa.exe` in the project folder, or `publish\win-x64\Nexa.exe`. This Windows x64 package includes the .NET runtime. Startup and embedded Bible resources are verified during packaging.

To rebuild using the locally cached .NET runtime packages, run `powershell -ExecutionPolicy Bypass -File .\Publish-Exe.ps1`. The script uses an isolated build directory and project-local NuGet configuration.
