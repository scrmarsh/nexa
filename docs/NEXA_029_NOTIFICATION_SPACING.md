# Fullscreen notification spacing follow-up

October 28, 2026. Continues clean checkpoint `6919df510ba5419cd265f9c8700b7f70b683f8a0` on codex/nexa-029-polish.

Fullscreen peripheral notifications now use the full viewport and an equal 8 DIP side/bottom inset. Previously they retained the taskbar's reserved work area, lifting bottom notifications above their intended baseline. Autosave/manual save, tips and other shared notification cards use the same bounds; the centered Productivity score/streak banner uses the same 8 DIP bottom baseline. Widget undo feedback also uses matching fullscreen edge spacing and updates when entering/leaving fullscreen. Standard windowed work-area positioning is retained. No persistence, timing, scoring or session behavior changed.

Changed: NexaWindowBounds.cs (shared fullscreen-aware notification area/inset), NexaNotifications.cs (shared card margins), ProductivityStreakOverlay.cs (same banner baseline), MainWindow.WidgetUndo.cs and MainWindow.xaml.cs (undo inset on creation/fullscreen toggle), tests/Nexa029Checks.cs (six actual layout/bounds assertions).

Release build: zero warnings/errors, isolated bin/Nexa029 output. Scoped regression routes: nexa029 85, reliability 19, audit025 38, arrivals 32, followup-design 31, handoff026 95: **300 passing assertions**. The six new assertions cover actual left/right/bottom card gaps, full viewport area, Productivity baseline, generic notification spacing and restoration of the standard windowed inset. No failures in this follow-up. The full 19-route suite was not repeated for this layout-only change; prior full-suite evidence remains scoped to its recorded commit.

Native WPF capture inspected: [fullscreen notification edges](media/nexa029/fullscreen-notification-edges.png). Build and route logs: docs/verification/nexa029/notification-spacing/. Physical multi-monitor/DPI acceptance remains open. No push or release.
