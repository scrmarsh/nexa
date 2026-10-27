# Legacy Versions

This file records preserved release snapshots for Session Dashboard. Each tagged source snapshot can be checked out and rebuilt if an older release is needed.

## v1.0.2 - Current release snapshot
- Release source is preserved by the Git tag `v1.0.2`.
- This snapshot includes the System/Light/Dark theme setting, homepage action icons, screen transitions, widget micro mode, Developer Mode tools, and the staged Pixel Blast/Pixel Swarm games.
- To reopen the exact source snapshot, run `git switch --detach v1.0.2`; to return to the development branch, run `git switch main`.
- To rebuild the Windows application from this snapshot, use `dotnet publish SessionDashboard.csproj -p:PublishProfile=WindowsStandalone` on Windows with the .NET 10 SDK.

## v1.0.1 - bug fixes

## v1.0.0 - Initial release
- Baseline desktop release for local workflow, dashboards, session timing, autosave, and history
- Stable release used as the foundation for all future updates
- Launch from the published app folder or from an archived release copy when an older build is needed

## Legacy access guidance
- Keep older executables or release directories in dated folders when they are no longer the active build
- Use the published Windows build as the current default while preserving older copies for fallback use
- When an older version is needed, open the archived build, confirm compatibility, and document any reasons it was restored

## Archive note
- This file is intentionally dedicated to previous versions so the current build can continue to evolve without losing access to earlier working releases
