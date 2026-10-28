namespace SessionDashboard;

internal sealed record ReleaseNote(string Version, string Date, string Title, string Summary, string[] Updates);

internal static class ReleaseNotes
{
    internal const string Description = "Nexa is a local desktop app for building flexible Spaces with sessions, movable widgets, notes, Scripture, flashcards, recordings and saved history.";
    internal const string UseCases = "Use Nexa for classroom lessons, study and focus sessions, church meetings, team discussions and work shifts. Build a Space around the information you need, edit it directly, and continue your saved work later.";
    internal static readonly ReleaseNote[] All =
    [
        new("0.0.29", "October 28, 2026", "NEXAWEEN: Interface Polish", "Nexaween Alpha prerelease focused on a cleaner, more consistent interface. Physical and provider acceptance remains open.",
        [
            "Interface polish leads this Nexaween update: clearer typography, larger Home titles with icons on the left, Space name before its icon in the top bar, and even twelve-DIP fullscreen notification padding.",
            "Completed sessions archive their original notes, counters and recordings before resetting session values. Persistent tasks, layouts, preferences and all-time statistics remain intact.",
            "Fullscreen uses native monitor bounds, compact top navigation, an overlay notice and optional Windows battery, connectivity and default-output volume indicators.",
            "Home and Space wallpapers are independent, with previews, fit/fill and restore defaults. Session Settings embeds the existing timer editor.",
            "Lowercase branding, five-second compact splash, Classic executable icon, larger Space titles, responsive transcripts, live productivity breakdown and playful distraction messages.",
            "History offers explicit resume or load-as-new choices. Friendly settings retain advanced legacy modes; spider silk is white and new webs use orb or corner construction."
        ]),
        new("0.0.28", "October 28, 2026", "NEXAWEEN EVOLVED", "Unpublished Alpha development. Navigation profiling, recovery verification and focused ecosystem improvements; Beta acceptance remains open.",
        [
            "Large Spaces reuse existing lazy loading for all offscreen widget bodies while keeping every saved model. Navigation diagnostics now include layout, source-window teardown and dispatcher-idle timing.",
            "Creepy behavior is selectable again through the existing simulation. Attacked creatures in Combative mode receive immediate defensive orders; no species damage rebalance was applied.",
            "Direct squashes activate a capped twenty-second 2x Bug Frenzy alongside the existing two-minute shared streak. Bonuses do not multiply each other; the greater eligible multiplier applies. Duplicate or delayed events cannot refresh the window incorrectly.",
            "Approved Nexaween branding and optional Advanced Animations remain intact. Recovery, scheduling and Beta readiness are documented with automated scope and outstanding physical/live acceptance."
        ]),
        new("0.0.27", "October 28, 2026", "NEXAWEEN", "Unpublished Alpha development. Continues the verified October 28 handoff; physical and live-account acceptance remains open.",
        [
            "Alpha development uses v0.0.X; the first planned public Beta is v0.1.0. Legacy 0.1.X Alpha packages compare by their development revision without changing package provenance.",
            "Adorable Mode and Advanced Animations sit together under Add Bugs. Advanced Animations defaults off; the existing animation renderer remains opt-in and respects reduced motion. Costume selection is independent of appearance.",
            "Startup and in-window Splash Preview retain shared branding, tagline ordering and real progress with a minimum three-second display.",
            "The supplied green/purple seasonal icon keeps its orange and light-orange hourglass with the selected bold black perimeter/silhouette outlines. Borderless wordmark letters retain their geometry; the X follows the classic contrast with a white top-left-to-bottom-right stroke and light-orange opposite stroke. Windows icon sizes are synchronized.",
            "Existing Home, seasonal decorations, six species costumes, scoring, timer drafts and save/history behavior are retained. The audit and next-version handoff record verification and outstanding acceptance."
        ]),
        new("0.1.26", "October 28, 2026", "Spaces in Motion", "Published alpha: session recaps, cumulative productivity, guided setup and seasonal animation. Subsequent handoff corrections are described separately above. Manual acceptance remains incomplete.",
        [
            "Home has paired Create and Import actions, six initial Space cards, and Show More / Hide More controls.",
            "Portable vector Space icons, saved with layouts and exports, appear on Home and dashboard headers.",
            "Settings pages initialize on demand. DEV preserves existing timer state and widget positions; navigation file preparation runs off the dispatcher.",
            "Tips use a 30-second interval and five-second lifetime. Productivity streak feedback is compact and flame-free. Normal beverage colors restore outside Nexaween.",
            "Productivity persists per Space. Candy and direct squashes share a bounded streak; saved End Session recaps retain actual elapsed duration and never duplicate completion.",
            "A larger web-bound home spider, organic vines, warm dimensional pumpkins and animated palette resources complete the seasonal presentation.",
            "The streamlined setup skips the redundant activity screen and offers Space icons and a seasonal toggle. The optional guide spotlights real widget actions and resumes after restart."
        ]),
        new("0.1.25", "October 27, 2026", "Dashboard refinements · development", "Intermediate testing work, not a public release.",
        ["Gradual bug arrivals, shared spider silk, compact tips, larger squash illustrations, widget jack-o’-lanterns and scrolling feedback refinements."]),
        new("0.1.24", "October 27, 2026", "Getting Candy for Trick-or-Treaters", "A space for whatever haunts you next. Synchronized session progress, reliable saving, a consolidated interface and a seasonal bug ecosystem.",
        [
            "The Dashboard Has Been Haunted — Seasonal branding uses the checked-task accent. The animated Home tagline is retained; only haunts is orange. Candy backgrounds, contextual widget corners and curved vines stay clear of controls.",
            "Time Is Under a Spell — Session pixels fill from the top-left, clear in reverse between classes, and share timestamp-based progress with linked bars. Weekday/date/duration editing, time zones, three-decimal precision and controlled time-picker scrolling.",
            "Creepy Crawlies — Classic, Cozy and Combative behaviors have independent Classic, Creepy and Halloween appearance controls. Species and density update the maintained population. Widget-edge paths, visible flight and progressive spider-owned webs share one persistent ecosystem.",
            "Productivity Witchcraft — Prominent lightning score and centered multiplier. Direct squashes start two-minute streaks with 0.5 steps and a configurable cap. Saved positive actions get green edge feedback; configured negative categories get red feedback.",
            "The Archives Awaken — Central readable history, continuous/event-driven saving, configurable checkpoints, revision protection, saved recovery state and save-before-reset safeguards remain in place.",
            "Spells of Convenience — One Session entry for history, saving and tips; summary interval in Timeline; unified Appearance and separate Sounds. Pumpkin settings and on/off switch, real startup stages, left autosave notices, right tips, and extended dashboard scrolling."
        ]),
        new("0.1.23", "October 19, 2026", "Decorating for Halloween", "The Halloween preparation release.",
        [
            "Retained as Decorating for Halloween. Superseded Living Worlds and game-development material is preserved separately in LEGACY."
        ]),
        new("0.1.22", "October 19, 2026", "GPU Performance Fix", "Timer-paced rendering preserves hardware acceleration and avoids a compositor-driven update loop.",
        [
            "Dashboard bug updates use the existing paced timer instead of advancing and invalidating inside every composition callback.",
            "Hardware diagnostics now default to application-like timer pacing and retain the legacy render loop for controlled comparisons.",
            "GPU rendering, high-quality sprites, software fallback, existing world saves and gameplay rules are retained."
        ]),
        new("0.1.21", "October 19, 2026", "Infinite Warfare", "One persistent insect world, shared by your dashboard and a widget-free battlefield.",
        [
            "Infinite Warfare is the default. Surviving civilizations continue after victories; safe migration creates new identities. Classic Rounds remains a separate temporary dashboard mode.",
            "Local snapshots preserve armies, resources, leaders, strategy, territory, damaged webs, recent history and random state. Atomic saves, backups and explicit recovery protect existing progress.",
            "The tiny home link, I just wanna play with bugs, resumes the same world on a blank battlefield. Add Bugs also opens it, with pause, recovery, reset, pan, zoom and follow controls.",
            "Home guards, patrols, scouts, reserves and expeditionary forces support faction campaigns and withdrawals. Sustained occupation captures territory and settlements; survivors can reclaim their homeland.",
            "Subtle territory colors, borders and labels follow ownership. Irregular shared webs build strand by strand, trap temporarily, connect and repair. Dashboard scrolling preserves world coordinates."
        ]),
        new("0.1.20", "October 19, 2026", "Armies and Shared Webs", "Larger armies, active retaliation and survivor recovery in Arcade Colony Wars.",
        [
            "Nearby threats take priority over distant leaders. All six species retain distinct attacks, cooldowns and defensive abilities.",
            "Spiders cooperate on bounded radial webs, repair cut strands and ambush enemies slowed by contact. Web control expires and grants immunity.",
            "Default colonies support 24-28 visible individuals with configurable 150/180 soft/hard budgets. Reinforcements consume colony reserves.",
            "Leader loss preserves survivors. Eligible reproductive adults can rebuild once, spending reserves after regrouping safely at home; ants require their queen.",
            "Named armies, confirmed individual kills and earned warrior titles feed a bounded simulation event history. History remains local to the running simulation."
        ]),
        new("0.1.19", "October 15, 2026", "Colony Wars Overhaul", "Six autonomous armies battle for their royal leaders in continuously restarting matches.",
        [
            "Stationary habitats are removed. Six leaders enter from invisible edge territories immediately, followed by protective waves of soldiers.",
            "A shared targeting and damage system gives every species attacks, retaliation, defense, retreats and formations. Ant coordination, roach armor, ladybug healing, centipede dashes and fast aerial fly squads have distinct limits.",
            "Spider combat webs are visible, temporary projectiles with cooldowns and anti-stun protection. Sparse decorative webs do not collide, cannot overlap nearby webs, and fade automatically.",
            "Leader deaths eliminate colonies, queue credited conquest banners and end in victory or a draw. The battlefield clears and automatically starts another match.",
            "A compact live scoreboard tracks units, leader health, kills, match number and wins for the running simulation. Configurable balance, 80/120 soft/hard population caps and a 60 FPS target keep activity bounded."
        ]),
        new("0.1.18", "October 15, 2026", "Bug Fixes", "Sharper displays, smooth scrolling and a calmer interactive arcade.",
        [
            "Dashboard widgets render first; bugs wait three seconds, spawn more gradually and run below input priority at a 30 FPS target.",
            "Per-monitor DPI scaling, vector bug rendering and immediate scroll offsets preserve sharpness and widget attachment.",
            "Add Bugs offers Ants, Roaches, Flies, Spiders, Ladybugs, Centipedes and one All Bugs choice containing all six types.",
            "Arcade supports up to 240 bugs, spawning once every five seconds. Six large labeled habitat widgets occupy free slots and retain their positions. Centipedes arrive early and make up about 15% of subsequent mixed spawns. Spiders build up to 18 larger organic webs with widget anchors; drag exposed strands to cut them.",
            "Names have a 2% chance from fifty names. Combat, homes, webs and names are exclusive to Arcade. Ants stay tiny; other species have distinct larger silhouettes.",
            "Water uses an unbranded ridged plastic bottle. Alcoholic Beverage replaces Beer Counter and offers Wine Glass, Shot Glass and a plain slim Beer Bottle, with progress-driven fills and saved vessel choice."
        ]),
        new("0.1.17", "October 14, 2026", "Drink Counters, Laps & Bug Arcade", "Larger drink visuals, stopwatch laps, new species and escalating Arcade bugs.",
        [
            "Water and Beer remain separate widgets with larger glass and yellow bottle artwork, compact controls and distinct catalog icons.",
            "Beer goals start at one; goals above four show a gentle warning and every two beers logged prompt a sip of water.",
            "Stopwatch laps appear after the first lap and persist with the Space. Pixel Swarm retains the dashboard pixel color.",
            "Squashing has a one-in-four chance to burst into 4–16 bugs. Spider webs hold captured bugs until a spider approaches, feeds and spins another web.",
            "Ladybugs take two taps with a cracked-shell first-hit cue. Centipedes are rare, larger segmented crawlers.",
            "Infestation supports 600 bugs. Arcade starts normally and escalates automatically; rapid hits accelerate progression. Shared sprite frames keep animation lightweight."
        ]),
        new("0.1.16", "October 5, 2026", "Stable Layout, Bugs and Counters", "Keeps widget placement predictable and expands the Add Bugs and counter features.",
        [
            "Widget drops preserve unaffected cards and reject occupied positions; removing a card leaves its space open.",
            "Bugs use dashboard content coordinates while scrolling, with stronger ant and roach behavior and a larger infestation.",
            "Water gains a deeper liquid visual; the new Beer Counter has its own goal, golden liquid, foam and carbonation.",
            "Appearance choices are directly selectable and Home footer spacing is improved."
        ]),
        new("0.1.15", "October 4, 2026", "Add Bugs Settings Fix", "Makes bug type and intensity selectable directly in Settings.",
        [
            "Bug type now offers Ants, Roaches, Flies and All Three as visible choices.",
            "Intensity now offers Low, Normal and Infestation as visible choices.",
            "Choices can be set before enabling bugs, apply immediately and persist when Settings is reopened."
        ]),
        new("0.1.14", "October 4, 2026", "Add Bugs", "Adds optional animated insects to Spaces.",
        [
            "Add ants, roaches, flies or all three inside a Space at Low, Normal or Infestation density.",
            "Bugs follow widget edges, move across the dashboard and can be squashed with a click.",
            "The feature is off by default, stores preferences per Windows user and pauses when the Space is hidden."
        ]),
        new("0.1.13", "October 4, 2026", "Pixel Swarm & Appearance", "Adds a staged Pixel Swarm game and live appearance updates.",
        [
            "Pixel Swarm has a start menu, escalating stages, upgrades and local best-score tracking.",
            "Appearance choices update live and persist across restarts."
        ]),
        new("0.1.12", "October 3, 2026", "Dashboard & Widget UX Update", "Improves dashboard placement, simplifies the Pixel Timer, adds responsive widgets and optional calendars while preserving local Spaces.",
        [
            "Task List, Notes and Counter absorb compatible older widget types without discarding saved content or layouts.",
            "Cards compact after edits, support edge scrolling and show a blue landing preview. The anchored Pixel Timer resizes horizontally and leaves room for a neighbor.",
            "Clock, Stopwatch and Calendar have direct Compact and Expanded modes. Session settings belong to each Space; the timer target menu is simpler.",
            "Session Timeline opens the local Audio Recorder or Hedy Notes. Notes has a ruled writing surface and Water Bottle Counter has a filling glass.",
            "Google and Outlook calendars can show a selected agenda in a Space. Connections are optional and stored with Windows protection.",
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
