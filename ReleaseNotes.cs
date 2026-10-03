namespace SessionDashboard;

internal sealed record ReleaseNote(string Version, string Date, string Summary, string[] Updates);

internal static class ReleaseNotes
{
    internal const string Description = "Nexa is a local desktop app for building flexible Spaces with sessions, movable widgets, notes, Scripture, flashcards, recordings and saved history.";
    internal const string UseCases = "Use Nexa for classroom lessons, study and focus sessions, church meetings, team discussions and work shifts. Build a Space around the information you need, edit it directly, and continue your saved work later.";
    internal static readonly ReleaseNote[] All =
    [
        new("1.0.5", "October 2, 2026", "Nexa 1.0.5 — Focus First",
        [
            "The signature Pixel Timer is a full-width banner using the existing session clock; Pixel Swarm remains connected to its pixels.",
            "New Class, Study, Focus, Church and Meeting Spaces start with fewer widgets; every supported widget remains available.",
            "Stationary neighbors and a grid placement preview make widget movement predictable. Resizing preserves useful minimums and saved dimensions.",
            "Click the countdown to edit session timing or the clock for a quick seconds toggle. Widget actions appear on hover or keyboard focus.",
            "Focus Reset offers five gentle breaths and a local notes/time recap. Optional return-to-app nudges have a ten-minute cooldown.",
            "Opt-in soft interface sounds, reusable vector controls and Per-Monitor V2 awareness keep Nexa calm and sharp."
        ]),
        new("1.0.4", "October 2, 2026", "Spaces become more personal, responsive and useful for study and live sessions.",
        [
            "Home now scales cleanly to maximized windows, keeps its footer in the natural scroll flow and shows permanent labels for Create Space, Resume and Import Space.",
            "Settings is a single dismissible two-pane window with Appearance, Session, Hedy, Account and Data pages, plus direct links to the requested section.",
            "Each Space can use its default background, a solid color or a local image with adjustable blur and dimming while widgets remain sharp and readable.",
            "The responsive grid reflows minimized cards, supports wider useful widget sizes and keeps Bible and long Session Notes content scrollable where needed.",
            "Clock cards gain a responsive analog face. Session Notes show chronological interval summaries and support optional Hedy or consent-based local recording.",
            "Flashcards add local decks, card editing, study controls, progress, review state and CSV or JSON import.",
            "Light mode contrast, themed checkboxes, story mode, autosave feedback, common dialogs and Space terminology have been polished throughout Nexa."
        ]),
        new("1.0.3", "October 2, 2026", "A more direct, polished session workspace.",
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
        new("1.0.2", "October 1, 2026", "The desktop foundation.",
        [
            "Local dashboards with presets, timers, notes, checklists and session continuity.",
            "Autosave, recovery, dated history, dashboard import and export.",
            "Offline Bible reading, Scripture Pins, sample dashboards and Pixel Swarm."
        ])
    ];
}
