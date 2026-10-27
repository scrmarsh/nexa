using System.IO;
using System.Text.Json;

namespace SessionDashboard;

internal sealed record HistoryEntry(Guid DashboardId, string Space, DashboardSession? Session, string Raw, string? Problem)
{
    internal bool Editable => Problem == null && Session is { IsActive: false };
    public string Display => $"{Space}\n{Session?.Title ?? "Unreadable session"}\n{(Session?.OpenedAt is {} at && at != default ? at.LocalDateTime.ToString("g") : "Time not recorded")}";
    internal string Report => HistoryArchive.Report(Session, Raw, Problem);
}

// Read-only projections of the existing workspace, never a competing history database.
internal static class HistoryArchive
{
    internal static bool Recoverable(Exception ex) => ex is InvalidDataException or IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or NullReferenceException or OverflowException or KeyNotFoundException or NotSupportedException;
    internal static void Log(StateStore store, Exception ex)
    {
        try
        {
            var directory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(store.FilePath))!, "diagnostics");
            Directory.CreateDirectory(directory); var path = Path.Combine(directory, "history.log");
            if (File.Exists(path) && new FileInfo(path).Length > 262144) File.Move(path, path + ".previous", true);
            File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O} {ex.GetType().Name}: {ex.Message}\n");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }
    internal static string Report(DashboardSession? session, string? raw = null, string? problem = null)
    {
        try
        {
            if (session == null || problem != null) throw new InvalidDataException(problem ?? "Missing session record.");
            Continuity.ValidateData(session.Data);
            return MainWindow.BuildSessionReport(session);
        }
        catch (Exception ex) when (Recoverable(ex))
        {
            if (raw == null)
            {
                try { raw = JsonSerializer.Serialize(session, Continuity.Json); }
                catch (Exception error) when (Recoverable(error)) { raw = "The record could not be serialized. The original save has not been modified."; }
            }
            return $"RECOVERED ARCHIVE VIEW\nThis record could not be fully interpreted: {problem ?? ex.Message}\nNo saved information was changed. The original recorded fields are shown below.\n\n{raw}";
        }
    }
    internal static (HistoryEntry[] Entries, string? Warning) Read(StateStore store)
    {
        var warnings = new List<string>();
        foreach (var path in new[] { store.FilePath, store.FilePath + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                using var document = JsonDocument.Parse(store.ReadHistoryText(path));
                var root = document.RootElement;
                if (!root.TryGetProperty("Dashboards", out var dashboards) || dashboards.ValueKind != JsonValueKind.Array)
                    return ([], "This save has no recorded session archive yet.");
                if (Text(root, "UserId") != store.UserId) throw new UnauthorizedAccessException("This archive belongs to another Windows user.");
                var entries = new List<HistoryEntry>();
                foreach (var dashboard in dashboards.EnumerateArray())
                {
                    if (dashboard.ValueKind != JsonValueKind.Object || Text(dashboard, "UserId") != store.UserId)
                    { warnings.Add("An invalid or foreign Space was skipped."); continue; }
                    Guid.TryParse(Text(dashboard, "DashboardId"), out var dashboardId);
                    var space = Text(dashboard, "Name") ?? "Unnamed Space";
                    void Add(JsonElement raw)
                    {
                        DashboardSession? session = null; string? problem = null;
                        try
                        {
                            if (raw.ValueKind != JsonValueKind.Object || !raw.TryGetProperty("SessionId", out _) || !raw.TryGetProperty("OpenedAt", out _) || !raw.TryGetProperty("Data", out _)) throw new InvalidDataException("Missing recorded fields.");
                            session = raw.Deserialize<DashboardSession>(Continuity.Json) ?? throw new InvalidDataException("Missing session record.");
                            if (session.UserId != store.UserId || session.DashboardId != dashboardId) throw new UnauthorizedAccessException("Invalid session ownership.");
                            if (session.SessionId == Guid.Empty) throw new InvalidDataException("Missing session identity.");
                            Continuity.ValidateData(session.Data);
                            if (session.Title == null || session.TimeZoneId == null) throw new InvalidDataException("Missing session name or time zone.");
                        }
                        catch (UnauthorizedAccessException ex) { Log(store, ex); warnings.Add("A record with mismatched ownership was skipped."); return; }
                        catch (Exception ex) when (Recoverable(ex)) { problem = ex.Message; Log(store, ex); warnings.Add("Some records require the recovered archive view."); }
                        entries.Add(new(dashboardId, space, session, raw.GetRawText(), problem));
                    }
                    if (dashboard.TryGetProperty("Current", out var current)) Add(current);
                    if (dashboard.TryGetProperty("History", out var history) && history.ValueKind == JsonValueKind.Array)
                        foreach (var record in history.EnumerateArray()) Add(record);
                    else { warnings.Add("A Space has a missing or invalid history list."); if (history.ValueKind != JsonValueKind.Undefined) Add(history); }
                    if (dashboard.TryGetProperty("PreservedHistory", out var preserved) && preserved.ValueKind == JsonValueKind.Array)
                    { foreach (var record in preserved.EnumerateArray()) Add(record); if (preserved.GetArrayLength() > 0) warnings.Add("Some original historical records are preserved for recovery."); }
                }
                if (path != store.FilePath) warnings.Add("The primary archive could not be read; showing its last readable backup.");
                return (entries.ToArray(), warnings.Count == 0 ? null : string.Join(" ", warnings.Distinct()));
            }
            catch (UnauthorizedAccessException ex) { Log(store, ex); return ([], ex.Message); }
            catch (Exception ex) when (Recoverable(ex)) { Log(store, ex); warnings.Add("An archive file could not be read."); }
        }
        return ([], warnings.Count == 0 ? null : string.Join(" ", warnings.Distinct()) + " The original files have not been modified.");
    }
    internal static WorkspaceDocument LoadWorkspace(string json, string userId, out bool recovered)
    {
        recovered = false;
        var root = System.Text.Json.Nodes.JsonNode.Parse(json) as System.Text.Json.Nodes.JsonObject ?? throw new InvalidDataException("Invalid workspace document.");
        if (root["UserId"]?.GetValue<string>() != userId) throw new UnauthorizedAccessException("This workspace belongs to another Windows user.");
        if (root["Dashboards"] is not System.Text.Json.Nodes.JsonArray dashboards) throw new InvalidDataException("Missing Spaces.");
        foreach (var node in dashboards)
        {
            if (node is not System.Text.Json.Nodes.JsonObject dashboard || dashboard["UserId"]?.GetValue<string>() != userId) continue;
            if (!dashboard.ContainsKey("History")) continue;
            if (dashboard["History"] is not System.Text.Json.Nodes.JsonArray history)
            {
                Preserve(dashboard, dashboard["History"]?.DeepClone());
                dashboard["History"] = new System.Text.Json.Nodes.JsonArray(); recovered = true; continue;
            }
            for (var index = history.Count - 1; index >= 0; index--)
            {
                try
                {
                    var raw = history[index];
                    if (raw is not System.Text.Json.Nodes.JsonObject record || record["SessionId"] == null || record["OpenedAt"] == null || record["Data"] == null)
                        throw new InvalidDataException("Missing historical session fields.");
                    var session = raw.Deserialize<DashboardSession>(Continuity.Json) ?? throw new InvalidDataException("Missing historical session.");
                    if (session.UserId != userId || (!Guid.TryParse(dashboard["DashboardId"]?.GetValue<string>(), out var storedDashboard) || session.DashboardId != storedDashboard)) throw new InvalidDataException("Invalid historical ownership.");
                    if (session.SessionId == Guid.Empty || session.Snapshots == null || session.MergedFrom == null) throw new InvalidDataException("Invalid historical identity.");
                    Continuity.ValidateData(session.Data);
                    foreach (var snapshot in session.Snapshots) Continuity.ValidateData(snapshot?.Data);
                }
                catch (Exception ex) when (Recoverable(ex))
                {
                    var original = history[index]?.DeepClone(); history.RemoveAt(index);
                    Preserve(dashboard, original); recovered = true;
                }
            }
        }
        return (recovered ? root.ToJsonString() : json).DeserializeWorkspace();
    }
    private static void Preserve(System.Text.Json.Nodes.JsonObject dashboard, System.Text.Json.Nodes.JsonNode? original)
    {
        var preserved = dashboard["PreservedHistory"] as System.Text.Json.Nodes.JsonArray;
        if (preserved == null)
        {
            var previous = dashboard["PreservedHistory"]?.DeepClone();
            preserved = new(); if (previous != null) preserved.Add(previous);
            dashboard["PreservedHistory"] = preserved;
        }
        preserved.Add(original);
    }
    private static WorkspaceDocument DeserializeWorkspace(this string json) => JsonSerializer.Deserialize<WorkspaceDocument>(json, Continuity.Json) ?? throw new InvalidDataException("Empty workspace.");
    private static string? Text(JsonElement node, string key) => node.ValueKind == JsonValueKind.Object && node.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
