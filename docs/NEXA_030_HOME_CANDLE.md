# Home-only candle flicker follow-up

October 28, 2026. The user requested visible candle flicker only on the homepage pumpkin.

The Home renderer is now the only caller of `NexaPumpkin.DrawLight`. Dashboard and widget-edge pumpkins retain their steady illustrated glow. The Home light has stronger, smoothly irregular warm fluctuations localized to the carved eyes and mouth; body, vines and position remain stationary. Existing seasonal-animation and reduced-motion gates remain effective.

The Release application/checks build passed with zero warnings/errors, using isolated output `bin/Nexa030Candle/` to avoid replacing the running executable. The existing `--nexa030` route passed all 63 assertions, including the adjusted candle amplitude/smoothness check and hidden/reduced-motion timer checks. The earlier full 849-assertion run remains evidence for the original checkpoint; it was not rerun for this small renderer follow-up.

```powershell
dotnet build tests/SessionDashboard.Checks.csproj -c Release -p:BaseOutputPath=bin/Nexa030Candle/ -p:UseSharedCompilation=false -nodeReuse:false -m:1
& tests/bin/Nexa030Candle/Release/net10.0-windows/SessionDashboard.Checks.exe --nexa030
```

[Build log](verification/nexa030/nexa030-home-candle-build.log), [focused checks](verification/nexa030/nexa030-home-candle-checks.log). Existing captures remain the original checkpoint's evidence. No release or push was performed.
