# Nexa .35 — Home Light mode and installer appearance

Local candidate: `verification/release035-theme/`. No release was published and the production installation was not replaced.

Home's Nexa Default background now follows the selected Light/Dark/System mode. Light uses #EFF3F8, readable dark controls/text, and the original logo with only its white lettering changed to #171B26. Dark restores #121620 and white lettering. Classic and seasonal Home variants use the same rule; custom wallpaper selections remain intact. The background preview uses the same light/dark default.

SplashWindow, NexaChrome, app icons and original SVG assets have no changes. The logo override is applied only to the Home visual tree. Pixel comparisons confirm the colored logo tile is identical in Light and Dark.

Installer: `modern dynamic polar includetitlebar hidebevels` follows Windows appearance at launch (Inno Setup checks the preference once at startup). Separate light/dark welcome and header PNGs are rendered from the original vectors at 984×1884 and 330×330. Font: Segoe UI. Desktop and Start Menu choices are independent native checkboxes connected to the existing task selection and command-line options. The migration report uses a read-only memo so runtime text follows theme colors instead of retaining black rich-text formatting.

Validation:

- Checks build: succeeds; NuGet vulnerability-feed availability warnings only.
- Home Light rendered verification: 10 checks pass, including startup Light, Dark restoration, logo tile preservation, custom background preservation, returning to Default, seasonal Light, and unchanged app/title-bar icons.
- .34 regression route: 73 checks pass. Existing NTFS-junction fixture remains unverified under this token.
- .35 regression route: 108 checks pass, including the original detailed Home pumpkin and small flat decoration separation.
- Native installer lifecycle: 40 checks pass, including fresh/repeated installs, correct/corrupt/missing payload repair, private-save preservation, shortcut opt-out, blocked portable replacement, and fixture-only confirmed data removal.
- Native packaged executable: 17 startup/update checks pass. The initial sandboxed attempt could not complete startup; native retry succeeds.
- Native wizard reviewed on this PC's dark setting: sharp welcome logo, dark title bar, white text, folder browser, mode/destination/tasks/review pages, and dark exit dialog. Unchecking Desktop leaves only Start Menu in Ready to Install. Final migration viewer confirmed readable white text.
- Light installer mode is configured with its separate colors/artwork and the documented dynamic style; the Windows preference was not changed for a second manual visual pass.

Screenshots: `docs/media/nexa035/home-light.png`, `home-light-nexaween.png`.
Logs: `verification/theme035/`. Package digests: `verification/release035-theme/SHA256SUMS.txt`.

The candidate is built from base commit 5771d41 plus the working changes recorded in `verification/theme035/source.patch`; the base commit in build-identity.json does not by itself identify those working changes. No fresh full 1,308-assertion suite was claimed for this refinement.
