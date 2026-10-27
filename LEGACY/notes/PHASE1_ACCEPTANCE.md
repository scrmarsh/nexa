# Nexa v0.1.24 Alpha — Phase 1 acceptance

The complete master prompt has now been supplied. The user confirmed the full new ecosystem is required. Old Aevoria, Colony Wars, Living Worlds and battlefield code stays archived; implement the new ecosystem independently.

Confirmed timer override: elapsed digits count **up** during class; sequential pixels deplete toward the class end. Digits and pixels count **down** between classes. Version remains `0.1.24`; splash/home text is `nexa v0.1.24 alpha` without a suffix. Green branding and the requested drink colors remain.

## Verified core milestone

Release build: zero warnings/errors. Six sequential routes passed 165 checks on October 26, 2026:

| Route | Checks | Scope |
|---|---:|---|
| master | 23 | Weekly editor, count-up/countdown cycles, M/W gaps, overnight/DST, persistence, contiguous pixels, productivity UI |
| reliability | 19 | Zone rebasing, per-pin/task permanence, provider history/privacy, study events, filters, native work-area bounds, notifications |
| core | 64 | Autosave settings, failure-safe resets, postponement, session identity, Notes & History, seasonal palette and activation |
| continuity | 42 | Owner isolation, snapshots, exports, real 30-second continuous-typing autosave, close/reopen, stale writes, failed saves |
| shared | 10 | Existing shared widget geometry and saved normal appearance |
| menus | 7 | Panel dismissal, nested settings, preserved developer dashboard |

Logs: `verification/phase1-acceptance`. Individual logs contain artifact directories. Representative screenshots are inside those directories. Historical assertion descriptions mentioning the old mixed-case version text are legacy wording; the assertion now verifies the requested lowercase label.

## Changes in this pass

- Direct weekly class editor from the Pixel Timer and Session settings, day shortcuts, saved presets, automatic recurrence.
- Pure timestamp-based timer phases, deterministic pixel prefix, count-up digits during class, missed-start display reconciliation.
- Native monitor work-area maximization and real fullscreen bounds; raised, wrapped floating save/tip/error notifications and actual-operation save spinner.
- Preserved factual past UTC instants while rebasing future recurrence wall clocks to a new selected zone; provider calendar boundaries use that zone.
- Individual permanent Scripture pins, retained permanent tasks/content, session-scoped integration snapshot reset.
- Meaningful flashcard study activity and bounded, deduplicated local provider snapshots in centralized history. Configuration-only exports strip snapshots.
- Configurable productivity statistics and recent activity. Both productivity widgets render from the normal library; scoring runs independently.
- Session Timeline includes manual/Hedy/recording notes and lecture summaries, not the productivity feed.
- Valid focused/standard Nexa test project without the archived game dependency.

## Limits and remaining acceptance

- These are isolated tests; production user data was not edited or migrated.
- Physical sleep/hibernate, power interruption, multi-monitor DPI movement, and extended-session benchmarks still require system/manual verification. No such results are claimed.
- All seven autosave intervals were checked for immediate application/persistence; the continuous-typing duration test exercised 30 seconds, not every interval's full wall-clock duration.
- Provider snapshots were tested with synthetic observed data. Live Hedy, credentials, microphone/transcription and connected service requests were not exercised.
- Advanced animation work must wait until seasonal/ecosystem functionality is implemented and validated.
- Old packaged executables and output folders still require final archival cleanup and clean packaging; the currently running earlier candidate remains separate.
