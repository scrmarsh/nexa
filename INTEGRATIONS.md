# Nexa 1.0.6 integrations

All connections are optional. Local notes, timers, Focus Reset, Bible, layouts, themes, history and autosave work without an account or internet access. Add widgets from the usual widget catalog; manage accounts in Settings → Integrations.

## Todoist

1. Copy your personal API token from Todoist Settings → Integrations → Developer.
2. Paste it into the masked Todoist field in Nexa Settings → Integrations and choose Connect. Nexa validates the token before saving it.
3. Add a Todoist widget, select a project or All projects, and choose Today, Upcoming or All. Today includes tasks due today; Upcoming includes future dated tasks; All also includes undated tasks.
4. Check a task to complete it in Todoist, or enter a title and choose Add task. New tasks have no due date; Nexa switches to All so they remain visible. Refresh updates the list.

Connection uses Todoist's supported API token flow, not an embedded account/password sign-in. See the [official API documentation](https://developer.todoist.com/api/v1/).

## Google Tasks and Google Calendar

These providers are implemented but require a registered **desktop OAuth client**. No Google app registration or credentials ship in this repository. Live account validation requires your own configuration.

One-time setup:

1. In your Google Cloud project, enable the Google Tasks API and Google Calendar API. Configure the OAuth consent screen and register an OAuth client of type Desktop app. If the consent screen is in testing, add your intended account as a test user.
2. Download the desktop credentials JSON into a private folder outside this repository.
3. Choose **Set up Google connection…** in Nexa's Integrations settings and select that file. Nexa encrypts the imported client configuration with Windows DPAPI; it does not copy a plaintext configuration into its storage.
4. Choose Connect for the desired service. Your normal browser opens Google's consent screen. Return to Nexa after granting permission. Closing Integrations cancels a pending connection; browser authorization also times out after three minutes.

Tasks requests the Google Tasks scope for reading, completing and adding tasks. Calendar requests only `calendar.readonly` for calendar selection, today's schedule, and current/next timed events. Nexa has no external calendar create, update or delete operations.

Google sign-in uses authorization code + PKCE, a random state value, and a temporary loopback listener bound to `127.0.0.1`. Refresh authorization is renewed when needed. Google may restrict testing accounts or require consent verification for wider distribution. See [desktop OAuth setup](https://developers.google.com/identity/protocols/oauth2/native-app), [Google Tasks reference](https://developers.google.com/workspace/tasks/reference/rest), and [Calendar events reference](https://developers.google.com/workspace/calendar/api/v3/reference/events/list).

## Privacy, storage and failures

- Profiles and preferences live under `profiles/<hash of Nexa user ID>/` beside the existing local store. Existing eligible theme/sound preferences are copied once; originals are retained.
- Credentials use Windows DPAPI CurrentUser encryption with additional entropy derived from the Nexa user ID. Encrypted credentials and client configuration remain separate from dashboard state, history and exports.
- Task and calendar responses remain in memory only. Nexa does not persist personal integration caches. Widget configuration stores only the chosen list/calendar identifier and view; configuration exports may therefore contain that identifier, never credentials.
- No credentials or provider response bodies are written to logs. Failed authentication requests a reconnect. Offline and timeout states provide Refresh without blocking the dashboard. Failed writes are not automatically retried; refresh to check the provider before repeating an add operation.
- Disconnect deletes the local encrypted authorization and clears the open widget through a re-render. It does not delete external tasks/events or revoke the provider's server-side permission. Revoke that separately in your Google/Todoist account if desired.
- Keep downloaded OAuth client configuration and personal tokens out of source control. New credential/profile files are ignored by Git as an additional precaution.

Automated validation uses synthetic credentials and mocked HTTP responses. No real Todoist or Google account was connected during release validation.
