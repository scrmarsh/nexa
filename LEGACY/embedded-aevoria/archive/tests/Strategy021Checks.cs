using System.IO;
using System.Windows;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckStrategy021()
    {
        var quiet=Infinite021(300);quiet.Configure(quiet.Preferences with{Arcade=quiet.BattleOptions with{EnableCampaigns=false}});
        for(var i=0;i<1200;i++)quiet.Advance(.033);
        Assert(!quiet.History.Any(e=>e.Type is ColonyEventType.Mobilized or ColonyEventType.InvasionLaunched),"Disabling strategic campaigns preserves peaceful preparation and defensive combat without invasions");
        var neutral=Infinite021(301);var guard=Fighter(neutral,BugSpecies.Ants,new Point(800,500));var enemy=Fighter(neutral,BugSpecies.Roaches,new Point(810,500));guard.ThinkTimer=enemy.ThinkTimer=0;
        neutral.ApplyBattleDamage(enemy,guard,1);var hp=enemy.Health;neutral.Advance(.016);
        Assert(guard.TargetUnit==enemy && enemy.Health<hp,"Direct retaliation always remains available outside organized campaigns");
        var samples=new List<object>();var launched=0;var withdrawn=0;var peaceful=0;var crowd=0;var engaged=0;var maxWars=0;
        for(var seed=0;seed<6;seed++)
        {
            var m=Infinite021(310+seed);var mobilized=new HashSet<(int,double)>();
            for(var i=0;i<6000;i++)
            {
                // Protect entities so the strategy test observes complete campaign lifecycles.
                foreach(var b in m.Bugs)b.Protection=1000;m.Advance(.033);
                if(i%30!=0)continue;
                var campaigns=m.Colonies.Values.Count(c=>c.Strategy is ArmyStrategy.Preparing or ArmyStrategy.Mobilizing or ArmyStrategy.Raiding or ArmyStrategy.Invading);maxWars=Math.Max(maxWars,campaigns);
                peaceful+=m.Colonies.Values.Count(c=>c.Strategy is ArmyStrategy.Peace or ArmyStrategy.Scouting or ArmyStrategy.Recovering);
                var fighters=m.Bugs.Where(b=>b.AttackFlash>0).Select(b=>b.Species).Distinct().Count();if(fighters>0) {engaged++;if(fighters>=3)crowd++;}
                foreach(var c in m.Colonies.Values)
                    if(c.Strategy is ArmyStrategy.Raiding or ArmyStrategy.Invading)mobilized.Add((c.Id,c.CampaignStarted));
            }
            var invasions=m.History.Count(e=>e.Type==ColonyEventType.InvasionLaunched);var withdrawals=m.History.Count(e=>e.Type==ColonyEventType.CampaignWithdrawn);
            launched+=invasions;withdrawn+=withdrawals;
            var home=m.Bugs.Where(b=>BugManager.Alive(b) && b.Role==ArmyRole.HomeGuard && !b.CampaignDeployed && b.TargetUnit==null).ToArray();
            Assert(home.Length>0 && home.All(b=>(b.OrderPoint-m.Colonies[b.Species].Territory).Length<110),"Seed "+seed+" keeps unengaged home guards around their settlement");
            Assert(invasions>0 && withdrawals>0 && m.History.Where(e=>e.Type==ColonyEventType.BattleWon).All(e=>m.History.Any(n=>n.Type is ColonyEventType.NestConquered or ColonyEventType.HomelandReclaimed && n.FactionId==e.FactionId && n.Timestamp==e.Timestamp)),"Seed "+seed+" mobilizes real raids, withdraws, and ties victories to real nest ownership while all units are protected");
            samples.Add(new {Seed=seed,Invasions=invasions,Withdrawals=withdrawals,ObservedCampaigns=mobilized.Count,m.BattleAttacks});
        }
        Assert(maxWars<=2 && peaceful>0 && launched>0 && withdrawn>0,"Faction commitments keep organized campaigns bounded and preserve peaceful intervals");
        Console.WriteLine($"Strategy sample: invasions={launched}, withdrawals={withdrawn}, max simultaneous campaigns={maxWars}, three-or-more-faction active-attack samples={crowd}/{engaged}");
        File.WriteAllText(Path.Combine(output,"strategy021.json"),System.Text.Json.JsonSerializer.Serialize(new {Samples=samples,MaximumSimultaneousCampaigns=maxWars,MultiFactionAttackSamples=crowd,EngagedSamples=engaged},new System.Text.Json.JsonSerializerOptions {WriteIndented=true}));
    }
}
