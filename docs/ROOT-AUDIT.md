# Root audit — 0.1.24 Alpha

Audited October 27, 2026 against the actual WPF project, root type references, resource includes and current ten-route verification suite.

## Archived in this pass

| Original root file | Archive | Reason |
| --- | --- | --- |
| `Story.cs` | `LEGACY/source/Story.cs` | No references to the story engine from the active app or current checks |
| `StoryArtwork.cs` | `LEGACY/source/StoryArtwork.cs` | No active instantiation or type references |
| `CHANGELOG.md` | `LEGACY/notes/CHANGELOG-through-20261027.md` | Full mixed historical/development log; replaced at root by the verified current release and archive link |
| `INTEGRATIONS.md` | `LEGACY/notes/INTEGRATIONS-0.1.6.md` | Old version-labeled guide; current account setup moved to `docs/INTEGRATIONS.md` |

Pre-naming-scheme `1.0.4` prompts, validation notes and checks were already under `LEGACY/`. The current root has no 1.0.4 release identity. Historical in-app release entries remain readable. Files are archived by their role and dependencies rather than by a version string alone.

## Compatibility retained deliberately

- `Models.cs` and continuity code retain old story fields so existing saves import and round-trip without discarding user data. They do not reference the retired engine/artwork.
- The historical internal `StoryArea`, `StoryClick`, `storyOpen` and `RenderStory` names now serve the **active Pixel Swarm** UI. Its implementation (`MainWindow.Boredom.cs`, `BoredomGame.cs`, `PixelSwarmRules.cs`) stays in the build.
- `LegacyBugHabitatMigration.cs` is required by existing-data migration. A legacy name alone is not evidence of dead code.
- `ReleaseNotes.cs` includes the in-app release history and current Nexaween entry. The historical project filename and namespace preserve build/save compatibility.
- Every other declared root type has references beyond its own declaration. Current tests, offline indexes, normal/seasonal artwork and reproducible publishing inputs are retained.

## Generated and personal files

`bin/`, `obj/`, `publish/`, `verification/` and the local current `Nexa.exe` are ignored build/acceptance outputs, not project source. Recovery backups and earlier executables remain local. They are excluded from the GitHub source tree and release upload. Release assets contain only the verified executable and its checksum; no profiles, credentials or recordings.

## Retained tracked root files

