# Nexa 0.1.6 Alpha integrations

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

## Outlook Calendar

Outlook Calendar is an optional, read-only Microsoft Graph connection. No Microsoft application ID or secret ships with Nexa.

1. Register a Microsoft application that supports the account types you intend to connect. Enable public client/desktop authentication and a `http://localhost` loopback redirect URI. Grant delegated `Calendars.Read` and `User.Read` permission.
2. Paste the application (client) ID into **Settings → Integrations → Microsoft public application client ID**, then choose **Save Microsoft app ID**.
3. Choose **Connect** under Outlook Calendar. Your browser opens the Microsoft consent page. Nexa uses authorization code with PKCE and a temporary localhost listener; no client secret is needed.
4. In a Calendar widget, choose **Outlook Calendar** and the desired calendar. That selection saves with the Space. The widget lists today's events and current/next timing; it does not create or change events.

Nexa renews expired access with the refresh token kept in Windows protected storage. A disconnected or offline account leaves the Calendar widget and local Space usable. See Microsoft's [authorization-code flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow), [calendar listing](https://learn.microsoft.com/en-us/graph/api/user-list-calendars?view=graph-rest-1.0), and [calendarView](https://learn.microsoft.com/en-us/graph/api/user-list-calendarview?view=graph-rest-1.0) documentation. Live sign-in requires your own app registration and was not exercised by the automated checks.

## Privacy, storage and failures

- Profiles and preferences live under `profiles/<hash of Nexa user ID>/` beside the existing local store. Existing eligible theme/sound preferences are copied once; originals are retained.
- Credentials use Windows DPAPI CurrentUser encryption with additional entropy derived from the Nexa user ID. Encrypted credentials and client configuration remain separate from dashboard state, history and exports.
- Task and calendar responses remain in memory only. Nexa does not persist personal integration caches. Widget configuration stores only the chosen list/calendar identifier, calendar provider and view; configuration exports may therefore contain those identifiers, never credentials.
- No credentials or provider response bodies are written to logs. Failed authentication requests a reconnect. Offline and timeout states provide Refresh without blocking the dashboard. Failed writes are not automatically retried; refresh to check the provider before repeating an add operation.
- Disconnect deletes the local encrypted authorization and clears the open widget through a re-render. It does not delete external tasks/events or revoke the provider's server-side permission. Revoke that separately in your Google/Todoist account if desired.
- Failed refreshes clear in-memory task/event/list data. Changing a task filter cannot restore stale data. Requests bind to a connection identity; delayed results and old task actions are rejected when the account changes. Leaving a widget cancels its pending requests.
- Keep downloaded OAuth client configuration and personal tokens out of source control. New credential/profile files are ignored by Git as an additional precaution.

Automated validation uses synthetic credentials and mocked HTTP responses. No real Todoist or Google account was connected during release validation.

## Nexa 0.1.8 Alpha local recording and Hedy

Hedy settings are under Integrations. Existing DPAPI-protected credentials remain in the account credential store, outside dashboard data and exports. Hedy Notes is a separate integration-dependent widget; Session Timeline also works with manual local notes.

Audio Recorder uses Windows waveaudio through the existing local recorder. It requests consent before recording, writes unique WAV files to the state-store directory's owner-scoped Recordings folder, and never uploads audio. Entries preserve source/time and an audio reference; exports do not embed the recording. Switching dashboards finalizes audio into the outgoing dashboard only. Removing unrelated widgets can continue capture; removing the last recorder finalizes it. Previously saved recording paths remain readable; legacy recordings outside the current owner folder must be managed in their original folder. Audio files are local files, not encrypted tokens.

`ITranscriptionProvider` and `ISessionSummaryProvider` are extension interfaces only. No transcription/summary service is configured or advertised. Connecting a future provider requires a separate implementation and explicit service choice.
