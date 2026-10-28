# nexa v0.1.26 alpha — testing

## Evidence collected so far

- Release build and tests project: initially passed with zero warnings/errors.
- Fourteen-route regression: **469 checks passed**, `verification/acceptance-20261027-222553`.
- New alpha route: 27 checks cover version, portable icons/fallbacks, six-card expansion and identity retention, lazy Settings/geometry, reduced motion, monotonic splash milestones, setup preference preservation and failed-navigation restoration.
- Native WPF Home image inspected: `verification/20261027-222553/home-026.png`; a tight Show More label was noticed and widened in a final follow-up.
- Initial new-regression run exposed changed-label expectations, a hardcoded 0.1.25 expectation and dispatcher context loss in synthetic async navigation. Corrected before the passing run. The runner now retains all route logs even after failures.
- Physical mouse/keyboard, mixed-DPI, maximized/restore, hibernation, long-session memory, provider credentials and complete tutorial/recap acceptance were **not** tested.

## Performance methodology

Three isolated instances with twelve saved sample Spaces, using the same constructor workload. This measures synchronous construction, not full interactive frame latency. Warm and cold results are separated; do not infer broad Space-load improvements from Settings results.

Before changes: `verification/20261027-221334/performance.txt`:

| Constructor | Cold ms | Warm 1 ms | Warm 2 ms |
| --- | ---: | ---: | ---: |
| Home | 524.72 | 14.99 | 33.04 |
| Settings | 452.52 | 32.60 | 27.07 |
| Space | 228.93 | 143.11 | 134.58 |

Final serial measurement and package verification are recorded below when completed. No guessed FPS, memory or load-time percentages are reported.

## Environment incident

A final build failed when C: ran out of free space (`MSB3883`). Temporary build files were redirected into `M:/Nexa/obj/temp026`, with a task-specific CLI home under obj. No disk cleanup or deletion of user files was performed. This was an environmental failure rather than a passed build.

## Final source verification

- Final redirected-temporary-directory Release build: **0 warnings / 0 errors**.
- Final fourteen-route run: **471 checks passed**, `verification/acceptance-20261027-223122`. The menu route now additionally verifies DEV timer preservation and unchanged outer bounds.
- New alpha Home render inspected at `verification/20261027-223123/home-026.png`. Seasonal tagline retains orange haunts; six sample cards and current alpha label are visible. This is native WPF rendering, not physical mouse testing.
- Live-data copy verification was not performed: automatic approval review rejected copying the potentially sensitive live state into the repository. No such copy occurred. Existing synthetic history, migration, saving and export checks passed; actual-user migration acceptance remains unverified.

Final serial constructor measurements: `verification/20261027-223409/performance.txt`:

| Constructor | Cold ms | Warm 1 ms | Warm 2 ms |
| --- | ---: | ---: | ---: |
| Home | 582.11 | 18.63 | 16.35 |
| Settings | 35.93 | 7.52 | 6.35 |
| Space | 284.60 | 155.95 | 154.85 |

Lazy Settings construction improved in these samples. Home and Space construction did **not** demonstrate an overall speedup; Space construction remains a performance acceptance gap. These are three samples per revision on the same machine, not a full benchmark, and exclude first-frame/animation completion. Developer end-to-end, theme latency, FPS and extended memory measurements remain unmeasured.
