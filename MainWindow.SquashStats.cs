using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace SessionDashboard;
public partial class MainWindow
{
    private void RecordSpeciesTotal(BugSpecies species)
    {
        var totals=state.Workspace!.AllTimeSquashesBySpecies;var key=species.ToString();totals[key]=totals.GetValueOrDefault(key)+1;
        UpdateWidgetClocks(DateTimeOffset.UtcNow);
    }
    private void BuildSquashesWidget(StackPanel panel)
    {
        var totals=new TextBlock{FontSize=18,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,12)};panel.Children.Add(totals);
        var illustrations=new System.Windows.Controls.Primitives.UniformGrid{Columns=3,HorizontalAlignment=HorizontalAlignment.Stretch};panel.Children.Add(illustrations);
        var labels=new Dictionary<BugSpecies,TextBlock>();
        foreach(var species in BugManager.AllSpecies)
        {
            var column=new StackPanel{Margin=new Thickness(4,0,4,10)};
            var count=new TextBlock{TextAlignment=TextAlignment.Center,FontSize=16,FontWeight=FontWeights.SemiBold,ToolTip="Session / all-time recorded species totals"};labels[species]=count;column.Children.Add(count);
            column.Children.Add(new Viewbox{Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,MaxHeight=132,Child=new SquashPortrait(species,()=>bugsOverlay?.Manager.EcosystemSettings??new EcosystemOptions()){Width=180,Height=132}});
            column.Children.Add(new TextBlock{Text=species.ToString(),TextAlignment=TextAlignment.Center,FontSize=10});illustrations.Children.Add(column);
        }
        var legacy=new TextBlock{FontSize=10,TextWrapping=TextWrapping.Wrap,Opacity=.7};panel.Children.Add(legacy);
        void Refresh()
        {
            totals.Text=$"This session: {state.Productivity.BugsSquashed:N0} · All time: {state.Workspace?.AllTimeSquashes??0:N0}";
            foreach(var item in labels)item.Value.Text=$"{state.Productivity.SquashesBySpecies.GetValueOrDefault(item.Key.ToString())} / {state.Workspace?.AllTimeSquashesBySpecies.GetValueOrDefault(item.Key.ToString())??0}";
            var unknown=(state.Workspace?.AllTimeSquashes??0)-(state.Workspace?.AllTimeSquashesBySpecies.Values.Sum()??0);
            legacy.Text=unknown>0?$"{unknown:N0} earlier squashes have no recorded species breakdown.":"Session / all-time counts";
        }
        Refresh();widgetTicks.Add(_=>Refresh());
    }
}
internal sealed class SquashPortrait(BugSpecies species,Func<EcosystemOptions> options) : FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        dc.PushTransform(new TranslateTransform(ActualWidth/2,ActualHeight/2));dc.PushTransform(new ScaleTransform(4.65,4.65));
        var preference=options();BugVisuals.Draw(dc,new BugEntity{Species=species},preference,Theme.SeasonalEnabled && preference.EffectiveAppearance==BugAppearance.Halloween);dc.Pop();dc.Pop();
    }
}
