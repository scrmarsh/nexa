# nexa Beta readiness — October 28, 2026

**Overall: NEEDS WORK. Keep v0.0.28 Alpha. No release authorization is inferred.**
Evidence and exact execution scope: [audit](NEXAWEEN_028_AUDIT.md),
[status](NEXAWEEN_028_STATUS.md). READY means the stated automated scope is
supported, not that every physical machine/live provider has been accepted.

Final .28 gate: Release build zero warnings/errors; all eighteen routes pass
695 distinct assertions. Eight separate close/view checks and two live read-only
release discovery checks also pass. Full execution scope and corrected failures
are recorded in status; these numbers do not certify the manual rows below.

| Area | Classification | Evidence / remaining acceptance |
| --- | --- | --- |
| Startup | READY | Shared metadata, three-second splash, native WPF startup/preview and handoff checks; clean Windows install remains separate. |
| Shutdown | UNVERIFIED | Normal close and ecosystem-view release tested; live recording shutdown and long-running sessions remain manual. |
| Saving | READY | Atomic replace, exclusive-lock failure/retry, corrupt-primary fallback and terminated writer process preserve committed identity/data; existing integrity routes retained. |
| Recovery | UNVERIFIED | Disposable process interruption and backup/snapshot recovery pass; physical power-loss/sleep/hibernate not certified. |
| History / widget persistence | READY | Current/archive/import/export fixtures pass, plus saves with deferred offscreen editors; no replacement storage architecture. |
| Timers | READY | Existing sequential rendering, manual/schedule drafts, timestamp, overnight and DST checks retained. |
| Scheduling | READY | Mon/Wed 13:00–14:15 sequence, +5m checkpoint, -10m warning, failed reset save, persisted postponement and repeated-action guard tested. Physical resume still manual. |
| Navigation | NEEDS WORK | Measured improvement in Developer loading; still ~1.1–1.3s in final reduced-motion sample and ~1.5s animated. Large workspace/physical input acceptance remains open. |
| Theme switching | READY | Seasonal visibility/hit testing/reduced motion and normal palettes pass inherited regression scope. Subjective/monitor appearance remains manual. |
| Branding | READY | .27 approved reference, 24-unit borders and classic white/light-orange X unchanged; existing exact-color/ICO/vector assertions pass. Physical approval checklist remains open. |
| Settings | READY | Current version, saved toggles/defaults/size, four behavior choices and independent appearance/costumes tested; complete physical interaction remains manual. |
| Fullscreen | UNVERIFIED | Existing F11/native monitor fixtures retained; actual taskbar, dialogs, multiple monitors and resize combinations require physical review. |
| Multimonitor / DPI | UNVERIFIED | Automated geometry does not certify 100/150/200% mixed-monitor movement or disconnect/reconnect. |
| Dashboard bug ecosystem | READY | One maintained world, stable IDs/positions, resources/nests/web construction and optional animation/defaults pass fixtures. Restored Creepy and immediate defense tested. |
| Advanced strategy / territories / leaders | NEEDS WORK | Active world has local raids and nests. Territory ownership, leaders, army victory rules/announcements and View Ecosystem UI are archived/excluded, not active functionality. Stage integration and save design before advertising them. |
| Species balance / functional combat webs | NEEDS WORK | Thirty ordered duel fixtures show differing survival; not army balance proof. Persistent drawn silk is present, but no active combat-web slow/trap effect was established. No blind damage rebalance applied. |
| Sustained performance | UNVERIFIED | Short cycle/simulation diagnostics are recorded in status; no several-hour, GPU, FPS or leak-free certification. |
| Integrations | BLOCKED | Hedy, Hevy and Todoist fixture identities remain separate. Live auth/microphone/retrieval/revocation require explicit test-account authorization; none used here. |
| Clean Windows installation | UNVERIFIED | SDK-host build succeeds; no clean VM/physical installation of a .28 distribution was performed. |
| Update compatibility | READY | Legacy Alpha ordering and mock feed/error/digest handling retained; older distributed binaries may require manual approved-build bootstrap. |
| Production package update | UNVERIFIED | No genuinely newer approved distribution installed; rollback/helper fixtures are not production-update acceptance. Do not publish Alpha to create a target. |
| Release packaging | UNVERIFIED | No .28 portable package promoted; prior .26 package provenance is historical evidence only. Prepare/test Beta package only when separately authorized. |
| GitHub configuration | UNVERIFIED | Existing release discovery evidence retained; no workflow, release, tag or asset changed. Beta publication/download verification remains a separate step. |
| Documentation | READY | Current README/changelog/version labels and targeted .28 reports updated; historical records preserved. |

No unresolved reproducible data-loss/startup failure is established by the final
automated checks. The demonstrated stale-reset defect was repaired. Optional
seasonal/strategic features do not automatically block a scoped productivity
Beta, provided the product accurately documents what is available and defaults
remain safe. This report does not declare the application ready for public Beta.

Before approval: complete the physical/live matrix in the .27 status (referenced
from .28 status), test a clean Windows install and real approved-package update,
review navigation on representative workspaces, and run sustained memory/CPU/GPU
tests. Decide whether advanced strategic simulation is a later optional feature
or a Beta scope requirement; implement it in explicit stages without replacing
the existing world manager. Additional Alpha updates are allowed.

When separately authorized, checkpoint source, change identity to v0.1.0 Beta,
prepare and test the distribution/migration, publish and verify public downloads.
Only after verified Beta publication should historical release cleanup be planned;
deleting release objects/assets requires separate approval. Preserve tags and
source history throughout.
