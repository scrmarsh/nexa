## Nexa 1.0.5 — Focus First

Nexa helps you stay present in class, study, church or meetings. The existing session clock now powers a full-width Pixel Timer banner with remaining time, total duration, progress and the next interval. **I'm bored** still opens Pixel Swarm across the window; timing and autosave keep running.

New defaults are calm: Class has Notes and Clock; Study/Focus have Notes and Focus Reset; Church has Bible, Notes and Scripture Pins; Meeting has Notes and Agenda. All include the Pixel Timer. Existing Spaces retain their widgets and saved configuration, and the complete widget catalog remains available.

Drag the grip to see a grid-aligned placement preview. Nearby cards stay still; occupied destinations place the moved card at the next free grid position below. Resize with any card edge or corner. Click the countdown to edit timing, the clock to toggle seconds, or a widget title to rename it. Secondary widget controls appear on hover or keyboard focus.

**Focus Reset** is available in the top bar and widget catalog. It gently dims the dashboard, guides five slow breaths for 40 seconds, asks you to look away for five seconds, and shows remaining time and a local note/transcript reminder. Return at any point; no online service is used.

Under **Settings → Appearance**, enable optional focus nudges or interface sounds and adjust volume. These preferences apply across Spaces and default to off. Nudges appear quietly on returning after three minutes away during Class, Study or Focus, with a ten-minute cooldown. Original soft chimes accompany task completion, adding widgets, completing Focus Reset and session completion.

Common controls use reusable vector icons. A Per-Monitor V2 manifest and WPF layout rounding improve sharpness between differently scaled monitors. Physical mixed-DPI monitor validation remains a manual release check.

# Nexa

Current release: **1.0.4** · Windows desktop · October 2, 2026

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
- **Session:** name, times, weekdays, 5/10/15-minute intervals and the active Space background.
- **Hedy:** optional API connection settings stored with Windows DPAPI protection.
- **Account:** a clear Coming Soon page without placeholder sign-in controls.
- **Data:** a clear Coming Soon page reserved for future export and storage tools.

Session changes preserve notes, transcripts and history identity. Weekday choices such as Monday and Wednesday persist. **Reset** beside the Pixel Timer’s Edit button asks for confirmation and restores the configured timer length without removing notes, widgets or history.

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

The current build is `bin\Release\net10.0-windows\Nexa.exe`. Launch it from the project directory:

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

Run checks with:

```powershell
dotnet run --project .\tests\SessionDashboard.Checks.csproj -c Checks
```

Nexa is implemented in C# and WPF. The original ClassDashboard and ArgoPantryDashboard references remain under `References`.
