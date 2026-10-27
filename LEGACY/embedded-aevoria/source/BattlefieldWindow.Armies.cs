using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed partial class BattlefieldWindow
{
    private StrategicArmy? selectedArmy;
    private Border? armyPanel;
    private readonly TextBlock armySummary=new(){TextWrapping=TextWrapping.Wrap};
    internal StrategicArmy? SelectedArmy=>selectedArmy;
    private void InitializeArmySelection()
    {
        var panel=new StackPanel();var summary=armySummary;panel.Children.Add(summary);
        armyPanel=new Border{Width=260,MaxHeight=400,Padding=new Thickness(12),Margin=new Thickness(12),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Visibility=Visibility.Collapsed,Child=panel};
        armyPanel.SetResourceReference(Border.BackgroundProperty,"PanelBrush");viewport.Children.Add(armyPanel);
        void Add(string name,Action action){var button=new Button{Content=name,Margin=new Thickness(0,7,0,0)};button.Click+=(_,_)=>action();panel.Children.Add(button);}
        Add("Focus Army",FocusArmy);Add("Focus Nation",()=>{if(selectedArmy is {} army && Living?.Nations?.Components(army.NationId).FirstOrDefault() is {} c)SelectNation(new(army.NationId,c.Key,false));});
        Add("Inspect units",()=>{if(selectedArmy is {} army)summary.Text+="\nAvailable units: "+string.Join(", ",World.Manager.Bugs.Where(b=>army.Units.Contains(b.UnitId) && BugManager.Alive(b)).Select(b=>$"#{b.UnitId} {b.Role} {b.Health:F0}/{b.MaxHealth:F0}"));});
        Add("Close army",()=>{selectedArmy=null;armyPanel.Visibility=Visibility.Collapsed;});
        viewport.PreviewMouseLeftButtonDown+=(_,e)=>
        {
            if(IsAevoriaHud(e.OriginalSource as DependencyObject))return;
            if(armyPanel.IsMouseOver || nationPanel?.IsMouseOver==true || Battlefield.SelectUnits)return;
            if(Battlefield.TerrainView?.ArmyView.Hit(e.GetPosition(Battlefield)) is not {} army)return;
            SelectArmy(army);e.Handled=true;
        };
    }
    internal void SelectArmy(StrategicArmy army)
    {selectedArmy=army;armySummary.Text=$"Military group {army.Id}\n{army.Nation}\nTroops: {army.Units.Length}\nOrders: {army.Orders}\nBattle: {(army.Fighting?"Engaged":"No active combat in group")}\nCommander: not recorded\nRead-only grouping of existing units.";if(armyPanel!=null)armyPanel.Visibility=Visibility.Visible;}
    internal void FocusArmy()
    {
        if(selectedArmy==null)return;var live=World.Manager.Bugs.Where(b=>selectedArmy.Units.Contains(b.UnitId) && BugManager.Alive(b)).ToArray();if(live.Length==0)return;
        AnimateNationCamera(new Point(live.Average(b=>b.Position.X),live.Average(b=>b.Position.Y)),Math.Clamp(.8,MinimumZoom,3));
    }
}
