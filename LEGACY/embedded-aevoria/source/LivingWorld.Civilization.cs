using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
namespace SessionDashboard;
internal sealed partial class LivingCivilization
{
    internal CivicSnapshot State {get;}
    private readonly LivingNationWorld politics;
    private readonly LivingCultures cultures=LivingCultures.Default;
    private readonly string journalDirectory;
    private readonly List<CivicFact> pending=[];
    private int writer=Environment.CurrentManagedThreadId;
    internal string? Warning {get;private set;}
    internal LivingCivilization(CivicSnapshot state,LivingNationWorld politics,string directory)
    {State=state;this.politics=politics;journalDirectory=Path.Combine(directory,"civilization-history");if(state.Economy!=null)Economy=new(state.Economy,this,politics);}
    internal void BindWriter()=>writer=Environment.CurrentManagedThreadId;
    private void Write(){if(Environment.CurrentManagedThreadId!=writer)throw new InvalidOperationException("Civilization updates require the simulation thread.");}
    internal static Guid Identity(Guid world,string key)=>new(SHA256.HashData(Encoding.UTF8.GetBytes(world.ToString("N")+"/civic/"+key))[..16]);
    internal static LivingCivilization Create(BugManager manager,string directory)
    {
        var politics=manager.Nations??throw new InvalidOperationException("Political ownership is required.");
        var world=new LivingCivilization(new(){WorldId=manager.WorldId,LastTick=manager.MatchAge},politics,directory);
        foreach(var nation in politics.Nations.Where(n=>n.Status==NationExistence.Active).ToArray())world.EstablishNation(nation,manager);
        world.AssignPopulation(manager);return world;
    }
    private CivicFact Emit(CivicEventKind kind,Guid nation,double time,string detail,Guid? ruler=null,Guid? settlement=null)
    {
        var seq=++State.Sequence;var fact=new CivicFact(Identity(State.WorldId,"event/"+seq),seq,time,kind,nation,ruler,settlement,detail);
        pending.Add(fact);State.RecentEvents=State.RecentEvents.Append(fact).TakeLast(64).ToArray();if(pending.Count>=64)try{FlushHistory();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException){Warning="History archive unavailable; civilization development will pause: "+ex.Message;}return fact;
    }
    internal void TerritorialEvent(CivicEventKind kind,Guid nation,double time,string detail)=>Emit(kind,nation,time,detail);
    // Content-addressed, backward-linked segments: constant-size head in the save,
    // bounded RAM, idempotent writes, and safe branches when a backup is restored.
    internal void FlushHistory()
    {
        Write();if(pending.Count==0)return;Directory.CreateDirectory(journalDirectory);
        var segment=new CivicJournal(State.WorldId,State.JournalHead,pending.ToArray());var bytes=JsonSerializer.SerializeToUtf8Bytes(segment,ArcadeWorldStore.JsonOptions);
        var hash=Convert.ToHexString(SHA256.HashData(bytes));var file=Path.Combine(journalDirectory,hash+".json");
        if(!File.Exists(file))
        {
            var temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes);stream.Flush(true);}File.Move(temp,file,false);}
            finally {if(File.Exists(temp))File.Delete(temp);}
        }
        else if(!File.ReadAllBytes(file).SequenceEqual(bytes))throw new InvalidDataException("History segment failed integrity validation.");
        State.JournalHead=hash;pending.Clear();Warning=null;
    }
    internal JsonElement CaptureJson(){FlushHistory();return JsonSerializer.SerializeToElement(State,ArcadeWorldStore.JsonOptions);}
    internal sealed record CivicJournal(Guid WorldId,string? Previous,CivicFact[] Events);
    internal static LivingCivilization Restore(JsonElement raw,LivingNationWorld politics,string directory)
    {
        var s=raw.Deserialize<CivicSnapshot>(ArcadeWorldStore.JsonOptions)??throw new InvalidDataException("Empty civilization snapshot.");
        Validate(s,politics);var result=new LivingCivilization(s,politics,directory);
        if(s.JournalHead is {} head)
        {
            var file=Path.Combine(result.journalDirectory,head+".json");
            if(!File.Exists(file) || new FileInfo(file).Length>128*1024)throw new InvalidDataException("Civilization history head is missing or oversized; restore the world's history directory with its save.");
            var bytes=File.ReadAllBytes(file);if(Convert.ToHexString(SHA256.HashData(bytes))!=head)throw new InvalidDataException("Civilization history integrity mismatch.");
            var journal=JsonSerializer.Deserialize<CivicJournal>(bytes,ArcadeWorldStore.JsonOptions);
            if(journal==null || journal.WorldId!=s.WorldId || journal.Events.Length==0 || journal.Events[^1].Sequence!=s.Sequence)throw new InvalidDataException("Civilization history does not match the snapshot.");
        }
        else if(s.Sequence!=0)throw new InvalidDataException("Civilization history reference is missing.");
        return result;
    }
    private void EstablishNation(LivingNation nation,BugManager manager)
    {
        if(State.Nations.Length>=512){Warning="Civilization capacity reached; base simulation continues.";return;}
        var profile=cultures.ForSpecies(nation.Species);if(profile==null)return;
        var polity=new CivicNation(nation.Id,profile.Id,profile.CultureId,profile.CivilizationId,profile.GovernmentId){OfficeVacant=true};State.Nations=State.Nations.Append(polity).ToArray();
        politics.AssignCulture(nation.Id,profile.CivilizationId,cultures.Governments[profile.GovernmentId].AtlasGovernmentId);
        Emit(CivicEventKind.NationFounded,nation.Id,manager.MatchAge,"Cultural government established in a fictional mixed-era world.");
        var colony=manager.Colonies.Values.FirstOrDefault(c=>c.Id==nation.FactionId);
        if(colony!=null && BugManager.Alive(colony.Leader))Accede(polity,colony.Leader,manager.MatchAge,null);
        if(politics.OwnerAt(nation.Capital)==nation.Id)
        {
            var core=manager.Settlements.FirstOrDefault(s=>s.OwnerId==nation.FactionId && (s.Position-nation.Capital).Length<10);
            Found(polity,nation.Capital,manager.MatchAge,core?.Id,true);
        }
    }
    private void Accede(CivicNation nation,BugEntity bug,double time,Guid? previous)
    {
        if(State.Royals.Length>=1024){Warning="Royal biography capacity reached; office remains vacant.";return;}
        var id=Identity(State.WorldId,"person/"+bug.UnitId);var profile=cultures.Profiles[nation.ProfileId];
        var known=State.Royals.FirstOrDefault(p=>p.Id==id);if(known?.DeathAt!=null)return;
        var dynasty=nation.DynastyId is {} d ? State.Dynasties.FirstOrDefault(x=>x.Id==d && x.EndedAt==null) : null;
        if(dynasty==null)
        {
            var dynastyId=Identity(State.WorldId,"dynasty/"+bug.UnitId);var name=LivingCultures.Name(profile.PersonalNames,id,profile.Id+"/dynasty")+" House";
            dynasty=new(dynastyId,nation.NationId,name,id,time);State.Dynasties=State.Dynasties.Append(dynasty).ToArray();nation.DynastyId=dynastyId;
            Emit(CivicEventKind.DynastyFounded,nation.NationId,time,name,id);
        }
        var person=known??new RoyalPerson{Id=id,UnitId=bug.UnitId,NationId=nation.NationId,CultureId=nation.CultureId,Name=LivingCultures.Name(profile.PersonalNames,id,profile.Id+"/person"),DynastyId=dynasty.Id,CreatedAt=time};
        person.Office=cultures.Governments[nation.GovernmentId].Offices[0].Title;person.AccededAt=time;person.PredecessorId=previous;
        if(known==null)State.Royals=State.Royals.Append(person).ToArray();if(previous is {} predecessor && State.Royals.FirstOrDefault(p=>p.Id==predecessor) is {} old)old.SuccessorId=id;
        dynasty.Rulers=dynasty.Rulers.Append(id).Distinct().ToArray();nation.CurrentRulerId=id;nation.OfficeVacant=false;bug.Name=person.Name;
        Emit(previous==null ? CivicEventKind.RoyalAccession : CivicEventKind.Succession,nation.NationId,time,person.Office+" "+person.Name,id);
    }
    internal string? Title(int unit)=>State.Royals.FirstOrDefault(p=>p.UnitId==unit && p.DeathAt==null)?.Office;
    internal void Observe(ColonyEvent fact)
    {
        if(Warning!=null && pending.Count>=128)return; // Existing core history retains the factual death for retry.
        if(fact.Type!=ColonyEventType.LeaderKilled || fact.UnitId==null)return;
        var royal=State.Royals.FirstOrDefault(p=>p.UnitId==fact.UnitId && p.DeathAt==null);if(royal!=null)Died(royal,fact.Timestamp,fact.OtherFactionId!=null ? "Battle" : null);
    }
    private void Died(RoyalPerson person,double time,string? cause)
    {
        if(person.DeathAt!=null)return;person.DeathAt=time;person.DeathCause=cause;
        var polity=State.Nations.First(n=>n.NationId==person.NationId);polity.CurrentRulerId=null;polity.OfficeVacant=true;
        Emit(CivicEventKind.RoyalDeath,person.NationId,time,cause??"Cause unknown",person.Id);
        Emit(CivicEventKind.OfficeVacant,person.NationId,time,person.Office,person.Id);
    }
    internal void Tick(BugManager manager)
    {
        Write();if(manager.WorldPaused)return;if(Warning!=null && pending.Count>=64){try{FlushHistory();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException){return;}}
        var now=manager.MatchAge;if(now-State.LastTick<1)return;var elapsed=Math.Min(5,now-State.LastTick);State.LastTick=now;
        foreach(var n in politics.Nations.Where(n=>n.Status==NationExistence.Active).ToArray())if(!State.Nations.Any(c=>c.NationId==n.Id))EstablishNation(n,manager);
        foreach(var nation in State.Nations)
        {
            var political=politics.Find(nation.NationId);var colony=manager.Colonies.Values.FirstOrDefault(c=>c.Id==political?.FactionId);
            var current=State.Royals.FirstOrDefault(p=>p.Id==nation.CurrentRulerId);
            if(current!=null)
            {
                var actual=manager.Bugs.FirstOrDefault(b=>b.UnitId==current.UnitId)??(colony?.Leader.UnitId==current.UnitId ? colony.Leader:null);
                var death=manager.History.LastOrDefault(e=>e.Type==ColonyEventType.LeaderKilled && e.UnitId==current.UnitId);
                if(death!=null || actual!=null && !BugManager.Alive(actual))Died(current,death?.Timestamp??now,death?.OtherFactionId!=null ? "Battle":null);
                else if(actual==null)Warning="A current ruler is absent from authoritative unit data; no death has been inferred.";
            }
            // Only an actual surviving core colony leader may take office. This does not
            // promote a soldier or change CanReproduce. Core recovery enforces eligibility.
            if(nation.OfficeVacant && colony!=null && BugManager.Alive(colony.Leader) && colony.Leader.IsLeader)
            {
                var predecessor=State.Royals.LastOrDefault(p=>p.NationId==nation.NationId)?.Id;
                // No descent is known: a recovered reproductive leader founds a new house.
                if(nation.DynastyId is {} dynastyId && State.Dynasties.FirstOrDefault(d=>d.Id==dynastyId && d.EndedAt==null) is {} dynasty)
                {dynasty.EndedAt=now;nation.DynastyId=null;Emit(CivicEventKind.DynastyEnded,nation.NationId,now,"No documented hereditary successor.");}
                Accede(nation,colony.Leader,now,predecessor);
            }
        }
        SynchronizeSettlements(manager);AssignPopulation(manager);
        Economy?.Tick(manager);
        foreach(var settlement in State.Settlements.Where(s=>s.Active)){if(Warning!=null && pending.Count>=128)break;Develop(settlement,manager,elapsed);}
        if(Warning==null || pending.Count<64)politics.Expansion?.Tick(manager,this);
        foreach(var nation in State.Nations)
        {
            var capital=State.Settlements.FirstOrDefault(s=>s.Id==nation.CapitalSettlementId && s.Active && s.OwnerId==nation.NationId);
            if(capital==null){nation.CapitalSettlementId=null;var refuge=State.Settlements.Where(s=>s.Active && s.OwnerId==nation.NationId).OrderByDescending(s=>s.Stage).ThenBy(s=>s.FoundedAt).FirstOrDefault();if(refuge!=null)Capital(nation,refuge,now);}
            if(politics.Find(nation.NationId)?.Status==NationExistence.Extinct && nation.CurrentRulerId==null && State.Dynasties.FirstOrDefault(d=>d.Id==nation.DynastyId && d.EndedAt==null) is {} ended)
            {ended.EndedAt=now;Emit(CivicEventKind.DynastyEnded,nation.NationId,now,"Nation became extinct with no living office holder.");}
        }
    }
    internal static void Validate(CivicSnapshot s,LivingNationWorld politics)
    {
        static void Need([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool v,string m){if(!v)throw new InvalidDataException("Invalid civilization save: "+m);}
        static bool Time(double t)=>double.IsFinite(t) && t>=0;
        Need((s.SchemaVersion==1 && s.Economy==null || s.SchemaVersion==2 && s.Economy!=null) && s.WorldId==politics.WorldId && Time(s.LastTick) && s.Sequence>=0,"schema/world/clock");
        Need(s.Nations!=null && s.Nations.Length<=512 && s.Royals!=null && s.Royals.Length<=1024 && s.Dynasties!=null && s.Dynasties.Length<=1024 && s.Settlements!=null && s.Settlements.Length<=128 && s.Buildings!=null && s.Buildings.Length<=2048 && s.RecentEvents!=null && s.RecentEvents.Length<=64 && s.Tasks!=null && s.Tasks.Length<=240,"bounded collections");
        Need(s.JournalHead==null || s.JournalHead.Length==64 && s.JournalHead.All(Uri.IsHexDigit),"history head");
        var profiles=LivingCultures.Default;
        foreach(var n in s.Nations!)Need(n!=null && politics.Find(n.NationId)!=null && profiles.Profiles.TryGetValue(n.ProfileId,out var p) && n.CultureId==p.CultureId && n.CivilizationId==p.CivilizationId && (n.GovernmentId==p.GovernmentId || p.AlternativeGovernments.Contains(n.GovernmentId)),"nation/culture references");
        Need(s.Nations.Select(n=>n.NationId).Distinct().Count()==s.Nations.Length,"duplicate nations");
        foreach(var r in s.Royals!)Need(r!=null && r.Id!=Guid.Empty && r.UnitId>0 && s.Nations.Any(n=>n.NationId==r.NationId && n.CultureId==r.CultureId) && !string.IsNullOrWhiteSpace(r.Name) && r.Name.Length<=100 && !string.IsNullOrWhiteSpace(r.Office) && Time(r.CreatedAt) && (r.AccededAt==null || Time(r.AccededAt.Value)) && (r.DeathAt==null || Time(r.DeathAt.Value) && r.DeathAt>=r.CreatedAt) && (r.DeathCause==null || r.DeathAt!=null && r.DeathCause.Length<=100) && r.Achievements!=null && r.Achievements.Length<=16,"royal identity");
        Need(s.Royals.Select(r=>r.Id).Distinct().Count()==s.Royals.Length,"duplicate royals");
        foreach(var d in s.Dynasties!)Need(d!=null && d.Id!=Guid.Empty && s.Nations.Any(n=>n.NationId==d.NationId) && s.Royals.Any(r=>r.Id==d.FounderId) && Time(d.FoundedAt) && (d.EndedAt==null || Time(d.EndedAt.Value)) && d.Rulers!=null && d.Rulers.Length<=1024 && d.Rulers.All(id=>s.Royals.Any(r=>r.Id==id)) && d.KnownGenerations>=1,"dynasty references");
        Need(s.Dynasties.Select(d=>d.Id).Distinct().Count()==s.Dynasties.Length,"duplicate dynasties");
        foreach(var r in s.Royals)Need(s.Dynasties.Any(d=>d.Id==r.DynastyId) && (r.PredecessorId==null || s.Royals.Any(p=>p.Id==r.PredecessorId)) && (r.SuccessorId==null || s.Royals.Any(p=>p.Id==r.SuccessorId)),"royal succession links");
        foreach(var n in s.Nations)Need((n.CurrentRulerId==null || s.Royals.Any(r=>r.Id==n.CurrentRulerId && r.NationId==n.NationId && r.DeathAt==null)) && (n.DynastyId==null || s.Dynasties.Any(d=>d.Id==n.DynastyId)) && (n.CapitalSettlementId==null || s.Settlements!.Any(c=>c.Id==n.CapitalSettlementId && c.OwnerId==n.NationId && c.Active)),"government references");
        foreach(var c in s.Settlements!)Need(c!=null && c.Id!=Guid.Empty && s.Nations.Any(n=>n.NationId==c.OwnerId) && profiles.Profiles.ContainsKey(c.StyleId) && !string.IsNullOrWhiteSpace(c.Name) && c.Name.Length<=100 && politics.Land.IsCapital(c.Position) && Enum.IsDefined(c.Stage) && c.Population>=0 && c.Workforce>=0 && c.Workforce<=c.Population && Time(c.FoundedAt) && new[]{c.Food,c.Materials,c.Production,c.DefensiveSupplies}.All(v=>double.IsFinite(v) && v>=0 && v<=1000),"settlement data");
        Need(s.Settlements.Select(c=>c.Id).Distinct().Count()==s.Settlements.Length,"duplicate settlements");
        foreach(var b in s.Buildings!)Need(b!=null && b.Id!=Guid.Empty && s.Settlements.Any(c=>c.Id==b.SettlementId && c.OwnerId==b.OwnerId) && profiles.Profiles.ContainsKey(b.StyleId) && politics.Land.IsCapital(b.Position) && Enum.IsDefined(b.Purpose) && double.IsFinite(b.Progress) && b.Progress>=0 && b.Progress<=1 && double.IsFinite(b.Condition) && b.Condition>=0 && b.Condition<=1 && (!b.Completed || b.Progress==1 && !b.Cancelled) && double.IsFinite(b.MaterialCost) && b.MaterialCost>=0 && b.MaterialCost<=1000,"building data");
        Need(s.Buildings.Select(b=>b.Id).Distinct().Count()==s.Buildings.Length,"duplicate buildings");
        long seq=0;foreach(var e in s.RecentEvents!) {Need(e!=null && e.Id==Identity(s.WorldId,"event/"+e.Sequence) && e.Sequence>seq && e.Sequence<=s.Sequence && Time(e.Time) && Enum.IsDefined(e.Kind) && s.Nations.Any(n=>n.NationId==e.NationId) && e.Detail!=null && e.Detail.Length<=500 && (e.RulerId==null || s.Royals.Any(r=>r.Id==e.RulerId)) && (e.SettlementId==null || s.Settlements.Any(c=>c.Id==e.SettlementId)),"event references/order");seq=e!.Sequence;}
        Need(s.Sequence==0 || seq==s.Sequence,"event high water");
        Need(s.Tasks!.Select(t=>t.UnitId).Distinct().Count()==s.Tasks.Length && s.Tasks.All(t=>s.Settlements.Any(c=>c.Id==t.SettlementId) && t.Work is "Gather food" or "Gather materials" or "Construct" or "Maintain"),"work assignments");
        Need(s.Buildings.All(b=>b.InvestmentCents>=0 && b.InvestmentCents<=LivingEconomy.CurrencyLimit),"building investment");
        s.Economy?.Validate(s);
    }
}
