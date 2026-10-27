using System.Windows;
namespace SessionDashboard;
internal sealed partial class BugManager
{
    internal int LivingGeneratorVersion;
    internal void InitializeInfiniteWorld(Size size)
    {
        var maximum=LivingGeneratorVersion==2 ? 12288 : LivingGeneratorVersion==3 ? 36864 : 4000;
        FixedWorldBounds=new Rect(0,0,Math.Clamp(size.Width,640,maximum),Math.Clamp(size.Height,480,maximum));
        Bounds=FixedWorldBounds.Value.Size;VisibleBounds=FixedWorldBounds.Value;
        if(Colonies.Count==0)StartBattle();
    }
    private void AdvanceInfiniteWorld()
    {
        Projectiles.RemoveAll(p=>!Alive(p.Target));
        var active=colonyOrder.Where(c=>!c.Eliminated).ToArray();
        if(active.Length==1 && lastDominantFaction!=active[0].Id)
        {lastDominantFaction=active[0].Id;Record(ColonyEventType.Victory,active[0]);Victories[active[0].Species]=Victories.GetValueOrDefault(active[0].Species)+1;}
        if(active.Length>1)lastDominantFaction=0;
        if(!BattleOptions.AllowNewCivilizations || MatchAge<nextEmergence || active.Length>2 || Bugs.Count>=BattleOptions.HardCap)return;
        nextEmergence=MatchAge+120;
        var vacant=colonyOrder.Where(c=>c.Eliminated).ToArray();if(vacant.Length==0)return;
        var old=vacant[random.Next(vacant.Length)];
        for(var attempt=0;attempt<16;attempt++)
        {
            var point=ClampArena(attempt<10 ? EdgePoint(BattleArena,random.NextDouble()) : new Point(Range(80,BattleArena.Width-80),Range(80,BattleArena.Height-80)),80);
            Neighbors(point,220);
            if(!CanFoundAt(point) || closeUnits.Any(Alive) || active.Any(c=>(c.Territory-point).Length<220))continue;
            var colony=new BugColony(old.Species) {Id=++nextFaction,Name=NewArmyName(),Territory=point,SurvivorCenter=point,ColorArgb=NewFactionColor()};
            InitializeColonyStrategy(colony);
            colonyOrder[colonyOrder.IndexOf(old)]=colony;Colonies[old.Species]=colony;
            Bugs.RemoveAll(b=>b.Species==old.Species && !Alive(b));
            colony.Leader=SpawnSoldier(colony,true);Record(ColonyEventType.Founded,colony);ClaimHomeland(colony,true);Record(ColonyEventType.Emerged,colony);
            break;
        }
    }
}
