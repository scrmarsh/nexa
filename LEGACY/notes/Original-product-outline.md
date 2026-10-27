**Nexa** is a customizable multi-user desktop dashboard app designed to help people manage different types of sessions from one place. It can be used for classes, work shifts, church meetings, study sessions, meetings, focus time, or any other activity that benefits from a live dashboard.

Each user logs in with their own email and password and gets a completely private workspace. Their dashboards, notes, presets, history, tracker data, and saved sessions are kept separate from every other user.

Nexa is built around a universal widget system. The included Class, Argo Pantry, Church, Study, Meeting, and Focus dashboards are simply preconfigured presets. Users can fully customize them by adding, removing, moving, resizing, or configuring widgets, and they can create entirely new dashboards from scratch.

Examples include a Class dashboard with a timer, notes, session timeline, and Hedy tools; an Argo Pantry dashboard that tracks students and customizable item categories like freezer, fridge, fruit, or canned goods; and a Church dashboard with Bible tools, Scripture pins, notes, prayer requests, and meeting information. Widgets are reusable across all dashboards, so a Bible widget, tracker, timer, task list, counter, notes panel, or other tool can be placed wherever the user wants.

Nexa automatically saves active dashboard changes every 30 seconds and briefly displays a **“Saved at [time]”** indicator. Dashboards also retain their data after the app closes, including notes, students, counters, categories, tasks, and other session information. By default, reopening a dashboard continues from its most recently saved state.

Sessions are stored by date so users can browse previous work, view old notes and data, continue an earlier session, or use **MERGE TO CURRENT DASHBOARD** to bring information from an older session into the current one. A deliberate **RESET DASHBOARD** action archives the current session and starts a fresh one without destroying the dashboard's layout or customization.

Nexa also includes a lightweight boredom mode built directly into the pixel timer. **Pixel Blast** lets the timer digits fall apart into physics-based blocks that can be destroyed for points. Clearing them unlocks **I AM STILL BORED**, which launches **Pixel Swarm**, a survival game where the player dodges incoming pixels. The actual session timer continues running throughout, and returning to the dashboard reconstructs the timer using the correct current time.

Overall, Nexa is meant to be a **modular personal session workspace**: presets give users a starting point, widgets provide the building blocks, autosave protects their work, and each person can shape their dashboards around exactly what they need.

Features

- Start a session immediately with a chosen duration, or schedule it with local start and end dates and times. Sessions can cross midnight and last up to seven days.
- Track waiting, active, stopped, and completed sessions using timestamps. The dashboard shows a live clock, countdown, elapsed time, percentage complete, a 540-pixel progress display, and an interval timeline. Time continues to elapse while the app is closed or the computer is asleep.
- Use the included Lecture, Focused work, Meeting, and Studying presets, or create, update, and delete presets. Presets retain their title, duration, interval, scheduled clock times, and colors.
- Edit notes for individual timeline intervals. Notes are grouped by interval and remain available with the saved session.
- Optionally connect to a Hedy account with API access and cloud sync. The app can list and link Hedy sessions, periodically retrieve transcript updates, and file newly received text under the active dashboard interval. The full transcript and interval notes are saved locally. Remembering the API key is optional and protects it for the current Windows user.
- Keep personal notes and a checklist across sessions. Show or hide either panel.
- Customize the accent and pixel colors with six-digit hex values, choose a preset theme, use compact mode or fullscreen, and keep the window always on top.
- Save settings, presets, notes, checklist, session, and story progress locally in `%LOCALAPPDATA%\SessionDashboard\state.json`. Saves are atomic and retain a backup for recovery.
- Run as a self-contained Windows x64 WPF application; the published executable includes the .NET runtime.
