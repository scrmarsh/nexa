using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace SessionDashboard;
internal sealed partial class BattlefieldWindow
{
    private Border? nationPanel;
    private readonly TextBlock nationSummary=new(){TextWrapping=TextWrapping.Wrap};
    private readonly Border nationSwatch=new(){Width=28,Height=5,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,7)};
    private DispatcherTimer? nationCamera;
    private readonly Stopwatch focusClock=new();
    private Guid? selectedNation;
    private int selectedComponent;
    private double startZoom,targetZoom;
    private double focusDuration=.38;
    private Vector startOffset,targetOffset;
    private long lastNationRevision=-1,lastSummaryAt;
    private Button? capitalButton;
    private CheckBox? unitFollowControl;
    private Guid? selectedSettlement;
    private Button? settlementFocusButton;
    internal Guid? SelectedSettlementId=>selectedSettlement;
    internal Guid? SelectedNationId=>selectedNation;
    internal bool FocusingNation=>nationCamera?.IsEnabled==true;
    internal Point? FocusDestination {get;private set;}
    private void InitializeNationMap()
    {
        var panel=new StackPanel();nationPanel=new Border{Child=Living?.Info.CivilizationMode==true ? new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled}:panel,Width=254,MaxHeight=Living?.Info.CivilizationMode==true ? 430:370,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(12),Padding=new Thickness(12),CornerRadius=new CornerRadius(8),BorderThickness=new Thickness(1),Visibility=Visibility.Collapsed};
        nationPanel.SetResourceReference(Border.BackgroundProperty,"PanelBrush");nationPanel.SetResourceReference(Border.BorderBrushProperty,"LineBrush");
        panel.Children.Add(nationSwatch);panel.Children.Add(nationSummary);var commands=new WrapPanel{Margin=new Thickness(0,9,0,0)};panel.Children.Add(commands);
        Button Add(string text,Action action){var button=new Button{Content=text,Padding=new Thickness(6),Margin=new Thickness(0,0,5,5),FontSize=11};button.Click+=(_,_)=>action();commands.Children.Add(button);return button;}
        Add("Focus Nation",()=>FocusNation());capitalButton=Add("Focus Capital",FocusCapital);Add("Return to World",FitWorld);Add("Close selection",ClearNationSelection);
        settlementFocusButton=Add("Focus Settlement",FocusSettlement);settlementFocusButton.Visibility=Visibility.Collapsed;
        viewport.Children.Add(nationPanel);
        nationCamera=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(33)};
        nationCamera.Tick+=(_,_)=>
        {
            var fraction=Math.Min(1,focusClock.Elapsed.TotalSeconds/focusDuration);var t=fraction*fraction*(3-2*fraction);
            zoom=startZoom+(targetZoom-startZoom)*t;cameraOffset=startOffset+(targetOffset-startOffset)*t;
            if(fraction>=1){nationCamera.Stop();wheelAnimating=false;Battlefield.TerrainView?.DeferDetailUpdates(false);}ApplyCamera();
        };
        viewport.PreviewMouseLeftButtonDown+=(_,e)=>
        {
            if(Battlefield.StrategicMap && IsAevoriaHud(e.OriginalSource as DependencyObject))return;
            if(nationPanel.IsMouseOver || armyPanel?.IsMouseOver==true || Battlefield.SelectUnits || Battlefield.TerrainView?.ArmyView.Hit(e.GetPosition(Battlefield))!=null)return;
            if(Battlefield.TerrainView?.NationView.HitLetter(e.GetPosition(Battlefield)) is {} letter){SelectNation(letter);e.Handled=true;return;}
            if(Battlefield.TerrainView?.SettlementView.Hit(e.GetPosition(Battlefield)) is {} city){SelectSettlement(city);e.Handled=true;return;}
            var hit=Battlefield.TerrainView?.NationView.Hit(e.GetPosition(Battlefield));
            if(hit!=null){SelectNation(hit);e.Handled=true;}else ClearNationSelection();
        };
        viewport.SizeChanged+=(_,_)=>{nationPanel.MaxHeight=Math.Max(150,Math.Min(430,viewport.ActualHeight-(Battlefield.StrategicMap?300:24)));if(!Battlefield.StrategicMap){if(selectedSettlement!=null)FocusSettlement();else if(selectedNation!=null)FocusNation();}};
        Living!.PoliticsChanged+=PoliticalReady;
        Loaded+=(_,_)=>RestoreNationSelection();
        Closed+=(_,_)=>{nationCamera.Stop();Living.PoliticsChanged-=PoliticalReady;};
    }
    private void PoliticalReady(){RestoreNationSelection();Battlefield.RefreshFrame();UpdateNationSelection(true);}
    private void RestoreNationSelection()
    {
        if(selectedNation!=null || Living?.Info.SelectedNationId is not {} id || Living.Nations?.Find(id) is not {Status:NationExistence.Active})return;
        var component=Living.Nations.Components(id).FirstOrDefault();if(component==null)return;
        selectedNation=id;selectedComponent=component.Key;Battlefield.TerrainView!.NationView.Selected=id;if(nationPanel!=null)nationPanel.Visibility=Visibility.Visible;UpdateNationSelection(true);
    }
    internal NationHit? SelectTerritoryAt(Point screen)
    {var hit=Battlefield.TerrainView?.NationView.Hit(screen);if(hit!=null)SelectNation(hit);else ClearNationSelection();return hit;}
    internal void SelectNation(NationHit hit)
    {
        selectedSettlement=null;if(settlementFocusButton!=null)settlementFocusButton.Visibility=Visibility.Collapsed;
        var world=Living?.Nations;var nation=world?.Find(hit.NationId);if(nation==null || nation.Status!=NationExistence.Active)return;
        selectedNation=hit.NationId;selectedComponent=hit.ComponentKey;followedFaction=followedUnit=null;follow.SelectedIndex=0;Battlefield.SelectUnits=false;if(unitFollowControl!=null)unitFollowControl.IsChecked=false;
        Battlefield.TerrainView!.NationView.Selected=selectedNation;Living!.Info.SelectedNationId=selectedNation;
        if(nationPanel!=null)nationPanel.Visibility=Visibility.Visible;UpdateNationSelection(true);FocusNation(!hit.MainLabel);
    }
    internal void FocusNation(bool clickedComponent=false)
    {
        if(selectedNation is not {} id || Living?.Nations is not {} world)return;
        var components=world.Components(id);var component=clickedComponent ? components.FirstOrDefault(c=>c.Key==selectedComponent) : components.FirstOrDefault();
        if(component==null){ClearNationSelection();return;}selectedComponent=component.Key;
        var anchor=component.Anchor;var bounds=component.Bounds;
        var halfWidth=Math.Max(anchor.X-bounds.Left,bounds.Right-anchor.X);var halfHeight=Math.Max(anchor.Y-bounds.Top,bounds.Bottom-anchor.Y);
        var fit=Math.Clamp(Math.Min(Math.Max(1,viewport.ActualWidth)*.80/(2*Math.Max(1,halfWidth)),Math.Max(1,viewport.ActualHeight)*.80/(2*Math.Max(1,halfHeight))),MinimumZoom,2.2);
        AnimateNationCamera(anchor,fit);
    }
    private void FocusCapital()
    {
        if(selectedNation is {} id && Living?.Nations is {} world && world.Find(id) is {} nation && world.OwnerAt(nation.Capital)==id)AnimateNationCamera(nation.Capital,Math.Clamp(1,MinimumZoom,3));
    }
    private void AnimateNationCamera(Point anchor,double fit)
    {
        if(nationCamera==null)return;fitCamera=false;wheelAnimating=false;followedFaction=followedUnit=null;focusDuration=.38;FocusDestination=anchor;startZoom=zoom;targetZoom=fit;startOffset=cameraOffset;targetOffset=new Vector(viewport.ActualWidth/2-anchor.X*fit,viewport.ActualHeight/2-anchor.Y*fit);Battlefield.TerrainView?.DeferDetailUpdates(true);focusClock.Restart();nationCamera.Start();
    }
    private void CancelNationFocus(){nationCamera?.Stop();wheelAnimating=false;Battlefield?.TerrainView?.DeferDetailUpdates(false);FocusDestination=null;}
    internal void ClearNationSelection()
    {
        selectedSettlement=null;if(settlementFocusButton!=null)settlementFocusButton.Visibility=Visibility.Collapsed;
        CancelNationFocus();selectedNation=null;if(nationPanel!=null)nationPanel.Visibility=Visibility.Collapsed;
        if(Battlefield?.TerrainView!=null)Battlefield.TerrainView.NationView.Selected=null;
        if(Living!=null)Living.Info.SelectedNationId=null;
        Battlefield?.RefreshFrame();
    }
    private void UpdateNationSelection(bool force=false)
    {
        if(Living==null)return;
        if(Living.PoliticsWarning is {} warning)status.Text+=" | "+warning;
        if(selectedNation is not {} id)return;var world=Living.Nations;var n=world?.Find(id);
        if(selectedSettlement is {} city && Living.Civilization is {} civic)
        {
            var settlement=civic.State.Settlements.FirstOrDefault(s=>s.Id==city && s.Active);if(settlement==null){ClearNationSelection();return;}
            var owner=world?.Find(settlement.OwnerId);if(owner==null){ClearNationSelection();return;}selectedNation=owner.Id;Battlefield.TerrainView!.NationView.Selected=owner.Id;
            var buildings=civic.State.Buildings.Where(b=>b.SettlementId==city && !b.Cancelled && b.Condition>0).ToArray();var job=buildings.FirstOrDefault(b=>!b.Completed);var capital=civic.State.Nations.Any(p=>p.CapitalSettlementId==city);
            nationSummary.Text=$"{settlement.Name}\n{owner.Name}\nCulture: {LivingCultures.Default.Profiles[settlement.StyleId].Name}\nStage: {settlement.Stage}"+(capital ? " (capital)":"")+$"\nPopulation: {settlement.Population} / {civic.Capacity(settlement)}\nWorkforce: {settlement.Workforce}\nFood: {settlement.Food:F1} | Materials: {settlement.Materials:F1}\nProduction: {settlement.Production:F1}\nDefense: {civic.Completed(settlement,CivicPurpose.Defense)} | Supplies: {settlement.DefensiveSupplies:F1}\nBuildings: "+string.Join(", ",buildings.Where(b=>b.Completed).Select(b=>b.Archetype))+"\nConstruction: "+(job==null ? "None" : $"{job.Archetype} {job.Progress:P0}");
            if(Battlefield.StrategicMap && civic.Economy is {} localEconomy && localEconomy.Settlement(settlement.Id) is {} conditions)nationSummary.Text+=$"\nProsperity: {conditions.Tier} ({conditions.Score:F0}/100)"+(localEconomy.Nation(settlement.OwnerId) is {} account?EconomySummary(account):"");
            if(Battlefield.StrategicMap && civic.Economy!=null)nationSummary.Text+="\nMean building condition: "+(buildings.Length==0?"No buildings":buildings.Average(b=>b.Condition).ToString("P0"))+"\nRecent developments: "+string.Join("; ",civic.State.RecentEvents.Where(e=>e.SettlementId==settlement.Id || e.NationId==settlement.OwnerId && e.Kind is CivicEventKind.SustainedProsperity or CivicEventKind.EconomicHardship or CivicEventKind.EconomicRecovery).TakeLast(2).Select(e=>e.Detail));
            if(capitalButton!=null)capitalButton.IsEnabled=true;return;
        }
        if(world==null || n==null || n.Status==NationExistence.Extinct || world.Area(id)==0){ClearNationSelection();return;}
        if(!force && world.Revision==lastNationRevision && Stopwatch.GetElapsedTime(lastSummaryAt).TotalSeconds<.5)return;
        lastNationRevision=world.Revision;lastSummaryAt=Stopwatch.GetTimestamp();var stats=world.Statistics(id,World.Manager);
        var civilization=n.CivilizationId is {} historical && HistoricalAtlas.Default.Entities.TryGetValue(historical,out var record) ? record.Name : "Not assigned";
        var ownsCapital=world.OwnerAt(n.Capital)==n.Id;
        nationSwatch.Background=new SolidColorBrush(Color.FromArgb(255,(byte)(n.Color>>16),(byte)(n.Color>>8),(byte)n.Color));
        nationSummary.Text=$"{n.Name}\n{n.Species} · {n.Status}\nCivilization: {civilization}\nCapital: {n.Capital.X:F0}, {n.Capital.Y:F0}"+(ownsCapital ? "" : " (occupied)")+ $"\nRuler: {stats.Ruler??"Unavailable"}\nFounded: "+(n.FoundedAt is {} time ? $"{time:F1}s of simulation" : "Unknown (legacy world)")+$"\nControlled land: {world.Area(id):N0} tiles\nPopulation: {stats.Population}\nMilitary units: {stats.Military}\nSettlements: {stats.Settlements}";
        if(Battlefield.StrategicMap && Living.Civilization?.Economy is {} economy && economy.Nation(id) is {} treasury)nationSummary.Text+=EconomySummary(treasury)+"\nMean prosperity: "+economy.State.Settlements.Where(s=>Living.Civilization!.State.Settlements.Any(t=>t.Id==s.SettlementId && t.OwnerId==id && t.Active)).Select(s=>s.Score).DefaultIfEmpty(0).Average().ToString("F0")+" / 100";
        if(capitalButton!=null)capitalButton.IsEnabled=ownsCapital;
        if(world.Expansion is {} expansion)
        {
            nationSummary.Text+="\nLand neighbors: "+string.Join(", ",world.Frontiers.Neighbors(id).Select(other=>world.Find(other)?.Name));
            if(Living.Civilization is {} borderCivic)nationSummary.Text+=$"\nBorder settlements: {world.Frontiers.Settlements(borderCivic).Count(s=>s.NationId==id)}";
            if(expansion.CapacityWarning!=null)nationSummary.Text+="\n"+expansion.CapacityWarning;
        }
        if(Living.Civilization is {} civilWorld && civilWorld.State.Nations.FirstOrDefault(c=>c.NationId==id) is {} court)
        {
            var person=civilWorld.State.Royals.FirstOrDefault(r=>r.Id==court.CurrentRulerId);var dynasty=civilWorld.State.Dynasties.FirstOrDefault(d=>d.Id==court.DynastyId);
            nationSummary.Text+=$"\nCulture: {court.CultureId}\nGovernment: {court.GovernmentId}\nOffice: "+(court.OfficeVacant ? "Vacant" : person?.Office??"Unavailable")+"\nDynasty: "+(dynasty?.Name??"None");
            if(person!=null)nationSummary.Text+=$"\nAcceded: {person.AccededAt:F1}s\nKnown since: {person.CreatedAt:F1}s";
        }
    }
    internal void SelectSettlement(Guid id)
    {
        var settlement=Living?.Civilization?.State.Settlements.FirstOrDefault(s=>s.Id==id && s.Active);if(settlement==null || Living!.Nations?.Components(settlement.OwnerId).FirstOrDefault() is not {} component)return;
        SelectNation(new(settlement.OwnerId,component.Key,false));selectedSettlement=id;if(settlementFocusButton!=null)settlementFocusButton.Visibility=Visibility.Visible;UpdateNationSelection(true);FocusSettlement();
    }
    private void FocusSettlement()
    {if(Living?.Civilization?.State.Settlements.FirstOrDefault(s=>s.Id==selectedSettlement && s.Active) is {} s)AnimateNationCamera(s.Position,Math.Clamp(1.5,MinimumZoom,3));}
}
