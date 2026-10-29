using System.IO;
using System.Media;

namespace SessionDashboard;

internal enum NexaSoundEffect { Shoot, PowerUp, Death, Crawl, Ghost, Candy, Bat, WitchBell, Pumpkin }

internal static class NexaEffectsAudio
{
    internal static readonly string[] Names = ["Shoot", "Power-up", "Death", "Bug crawl", "Ghost", "Candy pickup", "Bat flutter", "Witch bell", "Pumpkin pop"];
    private static readonly object gate = new();
    private static readonly Dictionary<NexaSoundEffect, long> lastPlayed = [];
    private static (NexaSoundEffect Effect, double Volume)? pending;
    private static bool running;
    private static int Priority(NexaSoundEffect effect) => effect switch { NexaSoundEffect.Death => 5, NexaSoundEffect.PowerUp or NexaSoundEffect.Candy => 4, NexaSoundEffect.Shoot => 3, NexaSoundEffect.Crawl => 0, _ => 1 };
    internal static bool Allowed(NexaSoundEffect effect, DashboardData state, NexaOptions options, bool active)
    {
        if (!active || !state.InterfaceSoundsEnabled || !double.IsFinite(state.InterfaceSoundVolume) || state.InterfaceSoundVolume <= 0) return false;
        return effect switch
        {
            NexaSoundEffect.Shoot or NexaSoundEffect.PowerUp or NexaSoundEffect.Death => state.GameSoundsEnabled,
            NexaSoundEffect.Crawl => state.BugSoundsEnabled,
            _ => options.SeasonalSounds && NexaSeason.Active(options, DateTimeOffset.UtcNow)
        };
    }
    internal static void Play(NexaSoundEffect effect, double volume, bool preview = false)
    {
        if (!double.IsFinite(volume) || volume <= 0) return;
        lock (gate)
        {
            var now = Environment.TickCount64;
            var gap = effect switch { NexaSoundEffect.Shoot => 55, NexaSoundEffect.PowerUp or NexaSoundEffect.Candy => 120, NexaSoundEffect.Death => 800, NexaSoundEffect.Crawl => 3500, _ => 2000 };
            if (!preview && lastPlayed.TryGetValue(effect, out var last) && now - last < gap) return;
            lastPlayed[effect] = now;
            // One worker and at most one pending cue; rapid fire cannot build a backlog.
            if (pending is { } queued && Priority(queued.Effect) > Priority(effect)) return;
            pending = (effect, Math.Clamp(volume, 0, 1));
            if (running) return;
            running = true;
            _ = Task.Run(Drain);
        }
    }
    private static void Drain()
    {
        while (true)
        {
            (NexaSoundEffect Effect, double Volume) cue;
            lock (gate)
            {
                if (pending is not { } next) { running = false; return; }
                cue = next; pending = null;
            }
            try { using var wav = Create(cue.Effect, cue.Volume); using var player = new SoundPlayer(wav); player.PlaySync(); }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception) { }
        }
    }
    internal static MemoryStream Create(NexaSoundEffect effect, double volume)
    {
        const int rate = 22050;
        var duration = effect switch { NexaSoundEffect.Shoot => .065, NexaSoundEffect.Crawl => .14, NexaSoundEffect.Ghost => .58, NexaSoundEffect.Death => .38, NexaSoundEffect.WitchBell => .48, _ => .25 };
        var count = (int)(duration * rate); volume = double.IsFinite(volume) ? Math.Clamp(volume, 0, 1) : 0;
        var stream = new MemoryStream(44 + count * 2);
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write("RIFF"u8); writer.Write(36 + count * 2); writer.Write("WAVEfmt "u8); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
            writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16); writer.Write("data"u8); writer.Write(count * 2);
            uint noise = 341;
            for (var i = 0; i < count; i++)
            {
                var t = i / (double)rate; var u = t / duration;
                noise = unchecked(noise * 1664525 + 1013904223); var hiss = (noise / (double)uint.MaxValue * 2 - 1);
                var envelope = Math.Sin(Math.PI * u) * Math.Exp(-u * 2);
                var wave = effect switch
                {
                    NexaSoundEffect.Shoot => Math.Sin(2 * Math.PI * (1300 * t - 6500 * t * t)) * .7 + hiss * .1,
                    NexaSoundEffect.PowerUp => Math.Sin(2 * Math.PI * (440 * t + 1400 * t * t)) * .7,
                    NexaSoundEffect.Death => Math.Sin(2 * Math.PI * (420 * t - 420 * t * t)) * .65 + hiss * .18,
                    NexaSoundEffect.Crawl => hiss * .17 * Math.Pow(Math.Max(0, Math.Sin(2 * Math.PI * 38 * t)), 6),
                    NexaSoundEffect.Ghost => Math.Sin(2 * Math.PI * 230 * t + 9 * Math.Sin(2 * Math.PI * 1.8 * t)) * .55 + hiss * .06,
                    NexaSoundEffect.Candy => (Math.Sin(2 * Math.PI * 1046.5 * t) + .5 * Math.Sin(2 * Math.PI * 1568 * t)) * .4 + hiss * .05,
                    NexaSoundEffect.Bat => hiss * .25 * Math.Pow(Math.Sin(2 * Math.PI * 13 * t), 2) + Math.Sin(2 * Math.PI * 1600 * t) * .1,
                    NexaSoundEffect.WitchBell => (Math.Sin(2 * Math.PI * 523.25 * t) + .5 * Math.Sin(2 * Math.PI * 622.25 * t) + .3 * Math.Sin(2 * Math.PI * 1046.5 * t)) * .35,
                    _ => Math.Sin(2 * Math.PI * (150 * t - 180 * t * t)) * .6 + hiss * .08
                };
                writer.Write((short)Math.Clamp(wave * envelope * volume * 6500, short.MinValue, short.MaxValue));
            }
        }
        stream.Position = 0; return stream;
    }
}
