# nexa v0.0.35 alpha — baseline audit

Audit date: October 29, 2026. Actual repository: `continuation-source`, remote `https://github.com/scrmarsh/nexa.git`. Starting branch: `codex/nexa-034-refinement`; clean HEAD `f7cd1da` (published .34 receipt), application baseline `66f6bbceb9ce2218ff3bc36bb47aeb2fadd2fd9d`. Work continues on `codex/nexa-035-ui-session-refinement`. No AGENTS.md was found in the workspace. The surrounding Nexa folder contains earlier staging scripts and a separate repository; it is not the application checkout.

Reviewed the .34 status, data/installer guide, verification report, release gate and publication records before implementation. Existing atomic navigation, finalization snapshots, source-controlled regression routes and installer lifecycle fixtures are retained. Publication of .34 does not authorize publication of .35.

## Findings and decisions

- Home's blue frame and purple border blur were distinct decorative layers, separate from saved solid/image wallpaper. Remove those layers and change only the default/fallback charcoal. Keep the left branding column and existing colored typing runs; use a zero-offset white text shadow for a small glyph-shaped halo.
- Profile previously used an inline, autosaving input. Add a dedicated owner-centered name editor with explicit Save/Cancel and validation, preserving the rest of Profile.
- Hedy recording/transcript references already identify the real Hedy implementation (`HedyClient`, Hedy-region/key state). Genuine `HevyConnection`, the Hevy vault identity, API key validation and workout endpoint are separate. Do not rename those to Hedy. Expose the existing Hedy settings once as a normal Integrations section, rather than hiding them in a separate expander. Existing Connect Hedy actions remain in the recording widgets.
- Living Worlds classes and their folder generator exist only under excluded `LEGACY/embedded-aevoria`. The active dashboard uses `NexaEcosystem` and `state.json.ecosystem.json` beside the canonical Nexa state. No Living Worlds rename is necessary. Narrow the general legacy migration so it does not import the other game's named folders. Never delete or modify that game data. Keep the tested ecosystem sidecar layout and ownership identifiers.
- Bug actors are rendered by a sibling overlay, not a child of WidgetScroller. An ordinary ancestor-only wheel search therefore misses the dashboard. Give the overlay an explicit scroll target in the existing single-consumer proportional wheel route.
- A window-opacity crossfade can expose the desktop. Use a frozen Home snapshot above a complete opaque Space, fading that image for 200 ms. Preserve the old opaque window until the reveal completes, with the existing atomic fallback when reduced motion is active or capture fails.
- Session finalization already archives real content, deduplicates recap identities, resets only established session values and preserves durable tasks/history. Home confirmation reuses that path and its existing recap; it does not invent a second completion mechanism.
- Timeline already has a flexible transcript viewport, but its common resize limit is 1,440 DIP. Raise the validated widget height limit to 2,400 DIP, including edge drag, form entry and save validation. Keep flexible viewport sizing and scroll overflow.
- Fullscreen already has a Core Audio endpoint wrapper with external-change notifications. Reuse it for the inline slider and mute button. Wi-Fi link state comes from wireless adapters, not internet reachability. Signal strength is explicitly unavailable through that interface; do not invent bars representing a measured percentage.
- Keep the large painterly Home pumpkin and its candlelight, as explicitly clarified by the user during implementation. Only the small background/widget-corner pumpkin decorations use simple two-orange/one-green vector shapes. Gameplay/costume art stays intact. Reuse actual appearance settings for Home and dashboard spiders and move shared leg roots behind the facial area.

## Scope safeguards

All test saves and generated packages remain under repository verification folders. No live user-data migration, installed-program replacement, GitHub push, release publication or Beta promotion is part of this work. Native installer testing, when recorded, uses the pre-existing separate fixture AppId and isolated app/data/shortcut folders.
