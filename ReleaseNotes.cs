namespace SessionDashboard;

internal sealed record ReleaseNote(string Version, string Date, string Title, string Summary, string[] Updates);

internal static class ReleaseNotes
{
    internal const string Description = "Nexa is a local desktop app for building flexible Spaces with sessions, movable widgets, notes, Scripture, flashcards, recordings and saved history.";
    internal const string UseCases = "Use Nexa for classroom lessons, study and focus sessions, church meetings, team discussions and work shifts. Build a Space around the information you need, edit it directly, and continue your saved work later.";
    internal static readonly ReleaseNote[] All =
    [
        new(App.AppVersion, "October 3, 2026", "Widget System Cleanup & Experimental Updates", "Consolidates overlapping widgets and adds an optional, verified GitHub Releases update flow.",
        [
            "Task List, Notes and Counter absorb compatible older widget types without discarding saved content or layouts.",
            "Clock and Timer have direct Compact and Expanded modes; Focus Reset lives under Mindfulness.",
            "Experimental Updates can check official releases, show notes and download a verified setup installer after confirmation. Startup checks are off by default."
        ]),
        new("0.1.11", "October 3, 2026", "Bug Fixes, UI Refinements & Pixel Timer Performance", "Stabilizes window resizing, Pixel Timer rendering, widget placement and internal navigation.",
        [
            "Fixed-size, device-aligned pixels reuse visuals and update only when the grid changes after resizing settles.",
            "Fullscreen restores the previous window state; internal Space actions preserve window bounds.",
            "Widget drop previews show insertion and stable reflow, with proportional widths where a row can fit three widgets.",
            "Recent Spaces uses four compact cards in one column and a side chevron for older columns.",
            "Settings navigation, empty Session state and persistent autosave footer are clearer. Create Space has a fixed Back/Close footer.",
            "History gains a themed confirmation before clearing all archived sessions. Developer sound preview and title controls are refined.",
            "Only one maximize control appears; maximized navigation has top clearance, Home utility actions stay at the bottom, and menus drag from blank space.",
            "A local Water Bottle Counter tracks bottles and a directly adjustable goal per Space."
        ]),
        new("0.1.10", "October 3, 2026", "Interface & Navigation Cleanup", "Makes Nexa's internal panels, Home and dashboard navigation cleaner and more consistent.",
        [
            "Settings, Add Widgets, History, Changelog and About use shared panel chrome with a persistent bottom-left Close button.",
            "Escape and outside clicks dismiss the topmost panel. Outside clicks are consumed so Add Widgets cannot reopen from the same click.",
            "A dedicated Add Widget button sits beside Session; empty dashboard space stays clear and remains clickable.",
            "Scrollbar controls are hidden while wheel, trackpad and keyboard scrolling remain available.",
            "Home removes greetings and its duplicate Changelog link. Recent Spaces starts with four compact cards and expands with More.",
            "Subtle centered dashboard tips leave widget layout and pointer input untouched. Session timing, autosave and saved data are preserved."
        ]),
        new("0.1.9", "October 3, 2026", "Versioning & Session Stability", "Introduces Nexa's Alpha version numbering system, improves release history and version labeling, and fixes active-session state handling and settings persistence.",
        [
            "One shared Alpha version label across Home, About, splash and release history.",
            "Settings recognizes the existing session from the dashboard or Home without creating or restarting it.",
            "Settings saves changes automatically, debounces typing and sliders, and flushes edits before close or Space changes.",
            "Existing Spaces, notes, layouts, history, profiles and integration credentials retain their formats."
        ]),
        new("0.1.8", "October 3, 2026", "Widget + Dashboard Stability", "Improves widget placement, responsive session pixels and local recording while preserving saved Spaces.",
        [
            "Responsive Pixel Timer pixels, optional Circle shape and calm theme-colored completion share the existing session clock.",
            "Grid packing, anchored resizing and remove-widget Undo/Redo improve dashboard stability.",
            "Persisted backgrounds, widget preferences, precise stopwatch and Calculator improve local tools.",
            "Separate Hedy Notes, a source-neutral Session Timeline and consent-based local Audio Recorder preserve notes and history."
        ]),
        new("0.1.7", "October 3, 2026", "Lightweight UI & Interaction Polish", "Polishes lightweight navigation, widget movement and responsive dashboard presentation.",
        [
            "The centered Settings gear turns on hover while its button stays still.",
            "Drag grips lift the actual card and follow the cursor freely. Releasing reserves the selected grid position, reflows occupied slots and compacts gaps with a short settle animation.",
            "Wide dashboards support three columns, including intentional right-column drops; narrower canvases retain responsive two- and one-column layouts.",
            "Pixel Timer cells stay square and centered; expanded Bible widgets give extra height to the passage.",
            "Focus Reset remains in the widget catalog; its redundant top-header button is removed. Existing saved data and integrations are preserved."
        ]),
        new("0.1.6", "October 2, 2026", "Onboarding & Integrations", "Helps new users personalize local Spaces and adds optional task and calendar connections.",
        [
            "Six short welcome screens help new users choose tools, theme, sounds and a ready-to-use first space. Existing users go straight to Home.",
            "Local Profile preferences and a rerunnable welcome flow personalize use cases, preferred tools and default session behavior.",
            "Optional Todoist and Google Tasks widgets share project/list selection, task views, completion and creation controls.",
            "Google Calendar shows today's schedule plus current and next timed events with read-only access. Google connections require a desktop OAuth client configuration.",
            "Dedicated Integrations settings and Windows DPAPI credential storage keep optional accounts separate from dashboard exports and local core features."
        ]),
        new("0.1.5", "October 2, 2026", "Focus First", "Makes Nexa simpler and more focused with a signature Pixel Timer, calmer defaults and optional focus tools.",
        [
            "The signature Pixel Timer is a full-width banner using the existing session clock; Pixel Swarm remains connected to its pixels.",
            "New Class, Study, Focus, Church and Meeting Spaces start with fewer widgets; every supported widget remains available.",
            "Stationary neighbors and a grid placement preview make widget movement predictable. Resizing preserves useful minimums and saved dimensions.",
            "Click the countdown to edit session timing or the clock for a quick seconds toggle. Widget actions appear on hover or keyboard focus.",
            "Focus Reset offers five gentle breaths and a local notes/time recap. Optional return-to-app nudges have a ten-minute cooldown.",
            "Opt-in soft interface sounds, reusable vector controls and Per-Monitor V2 awareness keep Nexa calm and sharp."
        ]),
        new("0.1.4", "October 2, 2026", "Polish and Bug Fixes", "Spaces become more personal, responsive and useful for study and live sessions.",
        [
            "Home now scales cleanly to maximized windows, keeps its footer in the natural scroll flow and shows permanent labels for Create Space, Resume and Import Space.",
            "Settings is a single dismissible two-pane window with Appearance, Session, Hedy, Account and Data pages, plus direct links to the requested section.",
            "Each Space can use its default background, a solid color or a local image with adjustable blur and dimming while widgets remain sharp and readable.",
            "The responsive grid reflows minimized cards, supports wider useful widget sizes and keeps Bible and long Session Notes content scrollable where needed.",
            "Clock cards gain a responsive analog face. Session Notes show chronological interval summaries and support optional Hedy or consent-based local recording.",
            "Flashcards add local decks, card editing, study controls, progress, review state and CSV or JSON import.",
            "Light mode contrast, themed checkboxes, story mode, autosave feedback, common dialogs and Space terminology have been polished throughout Nexa."
        ]),
        new("0.1.3", "October 2, 2026", "Session Workspace Polish", "A more direct, polished session workspace.",
        [
            "Widgets appear where you click. Dragged cards lift with a shadow while neighboring cards animate, resize and make space.",
            "Readable widget sizes, three-column layouts, rounded cards, small gaps and faster scrolling keep dashboard data accessible.",
            "The session name sits beside the logo; Home and Session are centered. Countdown and EDIT are inside the pixel timer.",
            "Session settings have separate Timing, Presets, Colors, Display, Hedy and Dashboard pages. Save color applies and saves dashboard colors immediately.",
            "Nexa colors stay consistent across the app and menus. Dashboard main and accent/pixel colors remain customizable.",
            "Home and dashboard keep their size when switching. Matching minimize, maximize/restore and close icons appear throughout the app.",
            "Hedy joins the session timeline with API setup in settings. Bible and other unsuitable widgets no longer minimize; Pomodoro cards migrate to normal timers.",
            "Home gains an animated tagline, themed About and release-history menus. Preview lives in About; History is at the bottom of Home."
            ,"Settings gains Appearance and Hedy pages, full app themes and secure API-key saving. Multi-day schedules and a data-safe Pixel Timer reset complete the session controls."
        ]),
        new("0.1.2", "October 1, 2026", "Desktop Foundation", "The desktop foundation.",
        [
            "Local dashboards with presets, timers, notes, checklists and session continuity.",
            "Autosave, recovery, dated history, dashboard import and export.",
            "Offline Bible reading, Scripture Pins, sample dashboards and Pixel Swarm."
        ]),
        new("0.1.1", "Early Alpha", "Bug Fixes", "General bug fixes and stability improvements during early Alpha development.", [])
    ];
}
