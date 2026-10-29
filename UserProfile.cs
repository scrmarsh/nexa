using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SessionDashboard;

public sealed class UserProfile
{
    public string UserId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<string> UseCases { get; set; } = [];
    public string PreferredDashboard { get; set; } = "Class";
    public List<string> PreferredWidgets { get; set; } = [];
    public bool TimedSessions { get; set; } = true;
    public int DefaultMinutes { get; set; } = 25;
    public bool OnboardingCompleted { get; set; }
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;
    public AppTheme Theme { get; set; } = AppTheme.Nexa;
    public bool Sounds { get; set; }
    public double Volume { get; set; } = .25;
    public bool FocusNudges { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public List<string> ConnectedIntegrations { get; set; } = [];
}

public sealed partial class StateStore
{
    internal string UserPreferencesDirectory => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(FilePath))!, "profiles", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(UserId))));
    private string ScopedPreferencePath(string name)
    {
        var target = Path.Combine(UserPreferencesDirectory, name);
        var legacy = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(FilePath))!, name);
        if (!File.Exists(target) && File.Exists(legacy) && OwnsLegacyPreferences())
        {
            Directory.CreateDirectory(UserPreferencesDirectory);
            // Copy once; keep legacy preferences intact for older versions.
            File.Copy(legacy, target, false);
        }
        return target;
    }
    private bool OwnsLegacyPreferences()
    {
        if (File.Exists(FilePath))
        {
            try { using var doc = JsonDocument.Parse(File.ReadAllText(FilePath)); if (doc.RootElement.TryGetProperty("UserId", out var id)) return id.ValueKind==JsonValueKind.String && id.GetString() == UserId; }
            catch (Exception ex) when (ex is IOException or JsonException) { return false; }
        }
        return UserId == System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value;
    }
    internal UserProfile LoadProfile()
    {
        var path = Path.Combine(UserPreferencesDirectory, "profile.json");
        if (File.Exists(path))
        {
            var profile = JsonSerializer.Deserialize<UserProfile>(File.ReadAllText(path)) ?? throw new InvalidDataException("Profile could not be read. Original data has been retained.");
            if (profile.UserId != UserId) throw new UnauthorizedAccessException("This profile belongs to another user.");
            profile.Theme=LoadAppTheme(); profile.ThemeMode=LoadThemeMode();
            var current=new DashboardData(); LoadInteractionPreferences(current); profile.Sounds=current.InterfaceSoundsEnabled; profile.Volume=current.InterfaceSoundVolume; profile.FocusNudges=current.FocusNudgesEnabled;
            profile.ConnectedIntegrations=ConnectedServices();
            return profile;
        }
        var interaction = new DashboardData(); LoadInteractionPreferences(interaction);
        return new UserProfile { UserId = UserId, OnboardingCompleted = File.Exists(FilePath) || File.Exists(FilePath + ".bak") || File.Exists(PreferencesPath) || File.Exists(InteractionPreferencesPath), ThemeMode = LoadThemeMode(), Theme = LoadAppTheme(), Sounds = interaction.InterfaceSoundsEnabled, Volume = interaction.InterfaceSoundVolume, FocusNudges = interaction.FocusNudgesEnabled, ConnectedIntegrations=ConnectedServices() };
    }
    private List<string> ConnectedServices()
    {
        var services=new[]{"Todoist","Google Tasks","Google Calendar","Outlook Calendar","Hevy"}.Where(new IntegrationVault(this).Connected).ToList();
        var credentials=new AppState();LoadCredentials(credentials);if(!string.IsNullOrWhiteSpace(credentials.HedyProtectedKey))services.Add("Hedy");return services;
    }
    internal void SaveProfile(UserProfile profile)
    {
        if (profile.UserId != UserId) throw new UnauthorizedAccessException("Cannot save another user's profile.");
        if (profile.DisplayName.Length > 80 || profile.DefaultMinutes is < 1 or > 10080 || !double.IsFinite(profile.Volume) || profile.Volume is < 0 or > 1) throw new InvalidDataException("Invalid profile preferences.");
        Directory.CreateDirectory(UserPreferencesDirectory);
        AtomicWrite(Path.Combine(UserPreferencesDirectory,"profile.json"), JsonSerializer.Serialize(profile, Options), true);
        SavePreferences(profile.ThemeMode, profile.Theme);
        var interaction = new DashboardData(); LoadInteractionPreferences(interaction);
        interaction.InterfaceSoundsEnabled = profile.Sounds; interaction.InterfaceSoundVolume = profile.Volume; interaction.FocusNudgesEnabled = profile.FocusNudges;
        SaveInteractionPreferences(interaction);
    }
}

internal static class GuidedSetup
{
    internal static string[] Types = ["Class", "Study", "Church", "Meeting", "Custom"];
    internal static string[] UseCases = ["Classes", "Studying / Focus", "Church / Small Group", "Meetings", "Work", "Personal use", "Other"];
    internal static string[] Recommendations(string type) => type switch
    {
        "Church" => ["Pixel timer", "Bible", "Notes", "Scripture pins"],
        "Study" => ["Pixel timer", "Notes", "Focus Reset", "Task List"],
        "Meeting" => ["Pixel timer", "Notes", "Task List"],
        "Custom" => ["Pixel timer", "Notes"],
        _ => ["Pixel timer", "Notes", "Clock"]
    };
    internal static DashboardRecord Create(UserProfile profile, string name, IEnumerable<string> tools, string userId)
    {
        var color=profile.Theme switch { AppTheme.Violet=>"#AA80FF", AppTheme.Aurora=>"#63D8BC", AppTheme.Ember=>"#FFAA70", AppTheme.Amber=>Theme.AppPalette(AppTheme.Amber).Accent, AppTheme.Green=>Theme.AppPalette(AppTheme.Green).Accent, _=>"#7395FF" };
        var data = new DashboardData { WidgetLayoutInitialized=true, AccentColor=color,PixelColor=color,Draft = new DashboardDraft { Title=name } };
        foreach(var kind in tools.Distinct()) data.Widgets.Add(WidgetLibrary.Create(kind,kind switch { "Pixel timer"=>"pixels", "Notes"=>"notes", "Tasks" or "Task List"=>"tasks", "Session timeline"=>"timeline", _=>"" }));
        WidgetLibrary.Arrange(data.Widgets,false);
        if(profile.TimedSessions) { var now=DateTimeOffset.Now; data.Session=new Session { Title=name, Start=now, End=now.AddMinutes(profile.DefaultMinutes), ConfiguredDurationMinutes=profile.DefaultMinutes, IntervalMinutes=5 }; }
        return Continuity.NewDashboard(name,profile.PreferredDashboard,data,userId);
    }
}
