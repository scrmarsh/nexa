# Updater validation and legacy bridge

October 28, 2026. No release, fake package or remote tag was published for testing.

## Confirmed cause and limits

The published [.24 updater source](https://github.com/scrmarsh/nexa/blob/4562d696f39e181e180d1b3b93fc4d060b941dce/ExperimentalUpdates.cs) and [.26 updater source](https://github.com/scrmarsh/nexa/blob/a308076fba1264f4bb64e53f4610e52caced3eab/ExperimentalUpdates.cs) were read at their original tag commits through the GitHub connector. Its comparator is numeric:

```csharp
candidate > current || candidate == current && ChannelRank(channel) > ChannelRank(currentChannel)
```

That rejects canonical `0.0.29` as older than installed `0.1.26`. The .24 source also used the earlier channel inequality that excluded higher-stage candidates from Alpha. The live feed was returned in `.26, .29, .24` order; choosing the first entry or sorting those raw legacy numbers is unsafe.

The new shared identity model explicitly canonicalizes confirmed Alpha history while keeping raw package version/URLs for validation. Discovery is paginated, rejects drafts/untrusted assets, checks channel eligibility and finds the newest identity without relying on response order. Genuine Beta numbering is preserved. Unconfirmed legacy-looking Alpha tags are rejected, and the reserved `.1.0-alpha` identity is invalid.

An incomplete newer package now raises an explicit metadata/asset error instead of becoming a false up-to-date result. An empty/ineligible feed is described as having no eligible package, not proof of currentness. HTTP 404, rate-limit/restriction, network/DNS, timeout and metadata failures have distinct messages. Startup checks remain optional, run after Home becomes usable and persist failures for Settings; channel preference persists. Updates is a permanent page with no Experimental subtitle.

**Important compatibility boundary:** new source cannot repair the comparator inside an already-installed old executable. Changing tags or release descriptions does not update that code. The corrected comparator is verified with an old-version input and the captured real feed; it is not represented as a remotely patched .26 binary.

## Existing-installation bridge without reinstalling

For affected old numeric-only installations, a one-time verified portable replacement is required:

1. Save current work and close nexa through its normal save/close flow.
2. Obtain the already published [.29 portable executable](https://github.com/scrmarsh/nexa/releases/download/v0.0.29-alpha/Nexa.exe), which includes canonical Alpha comparison, from the official repository.
3. Verify SHA-256 `badbdb24fc0345cc6ed00c27cc873bb92068f27194ca23f25452cba0f57cd9cd` against the official release asset/checksum.
4. Keep the old executable as a rollback copy and replace only the closed executable. Do not uninstall nexa or delete/move its user-data/profile folders.
5. Launch the fixed executable and use Settings → Updates for later eligible, approved releases.

This is a portable bridge, not a reinstall or a recommendation to erase user data. .31 is local/unpublished and is not advertised as a downloadable public update. A remote compatibility plan that retains the legacy URLs is [prepared for approval](NEXA_031_VERSION_MIGRATION.md).

## Real packaged verification

The approved cached .29 executable matches the live GitHub asset: 145,433,398 bytes, SHA-256 above. Tests use a **copy**, never overwrite that approved file. The new local self-contained Windows x64 single-file candidate is 72,660,057 bytes with SHA-256 `47C3B56503AC185E5F6E33642E8CBB4D1097376C4C8605D4DAE704520A06C24B`, version `0.0.31.0` / `nexa v0.0.31 alpha`.

The native packaged route passed **17 assertions**: original asset identity; genuine .29→.31 replacement; forced failed-health rollback to exact old bytes; unchanged workspace bytes/notes/permanent totals; durable Installed/RolledBack state; idempotent reapply; interrupted Installing recovery; tampered staged executable rejection; a separate real helper; separate restart with disposable data; and real startup health. Existing helper architecture remains unchanged. SHA-256/PE/product/x64 checks, backup, exclusive update lock, parent-exit wait and atomic replacement remain authoritative.

The restricted runner initially failed before managed startup, with `Failure processing application bundle` / inability to commit extracted files inside its sandbox temp directory. The direct stderr diagnosed that environment restriction. Repeating with normal Windows access succeeded, including the genuine upgrade and helper/restart tests. This does **not** mean users need administrator rights; it distinguishes normal Windows execution from the agent sandbox. The rollback test worked even during the restricted failure.

The normal desktop health report verified x64, `PerMonitorV2=true`, 759 square pixels, four presets and packaged Bible counts NLT 31,062 / CSB 31,086 / NKJV 31,102. This remains synthetic disposable data and does not certify physical mixed-DPI rendering.

See [test results and exact commands](NEXA_031_TEST_RESULTS.md), [final package checks](verification/nexa031/nexa031-package-final-checks.log), [native health](verification/nexa031/startup.json) and [sandbox diagnostic](verification/nexa031/nexa031-native-stderr.log). The initial restricted package restore logged NU1900 because NuGet vulnerability metadata was unavailable. A normal-network restore then succeeded without warnings, and the final committed-source build/publish completed without warnings/errors. The initial warning is retained in the failure journal; no audit suppression was added. Final informational version is `0.0.31-alpha+c1145c8fa3e1f37470712f8646ea76e91444216f`.

## Still unverified

The reported **second-computer** issue has not been exercised on a second physical computer. Validate official GitHub access/download, legacy bridge, eligible-version notification, actual shutdown/restart and preserved user data there before calling cross-device acceptance complete. No published executable, asset checksum or user workspace was altered to force a pass.
