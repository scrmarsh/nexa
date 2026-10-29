# nexa v0.1.26 alpha — handoff candidate testing

Final application/test source: `25391210c751b9059e269bbb9229bc68fafcb74b`. This section concerns the unpublished handoff candidate; historical published-build evidence is retained below.

- **604 checks passed across all sixteen routes**, including 95 handoff checks. Release application/checks build: **0 warnings, 0 errors**. Evidence: `verification/acceptance-20261028-113810`.
- **10 native updater checks against the exact final executable**: `verification/20261028-114444`. Disposable targets only; rollback, byte replacement, data preservation, repeat/interruption handling, tamper rejection, external helper, isolated restart and separate-process startup. SHA-256: `043FD59E18D22002035FA1EBD7A95B4C35AD762871CEEB8C93190B766D06FD31`.
- **2 live unauthenticated public discovery checks** passed at `verification/20261028-113218`; the existing published v0.1.26 asset still reports digest `1D08B896653932F0470BF09EEF5464887BC9ABB0C539E8E91AC549B227E0868C`. No download asset, tag or release was changed.
- Exact final package startup is also exercised by the updater route's separate native-startup and restart-health processes: x64, PerMonitorV2, 759 pixels, four presets and all NLT/CSB/NKJV data. Runtime and native libraries are embedded; no adjacent DLL or installed .NET runtime is required by the package.
- Baseline, final reduced-motion and preceding-source animated profiles each passed their sixteen deadline/geometry checks. These repeated diagnostic assertions are not added to the 604 regression total. Final comparison uses a308076 versus 2539121, not a cherry-picked earlier candidate sample.
- Native desktop screenshots and interactions cover normal/seasonal/fullscreen Home, expanded/collapsed Spaces, mouse wheel, keyboard reachability, Developer, splash preview, early completion and recap/Home navigation. Offscreen WPF control renders cover onboarding, Settings/time-zone search, compact scores, spotlights and illustrated recaps. See the [scope, gallery and performance table](HANDOFF-COMPLETION-0.1.26-alpha.md).

Earlier failures were retained and diagnosed: obsolete expander/immediate-progress/activity-summary expectations; cross-thread delayed card cleanup and startup; a frozen template transform; missed in-window viewport reset; swallowed native wheel events; missing late clock start; and raw native zone labels. A newly added onboarding capture fixture initially attempted DialogResult after showing nonmodally; its progression was corrected. An early rebuild collided with a still-running native fixture and was rerun after closing that fixture. No failing route remains in the final full run.

Publishing succeeded but emitted NU1900 warnings because NuGet vulnerability metadata was unavailable. The Release application/checks build was warning-free; an online vulnerability-audit pass is not claimed. A sandboxed single-file startup attempt could not complete; desktop-context isolated startup and helper tests succeeded with workspace-local extraction. This is recorded as an environment limitation rather than silently counted as a pass.

Outstanding: live accounts/microphone, actual sleep/hibernate/crash/power failure, approved private-data compatibility, full guide/Settings/About physical interaction, mixed monitors/DPI, subjective audio/artwork and long memory/GPU/FPS runs. Developer layout and teardown remain material latency. This candidate is not fully manually accepted.

## Historical published-candidate evidence — three passes

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

## October 28 pumpkin and fullscreen follow-up

- The shared pumpkin silhouette was corrected; the preceding separate-lobe treatment looked like an oval attached to its side.
- The Release application/test build completed with zero warnings and zero errors after registering the test-only preview source in the project's explicit compile list.
- The widget attachment expectations were updated from 76 to 88 logical pixels to match the deliberately enlarged artwork. Resize/move and scroll-coordinate invariants are unchanged.
- A native Windows capture of Home filling the 2560 x 1440 primary monitor was inspected and saved as [fullscreen Home](media/preview026/home-fullscreen.jpg). It uses only synthetic Spaces. This verifies the visible primary-monitor presentation, not mixed-DPI or all physical interactions.
- The native update test now uses App.AppVersion instead of a stale 0.1.25 expectation.

- Full post-art regression: **507 checks passed across all fifteen routes**, verification/acceptance-20261028-080105. This includes widget layout/scrolling, persistence, scheduling, guide, score, ecosystem and animation checks.

- Packaged Windows x64 startup passed: verification/packaged026-20261028-confirmed/startup.json. The first shell invocation returned before the GUI process exited; rerunning with an explicit process wait confirmed a zero exit code and successful health report.
- Approved/root and both earlier candidate executable hashes remain unchanged.

- Exact packaged candidate native updater regression: **10 checks passed**, verification/20261028-080527. Rollback, isolated replacement/data preservation, idempotence, interrupted state reconciliation, staged tamper rejection, external helper installation, isolated restart and real-process startup all passed.
