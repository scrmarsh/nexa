# Space icon order follow-up

October 28, 2026. User clarification supersedes the earlier tile placement requirement: dashboard top bar is Space name then Space icon; Home tiles are Space icon then Space name.

MainWindow.xaml now places SessionNameHost before SpaceIconHost with an 8 DIP gap. HomeWindow.Recent.cs reverses the tile's internal columns while retaining centered grouping, 19 DIP titles, truncation and separate timestamp rows. The application brand icon is independent of the Space icon.

Release application/checks build: zero warnings/errors. nexa029: 87 passing checks, including actual rendered coordinate assertions for both icon positions. handoff026: 95 passing Home/navigation/animation checks. Total scoped validation: 182 assertions. No failures. Full integrity was not repeated for this small layout edit. Logs: docs/verification/nexa029/space-icon-order/.

Captures: [Home icon on the left](media/nexa029/home-icon-left.png), [top-bar icon on the right](media/nexa029/top-bar-icon-right.png). No data/settings migration, release or push.