| File | Current role |
| --- | --- |
| `.gitattributes` | Repository policy and binary/data protection |
| `.gitignore` | Repository policy and binary/data protection |
| `AnalogClock.cs` | Active application / shared service; compiled by the current project |
| `App.xaml` | Active application / shared service; compiled by the current project |
| `App.xaml.cs` | Active application / shared service; compiled by the current project |
| `AssemblyInfo.cs` | Active application / shared service; compiled by the current project |
| `BibleLibrary.cs` | Active application / shared service; compiled by the current project |
| `BibleReader.cs` | Active application / shared service; compiled by the current project |
| `BoredomGame.cs` | Active application / shared service; compiled by the current project |
| `BugAnimation.cs` | Active application / shared service; compiled by the current project |
| `BugArtwork.cs` | Active application / shared service; compiled by the current project |
| `BugHabitatVisuals.cs` | Active application / shared service; compiled by the current project |
| `BugNavigation.cs` | Active application / shared service; compiled by the current project |
| `BugOverlay.cs` | Active application / shared service; compiled by the current project |
| `BugVisuals.Animation.cs` | Active application / shared service; compiled by the current project |
| `BugVisuals.cs` | Active application / shared service; compiled by the current project |
| `CHANGELOG.md` | Current project and release documentation |
| `Calculator.cs` | Active application / shared service; compiled by the current project |
| `ClockPicker.cs` | Active application / shared service; compiled by the current project |
| `ColorPickerButton.cs` | Active application / shared service; compiled by the current project |
| `CreateDesktopShortcut.ps1` | Current build, launch, desktop shortcut or Windows application configuration |
| `DarkControls.xaml` | Active application / shared service; compiled by the current project |
| `DarkDatePicker.cs` | Active application / shared service; compiled by the current project |
| `DashboardBugs.cs` | Active application / shared service; compiled by the current project |
| `DashboardContinuity.cs` | Active application / shared service; compiled by the current project |
| `DashboardCreation.cs` | Active application / shared service; compiled by the current project |
| `DistributionTracker.cs` | Active application / shared service; compiled by the current project |
| `DrinkArtwork.cs` | Active application / shared service; compiled by the current project |
| `ExperimentalUpdates.cs` | Active application / shared service; compiled by the current project |
| `GoogleAuthorization.cs` | Active application / shared service; compiled by the current project |
| `HedyClient.cs` | Active application / shared service; compiled by the current project |
| `HomeWindow.About.cs` | Active application / shared service; compiled by the current project |
| `HomeWindow.History.cs` | Active application / shared service; compiled by the current project |
| `HomeWindow.Recent.cs` | Active application / shared service; compiled by the current project |
| `HomeWindow.Settings.cs` | Active application / shared service; compiled by the current project |
| `HomeWindow.cs` | Active application / shared service; compiled by the current project |
| `Integrations.cs` | Active application / shared service; compiled by the current project |
| `InterfaceAudio.cs` | Active application / shared service; compiled by the current project |
| `LegacyBugHabitatMigration.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Bible.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Boredom.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Bugs.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.ClassSchedule.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Continuity.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Distribution.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.DistributionManagement.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Feedback.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Flashcards.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Focus.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Hedy.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Integrations.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Navigation.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.NotesHistory.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.PresetTiming.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Preview.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Productivity.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Recording.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Reliability.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Session.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.SessionWidgets.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Settings.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.SpaceBackground.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.TimeWidgets.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.TrackerIcons.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.WidgetFocus.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.WidgetPlacement.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.WidgetUndo.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.Widgets.cs` | Active application / shared service; compiled by the current project |
| `MainWindow.xaml` | Active application / shared service; compiled by the current project |
| `MainWindow.xaml.cs` | Active application / shared service; compiled by the current project |
| `MicrosoftAuthorization.cs` | Active application / shared service; compiled by the current project |
| `Models.cs` | Active application / shared service; compiled by the current project |
| `NexaBranding.cs` | Active application / shared service; compiled by the current project |
| `NexaChrome.cs` | Active application / shared service; compiled by the current project |
| `NexaConfirmation.cs` | Active application / shared service; compiled by the current project |
| `NexaEcosystem.Models.cs` | Active application / shared service; compiled by the current project |
| `NexaEcosystem.Simulation.cs` | Active application / shared service; compiled by the current project |
| `NexaEcosystem.Store.cs` | Active application / shared service; compiled by the current project |
| `NexaEcosystem.Window.cs` | Active application / shared service; compiled by the current project |
| `NexaFeedback.cs` | Active application / shared service; compiled by the current project |
| `NexaLogo.cs` | Active application / shared service; compiled by the current project |
| `NexaMotion.cs` | Active application / shared service; compiled by the current project |
| `NexaNotifications.cs` | Active application / shared service; compiled by the current project |
| `NexaPanels.cs` | Active application / shared service; compiled by the current project |
| `NexaReliability.cs` | Active application / shared service; compiled by the current project |
| `NexaSelection.cs` | Active application / shared service; compiled by the current project |
| `NexaWebGeometry.cs` | Active application / shared service; compiled by the current project |
| `NexaWindowBounds.cs` | Active application / shared service; compiled by the current project |
| `Nexaween.Animation.cs` | Active application / shared service; compiled by the current project |
| `Nexaween.Placement.cs` | Active application / shared service; compiled by the current project |
| `Nexaween.cs` | Active application / shared service; compiled by the current project |
| `OnboardingWindow.cs` | Active application / shared service; compiled by the current project |
| `PixelSwarmRules.cs` | Active application / shared service; compiled by the current project |
| `PixelTiming.cs` | Active application / shared service; compiled by the current project |
| `PreviewDashboards.cs` | Active application / shared service; compiled by the current project |
| `Publish-Exe.ps1` | Current build, launch, desktop shortcut or Windows application configuration |
| `README.md` | Current project and release documentation |
| `ReleaseNotes.cs` | Active application / shared service; compiled by the current project |
| `SessionContent.cs` | Active application / shared service; compiled by the current project |
| `SessionDashboard.csproj` | Current build, launch, desktop shortcut or Windows application configuration |
| `SettingsDialog.Autosave.cs` | Active application / shared service; compiled by the current project |
| `SettingsDialog.Bugs.cs` | Active application / shared service; compiled by the current project |
| `SettingsDialog.Navigation.cs` | Active application / shared service; compiled by the current project |
| `SettingsDialog.Profile.cs` | Active application / shared service; compiled by the current project |
| `SettingsDialog.Reliability.cs` | Active application / shared service; compiled by the current project |
| `SettingsDialog.Session.cs` | Active application / shared service; compiled by the current project |
| `SettingsDialog.Updates.cs` | Active application / shared service; compiled by the current project |
| `SettingsDialog.Widgets.cs` | Active application / shared service; compiled by the current project |
| `SettingsDialog.cs` | Active application / shared service; compiled by the current project |
| `SplashWindow.cs` | Active application / shared service; compiled by the current project |
| `StartupVerification.cs` | Active application / shared service; compiled by the current project |
| `TestAccess.cs` | Active application / shared service; compiled by the current project |
| `Theme.cs` | Active application / shared service; compiled by the current project |
| `TrackerIcons.cs` | Active application / shared service; compiled by the current project |
| `UserProfile.cs` | Active application / shared service; compiled by the current project |
| `WidgetActivity.cs` | Active application / shared service; compiled by the current project |
| `Widgets.cs` | Active application / shared service; compiled by the current project |
| `app.manifest` | Current build, launch, desktop shortcut or Windows application configuration |
| `run.bat` | Current build, launch, desktop shortcut or Windows application configuration |
