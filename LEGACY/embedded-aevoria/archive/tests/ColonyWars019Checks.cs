using System.Diagnostics;
using System.IO;
using System.Windows;
using SessionDashboard;
internal static partial class Program
{
    private static BugManager War(int seed=19)
    {
        var m=new BugManager(seed);m.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade,new ArcadeSettings{Mode=ArcadeRuleSet.ClassicRounds}));m.SetSurfaces([],new Size(1100,700));m.VisibleBounds=new Rect(0,0,1100,700);m.Advance(.016);return m;
    }
    private static void CheckColonyWars019()
    {
        Assert(App.VersionLabel=="Nexa 0.1.23 alpha" && ReleaseNotes.All[0].Title=="Living Worlds","Version and Alpha release metadata identify Colony Wars 0.1.19");
        var m=War();
        Assert(m.Bugs.Count==6 && m.Bugs.All(b=>b.IsLeader) && m.Colonies.Count==6,"All six leaders are the first units in their colonies");
        Assert(m.Bugs.All(b=>b.Protection>4.9 && b.MaxHealth>300),"Royal leaders start with extra health and brief spawn protection");
        for(var i=0;i<25;i++)m.Advance(.016);
        Assert(m.Bugs.Any(b=>!b.IsLeader),"Ordinary soldiers start spawning within half a second");
        var samples=new List<object>();var totalWins=new Dictionary<BugSpecies,int>();
        for(var seed=0;seed<18;seed++)
        {
            var war=War(seed);var maximum=0;var watch=Stopwatch.StartNew();
            for(var i=0;i<24000 && war.CompletedMatches<2;i++) {war.Advance(.033);maximum=Math.Max(maximum,war.Bugs.Count(BugManager.Alive));}
            Console.WriteLine($"Seed {seed}: matches={war.CompletedMatches}, age={war.MatchAge:F1}, attacks={war.BattleAttacks}, living={war.Bugs.Count(BugManager.Alive)}, colonies={war.Colonies.Values.Count(c=>!c.Eliminated)}, cpu={watch.Elapsed.TotalSeconds:F2}s");
            if(war.CompletedMatches<2)foreach(var c in war.Colonies.Values.Where(c=>!c.Eliminated))Console.WriteLine($"STALL {c.Species}: hp={c.Leader.Health:F1} pos={c.Leader.Position} order={c.Leader.OrderPoint} target={c.Leader.TargetUnit?.Species} units={c.Alive}");
            Assert(war.CompletedMatches>=2,$"Seed {seed} autonomously completes consecutive matches");
            Assert(maximum<=war.BattleOptions.HardCap && war.Projectiles.Count<=48 && war.Webs.Count<=4,$"Seed {seed} keeps population, projectiles and decorative webs bounded");
            foreach(var win in war.Victories)totalWins[win.Key]=totalWins.GetValueOrDefault(win.Key)+win.Value;
            samples.Add(new {seed,war.CompletedMatches,war.MatchAge,war.BattleAttacks,MaximumLiving=maximum,Victories=war.Victories});
        }
        Console.WriteLine("Victory distribution: "+string.Join(", ",totalWins.Select(p=>$"{p.Key} {p.Value}")));
        CheckCombat019();
        CheckColonySoak019();
        CheckColonyUi019();
        CheckBattlePerformance019();
        File.WriteAllText(Path.Combine(output,"battle-balance019.json"),System.Text.Json.JsonSerializer.Serialize(samples,new System.Text.Json.JsonSerializerOptions {WriteIndented=true}));
    }
}
