using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckEndurance021()
    {
        var samples=new List<object>();var totalFights=new Dictionary<BugSpecies,int>();var captures=new Dictionary<BugSpecies,int>();var launches=new Dictionary<BugSpecies,int>();var withdrawals=new Dictionary<BugSpecies,int>();
        long allAttacks=0;var multi=0;var contact=0;var pair=0;var peaceful=0;var groupSamples=0;var totalCampaigns=0;
        for(var seed=0;seed<6;seed++)
        {
            var m=Infinite021(610+seed,true);var id=m.WorldId;var observed=new HashSet<ColonyEvent>();var peak=0;var frames=new List<double>();var memory=GC.GetTotalMemory(false);var watch=Stopwatch.StartNew();
            var steps=seed==0 ? 90000 : 12000;
            for(var i=0;i<steps;i++)
            {
                var start=Stopwatch.GetTimestamp();m.Advance(.05);if(i%30==0)frames.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                peak=Math.Max(peak,m.Bugs.Count);
                if(m.Bugs.Count>m.BattleOptions.HardCap || m.Webs.Count>m.BattleOptions.MaximumSharedWebs || m.Projectiles.Count>48 || m.History.Count>2048 || m.Settlements.Count>24)throw new Exception("Infinite world resource bound exceeded");
                if(i%20!=0)continue;
                foreach(var e in m.History)
                {
                    if(!observed.Add(e))continue;
                    if(e.Type==ColonyEventType.InvasionLaunched)totalCampaigns++;
                    if(m.FactionSpecies.TryGetValue(e.FactionId,out var species))
                    {
                        if(e.Type==ColonyEventType.InvasionLaunched)launches[species]=launches.GetValueOrDefault(species)+1;
                        if(e.Type==ColonyEventType.CampaignWithdrawn)withdrawals[species]=withdrawals.GetValueOrDefault(species)+1;
                        if(e.Type==ColonyEventType.BattleWon)totalFights[species]=totalFights.GetValueOrDefault(species)+1;
                        if(e.Type is ColonyEventType.NestConquered or ColonyEventType.HomelandReclaimed)captures[species]=captures.GetValueOrDefault(species)+1;
                    }
                }
                var engaged=m.Bugs.Where(b=>b.AttackFlash>0).ToArray();var speciesInFight=engaged.Select(b=>b.Species).Distinct().Count();
                if(speciesInFight>0) {contact++;if(speciesInFight==2)pair++;if(speciesInFight>=3)multi++;}
                peaceful+=m.Colonies.Values.Count(c=>!c.Eliminated && c.Strategy is ArmyStrategy.Peace or ArmyStrategy.Scouting or ArmyStrategy.Recovering);
                groupSamples+=m.Colonies.Values.Count(c=>!c.Eliminated);
            }
            Assert(m.WorldId==id && m.MatchNumber==1 && m.MatchAge>steps*.05-.01 && m.Phase!=BattlePhase.Victory,"Seed "+seed+" continuously evolves without world resets");
            Assert(m.BattleAttacks>0,"Seed "+seed+" produces real unprotected combat");
            var json=System.Text.Json.JsonSerializer.Serialize(m.CaptureWorld(),ArcadeWorldStore.JsonOptions);
            var restored=BugManager.RestoreWorld(System.Text.Json.JsonSerializer.Deserialize<ArcadeWorldSnapshot>(json,ArcadeWorldStore.JsonOptions)!);
            for(var i=0;i<120;i++) {m.Advance(.033);restored.Advance(.033);}
            Assert(System.Text.Json.JsonSerializer.Serialize(m.CaptureWorld(),ArcadeWorldStore.JsonOptions)==System.Text.Json.JsonSerializer.Serialize(restored.CaptureWorld(),ArcadeWorldStore.JsonOptions),"Seed "+seed+" deterministically restores a developed continuous world");
            allAttacks+=m.BattleAttacks;
            var cpu=Percentile(frames,.95);Console.WriteLine($"Continuous seed {seed}: simulated={steps*.05:F0}s, attacks={m.BattleAttacks}, active={m.Colonies.Values.Count(c=>!c.Eliminated)}, identities={m.FactionSpecies.Count}, peak={peak}, CPU p95={cpu:F2}ms, wall={watch.Elapsed.TotalSeconds:F2}s");
            samples.Add(new {Seed=seed,Seconds=steps*.05,Attacks=m.BattleAttacks,ActiveFactions=m.Colonies.Values.Count(c=>!c.Eliminated),Identities=m.FactionSpecies.Count,PeakEntities=peak,CpuP95Ms=cpu,ManagedMemoryDelta=GC.GetTotalMemory(false)-memory});
        }
        Console.WriteLine($"Observed combat: attacks={allAttacks}, two-species active-attack samples={pair}/{contact}, three-or-more={multi}/{contact}, peaceful/recovering faction samples={peaceful}/{groupSamples}");
        Console.WriteLine("Battle victories by species: "+string.Join(", ",totalFights.Select(p=>$"{p.Key}={p.Value}")));
        Console.WriteLine("Campaign outcomes: "+string.Join(", ",BugHabitats.Species.Select(s=>$"{s}: wins={totalFights.GetValueOrDefault(s)}, withdrawals={withdrawals.GetValueOrDefault(s)}, launched={launches.GetValueOrDefault(s)}")));
        Console.WriteLine("Nest conquests/reclaims by species: "+string.Join(", ",captures.Select(p=>$"{p.Key}={p.Value}")));
        Assert(captures.Values.Sum()>0,"Unprotected seeded major invasions produce actual nest ownership changes");
        Assert(totalCampaigns>0 && peaceful>0 && allAttacks>0,"Unprotected scenarios include strategic campaigns, peaceful intervals and genuine combat");
        File.WriteAllText(Path.Combine(output,"continuous021.json"),System.Text.Json.JsonSerializer.Serialize(new {Samples=samples,BattleVictories=totalFights,CampaignLaunches=launches,CampaignWithdrawals=withdrawals,NestConquestsAndReclaims=captures,TwoFactionAttackSamples=pair,MultiFactionAttackSamples=multi,EngagedSamples=contact,PeacefulFactionSamples=peaceful,TotalFactionSamples=groupSamples},new System.Text.Json.JsonSerializerOptions {WriteIndented=true}));
        CheckRendering021();
    }
    private static void CheckRendering021()
    {
        var m=Infinite021(621);var table=new Dictionary<BugSpecies,SpeciesCombatStats>(m.BattleOptions.Species!);foreach(var key in table.Keys.ToArray())table[key]=table[key] with{MaximumUnits=30};
        m.Configure(m.Preferences with{Arcade=m.BattleOptions with{Species=table,EnableCampaigns=false}});
        for(var i=0;i<5000;i++) {foreach(var b in m.Bugs)b.Protection=1000;m.Advance(.033);}
        Assert(m.Bugs.Count(BugManager.Alive)==180 && m.Colonies.Values.All(c=>c.Alive==30),"A configured 180-unit world sustains six established 30-unit armies");
        var overlay=new BugOverlay(m) {ShowHud=false,Camera=new Matrix(.78,0,0,.78,8,8)};
        var host=new Grid {Background=new SolidColorBrush(Color.FromRgb(8,12,20))};host.Children.Add(overlay);
        var window=new Window {Content=host,Width=1280,Height=840,Left=-10000,Top=-10000,ShowActivated=false};window.Show();Pump(TimeSpan.FromMilliseconds(120));
        var image=new RenderTargetBitmap(1280,840,96,96,PixelFormats.Pbgra32);var cpu=new List<double>();var rendering=new List<double>();
        for(var i=0;i<100;i++)
        {
            var start=Stopwatch.GetTimestamp();m.Advance(1d/60);cpu.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            start=Stopwatch.GetTimestamp();overlay.RefreshFrame();overlay.UpdateLayout();image.Clear();image.Render(host);if(i>10)rendering.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        var cp=Percentile(cpu,.95);var rp=Percentile(rendering,.95);Console.WriteLine($"Infinite 180-unit performance: CPU p95={cp:F2}ms, software render p95={rp:F2}ms, webs={m.Webs.Count}, regions={m.Territories.Count}");
        Assert(cp<6 && cp+rp<40,"Full-world simulation and native-resolution software rendering remain within a responsive fallback budget");
        Save018(host,1280,840,"infinite-world-load021");File.WriteAllText(Path.Combine(output,"performance021.json"),System.Text.Json.JsonSerializer.Serialize(new {Units=m.Bugs.Count(BugManager.Alive),CpuP95Ms=cp,RenderP95Ms=rp,Webs=m.Webs.Count,Regions=m.Territories.Count},new System.Text.Json.JsonSerializerOptions {WriteIndented=true}));window.Close();
    }
}
