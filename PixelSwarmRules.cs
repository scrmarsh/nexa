using System.IO;
using System.Text.Json;

namespace SessionDashboard;

internal enum SwarmPhase { Menu, Playing, Paused, LevelUp, GameOver }
internal enum SwarmEnemyKind { Drifter, Chaser, Dasher, Shooter, Tank, Boss }
internal enum SwarmWeaponKind { PixelBlaster, MachineGun, GrenadeLauncher }
internal enum SwarmPickupKind { Relief, Health, Shield, MachineGun, GrenadeLauncher, RapidFire }

internal static class SwarmBalance
{
    internal const int MaxHealth = 3;
    internal const double PlayerSpeed = 260;
    internal const double InvulnerabilitySeconds = 1.15;
    internal const double StageTwoAt = 25;
    internal const double StageThreeAt = 55;
    internal const double ReliefSeconds = 6;
    internal const double PickupLifetime = 13;
    internal const double SpawnSafeDistance = 160;
    internal const int ProjectileCap = 130;
    internal const int ParticleCap = 180;
    internal static int EnemyCap(int stage) => Math.Min(150, 60 + stage * 20);
    internal static double SpawnInterval(int stage) => Math.Max(.21, 1.6 - stage * .2);
    internal static double EnemySpeed(int stage) => stage == 1 ? 36 : stage == 2 ? 72 : Math.Min(180, 100 + (stage - 3) * 12);
    internal static int ExperienceNeeded(int level) => 100 + (level - 1) * 75;
    internal sealed record Weapon(string Name, double Cooldown, double Speed, int Damage, int Ammo, double Radius, double Explosion);
    internal static Weapon Definition(SwarmWeaponKind kind) => kind switch
    {
        SwarmWeaponKind.MachineGun => new("Machine Gun", .075, 780, 1, 85, 3, 0),
        SwarmWeaponKind.GrenadeLauncher => new("Grenade Launcher", .48, 440, 3, 12, 6, 68),
        _ => new("Pixel Blaster", .31, 680, 1, int.MaxValue, 3, 0)
    };
}

internal sealed class SwarmStats
{
    public int BestScore { get; set; }
    public int HighestStage { get; set; } = 1;
    public double LongestRun { get; set; }
    public int TotalEnemiesDefeated { get; set; }
    public int TotalRuns { get; set; }
}

public sealed partial class StateStore
{
    private string SwarmStatsPath => Path.Combine(UserPreferencesDirectory, "pixel-swarm-stats.json");
    internal SwarmStats LoadSwarmStats()
    {
        try { return File.Exists(SwarmStatsPath) ? JsonSerializer.Deserialize<SwarmStats>(File.ReadAllText(SwarmStatsPath)) ?? new() : new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    internal void SaveSwarmStats(SwarmStats stats)
    {
        Directory.CreateDirectory(UserPreferencesDirectory);
        AtomicWrite(SwarmStatsPath, JsonSerializer.Serialize(stats, Options), true);
    }
}
