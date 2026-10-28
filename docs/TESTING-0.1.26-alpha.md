# nexa v0.1.26 alpha - testing after three passes

## Automated checks

- Pass 1's first full run caught missed-boundary review/reset timing. Fixed in the implementation; core then passed 64 checks. The original expectations were retained.
- Pass 2's fifteen-route regression passed at verification/acceptance-20261027-230410.
- Pass 3's full regression passed 506 checks at verification/acceptance-20261027-232244.
- Later tests cover independent candy respawning, save failure/retry, duplicate completion, original-occurrence recovery, actual guide gestures and restart, mode draft switching, signed scores, Hevy endpoint/header handling and encrypted storage.
- The old immediate-decoration-hide assertion now waits for the explicitly requested bounded fade. No saving/history expectations were removed.
- A candy-respawn test initially assumed exactly three timer ticks; it now waits for the observable result within a bounded deadline, accounting for a one-second lifecycle timer.
- One verification build overlapped an active test process and failed on a locked test DLL. It was rerun sequentially. Final build/suite outcomes are recorded below.

Native WPF Home, guide and recap images were inspected. These are actual controls rendered with synthetic data, not physical mouse or mixed-DPI certification.

## Comparable navigation profiling

Recovery source 278bbfe was exported into an ignored isolated source tree. The same eight-Space fixture and four navigation cycles ran serially on both revisions, with reduced motion to isolate view preparation/layout rather than animation delay. Baseline adaptation removes only new diagnostic hooks absent from the older source.

Baseline: verification/baseline026-source/verification/20261027-232133/navigation-profile.txt.
New source: verification/20261027-232236/navigation-profile.txt.

| Measurement (ms) | Baseline | New source |
| --- | ---: | ---: |
| First Space open | 680.26 | 421.15 |
| Warm Space opens | 329.20 / 343.55 / 372.97 | 232.78 / 230.56 / 751.67 |
| Return Home | 401.93 / 421.15 / 230.92 / 250.39 | 182.00 / 165.11 / 404.09 / 130.60 |
| Developer first layout | 1107.37 | 932.32 |
| Settings first layouts | 109.16 / 92.59 / 91.51 / 95.47 | 110.29 / 111.49 / 92.72 / 76.61 |
| Theme updates, including 30ms diagnostic pump | 24.49 / 160.90 / 146.32 / 45.21 | 143.70 / 58.76 / 167.56 / 143.22 |
| Infestation CPU workload, 90 simulated seconds | 118.22 | 100.32 |

Typical navigation improved in this small sample, but the 751.67ms Space-open and 404.09ms Home-return outliers remain. Settings/theme measurements do not establish an improvement. Simulation ended with 59 bugs in each run; these timings are not real-world display FPS.

Working set increased from 143,691,776 to 219,111,424 bytes in the baseline, and from 144,052,224 to 223,088,640 bytes in the new short run. Cached resources and a Developer page were present. This does not establish a leak or certify extended memory stability.

## Manual verification still required

- Physical mouse/keyboard guide use, rapid navigation and interruption, small windows, maximize/fullscreen, multiple monitors and mixed DPI.
- Repeated animated theme toggles, normal branding restoration, beverage colors, sounds and subjective animation/art acceptance.
- Real Windows sleep/hibernate, abrupt termination, live-data migration on an approved private fixture and lengthy classes.
- Actual Todoist/Hevy/Hedy accounts, provider authorization/revocation and updater installation/restart against a genuinely newer signed/verified package.
- Long-session memory, live rendering FPS with Infestation and investigation of the remaining load-time outlier.

Only synthetic data and isolated repositories/packages were used for this pass. No live user workspace was copied or rewritten.

## Final source results

- Full fifteen-route suite: **507 checks passed**, verification/acceptance-20261027-233017.
- Final artwork/reduced-motion follow-up: 36 pass checks, 27 alpha UI checks, 32 arrival checks and 30 layout/theme checks passed again (125 repeated checks, not added to the 507 distinct total). Artifacts: verification/20261027-233431 through verification/20261027-233449.
- Final Release application/test build: **0 warnings, 0 errors**.
- Native preview images, reviewed with synthetic data: [Home](media/preview026/home.png), [resize guide](media/preview026/guide.png), [recap](media/preview026/recap.png).
- Final geometry review moved the spider clear of the version label and joined vines to the actual selected pumpkin corners. Previously completed saved webs are visually revealed after their owners arrive; saved strand state is unchanged.

The testing candidate still requires the manual acceptance listed above. No physical UI, live integration account, live-data migration or real newer-release installation is claimed.
