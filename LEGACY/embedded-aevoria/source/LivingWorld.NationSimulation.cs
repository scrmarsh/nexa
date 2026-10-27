using System.Windows;
namespace SessionDashboard;
internal sealed partial class LivingNationWorld
{
    private double nextSync;
    internal void Conquer(BugManager manager,int coarseRegion,int oldFaction,int winner)
    {
        // New peaceful claims use the political frontier, never a coarse military rectangle.
        if(Expansion!=null && oldFaction==0)return;
        Write();var target=nations.FirstOrDefault(n=>n.FactionId==winner && n.Status==NationExistence.Active);if(target==null)return;
        var bounds=manager.RegionBounds(coarseRegion);
        var settlement=manager.Settlements.FirstOrDefault(s=>manager.RegionAt(s.Position)==coarseRegion)?.Id;
        for(var i=0;i<owners.Length;i++)
        {
            if(Land.Land[i]==0 || !bounds.Contains(Land.Center(i)))continue;var previous=Owner(i);
            if(Expansion!=null && previous==null)continue;
            Commit(i,previous,target.Id,manager.MatchAge,previous==null ? OwnershipReason.Occupation : OwnershipReason.Conquest,settlement);
        }
    }
    internal void Settlement(BugManager manager,ColonySettlement settlement)
    {
        Write();var nation=nations.FirstOrDefault(n=>n.FactionId==settlement.OwnerId && n.Status==NationExistence.Active);var cell=Land.At(settlement.Position);
        if(Expansion!=null)return; // Civic expansion owns the admission/cost policy in opted-in worlds.
        if(nation!=null && cell>=0 && Land.IsLand(settlement.Position) && Owner(cell)==null)Commit(cell,null,nation.Id,manager.MatchAge,OwnershipReason.Settlement,settlement.Id);
    }
    internal void Tick(BugManager manager)
    {
        Write();if(manager.WorldPaused || manager.MatchAge<nextSync)return;nextSync=manager.MatchAge+1;
        for(var i=0;i<nations.Count;i++)
        {
            var retired=nations[i];if(retired.FactionId==null || retired.Status==NationExistence.Extinct || manager.Colonies.Values.Any(c=>c.Id==retired.FactionId))continue;
            nations[i]=retired with{Status=NationExistence.Extinct};Revision++;foreach(var cell in Regions(retired.Id))Commit(cell,retired.Id,null,manager.MatchAge,OwnershipReason.Abandonment);
        }
        foreach(var colony in manager.Colonies.Values)
        {
            var index=nations.FindIndex(n=>n.FactionId==colony.Id);
            if(index<0)
            {
                if(nations.Count>=512){Diagnostic="Political identity capacity reached; base simulation continues. Further nation archival support is required.";continue;}
                var cell=Enumerable.Range(0,owners.Length).Where(i=>Land.Land[i]!=0 && Owner(i)==null).OrderBy(i=>(Land.Center(i)-colony.Territory).LengthSquared).FirstOrDefault(-1);
                if(cell<0 || colony.Eliminated)continue;
                var n=AddNation(new(Identity(WorldId,colony.Id),colony.Name,colony.Species.ToString(),Color(nations.Count),Land.Center(cell),colony.Id,manager.MatchAge,colony.Leader.UnitId));
                Commit(cell,null,n.Id,manager.MatchAge,OwnershipReason.Founding);index=nations.Count-1;
            }
            var status=colony.Eliminated ? NationExistence.Extinct : NationExistence.Active;var nation=nations[index];
            if(nation.Status!=status || nation.RulerUnitId!=colony.Leader.UnitId){nations[index]=nation with{Status=status,RulerUnitId=colony.Leader.UnitId};Revision++;}
            if(status==NationExistence.Extinct)foreach(var cell in Regions(nation.Id))Commit(cell,nation.Id,null,manager.MatchAge,OwnershipReason.Abandonment);
        }
        if(Expansion!=null){SynchronizeFrontierContests(manager);return;}
        for(var i=0;i<owners.Length;i++)
        {
            if(owners[i]==0)continue;var coarse=manager.Territories[manager.RegionAt(Land.Center(i))];
            var challenger=coarse.Contested ? nations.FirstOrDefault(n=>n.FactionId==coarse.ChallengerId && n.Status==NationExistence.Active)?.Id : null;
            Contest(i,challenger==Owner(i) ? null : challenger);
        }
    }
    internal (int Population,int Military,int Settlements,string? Ruler) Statistics(Guid id,BugManager manager)
    {
        var nation=Find(id);var colony=manager.Colonies.Values.FirstOrDefault(c=>c.Id==nation?.FactionId);if(colony==null)return (0,0,0,null);
        var units=manager.Bugs.Where(b=>BugManager.Alive(b) && b.Species==colony.Species).ToArray();
        return (units.Length,units.Count(b=>!b.IsLeader && b.Role!=ArmyRole.Reserve),Expansion!=null && manager.Civilization is {} civic ? civic.State.Settlements.Count(s=>s.Active && s.OwnerId==id) : manager.Settlements.Count(s=>s.OwnerId==colony.Id),BugManager.Alive(colony.Leader) ? colony.Leader.Name : null);
    }
}
