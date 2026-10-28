# Session-load live ecosystem lifecycle

The .31 request explicitly supersedes persistence of active bugs/webs across loaded sessions. Permanent user data and normal crash recovery remain protected.

`WorkspaceDocument.LiveEcosystemGeneration` and `EcosystemDocument.SessionGeneration` are additive GUIDs. Old saves default to empty GUIDs. Every explicit Home session load records a new workspace generation before constructing the dashboard. In-place Space navigation explicitly marks a load; `CommitWorkspace` also detects changed Space/session identity, covering new sessions, scheduled occurrences and restored/resumed history. Failed workspace persistence returns before the live world is reset.

After the workspace commit, seasonal interaction actors are cleared and `NexaEcosystem.ReconcileGeneration` compares the authoritative generation. If different, it drains only existing worker disk I/O, rebuilds live simulation using current preferences and stamps the new generation. Active bugs, webs, construction/arrival queues, orders, combat participants, splats and temporary candy are cleared. New creatures arrive through the existing paced-spawn behavior; new webs start empty and require physical construction.

World identity/seed, configured options/species/density, permanent encounter/death totals, pending original-session squash events and user productivity ledgers remain. Notes, tasks, provider connections, history, recordings, layouts and wallpapers are outside this reset. No score is awarded by reset itself.

Snapshots are captured from the current manager on its dispatcher; worker writes are serialized. A pre-reset write is drained before the reset checkpoint. Queued interaction continuations subsequently snapshot the current generation, so they cannot resurrect old captured geometry. Pending awards retain original session/event IDs and use existing idempotent current/history accounting. A workspace-generation marker committed before a crash lets the next constructor finish an interrupted reset rather than showing the previous session's webs.

An ordinary restart without an explicit load keeps the same generation and restores the active world, including unfinished strands. Reconciling the same generation is idempotent. Corrupt/owner-blocked ecosystem data keeps existing safeguards and original files; reset does not bypass ownership or silently overwrite unreadable files.

## Silk and spider scope

Shared rounded anatomy and front-body leg roots apply to static and advanced ecosystem/Home rendering. Four roots per side fan into eight articulated legs. Coordinated phase, gaze and gait infrastructure is retained. Spiders slow their displacement while turning, and construction bypasses the generic widget-escape teleport.

New ecosystem webs use 12 outer supports, 12 radials and 48 connected outward spiral segments, all initially empty. Existing builder movement visits unfinished strand tips; progress follows the spinneret projection, and completed silk relaxes into a thin curved strand. Completion is not an opacity reveal. Curves are cached and invalidated with generation/geometry changes; no per-strand disk write is added. Existing helper ownership, traps/hunting and persistent progress infrastructure remain rather than being replaced with simulated visual claims.

The Home white corner silk remains a decorative backdrop/territory, not a simulated persistent ecosystem web. The Home companion's wandering/candy behavior and Home-only pumpkin candlelight are preserved. A completed-structure screenshot fixture is explicitly synthetic; actual construction is separately tested through movement/progress and interrupted-owner checks.
