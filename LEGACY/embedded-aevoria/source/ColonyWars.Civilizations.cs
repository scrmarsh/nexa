using System.Windows;

namespace SessionDashboard;
internal sealed partial class BugManager
{
    private static readonly string[] ArmyNames=["Ammenidites","Varkonians","Parthians","Sassanids","Scythians","Kharadites","Veltharians","Dravonians","Aurelians","Mordavians","Seleucids","Illyrians","Numidians","Thracians","Dacians","Lydians","Hittites","Sarmatians","Arverni","Nabataeans"];
    private static readonly string[] ArmyTitles=["The Iron Phalanx","The Crimson Host","The Bronze Legion","The Silent Guard"];
    private static readonly string[] WarriorNames=["Leonidas","Aurelian","Scipio","Ragnar","\u00c6thelwulf","Belisarius","Hannibal","Artemisia","Cyrus","Cassius","Boudicca","Alaric","Theodoric","Zenobia","Darius","Pyrrhus","Agrippa","Tomyris","Hamilcar","Viriathus","Epaminondas","Sertorius","Lysander","Agesilaus"];
    private readonly Dictionary<int,string> factionNames=[];
    private string NewArmyName()
    {
        var names=random.NextDouble()<.85 ? ArmyNames : ArmyTitles;
        var start=random.Next(names.Length);
        for(var i=0;i<names.Length;i++)
        {var name=names[(start+i)%names.Length];if(!Colonies.Values.Any(c=>c.Name==name))return name;}
        return "Legion"+(nextFaction+1);
    }
    private void Record(ColonyEventType type,BugColony faction,BugColony? other=null,int? unit=null,int? otherId=null,int? regionId=null,int? settlementId=null)
    {
        // Bounded, run-only history. IDs remain unique across automatic match restarts.
        if(History.Count>=2048)
        {
            History.RemoveRange(0,256);
            var retained=History.SelectMany(e=>new[]{e.FactionId,e.OtherFactionId??e.FactionId}).Concat(Colonies.Values.Select(c=>c.Id)).Concat(Territories.SelectMany(r=>new[]{r.OwnerId,r.ChallengerId??0})).Concat(Webs.Select(w=>w.FactionId)).Concat(Settlements.SelectMany(n=>new[]{n.OwnerId,n.FounderId})).ToHashSet();
            if(otherId is {} retainedOpponent)retained.Add(retainedOpponent);
            foreach(var id in factionNames.Keys.Where(id=>!retained.Contains(id)).ToArray()){factionNames.Remove(id);FactionColors.Remove(id);FactionSpecies.Remove(id);foreach(var c in colonyOrder) {c.Rivalries.Remove(id);c.KillsAgainst.Remove(id);}}
        }
        FactionSpecies[faction.Id]=faction.Species;FactionColors[faction.Id]=faction.ColorArgb;factionNames[faction.Id]=faction.Name;if(other!=null)factionNames[other.Id]=other.Name;
        History.Add(new(MatchAge,MatchNumber,type,faction.Id,other?.Id??otherId,unit,regionId,settlementId));
        Civilization?.Observe(History[^1]);
    }
    internal string DescribeEvent(ColonyEvent entry)
    {
        string Name(int id)=>Colonies.Values.FirstOrDefault(c=>c.Id==id)?.Name ?? factionNames.GetValueOrDefault(id,"Faction "+id);
        return $"{Name(entry.FactionId)}: {entry.Type}"+(entry.OtherFactionId is {} other ? $" / {Name(other)}" : "");
    }
    private void Extinguish(BugColony colony)
    {
        if(colony.Eliminated)return;
        colony.Eliminated=true;colony.Regrouping=false;
        Record(ColonyEventType.Extinction,colony);
    }
    private void AdvanceColonies(double dt)
    {
        foreach(var colony in colonyOrder)
        {
            if(colony.Eliminated)continue;
            var sum=new Vector();var members=0;
            foreach(var member in combatUnits)if(Alive(member) && member.Species==colony.Species) {sum+=(Vector)member.Position;members++;}
            colony.SurvivorCenter=members>0 ? (Point)(sum/members) : colony.Territory;
            if(colony.Alive<=0) {Extinguish(colony);continue;}
            if(Alive(colony.Leader)) {if(OwnsSettlement(colony))colony.Resources=Math.Min(90,colony.Resources+dt*.25);continue;}
            if(MatchAge-colony.DefeatTime>30)colony.Regrouping=false;
            // Only non-eusocial adults marked reproductive at birth can rebuild.
            // Promotion preserves the survivor's ID, name, health fraction and kill history.
            if(!colony.Regrouping || colony.Recoveries>=1 || colony.Resources<25)continue;
            BugEntity? successor=null;
            foreach(var b in combatUnits)
                if(Alive(b) && b.Species==colony.Species && b.CanReproduce && b.Health>=b.MaxHealth*.35 && (b.Position-colony.Territory).Length<100)
                {successor=b;break;}
            if(successor==null) {colony.RecoveryTime=0;continue;}
            Neighbors(colony.Territory,160);
            if(closeUnits.Any(b=>Alive(b) && b.Species!=colony.Species)) {colony.RecoveryTime=0;continue;}
            colony.RecoveryTime+=dt;
            if(colony.RecoveryTime<12)continue;
            colony.Resources-=25;colony.Recoveries++;colony.Regrouping=false;
            var healthFraction=successor.Health/successor.MaxHealth;
            successor.IsLeader=true;successor.Role=ArmyRole.HomeGuard;successor.CampaignDeployed=false;successor.MaxHealth=Stats(colony.Species).LeaderHealth;
            successor.Health=successor.MaxHealth*healthFraction;successor.Speed=Stats(colony.Species).Speed*.58;
            successor.RetreatUntil=0;successor.ThinkTimer=0;colony.Leader=successor;
            Record(ColonyEventType.Recovery,colony,unit:successor.UnitId);
        }
        foreach(var b in combatUnits)
        {
            if(!Alive(b) || b.IsLeader || b.Health>=b.MaxHealth || (b.Position-Colonies[b.Species].Rally).Length>90)continue;
            Neighbors(b.Position,160);
            if(!closeUnits.Any(other=>Alive(other) && other.Species!=b.Species))b.Health=Math.Min(b.MaxHealth,b.Health+dt*1.5);
        }
    }
}
